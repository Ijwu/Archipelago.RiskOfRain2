﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.RiskOfRain2.Extensions;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace Archipelago.RiskOfRain2
{
    public class ArchipelagoItemLogicController
    {
        public int PickedUpItemCount { get; set; }
        public int ItemPickupStep { get; set; }

        public delegate void ItemDropProcessedEvent(int pickedUpCount);
        public event ItemDropProcessedEvent ItemDropProcessed;

        private int totalLocations;
        private bool finishedAllChecks = false;
        private bool hooked = false;
        private ArchipelagoSession session;
        private Queue<string> itemReceivedQueue = new Queue<string>();
        private Dictionary<int, string> itemLookupById;
        private Dictionary<int, string> locationLookupById;
        private GameData riskOfRainData;

        private bool IsInGame
        {
            get
            {
                return (RoR2Application.isInSinglePlayer || RoR2Application.isInMultiPlayer) && RoR2.Run.instance != null;
            }
        }

        public void EnqueueItem(int itemId)
        {
            var item = itemLookupById[itemId];
            itemReceivedQueue.Enqueue(item);
        }

        public void Hook(ArchipelagoSession session)
        {
            hooked = true;
            this.session = session;
            On.RoR2.PickupDropletController.CreatePickupDroplet += PickupDropletController_CreatePickupDroplet;
            On.RoR2.RoR2Application.Update += RoR2Application_Update;
        }

        public void Unhook()
        {
            if (!hooked)
            {
                return;
            }
            hooked = false;
            On.RoR2.PickupDropletController.CreatePickupDroplet -= PickupDropletController_CreatePickupDroplet;
            On.RoR2.RoR2Application.Update -= RoR2Application_Update;
            session = null;
        }

        public void ResetPickedUpItemCount()
        {
            PickedUpItemCount = 0;
        }

        public void HandleConnectedPacket(ConnectedPacket connectedPacket)
        {
            // Add 1 because the user's YAML will contain a value equal to "number of pickups before sent location"
            ItemPickupStep = Convert.ToInt32(connectedPacket.SlotData["itemPickupStep"]) + 1;
            totalLocations = Convert.ToInt32(connectedPacket.SlotData["totalLocations"]);

            // Add up pickedUpItemCount so that resuming a game is possible. The intended behavior is that you immediately receive
            // all of the items you are granted. This is for restarting (in case you lose a run but are not in commencement). 
            PickedUpItemCount = connectedPacket.ItemsChecked.Count * ItemPickupStep;
        }

        public void HandleDataPackage(DataPackagePacket dataPackagePacket)
        {
            itemLookupById = dataPackagePacket.DataPackage.Games["Risk of Rain 2"].ItemLookup.ToDictionary(x => x.Value, x => x.Key);
            locationLookupById = dataPackagePacket.DataPackage.Games["Risk of Rain 2"].LocationLookup.ToDictionary(x => x.Value, x => x.Key);

            riskOfRainData = dataPackagePacket.DataPackage.Games["Risk of Rain 2"];
        }

        private void RoR2Application_Update(On.RoR2.RoR2Application.orig_Update orig, RoR2Application self)
        {
            if (IsInGame && itemReceivedQueue.Any())
            {
                HandleReceivedItemQueueItem();
            }

            orig(self);
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
            var spawnItem = HandleItemDrop();
            
            if (ItemDropProcessed != null)
            {
                ItemDropProcessed(PickedUpItemCount);
            }

            // If finished all checks, don't do HandleItemDrop(), just let the item pickup spawn.
            if (finishedAllChecks || spawnItem)
            {
                orig(pickupIndex, position, velocity);
            }
        }

        private bool HandleItemDrop()
        {
            PickedUpItemCount += 1;

            if ((PickedUpItemCount % ItemPickupStep) == 0)
            {
                var itemSendIndex = PickedUpItemCount / ItemPickupStep;

                if (itemSendIndex > totalLocations)
                {
                    finishedAllChecks = true;
                    return true;
                }

                var itemSendName = $"ItemPickup{itemSendIndex}";
                var itemLocationId = riskOfRainData.LocationLookup[itemSendName];

                var packet = new LocationChecksPacket();
                packet.Locations = new List<int> { itemLocationId };

                session.SendPacket(packet);
                return false;
            }
            return true;
        }
    }
}
