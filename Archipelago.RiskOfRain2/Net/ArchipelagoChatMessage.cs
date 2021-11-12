using System;
using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Net
{
    public class ArchipelagoChatMessage : INetMessage
    {
        public static event Action<string, string> OnChatReceivedFromClient;

        string author;
        string message;

        public ArchipelagoChatMessage(string author, string message)
        {
            this.author = author;
            this.message = message;
        }

        public ArchipelagoChatMessage()
        {

        }

        public void Deserialize(NetworkReader reader)
        {
            author = reader.ReadString();
            message = reader.ReadString();
        }

        public void OnReceived()
        {
            if (OnChatReceivedFromClient != null)
            {
                OnChatReceivedFromClient(author, message);
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(author);
            writer.Write(message);
        }
    }
}
