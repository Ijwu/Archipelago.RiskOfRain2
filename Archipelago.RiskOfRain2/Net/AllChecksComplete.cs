using System;
using System.Collections.Generic;
using System.Text;
using Archipelago.RiskOfRain2.UI.Objectives;
using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Net
{
    public class AllChecksComplete : INetMessage
    {
        public static event Action OnAllChecksComplete;

        public void Deserialize(NetworkReader reader)
        {
            
        }

        public void OnReceived()
        {
            ArchipelagoTotalChecksObjectiveController.RemoveObjective();
            if (OnAllChecksComplete != null)
            {
                OnAllChecksComplete();
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            
        }
    }
}
