using System.Collections.Generic;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.RiskOfRain2.Enums;
using Archipelago.RiskOfRain2.Net;
using Archipelago.RiskOfRain2.UI.Objectives;
using BepInEx;
using BepInEx.Bootstrap;
using InLobbyConfig.Fields;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig")]
    [R2APISubmoduleDependency(nameof(NetworkingAPI), nameof(PrefabAPI), nameof(CommandHelper))]
    public class ArchipelagoPlugin : BaseUnityPlugin
    {
        public const string GameName = "Risk of Rain 2";
        public const string PluginGUID = "com.Ijwu.Archipelago";
        public const string PluginAuthor = "Ijwu";
        public const string PluginName = "Archipelago";
        public const string PluginVersion = "2.0";

        private ArchipelagoClient AP;
        private bool isPlayingAP = false;

        private bool isInLobbyConfigLoaded = false;
        private string apServerUri = "localhost";
        private int apServerPort = 38281;
        private bool willConnectToAP = true;
        private string apSlotName = "Ijwu";
        private string apPassword;
        private Color accentColor;

        private bool enableDeathlink = false;
        private DeathLinkDifficulty deathlinkDifficulty;

        private bool isHostOrSingleplayer = false;
        private bool isMultiplayerClient = false;

        private bool CheckIsHostOrSingleplayer() => (NetworkServer.active && RoR2Application.isInMultiPlayer) || RoR2Application.isInSinglePlayer;
        private bool CheckIsMultiplayerClient() => !NetworkServer.active && RoR2Application.isInMultiPlayer;

        public void Awake()
        {
            Log.Init(Logger);

            AP = new ArchipelagoClient();

            isInLobbyConfigLoaded = Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.InLobbyConfig");

            if (isInLobbyConfigLoaded)
            {
                CreateInLobbyMenu();
            }

            NetworkingAPI.RegisterMessageType<SyncLocationCheckProgress>();
            NetworkingAPI.RegisterMessageType<SyncTotalCheckProgress>();
            NetworkingAPI.RegisterMessageType<ArchipelagoChatMessage>();

            CommandHelper.AddToConsoleWhenReady();

            Run.onRunStartGlobal += Run_onRunStartGlobal;
            On.RoR2.Run.OnDestroy += Run_OnDestroy;

            accentColor = new Color(.8f, .5f, 1, 1);

            //todo: remove debug
            On.RoR2.Networking.GameNetworkManager.OnClientConnect += (self, user, t) => { };
            On.RoR2.UI.ChatBox.SubmitChat += ChatBox_SubmitChat;
            ArchipelagoChatMessage.OnChatReceivedFromClient += SendChatToAP;
        }

        private void Run_onRunStartGlobal(Run obj)
        {
            isHostOrSingleplayer = CheckIsHostOrSingleplayer();
            isMultiplayerClient = CheckIsMultiplayerClient();

            Log.LogDebug($"Run was started. Will connect to AP? {willConnectToAP} Is connected to AP? {isPlayingAP}");
            if (willConnectToAP)
            {
                ArchipelagoTotalChecksObjectiveController.AddObjective();
                AP.SetAccentColor(accentColor);
                Log.LogDebug($"Was network server active? {NetworkServer.active} Is local client active? {NetworkServer.localClientActive} Is network client active? {NetworkClient.active} Is in multiplayer? {RoR2Application.isInMultiPlayer} Is in singleplayer? {RoR2Application.isInSinglePlayer}");
                if (isHostOrSingleplayer)
                {
                    isPlayingAP = AP.Connect(apServerUri, apServerPort, apSlotName, apPassword);

                    if (enableDeathlink)
                    {
                        AP.EnableDeathLink(deathlinkDifficulty);
                    }
                }
                else if (isMultiplayerClient)
                {
                    isPlayingAP = true;
                    AP.SetupClientsideMode();
                }
            }
        }

        private void Run_OnDestroy(On.RoR2.Run.orig_OnDestroy orig, Run self)
        {
            Log.LogDebug($"Run was destroyed. Is connected to AP? {isPlayingAP}. Is socket connected? {AP?.Session?.Socket?.Connected}");
            Log.LogDebug($"IsHostOrSingleplayer: {CheckIsHostOrSingleplayer()} IsMultiplayerClient: {CheckIsMultiplayerClient()}");
            Log.LogDebug($"NetworkServer.Active: {NetworkServer.active} IsInMultiplayer: {RoR2Application.isInMultiPlayer} IsInSingleplayer: {RoR2Application.isInSinglePlayer}");
            if (isPlayingAP || (AP.Session != null && AP.Session.Socket.Connected))
            {
                if (isHostOrSingleplayer)
                {
                    Log.LogDebug("Run was destroyed. Disconnecting.");
                    AP.Disconnect();
                }
                else if (isMultiplayerClient)
                {
                    Log.LogDebug("Run was destroyed. Tearing down clientside mode.");
                    AP.TeardownClientsideMode();
                }

                isPlayingAP = false;
                ArchipelagoTotalChecksObjectiveController.RemoveObjective();
            }

            isHostOrSingleplayer = false;
            isMultiplayerClient = false;

            orig(self);
        }

        private void Run_onRunDestroyGlobal(Run obj)
        {
            Log.LogDebug($"Run was destroyed. Is connected to AP? {isPlayingAP}. Is socket connected? {AP?.Session?.Socket?.Connected}");
            Log.LogDebug($"IsHostOrSingleplayer: {CheckIsHostOrSingleplayer()} IsMultiplayerClient: {CheckIsMultiplayerClient()}");
            Log.LogDebug($"NetworkServer.Active: {NetworkServer.active} IsInMultiplayer: {RoR2Application.isInMultiPlayer} IsInSingleplayer: {RoR2Application.isInSinglePlayer}");
            if (isPlayingAP || (AP.Session != null && AP.Session.Socket.Connected))
            {
                if (CheckIsHostOrSingleplayer())
                {
                    Log.LogDebug("Run was destroyed. Disconnecting.");
                    AP.Disconnect();
                } 
                else if (CheckIsMultiplayerClient())
                {
                    Log.LogDebug("Run was destroyed. Tearing down clientside mode.");
                    AP.TeardownClientsideMode();
                }

                isPlayingAP = false;
                ArchipelagoTotalChecksObjectiveController.RemoveObjective();
            }
        }

        private void ChatBox_SubmitChat(On.RoR2.UI.ChatBox.orig_SubmitChat orig, RoR2.UI.ChatBox self)
        {
            if (isPlayingAP)
            {
                if (CheckIsMultiplayerClient())
                {
                    var player = LocalUserManager.GetFirstLocalUser();
                    var name = player.userProfile.name;
                    if (!string.IsNullOrWhiteSpace(self.inputField.text))
                    {
                        new ArchipelagoChatMessage(name, self.inputField.text).Send(NetworkDestination.Server);
                    }
                }
                else if (CheckIsHostOrSingleplayer())
                {
                    var player = LocalUserManager.GetFirstLocalUser();
                    var name = player.userProfile.name;
                    if (!string.IsNullOrWhiteSpace(self.inputField.text))
                    {
                        SendChatToAP(name, self.inputField.text);
                    }
                }

                self.SetShowInput(false);
            }
            else
            {
                orig(self);
            }
        }

        private void SendChatToAP(string author, string message)
        {
            if (message.StartsWith("!"))
            {
                AP.Session.Socket.SendPacket(new SayPacket() { Text = message });
            }
            else
            {
                AP.Session.Socket.SendPacket(new SayPacket() { Text = $"({author}): {message}" });
            }
        }

        private void CreateInLobbyMenu()
        {
            var configEntry = new InLobbyConfig.ModConfigEntry();
            configEntry.DisplayName = "Archipelago";
            configEntry.SectionFields.Add("Host Settings", new List<IConfigField>
            {
                new StringConfigField("Archipelago Slot Name", () => apSlotName, (newValue) => apSlotName = newValue),
                new StringConfigField("Archipelago Server Password", () => apPassword, (newValue) => apPassword = newValue),
                new StringConfigField("Archipelago Server URL", () => apServerUri, (newValue) => apServerUri = newValue),
                new IntConfigField("Archipelago Server Port", () => apServerPort, (newValue) => apServerPort = newValue),
                new BooleanConfigField("Enable Archipelago?", () => willConnectToAP, (newValue) => willConnectToAP = newValue),
                new BooleanConfigField("Enable DeathLink?", () => enableDeathlink, (newValue) => enableDeathlink = newValue),
                new EnumConfigField<DeathLinkDifficulty>("DeathLink Difficulty", () => deathlinkDifficulty, (newValue) => deathlinkDifficulty = newValue)
            });
            configEntry.SectionFields.Add("Client Settings", new List<IConfigField>
            {
                new ColorConfigField("Accent Color", () => accentColor, (newValue) => accentColor = ProcessAccentColor(newValue))
            });
            InLobbyConfig.ModConfigCatalog.Add(configEntry);
        }

        private Color ProcessAccentColor(Color accent)
        {
            return new Color(Mathf.InverseLerp(0, 255, accent.r), Mathf.InverseLerp(0, 255, accent.g), Mathf.InverseLerp(0, 255, accent.b), Mathf.InverseLerp(0, 255, accent.a));
        }

        //TODO: remove debug stuff when done hacking shit up
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                var player = LocalUserManager.GetFirstLocalUser();
                PickupDropletController.CreatePickupDroplet(
                    PickupCatalog.FindPickupIndex(RoR2Content.Items.Bear.itemIndex),
                    player.cachedBody.transform.position,
                    player.cachedBody.transform.forward * 20);
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                var player = LocalUserManager.GetFirstLocalUser();
                for (int i = 0; i < 15; i++)
                {
                    PickupDropletController.CreatePickupDroplet(
                    PickupCatalog.FindPickupIndex(RoR2Content.Items.Bear.itemIndex),
                    player.cachedBody.transform.position,
                    player.cachedBody.transform.forward * 20);
                }
            }
        }
    }
}
