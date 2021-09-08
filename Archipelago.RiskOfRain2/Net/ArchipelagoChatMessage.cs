using System;
using System.Collections.Generic;
using System.Text;
using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Net
{
    public class ArchipelagoChatMessage : INetMessage
    {
        public static event Action<string> OnChatReceivedFromClient;

        string message;

        public ArchipelagoChatMessage(string message)
        {
            this.message = message;
        }

        public ArchipelagoChatMessage()
        {

        }

        public void Deserialize(NetworkReader reader)
        {
            message = reader.ReadString();
        }

        public void OnReceived()
        {
            if (OnChatReceivedFromClient != null)
            {
                OnChatReceivedFromClient(message);
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(message);
        }
    }
}
