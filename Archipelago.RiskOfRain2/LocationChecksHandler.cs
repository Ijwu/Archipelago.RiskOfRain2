using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.RiskOfRain2.Net;
using Archipelago.RiskOfRain2.UI.Objectives;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System.Linq;
using UnityEngine;

namespace Archipelago.RiskOfRain2
{
    internal class LocationChecksHandler : IHandleSomething
    {
        private readonly LocationCheckHelper helper;
        private GameObject smokescreenPrefab;
        private PickupIndex[] skippedItems;
        private bool finishedAllChecks;

        public int TotalChecks { get; private set; }
        public int CurrentChecks { get; private set; }
        public int PickedUpItemCount { get; private set; }
        public int ItemPickupStep { get; private set; }

        public LocationChecksHandler(LocationCheckHelper helper)
        {
            this.helper = helper;
            smokescreenPrefab = Resources.Load<GameObject>("Prefabs/Effects/SmokescreenEffect").InstantiateClone("LocationCheckPoof", true);

            skippedItems = new PickupIndex[]
            {
                PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixBlue.equipmentIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixEcho.equipmentIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixGold.equipmentIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixHaunted.equipmentIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixLunar.equipmentIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixPoison.equipmentIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixRed.equipmentIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixWhite.equipmentIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixYellow.equipmentIndex),
                PickupCatalog.FindPickupIndex("LunarCoin.Coin0"),
                PickupCatalog.FindPickupIndex(RoR2Content.Items.ArtifactKey.itemIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Bomb.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Command.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.EliteOnly.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Enigma.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.FriendlyFire.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Glass.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.MixEnemy.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.MonsterTeamGainsItems.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.RandomSurvivorOnRespawn.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Sacrifice.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.ShadowClone.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.SingleMonsterType.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Swarms.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.TeamDeath.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.WeakAssKnees.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.WispOnDeath.artifactIndex),
            };
        }

        public void Hook()
        {
            On.RoR2.PickupDropletController.CreatePickupDroplet += PickupDropletController_CreatePickupDroplet;
        }

        public void Unhook()
        {
            On.RoR2.PickupDropletController.CreatePickupDroplet -= PickupDropletController_CreatePickupDroplet;
        }

        private void PickupDropletController_CreatePickupDroplet(On.RoR2.PickupDropletController.orig_CreatePickupDroplet orig, PickupIndex pickupIndex, Vector3 position, Vector3 velocity)
        {
            if (skippedItems.Contains(pickupIndex))
            {
                orig(pickupIndex, position, velocity);
                return;
            }

            // Run `HandleItemDrop()` first so that the `PickedUpItemCount` is incremented by the time `ItemDropProcessed()` is called.
            var spawnItem = HandleItemDrop();

            if (spawnItem)
            {
                orig(pickupIndex, position, velocity);
            }

            if (!spawnItem)
            {
                EffectManager.SpawnEffect(smokescreenPrefab, new EffectData() { origin = position }, true);
            }

            new SyncTotalCheckProgress(finishedAllChecks ? TotalChecks : CurrentChecks, TotalChecks).Send(NetworkDestination.Clients);

            if (finishedAllChecks)
            {
                ArchipelagoTotalChecksObjectiveController.RemoveObjective();
                new AllChecksComplete().Send(NetworkDestination.Clients);
            }
        }

        private bool HandleItemDrop()
        {
            PickedUpItemCount += 1;

            if ((PickedUpItemCount % ItemPickupStep) == 0)
            {
                CurrentChecks = PickedUpItemCount / ItemPickupStep;

                ArchipelagoTotalChecksObjectiveController.CurrentChecks = CurrentChecks;

                if (CurrentChecks == TotalChecks)
                {
                    ArchipelagoTotalChecksObjectiveController.CurrentChecks = ArchipelagoTotalChecksObjectiveController.TotalChecks;
                    finishedAllChecks = true;
                }

                //TODO: prepopulate item send list and allow randomization
                var itemSendName = $"ItemPickup{CurrentChecks}";
                var itemLocationId = helper.GetLocationIdFromName(itemSendName);
                Log.LogDebug($"Sent out location {itemSendName} (id: {itemLocationId})");

                helper.CompleteLocationChecks(itemLocationId);
                return false;
            }
            return true;
        }
    }
}