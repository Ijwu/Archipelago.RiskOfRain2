using System;
using System.Collections.Generic;
using System.Text;
using Archipelago.RiskOfRain2.UI;
using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Net
{
    public class RemoveCheckObjective : INetMessage
    {
        public void Deserialize(NetworkReader reader)
        {
            
        }

        public void OnReceived()
        {
            ArchipelagoTotalChecksObjectiveController.RemoveObjective();
        }

        public void Serialize(NetworkWriter writer)
        {
            
        }
    }
}
