using System;
using System.Collections;
using System.Collections.Generic;
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
        
        public ArchipelagoItemLogicController ItemLogic;
        public ArchipelagoLocationCheckProgressBarUI LocationCheckBar;

        private ArchipelagoSession session;
        private DataPackagePacket dataPackagePacket;
        private ConnectedPacket connectedPacket;
        private ConnectPacket connectPacket;

        private Dictionary<int, string> playerNameById;        
        private ulong seed;
        private string lastServerUrl;
        private bool reconnecting = false;

        public ArchipelagoClient()
        {
            connectPacket = new ConnectPacket();

            connectPacket.Game = "Risk of Rain 2";
            connectPacket.Uuid = Guid.NewGuid().ToString();
            connectPacket.Version = new Version(0, 1, 0);
            connectPacket.Tags = new List<string> { "AP" };
        }

        public void Connect(string url, string slotName, string password = null)
        {
            Dispose();

            connectPacket.Name = slotName;
            connectPacket.Password = password;

            lastServerUrl = url;
            session = new ArchipelagoSession(url);
            ItemLogic = new ArchipelagoItemLogicController(session);
            LocationCheckBar = new ArchipelagoLocationCheckProgressBarUI();
            ItemLogic.OnItemDropProcessed += ItemLogicHandler_ItemDropProcessed;

            session.ConnectAsync();
            session.PacketReceived += Session_PacketReceived;
            session.SocketClosed += Session_SocketClosed;

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
        }

        private void UnhookGame()
        {
            On.RoR2.UI.ChatBox.SubmitChat -= ChatBox_SubmitChat;
            RoR2.Run.onRunDestroyGlobal -= Run_onRunDestroyGlobal;
            On.RoR2.Run.BeginGameOver -= Run_BeginGameOver;
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

            if (OnClientDisconnect != null)
            {
                OnClientDisconnect(e.Code, e.Reason, e.WasClean);
            }
        }

        public IEnumerator AttemptReconnect()
        {
            reconnecting = true;
            var retryCounter = 0;

            while ((session == null || !session.Connected)&& retryCounter < 5)
            {
                ChatMessage.Send($"Reconnection attempt #{retryCounter}");
                retryCounter++;
                yield return new WaitForSeconds(3f);
                Connect(lastServerUrl, connectPacket.Name, connectPacket.Password);
            }

            if (session == null || !session.Connected)
            {
                ChatMessage.SendColored("Could not reconnect to Archipelago.", Color.red);
            }
            else if (session != null && session.Connected)
            {
                ChatMessage.SendColored("Re-established Archipelago connection.", Color.green);
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
            // TODO: prevent game over if more dio's can be incoming

            // Are we in commencement?
            if (Stage.instance.sceneDef.baseSceneName == "moon2")
            {
                var packet = new StatusUpdatePacket();
                packet.Status = ArchipelagoClientState.ClientGoal;
                session.SendPacket(packet);
            }
            orig(self, gameEndingDef);
        }

        private void Run_onRunDestroyGlobal(Run obj)
        {
            Dispose();
        }
    }
}
