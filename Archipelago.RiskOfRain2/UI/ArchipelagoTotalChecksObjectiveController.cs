using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using R2API.Utils;
using RoR2.UI;
using static RoR2.UI.ObjectivePanelController;

namespace Archipelago.RiskOfRain2.UI
{
    public class ArchipelagoTotalChecksObjectiveController
    {
        public class TotalChecksObjectiveTracker : ObjectiveTracker
        {
            public override string GenerateString()
            {
                return $"Complete location checks: {CurrentChecks}/{TotalChecks}";
            }

            public override bool IsDirty()
            {
                return true;
            }
        }

        static ArchipelagoTotalChecksObjectiveController()
        {
            ObjectivePanelController.collectObjectiveSources += ObjectivePanelController_collectObjectiveSources;
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
