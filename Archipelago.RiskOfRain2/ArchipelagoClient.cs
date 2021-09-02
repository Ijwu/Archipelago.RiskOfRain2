using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.RiskOfRain2;
using Archipelago.RiskOfRain2.Extensions;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace Archipelago.RiskOfRain2
{
    //TODO: perhaps only use particular drops as fodder for item pickups (i.e. only chest drops/interactable drops) then set options based on them maybe
    public class ArchipelagoClient : IDisposable
    {
        public ArchipelagoItemLogicController ItemLogic;
        public ArchipelagoHUDController HudController = new ArchipelagoHUDController();

        private ArchipelagoSession session;
        private DataPackagePacket dataPackagePacket;
        private ConnectedPacket connectedPacket;
        private ConnectPacket connectPacket;

        private Dictionary<int, string> playerNameById;        
        private ulong seed;
        private string lastServerUrl;

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
            if (session != null && session.Connected)
            {
                session.Disconnect();
            }

            if (ItemLogic != null)
            {
                ItemLogic.Dispose();
            }

            if (HudController != null)
            {
                HudController.Dispose();
            }

            connectPacket.Name = slotName;
            connectPacket.Password = password;

            lastServerUrl = url;
            session = new ArchipelagoSession(url);
            ItemLogic = new ArchipelagoItemLogicController(session);
            HudController = new ArchipelagoHUDController();
            ItemLogic.ItemDropProcessed += ItemLogicHandler_ItemDropProcessed;

            session.ConnectAsync();
            session.PacketReceived += Session_PacketReceived;
            session.SocketClosed += Session_SocketClosed;
            HookGame();
        }

        public void Dispose()
        {
            if (session.Connected)
            {
                session.Disconnect();
            }

            if (ItemLogic != null)
            {
                ItemLogic.ItemDropProcessed -= ItemLogicHandler_ItemDropProcessed;
                ItemLogic.Dispose();
            }

            if (HudController != null)
            {

            }
            HudController.Dispose();
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
            Log.LogInfo("Called and was " + pickedUpCount);
            if (HudController != null)
            {
                Log.LogInfo("inner called");
                HudController.CurrentItemCount = pickedUpCount;
            }
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
            // Clean up
            if (ItemLogic != null)
            {
                ItemLogic.Dispose();
            }

            if (HudController != null)
            {
                HudController.Dispose();
            }

            UnhookGame();

            if (e.WasClean)
            {
                return;
            }

            //TODO: improve reconnect logic. immediate reconnect is nearly useless.
            // Reconnect
            Connect(lastServerUrl, connectPacket.Name, connectPacket.Password);
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
                        HudController.ItemPickupStep = ItemLogic.ItemPickupStep;

                        //TODO: perhaps fix seed setting
                        seed = Convert.ToUInt64(connectedPacket.SlotData["seed"]);
                        On.RoR2.Run.Start += (orig, instance) => { instance.seed = seed; orig(instance); };

                        break;
                    }
                case ArchipelagoPacketType.ReceivedItems:
                    {
                        var p = packet as ReceivedItemsPacket;
                        foreach (var newItem in p.Items)
                        {
                            ItemLogic.EnqueueItem(newItem.Item);
                        }
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
