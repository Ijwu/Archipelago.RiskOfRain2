using System;
using System.Collections.Generic;
using System.Text;
using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Net
{
    public class ArchipelagoEndMessage : INetMessage
    {
        public static event Action OnArchipelagoSessionEnd;

        public void Deserialize(NetworkReader reader)
        {
            Log.LogInfo("Receiving ArchipelagoEndMessage");
        }

        public void OnReceived()
        {
            if (OnArchipelagoSessionEnd != null)
            {
                OnArchipelagoSessionEnd();
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            Log.LogInfo("Sending ArchipelagoEndMessage");
        }
    }
}
