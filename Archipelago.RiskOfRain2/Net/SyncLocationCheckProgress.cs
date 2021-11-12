using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Net
{
    public class SyncLocationCheckProgress : INetMessage
    {
        public delegate void LocationCheckSyncHandler(int count, int step);
        public static event LocationCheckSyncHandler OnLocationSynced;

        int itemPickupCount;
        int itemPickupStep;

        public SyncLocationCheckProgress()
        {

        }

        public SyncLocationCheckProgress(int itemCount, int pickupStep)
        {
            itemPickupCount = itemCount;
            itemPickupStep = pickupStep;
        }

        public void Deserialize(NetworkReader reader)
        {
            itemPickupStep = reader.ReadInt32();
            itemPickupCount = reader.ReadInt32();
        }

        public void OnReceived()
        {
            if (OnLocationSynced != null)
            {
                OnLocationSynced(itemPickupCount, itemPickupStep);
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(itemPickupStep);
            writer.Write(itemPickupCount);
        }
    }
}
