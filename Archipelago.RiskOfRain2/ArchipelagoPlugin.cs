using System.Collections.Generic;
using Archipelago.RiskOfRain2.Enums;
using Archipelago.RiskOfRain2.Net;
using BepInEx;
using BepInEx.Bootstrap;
using InLobbyConfig.Fields;
using R2API;
using R2API.Networking;
using R2API.Utils;
using RoR2;

namespace Archipelago.RiskOfRain2
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig")]
    [R2APISubmoduleDependency(nameof(NetworkingAPI), nameof(PrefabAPI), nameof(CommandHelper))]
    public class ArchipelagoPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.Ijwu.Archipelago";
        public const string PluginAuthor = "Ijwu";
        public const string PluginName = "Archipelago";
        public const string PluginVersion = "2.0";

        private ArchipelagoClient2 AP;
        private bool isInLobbyConfigLoaded = false;
        private string apServerUri = "localhost";
        private int apServerPort = 38281;
        private bool willConnectToAP = true;
        private string apSlotName = "Ijwu";
        private string apPassword;
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

            On.RoR2.Run.Start += Run_Start;
        }

        private void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            if (willConnectToAP)
            {
                AP.Connect(apServerUri, apServerPort, apSlotName, apPassword);

                if (enableDeathlink)
                {
                    AP.EnableDeathLink(deathlinkDifficulty);
                }
            }

            orig(self);
        }

        private void CreateInLobbyMenu()
        {
            var configEntry = new InLobbyConfig.ModConfigEntry();
            configEntry.DisplayName = "Archipelago";
            configEntry.SectionFields.Add("Client Config", new List<IConfigField>
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
            InLobbyConfig.ModConfigCatalog.Add(configEntry);
        }
    }
}
