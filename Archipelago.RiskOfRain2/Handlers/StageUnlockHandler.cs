using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.RiskOfRain2.Extensions;
using RoR2;

namespace Archipelago.RiskOfRain2.Handlers
{
    internal class StageUnlockHandler : IHandleSomething
    {
        private readonly ReceivedItemsHelper helper;

        private Dictionary<int, List<SceneDef>> unlockedStages = new Dictionary<int, List<SceneDef>>();

        public StageUnlockHandler(ReceivedItemsHelper helper)
        {
            this.helper = helper;

            //Pre-init UnlockedStages
            for (int i = 1; i <= 5; i++)
            {
                unlockedStages.Add(i, new List<SceneDef>());
            }
        }

        public void Hook()
        {
            helper.ItemReceived += Helper_ItemReceived;
            On.RoR2.Run.AdvanceStage += Run_AdvanceStage;
            On.RoR2.Run.Start += Run_Start;
        }

        public void Unhook()
        {
            helper.ItemReceived -= Helper_ItemReceived;
            On.RoR2.Run.AdvanceStage -= Run_AdvanceStage;
            On.RoR2.Run.Start -= Run_Start;
        }

        private void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            orig(self);

            unlockedStages[1].Add(SceneCatalog.mostRecentSceneDef);
        }

        private void Helper_ItemReceived(ReceivedItemsHelper helper)
        {
            var itemName = helper.PeekItemName();
            
            if (itemName.StartsWith("Progressive Stage"))
            {
                _ = helper.DequeueItem();
            }

            var stageLevel = int.Parse(itemName.Replace("Progressive Stage ", ""));

            UnlockRandomStageForLevel(stageLevel);
        }

        private void Run_AdvanceStage(On.RoR2.Run.orig_AdvanceStage orig, Run self, SceneDef nextScene)
        {
            var targetLevel = nextScene.stageOrder;
            if (unlockedStages[targetLevel].Any())
            {
                nextScene = unlockedStages[targetLevel].Choice();
            }
            else
            {
                nextScene = unlockedStages[1].Choice();
            }

            // TODO: handle commencement teleports, this just loops people unless they unlock stuff.

            orig(self, nextScene);
        }

        private void UnlockRandomStageForLevel(int level)
        {
            var levelsForStage = SceneCatalog.allStageSceneDefs.Where(x => x.sceneType == SceneType.Stage && x.stageOrder == level)
                                                               .Where(x => !unlockedStages[level].Contains(x));
            
            if (!levelsForStage.Any())
            {
                return;
            }

            var levelToUnlock = levelsForStage.Choice();

            unlockedStages[level].Add(levelToUnlock);
        }
    }
}
