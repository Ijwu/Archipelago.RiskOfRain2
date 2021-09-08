using System;
using System.Collections.Generic;
using System.Text;
using Archipelago.RiskOfRain2.UI;
using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Net
{
    public class SyncTotalCheckProgress : INetMessage
    {
        int currentChecks;
        int totalChecks;

        public SyncTotalCheckProgress()
        {

        }

        public SyncTotalCheckProgress(int current, int total)
        {
            currentChecks = current;
            totalChecks = total;
        }

        public void Deserialize(NetworkReader reader)
        {
            currentChecks = reader.ReadInt32();
            totalChecks = reader.ReadInt32();
        }

        public void OnReceived()
        {
            ArchipelagoTotalChecksObjectiveController.CurrentChecks = currentChecks;
            ArchipelagoTotalChecksObjectiveController.TotalChecks = totalChecks;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(currentChecks);
            writer.Write(totalChecks);
        }
    }
}
