using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.RiskOfRain2.Enums;
using Archipelago.RiskOfRain2.Extensions;
using RoR2;
using RoR2.Artifacts;
using UnityEngine;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Handlers
{
    internal class DeathLinkHandler : IHandleSomething
    {
        private readonly DeathLinkService deathLink;
        private readonly DeathLinkDifficulty difficulty;
        private readonly Dictionary<DeathLinkDifficulty, Action<DeathLink>> handlers;

        public DeathLinkHandler(DeathLinkService deathLink, DeathLinkDifficulty difficulty)
        {
            Log.LogDebug($"DeathLink handler constructor. Difficulty: {difficulty}");
            this.deathLink = deathLink;
            this.difficulty = difficulty;

            handlers = new Dictionary<DeathLinkDifficulty, Action<DeathLink>>()
            {
                [DeathLinkDifficulty.Drizzle] = RunDrizzle,
                [DeathLinkDifficulty.Rainstorm] = RunRainstorm,
                [DeathLinkDifficulty.Monsoon] = RunMonsoon,
                [DeathLinkDifficulty.Typhoon] = RunTyphoon,
                [DeathLinkDifficulty.AHAHAHAHAHA] = BringThePain
            };
        }

        public void Hook()
        {
            deathLink.OnDeathLinkReceived += DeathLink_OnDeathLinkReceived;
        }

        public void Unhook()
        {
            deathLink.OnDeathLinkReceived -= DeathLink_OnDeathLinkReceived;
        }

        private void DeathLink_OnDeathLinkReceived(DeathLink deathLink)
        {
            Log.LogDebug($"Deathlink received. Source: {deathLink.Source} Cause: {deathLink.Cause} Timestamp: {deathLink.Timestamp}");
            handlers[difficulty](deathLink);
        }

        private void RunDrizzle(DeathLink dl)
        {
            Log.LogDebug("Running Drizzle DeathLink");
            var listField = typeof(CharacterMaster).GetField("deployablesList", BindingFlags.NonPublic | BindingFlags.Instance);
            var instances = PlayerCharacterMasterController.instances;
            foreach (PlayerCharacterMasterController playerCharacterMaster in instances)
            {
                var deployables = listField.GetValue(playerCharacterMaster.master) as List<DeployableInfo>;
                foreach (var deployable in deployables)
                {
                    var master = deployable.deployable.GetComponent<CharacterMaster>();
                    var body = master.GetBodyObject();
                    var health = body.GetComponent<HealthComponent>();
                    health.Suicide();
                }
            }
        }

        private void RunRainstorm(DeathLink dl)
        {
            Log.LogDebug("Running Rainstorm DeathLink");
            var instances = PlayerCharacterMasterController.instances;
            foreach (PlayerCharacterMasterController playerCharacterMaster in instances)
            {
                playerCharacterMaster.master.money = 0;
            }
        }

        private void RunMonsoon(DeathLink dl)
        {
            Log.LogDebug("Running Monsoon DeathLink");
            var instances = PlayerCharacterMasterController.instances;
            foreach (PlayerCharacterMasterController playerCharacterMaster in instances)
            {
                var itemToRemove = playerCharacterMaster.master.inventory.itemAcquisitionOrder.Choice();
                playerCharacterMaster.master.inventory.RemoveItem(itemToRemove);
            }
        }

        private void RunTyphoon(DeathLink dl)
        {
            Log.LogDebug("Running Typhoon DeathLink");
            DoppelgangerInvasionManager.PerformInvasion(new Xoroshiro128Plus(Run.instance.seed));
        }

        private void BringThePain(DeathLink dl)
        {
            Log.LogDebug("AHAHAHAHAHAHAHAHA");
            var randomPlayer = PlayerCharacterMasterController.instances.Choice();
            Log.LogDebug($"Selected player {randomPlayer.GetDisplayName()} to die. NetID: {randomPlayer.netId}");
            randomPlayer.master.GetBody().healthComponent.Suicide(damageType: DamageType.VoidDeath);
        }
    }
}
