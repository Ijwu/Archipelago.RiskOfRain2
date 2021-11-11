using System.Collections.Generic;
using Archipelago.RiskOfRain2.Enums;
using Archipelago.RiskOfRain2.Net;
using Archipelago.RiskOfRain2.UI.Objectives;
using BepInEx;
using BepInEx.Bootstrap;
using InLobbyConfig.Fields;
using R2API;
using R2API.Networking;
using R2API.Utils;
using RoR2;
using UnityEngine;

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

        private ArchipelagoClient2 AP;
        private bool isConnectedToAP = false;

        private bool isInLobbyConfigLoaded = false;
        private string apServerUri = "localhost";
        private int apServerPort = 38281;
        private bool willConnectToAP = true;
        private string apSlotName = "Ijwu";
        private string apPassword;
        private Color accentColor;

        private bool enableDeathlink = false;
        private DeathLinkDifficulty deathlinkDifficulty;

        public void Awake()
        {
            Log.Init(Logger);

            AP = new ArchipelagoClient2();

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

            Run.onRunStartGlobal += Run_onRunStartGlobal;
            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;

            accentColor = new Color(.8f, .5f, 1, 1);
        }

        private void Run_onRunStartGlobal(Run obj)
        {
            if (willConnectToAP)
            {
                AP.SetAccentColor(accentColor);
                AP.Connect(apServerUri, apServerPort, apSlotName, apPassword);

                if (enableDeathlink)
                {
                    AP.EnableDeathLink(deathlinkDifficulty);
                }

                isConnectedToAP = true;

                ArchipelagoTotalChecksObjectiveController.AddObjective();
            }
        }

        private void Run_onRunDestroyGlobal(Run obj)
        {
            if (isConnectedToAP)
            {
                AP.Disconnect();

                isConnectedToAP = false;

                ArchipelagoTotalChecksObjectiveController.RemoveObjective();
            }
        }

        private void CreateInLobbyMenu()
        {
            var configEntry = new InLobbyConfig.ModConfigEntry();
            configEntry.DisplayName = "Archipelago";
            configEntry.SectionFields.Add("Server Connection Settings", new List<IConfigField>
            {
                new StringConfigField("Archipelago Slot Name", () => apSlotName, (newValue) => apSlotName = newValue),
                new StringConfigField("Archipelago Server Password", () => apPassword, (newValue) => apPassword = newValue),
                new StringConfigField("Archipelago Server URL", () => apServerUri, (newValue) => apServerUri = newValue),
                new IntConfigField("Archipelago Server Port", () => apServerPort, (newValue) => apServerPort = newValue),
                new BooleanConfigField("Enable Archipelago?", () => willConnectToAP, (newValue) => willConnectToAP = newValue)
            });
            configEntry.SectionFields.Add("DeathLink Settings", new List<IConfigField>
            {
                new BooleanConfigField("Enable DeathLink?", () => enableDeathlink, (newValue) => enableDeathlink = newValue),
                new EnumConfigField<DeathLinkDifficulty>("DeathLink Difficulty", () => deathlinkDifficulty, (newValue) => deathlinkDifficulty = newValue)
            });
            configEntry.SectionFields.Add("Client Side Settings", new List<IConfigField>
            {
                new ColorConfigField("Accent Color", () => accentColor, (newValue) => accentColor = newValue)
            });
            InLobbyConfig.ModConfigCatalog.Add(configEntry);
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
