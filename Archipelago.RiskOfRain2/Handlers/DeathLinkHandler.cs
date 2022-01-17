using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.RiskOfRain2.Enums;
using Archipelago.RiskOfRain2.Extensions;
using R2API.Utils;
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
            On.RoR2.CharacterMaster.OnBodyDeath += CharacterMaster_OnBodyDeath;
        }

        private void CharacterMaster_OnBodyDeath(On.RoR2.CharacterMaster.orig_OnBodyDeath orig, CharacterMaster self, CharacterBody body)
        {
            if (PlayerCharacterMasterController.instances.Select(x => x.master).Contains(self))
            {
                deathLink.SendDeathLink(new DeathLink(self.playerCharacterMasterController.GetDisplayName()));
            }

            orig(self, body);
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
            ChatMessage.SendColored($"{dl.Source} lost their life. Your deployables end their own in your friend's honor.", Color.red);

            Log.LogDebug("Running Drizzle DeathLink");
            var listField = typeof(CharacterMaster).GetField("deployablesList", BindingFlags.NonPublic | BindingFlags.Instance);
            Log.LogDebug($"DeathLink.Drizzle: List Field is null? {listField == null}");
            var instances = PlayerCharacterMasterController.instances;
            foreach (PlayerCharacterMasterController playerCharacterMaster in instances)
            {
                var deployables = listField.GetValue(playerCharacterMaster.master) as List<DeployableInfo>;
                Log.LogDebug($"DeathLink.Drizzle: Is deployables null? {deployables == null}");
                if (deployables != null)
                {
                    foreach (var deployableInfo in deployables)
                    {
                        var master = deployableInfo.deployable?.GetComponent<CharacterMaster>();
                        var body = master?.GetBodyObject();
                        Log.LogDebug($"DeathLink.Drizzle: Is body null? {body == null} Body name: {body?.name ?? "null"} Owner master name: {deployableInfo.deployable?.ownerMaster?.name}");
                        if (!body)
                        {
                            continue;
                        }
                        var health = body.GetComponent<HealthComponent>();
                        health.Suicide();
                    }
                }
            }
        }

        private void RunRainstorm(DeathLink dl)
        {
            ChatMessage.SendColored($"{dl.Source} lost their life, lose all your money to pay for their funeral.", Color.red);

            Log.LogDebug("Running Rainstorm DeathLink");
            var instances = PlayerCharacterMasterController.instances;
            foreach (PlayerCharacterMasterController playerCharacterMaster in instances)
            {
                playerCharacterMaster.master.money = 0;
            }
        }

        private void RunMonsoon(DeathLink dl)
        {
            ChatMessage.SendColored($"{dl.Source} lost their life, lose an item to give to their begrieved.", Color.red);

            Log.LogDebug("Running Monsoon DeathLink");
            var instances = PlayerCharacterMasterController.instances;
            foreach (PlayerCharacterMasterController playerCharacterMaster in instances)
            {
                var itemToRemove = playerCharacterMaster.master.inventory.itemAcquisitionOrder.Choice();
                DirectMessage.SendDirectMessage($"You lost a single {Language.GetString(ItemCatalog.GetItemDef(itemToRemove).nameToken)}.", playerCharacterMaster.networkUser);
                playerCharacterMaster.master.inventory.RemoveItem(itemToRemove);
            }
        }

        private void RunTyphoon(DeathLink dl)
        {
            ChatMessage.SendColored($"{dl.Source} lost their life, their ghost takes shape and comes back to haunt you.", Color.red);

            Log.LogDebug("Running Typhoon DeathLink");
            DoppelgangerInvasionManager.PerformInvasion(new Xoroshiro128Plus(Run.instance.seed));
        }

        private void BringThePain(DeathLink dl)
        {
            Log.LogDebug("AHAHAHAHAHAHAHAHA");
            var randomPlayer = PlayerCharacterMasterController.instances.Choice();
            ChatMessage.SendColored($"{dl.Source} lost their life, it's only fitting {randomPlayer.GetDisplayName()} loses their own.", Color.red);
            Log.LogDebug($"Selected player {randomPlayer.GetDisplayName()} to die. NetID: {randomPlayer.netId}");
            randomPlayer.master.GetBody().healthComponent.Suicide(damageType: DamageType.VoidDeath);
        }
    }
}
