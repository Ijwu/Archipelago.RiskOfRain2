using System.Collections.Generic;
using static RoR2.UI.ObjectivePanelController;

namespace Archipelago.RiskOfRain2.UI.Objectives
{
    public class ArchipelagoTotalChecksObjectiveController
    {
        public class TotalChecksObjectiveTracker : ObjectiveTracker
        {
            protected override string GenerateString()
            {
                return $"Complete location checks: {CurrentChecks}/{TotalChecks}";
            }

            protected override bool IsDirty()
            {
                return true;
            }
        }

        static ArchipelagoTotalChecksObjectiveController()
        {
            collectObjectiveSources += ObjectivePanelController_collectObjectiveSources;
        }

        private static void ObjectivePanelController_collectObjectiveSources(RoR2.CharacterMaster arg1, List<ObjectiveSourceDescriptor> arg2)
        {
            if (addObjective)
            {
                arg2.Add(new ObjectiveSourceDescriptor()
                {
                    master = arg1,
                    objectiveType = typeof(TotalChecksObjectiveTracker),
                    source = null
                });
            }
        }

        public static int CurrentChecks { get; set; }

        public static int TotalChecks { get; set; }

        private static bool addObjective;

        public static void AddObjective()
        {
            addObjective = true;
        }

        public static void RemoveObjective()
        {
            addObjective = false;
        }
    }
}
