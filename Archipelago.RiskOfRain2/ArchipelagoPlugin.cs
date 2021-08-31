using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using InLobbyConfig.Fields;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archipelago.RiskOfRain2
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.SoftDependency)]
    public class ArchipelagoPlugin : BaseUnityPlugin
    {
        public static event EventHandler OnUpdate;

        public const string PluginGUID = "9EBA2DD6-D072-4CEE-AE77-448F69A6424B";
        public const string PluginAuthor = "Ijwu";
        public const string PluginName = "Archipelago";
        public const string PluginVersion = "1.0.0";

        private ArchipelagoClient AP;
        private bool isInLobbyConfigLoaded = false;
        private string apServerUri = "localhost";
        private int apServerPort = 38281;
        private bool apEnabled = false;

        public void Awake()
        {
            Log.Init(Logger);

            AP = new ArchipelagoClient();
            On.RoR2.PreGameController.StartRun += PreGameController_StartRun;
            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

            isInLobbyConfigLoaded = Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.InLobbyConfig");

            if (isInLobbyConfigLoaded)
            {
                CreateInLobbyMenu();
            }
        }

        private void PreGameController_StartRun(On.RoR2.PreGameController.orig_StartRun orig, PreGameController self)
        {
            if (apEnabled)
            {
                var uri = new UriBuilder();
                uri.Scheme = "ws://";
                uri.Host = apServerUri;
                uri.Port = apServerPort;

                AP.ResetItemReceivedCount();
                AP.Connect(uri.Uri.AbsoluteUri);
            }

            orig(self);
        }

        private void CreateInLobbyMenu()
        {
            var configEntry = new InLobbyConfig.ModConfigEntry();
            configEntry.DisplayName = "Archipelago";
            configEntry.SectionFields.Add("Archipelago Client Config", new List<IConfigField>
            {
                new StringConfigField("Archipelago Server URL", () => apServerUri, (newValue) => apServerUri = newValue),
                new IntConfigField("Archipelago Server Port", () => apServerPort, (newValue) => apServerPort = newValue),
                new BooleanConfigField("Enable Archipelago?", () => apEnabled, (newValue) => apEnabled = newValue)
            });
            InLobbyConfig.ModConfigCatalog.Add(configEntry);
        }

        private void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            if (to.name == "splash")
            {
                SceneManager.LoadScene("title");
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                Log.LogInfo("You hit F2, yay.");
                AP.Connect("ws://localhost:38281");
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                var teddyBear = PickupCatalog.FindPickupIndex(RoR2Content.Items.Bear.itemIndex);
                var transform = LocalUserManager.GetFirstLocalUser().cachedBodyObject.transform;
                PickupDropletController.CreatePickupDroplet(teddyBear, transform.position, transform.forward * 20);
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2Content.Items.JumpBoost.itemIndex), transform.position, transform.forward * 20);
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

            if (OnUpdate != null)
            {
                OnUpdate.Invoke(null, new EventArgs());
            }
        }
    }
}
