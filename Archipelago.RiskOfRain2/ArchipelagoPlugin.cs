using System;
using System.Collections.Generic;
using Archipelago.RiskOfRain2.Console;
using Archipelago.RiskOfRain2.Net;
using Archipelago.RiskOfRain2.UI;
using BepInEx;
using BepInEx.Bootstrap;
using InLobbyConfig.Fields;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.HardDependency)]
    [R2APISubmoduleDependency(nameof(NetworkingAPI), nameof(PrefabAPI), nameof(CommandHelper))]
    public class ArchipelagoPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.Ijwu.Archipelago";
        public const string PluginAuthor = "Ijwu";
        public const string PluginName = "Archipelago";
        public const string PluginVersion = "1.1.4";

        private ArchipelagoClient AP;
        private bool isInLobbyConfigLoaded = false;
        private string apServerUri = "archipelago.gg";
        private int apServerPort = 38281;
        private bool willConnectToAP = true;
        private bool isPlayingAP = false;
        private string apSlotName;
        private string apPassword;

        public void Awake()
        {
            Log.Init(Logger);

            AP = new ArchipelagoClient();
            AP.OnClientDisconnect += AP_OnClientDisconnect;
            Run.onRunStartGlobal += Run_onRunStartGlobal;
            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
            ArchipelagoStartMessage.OnArchipelagoSessionStart += ArchipelagoStartMessage_OnArchipelagoSessionStart;
            ArchipelagoEndMessage.OnArchipelagoSessionEnd += ArchipelagoEndMessage_OnArchipelagoSessionEnd;
            ArchipelagoConsoleCommand.OnArchipelagoCommandCalled += ArchipelagoConsoleCommand_ArchipelagoCommandCalled;
            NetworkManagerSystem.onStopClientGlobal += GameNetworkManager_onStopClientGlobal;
            On.RoR2.UI.ChatBox.SubmitChat += ChatBox_SubmitChat;

            isInLobbyConfigLoaded = Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.InLobbyConfig");

            if (isInLobbyConfigLoaded)
            {
                CreateInLobbyMenu();
            }

            NetworkingAPI.RegisterMessageType<SyncLocationCheckProgress>();
            NetworkingAPI.RegisterMessageType<ArchipelagoStartMessage>();
            NetworkingAPI.RegisterMessageType<ArchipelagoEndMessage>();
            NetworkingAPI.RegisterMessageType<SyncTotalCheckProgress>();
            NetworkingAPI.RegisterMessageType<AllChecksComplete>();
            NetworkingAPI.RegisterMessageType<ArchipelagoChatMessage>();

            CommandHelper.AddToConsoleWhenReady();
        }

        private void GameNetworkManager_onStopClientGlobal()
        {
            if (!NetworkServer.active && isPlayingAP)
            {
                if (AP.LocationCheckBar != null)
                {
                    AP.LocationCheckBar.Dispose();
                }
            }
        }

        private void ChatBox_SubmitChat(On.RoR2.UI.ChatBox.orig_SubmitChat orig, RoR2.UI.ChatBox self)
        {
            if (!NetworkServer.active && isPlayingAP)
            {
                new ArchipelagoChatMessage(self.inputField.text).Send(NetworkDestination.Server);
                self.inputField.text = "";
                orig(self);
            }
            else
            {
                orig(self);
            }
        }

        private void ArchipelagoEndMessage_OnArchipelagoSessionEnd()
        {
            // This is for clients that are in a lobby but not the host of the lobby.
            // They end up with multiple bars if they join multiple sessions otherwise.
            if (!NetworkServer.active && isPlayingAP)
            {
                if (AP.LocationCheckBar != null)
                {
                    AP.LocationCheckBar.Dispose();
                }
            }
        }

        private void AP_OnClientDisconnect(string reason)
        {
            Log.LogWarning($"Archipelago client was disconnected from the server because `{reason}`");
            ChatMessage.SendColored($"Archipelago client was disconnected from the server.", Color.red);
            var isHost = NetworkServer.active && RoR2Application.isInMultiPlayer;
            if (isPlayingAP && (isHost || RoR2Application.isInSinglePlayer))
            {
                //StartCoroutine(AP.AttemptConnection());
            }
        }

        private void ArchipelagoConsoleCommand_ArchipelagoCommandCalled(string url, int port, string slot, string password)
        {
            willConnectToAP = true;
            isPlayingAP = true;
            var uri = new UriBuilder();
            uri.Scheme = "ws://";
            uri.Host = url;
            uri.Port = port;

            AP.Connect(uri.Uri, slot, password);
            //StartCoroutine(AP.AttemptConnection());
        }

        /// <summary>
        /// Server -> Client packet responder. Should not run on server.
        /// </summary>
        private void ArchipelagoStartMessage_OnArchipelagoSessionStart()
        {
            if (!NetworkServer.active)
            {
                AP.LocationCheckBar = new ArchipelagoLocationCheckProgressBarUI();
                isPlayingAP = true;
            }
        }

        private void Run_onRunStartGlobal(Run obj)
        {
            var isHost = NetworkServer.active && RoR2Application.isInMultiPlayer;
            if (willConnectToAP && (isHost || RoR2Application.isInSinglePlayer))
            {
                isPlayingAP = true;
                var uri = new UriBuilder();
                uri.Scheme = "ws://";
                uri.Host = apServerUri;
                uri.Port = apServerPort;
                
                AP.Connect(uri.Uri, apSlotName, apPassword);
            }

            if (isPlayingAP)
            {
                ArchipelagoTotalChecksObjectiveController.AddObjective();
            }
        }

        private void Run_onRunDestroyGlobal(Run obj)
        {
            if (isPlayingAP)
            {
                ArchipelagoTotalChecksObjectiveController.RemoveObjective();
            }
        }

        private void CreateInLobbyMenu()
        {
            var configEntry = new InLobbyConfig.ModConfigEntry();
            configEntry.DisplayName = "Archipelago";
            configEntry.SectionFields.Add("Archipelago Client Config", new List<IConfigField>
            {
                new StringConfigField("Archipelago Slot Name", () => apSlotName, (newValue) => apSlotName = newValue),
                new StringConfigField("Archipelago Server Password", () => apPassword, (newValue) => apPassword = newValue),
                new StringConfigField("Archipelago Server URL", () => apServerUri, (newValue) => apServerUri = newValue),
                new IntConfigField("Archipelago Server Port", () => apServerPort, (newValue) => apServerPort = newValue),
                new BooleanConfigField("Enable Archipelago?", () => willConnectToAP, (newValue) => willConnectToAP = newValue)
            });
            InLobbyConfig.ModConfigCatalog.Add(configEntry);
        }
    }
}
