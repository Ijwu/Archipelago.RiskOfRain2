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
    //TODO: future: implement UI for AP connection in lobby menu or somewhere appropriate
    //TODO: perhaps only use particular drops as fodder for item pickups (i.e. only chest drops/interactable drops) then set options based on them maybe
    //TODO: auto reconnect
    public class ArchipelagoClient
    {
        private ArchipelagoSession session;

        private DataPackagePacket dataPackagePacket;
        private ConnectedPacket connectedPacket;
        private LocationInfoPacket locationInfoPacket;
        private ConnectPacket connectPacket;

        private Dictionary<int, string> itemLookupById;
        private Dictionary<int, string> locationLookupById;
        private Dictionary<int, string> playerNameById;
        private int pickedUpItemCount = 0;

        private Queue<string> itemReceivedQueue = new Queue<string>();
        private int itemPickupStep = 5;
        private int totalLocations;
        private bool finishedAllChecks = false;
        private ulong seed;
        private string lastServerUrl;

        private bool IsInGame 
        { 
            get
            {
                return (RoR2Application.isInSinglePlayer || RoR2Application.isInMultiPlayer) && RoR2.Run.instance != null;
            } 
        }

        public ArchipelagoClient()
        {
            connectPacket = new ConnectPacket();

            connectPacket.Game = "Risk of Rain 2";
            connectPacket.Uuid = Guid.NewGuid().ToString();
            connectPacket.Version = new Version(0, 1, 0);
            connectPacket.Tags = new List<string> { "AP" };
        }

        public void ResetItemReceivedCount()
        {
            pickedUpItemCount = 0;
        }

        public void Connect(string url, string slotName, string password = null)
        {
            if (session != null && session.Connected)
            {
                session.Disconnect();
            }

            connectPacket.Name = slotName;
            connectPacket.Password = password;

            lastServerUrl = url;
            session = new ArchipelagoSession(url);
            session.ConnectAsync();
            session.PacketReceived += Session_PacketReceived;
            session.SocketClosed += Session_SocketClosed;

            On.RoR2.PickupDropletController.CreatePickupDroplet += PickupDropletController_CreatePickupDroplet;
            RoR2.Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
            On.RoR2.RoR2Application.Update += RoR2Application_Update;
            On.RoR2.Run.BeginGameOver += Run_BeginGameOver;
        }

        private void Session_SocketClosed(WebSocketSharp.CloseEventArgs e)
        {
            if (e.WasClean)
            {
                return;
            }

            // Reconnect
            session = new ArchipelagoSession(lastServerUrl);
            session.ConnectAsync();
            session.PacketReceived += Session_PacketReceived;
            session.SocketClosed += Session_SocketClosed;
        }

        private void Run_BeginGameOver(On.RoR2.Run.orig_BeginGameOver orig, Run self, GameEndingDef gameEndingDef)
        {
            // TODO: prevent game over if more dio's can be incoming

            // Are we in commencement?
            if (Stage.instance.sceneDef.baseSceneName == "moon2")
            {
                var packet = new LocationChecksPacket();
                var id = dataPackagePacket.DataPackage.Games["Risk of Rain 2"].LocationLookup["Victory"];
                packet.Locations = new List<int>() { id };
                session.SendPacket(packet);
            }
            orig(self, gameEndingDef);
        }

        private void Run_onRunDestroyGlobal(Run obj)
        {
            if (session.Connected)
            {
                session.Disconnect();
            }

            On.RoR2.PickupDropletController.CreatePickupDroplet -= PickupDropletController_CreatePickupDroplet;
            RoR2.Run.onRunDestroyGlobal -= Run_onRunDestroyGlobal;
            On.RoR2.RoR2Application.Update -= RoR2Application_Update;
            On.RoR2.Run.BeginGameOver -= Run_BeginGameOver;

            session = null;
        }

        private void RoR2Application_Update(On.RoR2.RoR2Application.orig_Update orig, RoR2Application self)
        {
            if (IsInGame && itemReceivedQueue.Any())
            {
                HandleReceivedItemQueueItem();
            }
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

                        // Add 1 because the user's YAML will contain a value equal to "number of pickups before sent location"
                        itemPickupStep = Convert.ToInt32(connectedPacket.SlotData["itemPickupStep"]) + 1;
                        totalLocations = Convert.ToInt32(connectedPacket.SlotData["totalLocations"]);

                        //TODO: perhaps fix seed setting
                        seed = Convert.ToUInt64(connectedPacket.SlotData["seed"]);

                        // Add up pickedUpItemCount so that resuming a game is possible. The intended behavior is that you immediately receive
                        // all of the items you are granted. This is for restarting (in case you lose a run but are not in commencement). 
                        pickedUpItemCount = connectedPacket.ItemsChecked.Count * itemPickupStep;

                        On.RoR2.Run.Start += (orig, instance) => { instance.seed = seed; orig(instance); };

                        break;
                    }
                case ArchipelagoPacketType.ReceivedItems:
                    {
                        var p = packet as ReceivedItemsPacket;
                        foreach (var newItem in p.Items)
                        {
                            var itemName = itemLookupById[newItem.Item];
                            itemReceivedQueue.Enqueue(itemName);
                        }
                        break;
                    }
                case ArchipelagoPacketType.LocationInfo:
                    {
                        locationInfoPacket = packet as LocationInfoPacket;
                        break;
                    }
                case ArchipelagoPacketType.RoomUpdate:
                    {
                        var p = packet as RoomUpdatePacket;
                        break;
                    }
                case ArchipelagoPacketType.Print:
                    {
                        var p = packet as PrintPacket;
                        ChatMessage.Send(p.Text);
                        break;
                    }
                case ArchipelagoPacketType.PrintJSON:
                    {
                        var p = packet as PrintJsonPacket;
                        string text = "";
                        foreach (var part in p.Data)
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

                        itemLookupById = dataPackagePacket.DataPackage.Games["Risk of Rain 2"].ItemLookup.ToDictionary(x => x.Value, x => x.Key);
                        locationLookupById = dataPackagePacket.DataPackage.Games["Risk of Rain 2"].LocationLookup.ToDictionary(x => x.Value, x => x.Key);

                        session.SendPacket(connectPacket);
                        break;
                    }
            }
        }

        private void HandleReceivedItemQueueItem()
        {
            string itemReceived = itemReceivedQueue.Dequeue();

            switch (itemReceived)
            {
                case "Common Item":
                    var common = Run.instance.availableTier1DropList.Choice();
                    GiveItemToPlayers(common);
                    break;
                case "Uncommon Item":
                    var uncommon = Run.instance.availableTier2DropList.Choice();
                    GiveItemToPlayers(uncommon);
                    break;
                case "Legendary Item":
                    var legendary = Run.instance.availableTier3DropList.Choice();
                    GiveItemToPlayers(legendary);
                    break;
                case "Boss Item":
                    var boss = Run.instance.availableBossDropList.Choice();
                    GiveItemToPlayers(boss);
                    break;
                case "Lunar Item":
                    var lunar = Run.instance.availableLunarDropList.Choice();
                    GiveItemToPlayers(lunar);
                    break;
                case "Equipment":
                    var equipment = Run.instance.availableEquipmentDropList.Choice();
                    GiveEquipmentToPlayers(equipment);
                    break;
                case "Item Scrap, White":
                    GiveItemToPlayers(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapWhite.itemIndex));
                    break;
                case "Item Scrap, Green":
                    GiveItemToPlayers(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapGreen.itemIndex));
                    break;
                case "Item Scrap, Red":
                    GiveItemToPlayers(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapRed.itemIndex));
                    break;
                case "Item Scrap, Yellow":
                    GiveItemToPlayers(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapYellow.itemIndex));
                    break;
                case "Dio's Best Friend":
                    GiveItemToPlayers(PickupCatalog.FindPickupIndex(RoR2Content.Items.ExtraLife.itemIndex));
                    break;
            }
        }

        private void GiveEquipmentToPlayers(PickupIndex pickupIndex)
        {
            foreach (var player in PlayerCharacterMasterController.instances)
            {
                var inventory = player.master.inventory;
                inventory.SetEquipmentIndex(PickupCatalog.GetPickupDef(pickupIndex)?.equipmentIndex ?? EquipmentIndex.None);
                DisplayPickupNotification(pickupIndex);
            }
        }

        private void GiveItemToPlayers(PickupIndex pickupIndex)
        {
            foreach (var player in PlayerCharacterMasterController.instances)
            {
                var inventory = player.master.inventory;
                inventory.GiveItem(PickupCatalog.GetPickupDef(pickupIndex)?.itemIndex ?? ItemIndex.None);
                DisplayPickupNotification(pickupIndex);
            }
        }

        private void DisplayPickupNotification(PickupIndex index)
        {
            // Dunno any better so hit every queue there is.
            foreach (var queue in NotificationQueue.readOnlyInstancesList)
            {
                foreach (var player in PlayerCharacterMasterController.instances)
                {
                    queue.OnPickup(player.master, index);
                }
            }
        }

        private void PickupDropletController_CreatePickupDroplet(On.RoR2.PickupDropletController.orig_CreatePickupDroplet orig, PickupIndex pickupIndex, Vector3 position, Vector3 velocity)
        {
            // If finished all checks, don't do HandleItemDrop(), just let the item pickup spawn.
            if (finishedAllChecks || HandleItemDrop())
            {
                orig(pickupIndex, position, velocity);
            }
        }

        private bool HandleItemDrop()
        {
            pickedUpItemCount += 1;

            if ((pickedUpItemCount % itemPickupStep) == 0)
            {
                var itemSendIndex = pickedUpItemCount / itemPickupStep;

                if (itemSendIndex > totalLocations)
                {
                    finishedAllChecks = true;
                    return true;
                }

                var itemSendName = $"ItemPickup{itemSendIndex}";
                var itemLocationId = dataPackagePacket.DataPackage.Games["Risk of Rain 2"].LocationLookup[itemSendName];

                connectedPacket.ItemsChecked.Add(itemLocationId);
                connectedPacket.MissingChecks.Remove(itemLocationId);

                var packet = new LocationChecksPacket();
                packet.Locations = new List<int> { itemLocationId };

                session.SendPacket(packet);

                return false;
            }

            return true;
        }
    }
}
