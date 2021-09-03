using System;
using System.Collections.Generic;
using Archipelago.RiskOfRain2.Net;
using Archipelago.RiskOfRain2.UI;
using BepInEx;
using BepInEx.Bootstrap;
using InLobbyConfig.Fields;
using R2API;
using R2API.Networking;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Archipelago.RiskOfRain2
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.HardDependency)]
    [R2APISubmoduleDependency(nameof(NetworkingAPI), nameof(PrefabAPI))]
    public class ArchipelagoPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.Ijwu.Archipelago";
        public const string PluginAuthor = "Ijwu";
        public const string PluginName = "Archipelago";
        public const string PluginVersion = "0.1.5";

        private ArchipelagoClient AP;
        private bool isInLobbyConfigLoaded = false;
        private string apServerUri = "localhost";
        private int apServerPort = 38281;
        private bool apEnabled = true;
        private string apSlotName = "IJ";
        private string apPassword;

        public void Awake()
        {
            Log.Init(Logger);

            AP = new ArchipelagoClient();
            Run.onRunStartGlobal += Run_onRunStartGlobal;
            ArchipelagoStartMessage.ArchipelagoStarted += ArchipelagoStartMessage_ArchipelagoStarted;

            isInLobbyConfigLoaded = Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.InLobbyConfig");

            if (isInLobbyConfigLoaded)
            {
                CreateInLobbyMenu();
            }

            NetworkingAPI.RegisterMessageType<SyncLocationCheckProgress>();
            NetworkingAPI.RegisterMessageType<ArchipelagoStartMessage>();

            On.RoR2.Networking.GameNetworkManager.OnClientConnect += (self, user, t) => { };
        }

        private void ArchipelagoStartMessage_ArchipelagoStarted()
        {
            if (!NetworkServer.active)
            {
                AP.HudController = new ArchipelagoHUDController();
            }
        }

        private void Run_onRunStartGlobal(Run obj)
        {
            var isHost = NetworkServer.active && RoR2Application.isInMultiPlayer;
            if (apEnabled && (isHost || RoR2Application.isInSinglePlayer))
            {
                var uri = new UriBuilder();
                uri.Scheme = "ws://";
                uri.Host = apServerUri;
                uri.Port = apServerPort;
                
                AP.Connect(uri.Uri.AbsoluteUri, apSlotName, apPassword);
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
                new BooleanConfigField("Enable Archipelago?", () => apEnabled, (newValue) => apEnabled = newValue)
            });
            InLobbyConfig.ModConfigCatalog.Add(configEntry);
        }

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
    }
}
