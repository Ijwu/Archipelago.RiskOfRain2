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
        public const string PluginGUID = "9EBA2DD6-D072-4CEE-AE77-448F69A6424B";
        public const string PluginAuthor = "Ijwu";
        public const string PluginName = "Archipelago";
        public const string PluginVersion = "0.1.1";

        private ArchipelagoClient AP;
        private bool isInLobbyConfigLoaded = false;
        private string apServerUri = "localhost";
        private int apServerPort = 38281;
        private bool apEnabled = false;
        private string apSlotName;
        private string apPassword;

        public void Awake()
        {
            Log.Init(Logger);

            AP = new ArchipelagoClient();
            Run.onRunStartGlobal += Run_onRunStartGlobal;

            isInLobbyConfigLoaded = Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.InLobbyConfig");

            if (isInLobbyConfigLoaded)
            {
                CreateInLobbyMenu();
            }
        }

        private void Run_onRunStartGlobal(Run obj)
        {
            if (apEnabled)
            {
                var uri = new UriBuilder();
                uri.Scheme = "ws://";
                uri.Host = apServerUri;
                uri.Port = apServerPort;

                AP.ResetItemReceivedCount();
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
    }
}
