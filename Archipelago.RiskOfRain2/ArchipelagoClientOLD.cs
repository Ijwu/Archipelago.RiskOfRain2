using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.RiskOfRain2.Net;
using Archipelago.RiskOfRain2.UI;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace Archipelago.RiskOfRain2
{
    //TODO: perhaps only use particular drops as fodder for item pickups (i.e. only chest drops/interactable drops) then set options based on them maybe
    public class ArchipelagoClient : IDisposable
    {
        // Making this static is dirty, I know. But this is gamedev. Gamedev is a cesspool and you know it.
        public static bool RecentlyReconnected = false;

        public delegate void ClientDisconnected(ushort code, string reason, bool wasClean);
        public event ClientDisconnected OnClientDisconnect;

        public string LastServerUrl { get; set; }
        public string SlotName
        {
            get
            {
                return connectPacket.Name;
            }

            set
            {
                connectPacket.Name = value;
            }
        }
    
        public string Password
        {
            get
            {
                return connectPacket.Password;
            }

            set
            {
                connectPacket.Password = value;
            }
        }

        public ArchipelagoItemLogicController ItemLogic;
        public ArchipelagoLocationCheckProgressBarUI LocationCheckBar;

        private ArchipelagoSession session;
        private DataPackagePacket dataPackagePacket;
        private ConnectedPacket connectedPacket;
        private ConnectPacket connectPacket;

        private Dictionary<int, string> playerNameById;        
        private ulong seed;
        
        private bool reconnecting = false;

        public ArchipelagoClient()
        {
            connectPacket = new ConnectPacket();

            connectPacket.Game = "Risk of Rain 2";
            connectPacket.Uuid = Guid.NewGuid().ToString();
            connectPacket.Version = new Version(0, 1, 9);
            connectPacket.Tags = new List<string> { "AP" };
        }

        public void Connect(string url, string slotName, string password = null)
        {
            Dispose();

            connectPacket.Name = slotName;
            connectPacket.Password = password;
            LastServerUrl = url;

            session = new ArchipelagoSession(url);
            ItemLogic = new ArchipelagoItemLogicController(session);
            LocationCheckBar = new ArchipelagoLocationCheckProgressBarUI();

            session.ConnectAsync();

            session.PacketReceived += Session_PacketReceived;
            session.SocketClosed += Session_SocketClosed;
            ItemLogic.OnItemDropProcessed += ItemLogicHandler_ItemDropProcessed;

            if (reconnecting)
            {
                return;
            }

            HookGame();
            new ArchipelagoStartMessage().Send(NetworkDestination.Clients);
        }

        public void Dispose()
        {
            if (session != null && session.Connected)
            {
                session.Disconnect();
            }
            
            if (ItemLogic != null)
            {
                ItemLogic.OnItemDropProcessed -= ItemLogicHandler_ItemDropProcessed;
                ItemLogic.Dispose();
            }
            
            if (LocationCheckBar != null)
            {
                LocationCheckBar.Dispose();
            }
         
            UnhookGame();
            session = null;
        }

        private void UpdatePlayerList(List<NetworkPlayer> players)
        {
            playerNameById = new Dictionary<int, string>
            {
                { 0, "Archipelago Server" }
            };

            foreach (var player in players)
            {
                playerNameById[player.Slot] = player.Name;
            }
        }
        private void HookGame()
        {
            On.RoR2.UI.ChatBox.SubmitChat += ChatBox_SubmitChat;
            RoR2.Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
            On.RoR2.Run.BeginGameOver += Run_BeginGameOver;
            ArchipelagoChatMessage.OnChatReceivedFromClient += ArchipelagoChatMessage_OnChatReceivedFromClient;
        }

        private void UnhookGame()
        {
            On.RoR2.UI.ChatBox.SubmitChat -= ChatBox_SubmitChat;
            RoR2.Run.onRunDestroyGlobal -= Run_onRunDestroyGlobal;
            On.RoR2.Run.BeginGameOver -= Run_BeginGameOver;
            ArchipelagoChatMessage.OnChatReceivedFromClient -= ArchipelagoChatMessage_OnChatReceivedFromClient;
        }

        private void ArchipelagoChatMessage_OnChatReceivedFromClient(string message)
        {
            if (session.Connected && !string.IsNullOrEmpty(message))
            {
                var sayPacket = new SayPacket();
                sayPacket.Text = message;
                session.SendPacket(sayPacket);
            }
        }

        private void ItemLogicHandler_ItemDropProcessed(int pickedUpCount)
        {
            if (LocationCheckBar != null)
            {
                LocationCheckBar.CurrentItemCount = pickedUpCount;
                if ((LocationCheckBar.CurrentItemCount % ItemLogic.ItemPickupStep) == 0)
                {
                    LocationCheckBar.CurrentItemCount = 0;
                }
                else
                {
                    LocationCheckBar.CurrentItemCount = LocationCheckBar.CurrentItemCount % ItemLogic.ItemPickupStep;
                }
            }
            new SyncLocationCheckProgress(LocationCheckBar.CurrentItemCount, LocationCheckBar.ItemPickupStep).Send(NetworkDestination.Clients);
        }

        private void ChatBox_SubmitChat(On.RoR2.UI.ChatBox.orig_SubmitChat orig, ChatBox self)
        {
            var text = self.inputField.text;
            if (session.Connected && !string.IsNullOrEmpty(text))
            {
                var sayPacket = new SayPacket();
                sayPacket.Text = text;
                session.SendPacket(sayPacket);

                self.inputField.text = string.Empty;
                orig(self);
            }
            else
            {
                orig(self);
            }
        }

        private void Session_SocketClosed(WebSocketSharp.CloseEventArgs e)
        {
            Dispose();
            new ArchipelagoEndMessage().Send(NetworkDestination.Clients);

            if (OnClientDisconnect != null)
            {
                OnClientDisconnect(e.Code, e.Reason, e.WasClean);
            }
        }

        public IEnumerator AttemptConnection()
        {
            reconnecting = true;
            var retryCounter = 0;

            while ((session == null || !session.Connected)&& retryCounter < 5)
            {
                ChatMessage.Send($"Connection attempt #{retryCounter+1}");
                retryCounter++;
                yield return new WaitForSeconds(3f);
                Connect(LastServerUrl, connectPacket.Name, connectPacket.Password);
            }

            if (session == null || !session.Connected)
            {
                ChatMessage.SendColored("Could not connect to Archipelago.", Color.red);
                Dispose();
            }
            else if (session != null && session.Connected)
            {
                ChatMessage.SendColored("Established Archipelago connection.", Color.green);
                new ArchipelagoStartMessage().Send(NetworkDestination.Clients);
            }

            reconnecting = false;
            RecentlyReconnected = true;
        }

        private void Session_PacketReceived(ArchipelagoPacketBase packet)
        {
            switch (packet.PacketType)
            {
                case ArchipelagoPacketType.RoomInfo:
                    {
                        session.SendPacket(new GetDataPackagePacket());
                        break;
                    }
                case ArchipelagoPacketType.ConnectionRefused:
                    {
                        var p = packet as ConnectionRefusedPacket;
                        foreach (string err in p.Errors)
                        {
                            ChatMessage.SendColored(err, Color.red);
                            Log.LogError(err);
                        }
                        break;
                    }
                case ArchipelagoPacketType.Connected:
                    {
                        connectedPacket = packet as ConnectedPacket;
                        UpdatePlayerList(connectedPacket.Players);
                        LocationCheckBar.ItemPickupStep = ItemLogic.ItemPickupStep;

                        //TODO: perhaps fix seed setting
                        seed = Convert.ToUInt64(connectedPacket.SlotData["seed"]);
                        //On.RoR2.Run.Start += (orig, instance) => { instance.seed = seed; orig(instance); };
                        break;
                    }
                case ArchipelagoPacketType.Print:
                    {
                        var printPacket = packet as PrintPacket;
                        ChatMessage.Send(printPacket.Text);
                        break;
                    }
                case ArchipelagoPacketType.PrintJSON:
                    {
                        var printJsonPacket = packet as PrintJsonPacket;
                        string text = "";
                        foreach (var part in printJsonPacket.Data)
                        {
                            switch (part.Type)
                            {
                                case "player_id":
                                    {
                                        int player_id = int.Parse(part.Text);
                                        text += playerNameById[player_id];
                                        break;
                                    }
                                case "item_id":
                                    {
                                        int item_id = int.Parse(part.Text);
                                        text += dataPackagePacket.DataPackage.ItemLookup[item_id];
                                        break;
                                    }
                                case "location_id":
                                    {
                                        int location_id = int.Parse(part.Text);
                                        text += dataPackagePacket.DataPackage.LocationLookup[location_id];
                                        break;
                                    }
                                default:
                                    {
                                        text += part.Text;
                                        break;
                                    }
                            }
                        }
                        ChatMessage.Send(text);
                        break;
                    }
                case ArchipelagoPacketType.DataPackage:
                    {
                        dataPackagePacket = packet as DataPackagePacket;
                        session.SendPacket(connectPacket);
                        break;
                    }
            }
        }
        private void Run_BeginGameOver(On.RoR2.Run.orig_BeginGameOver orig, Run self, GameEndingDef gameEndingDef)
        {
            var acceptableEndings = new[] { RoR2Content.GameEndings.MainEnding, RoR2Content.GameEndings.ObliterationEnding, RoR2Content.GameEndings.LimboEnding};
            var isAcceptableEnding = (acceptableEndings.Contains(gameEndingDef)) || (gameEndingDef == RoR2Content.GameEndings.StandardLoss && Stage.instance.sceneDef.baseSceneName == "moon2");

            // Are we in commencement or have we obliterated?
            if (isAcceptableEnding)
            {
                var packet = new StatusUpdatePacket();
                packet.Status = ArchipelagoClientState.ClientGoal;
                session.SendPacket(packet);

                new ArchipelagoEndMessage().Send(NetworkDestination.Clients);
            }
            orig(self, gameEndingDef);
        }

        private void Run_onRunDestroyGlobal(Run obj)
        {
            Dispose();
        }
    }
}
