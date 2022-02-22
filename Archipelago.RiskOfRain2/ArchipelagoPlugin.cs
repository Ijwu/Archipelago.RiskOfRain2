using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.RiskOfRain2.Enums;
using Archipelago.RiskOfRain2.Net;
using Archipelago.RiskOfRain2.UI.Objectives;
using BepInEx;
using BepInEx.Bootstrap;
using InLobbyConfig.Fields;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

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

        private ArchipelagoOrchestrator AP;
        private bool isPlayingAP = false;

        //TODO: reset these defaults to something resonable for release
        private string apServerUri = "localhost";
        private int apServerPort = 38281;
        private bool willConnectToAP = true;
        private string apSlotName = "ijwu";
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

            AP = new ArchipelagoOrchestrator();

            var isInLobbyConfigLoaded = Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.InLobbyConfig");

            if (isInLobbyConfigLoaded)
            {
                CreateInLobbyMenu();
            }
            else
            {
                Log.LogError("Cannot find InLobbyConfig mod, Archipelago menu will not be present.");
            }

            NetworkingAPI.RegisterMessageType<SyncLocationCheckProgress>();
            NetworkingAPI.RegisterMessageType<SyncTotalCheckProgress>();
            NetworkingAPI.RegisterMessageType<ArchipelagoChatMessage>();

            CommandHelper.AddToConsoleWhenReady();

            On.RoR2.Run.Start += Run_onRunStartGlobal;
            On.RoR2.Run.OnDestroy += Run_OnDestroy;

            accentColor = new Color(.8f, .5f, 1, 1);

            On.RoR2.UI.ChatBox.SubmitChat += ChatBox_SubmitChat;
            ArchipelagoChatMessage.OnChatReceivedFromClient += SendChatToAP;
            On.RoR2.UI.CharacterSelectController.Awake += OnCharacterSelectAwake;

            //todo: remove debug
            On.RoR2.Networking.GameNetworkManager.OnClientConnect += (self, user, t) => { };
            IL.RoR2.UI.ConsoleWindow.CheckConsoleKey += ChangeConsoleKey;
        }

        private void OnCharacterSelectAwake(On.RoR2.UI.CharacterSelectController.orig_Awake orig, RoR2.UI.CharacterSelectController self)
        {
            orig(self);
            var isHost = CheckIsHostOrSingleplayer();
            if (isHost)
            {
                var apButton = ConstructArchipelagoButton();
                var parentPanelTransform = GameObject.Find("CharacterSelectUI/SafeArea/FooterPanel/").transform;
                apButton.transform.SetParent(parentPanelTransform);
                var apButtonRect = apButton.GetComponent<RectTransform>();
                apButtonRect.anchoredPosition = Vector2.zero;
                apButtonRect.anchoredPosition3D = Vector3.zero;
                apButtonRect.anchorMax = new Vector2(.6f, 1f);
                apButtonRect.anchorMin = new Vector2(.5f, 0f);
                apButton.transform.localPosition = new Vector3(515f, 24f, 0f);
                apButton.transform.localScale = Vector3.one;
            }
        }

        private GameObject ConstructArchipelagoButton()
        {
            GameObject menuButton = GameObject.Find("GenericMenuButton (Loadout)").InstantiateClone("Archipelago Connect Button");
            var buttonTextObject = menuButton.transform.Find("ButtonText");
            var textComponent = buttonTextObject.GetComponent<HGTextMeshProUGUI>();
            textComponent.SetText("Connect to AP");
            
            var imageObject = menuButton.GetComponent<Image>();
            imageObject.color = new Color(0.3767f, 0.8971f, 0.2111f, 1);

            var buttonObject = menuButton.GetComponent<Button>();
            buttonObject.onClick.AddListener(new UnityAction(ConnectToArchipelago));

            menuButton.transform.localScale = new Vector3(1f, 1f, 1f);
            return menuButton;
        }

        private void ChangeConsoleKey(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.IL.Replace(cursor.Instrs[0], Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)50));
        }

        private void Run_onRunStartGlobal(On.RoR2.Run.orig_Start orig, Run obj)
        {
            if (!isPlayingAP && willConnectToAP)
            {
                ConnectToArchipelago();
            }

            Log.LogDebug($"Run is starting. Will connect to AP? {willConnectToAP} Is connected to AP? {isPlayingAP}");
            Log.LogDebug($"Was network server active? '{NetworkServer.active}' Is local client active? '{NetworkServer.localClientActive}' Is network client active? '{NetworkClient.active}' Is in multiplayer? '{RoR2Application.isInMultiPlayer}' Is in singleplayer? '{RoR2Application.isInSinglePlayer}' Is multiplayer client? '{isMultiplayerClient}'");
            Log.LogDebug($"Is deathlink option selected? '{enableDeathlink}' Difficulty: '{deathlinkDifficulty}'");
            orig(obj);

            if (!isPlayingAP && willConnectToAP)
            { 
                if (CheckIsHostOrSingleplayer())
                {
                    AP.HookEverything();    
                }
            }
        }

        private void ConnectToArchipelago()
        {
            isHostOrSingleplayer = CheckIsHostOrSingleplayer();
            isMultiplayerClient = CheckIsMultiplayerClient();

            
            if (!willConnectToAP)
            {
                ChatMessage.SendColored($"Cannot connect to Archipelago. Please enable the setting in your lobby config.", Color.red);
                return;
            }

            ArchipelagoTotalChecksObjectiveController.AddObjective();
            AP.SetAccentColor(accentColor);
            if (isHostOrSingleplayer)
            {
                if (enableDeathlink)
                {
                    AP.EnableDeathLink(deathlinkDifficulty);
                }

                isPlayingAP = AP.Login(apServerUri, apServerPort, apSlotName, apPassword);
            }
            else if (isMultiplayerClient)
            {
                isPlayingAP = true;
                AP.SetupClientsideMode();
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
    }
}
