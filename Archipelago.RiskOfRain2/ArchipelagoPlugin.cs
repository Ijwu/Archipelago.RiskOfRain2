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
        public const string PluginVersion = "0.1.7";

        private ArchipelagoClient AP;
        private bool isInLobbyConfigLoaded = false;
        private string apServerUri = "localhost";
        private int apServerPort = 38281;
        private bool willConnectToAP = true;
        private bool isPlayingAP = false;
        private string apSlotName = "IJ";
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
            GameNetworkManager.onStopClientGlobal += GameNetworkManager_onStopClientGlobal;
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

            On.RoR2.Networking.GameNetworkManager.OnClientConnect += (self, user, t) => { };
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

        private void AP_OnClientDisconnect(ushort code, string reason, bool wasClean)
        {
            Log.LogWarning($"Archipelago client was disconnected from the server{(wasClean ? " in a dirty manner" : "")}: ({code}) {reason}");
            ChatMessage.SendColored($"Archipelago client was disconnected from the server.", wasClean ? Color.white : Color.red);
            var isHost = NetworkServer.active && RoR2Application.isInMultiPlayer;
            if (isPlayingAP && (isHost || RoR2Application.isInSinglePlayer) && !wasClean)
            {
                StartCoroutine(AP.AttemptConnection());
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

            AP.LastServerUrl = uri.Uri.AbsoluteUri;
            AP.SlotName = slot;
            AP.Password = password;

            StartCoroutine(AP.AttemptConnection());
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
            Log.LogInfo($"ArchipelagoStartMessage_OnArchipelagoSessionStart (NetworkServer.Active: {NetworkServer.active}, isPlayingAP: {isPlayingAP})");
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
                
                AP.Connect(uri.Uri.AbsoluteUri, apSlotName, apPassword);
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

#if DEBUG
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3))
            {
                var teddyBear = PickupCatalog.FindPickupIndex(RoR2Content.Items.Bear.itemIndex);
                var transform = LocalUserManager.GetFirstLocalUser().cachedBodyObject.transform;
                PickupDropletController.CreatePickupDroplet(teddyBear, transform.position, transform.forward * 20);
            }
            else if (Input.GetKeyDown(KeyCode.F4))
            {
                var player = LocalUserManager.GetFirstLocalUser();
                player.cachedMaster.inventory.GiveItem(RoR2Content.Items.ExtraLife.itemIndex, 10);
                player.cachedMaster.inventory.GiveItem(RoR2Content.Items.BossDamageBonus, 10);
                player.cachedMaster.inventory.GiveItem(RoR2Content.Items.BoostDamage, 10);
                player.cachedMaster.inventory.GiveItem(RoR2Content.Items.CritGlasses, 10);
                player.cachedMaster.inventory.GiveItem(RoR2Content.Items.FlatHealth, 10);
                player.cachedMaster.inventory.GiveItem(RoR2Content.Items.ShinyPearl, 10);
                player.cachedMaster.inventory.GiveItem(RoR2Content.Items.Bear, 10);
                player.cachedMaster.inventory.GiveItem(RoR2Content.Items.SprintBonus, 10);
            }
            else if (Input.GetKeyDown(KeyCode.F5))
            {
                var player = LocalUserManager.GetFirstLocalUser();
                player.cachedMaster.GiveMoney(500);
            }
            else if (Input.GetKeyDown(KeyCode.F6))
            {
                RoR2.Run.instance.AdvanceStage(RoR2.Run.instance.nextStageScene);
            }
            else if (Input.GetKeyDown(KeyCode.F8))
            {
                Log.LogInfo("------- Log Marker ---------");
            }
        }
#endif
    }
}
