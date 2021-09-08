using System;
using System.Collections.Generic;
using System.Text;
using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Net
{
    public class ArchipelagoStartMessage : INetMessage
    {
        public static event Action OnArchipelagoSessionStart;

        public void Deserialize(NetworkReader reader)
        {
            
        }

        public void OnReceived()
        {
            if (OnArchipelagoSessionStart != null)
            {
                OnArchipelagoSessionStart();
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            
        }
    }
}
