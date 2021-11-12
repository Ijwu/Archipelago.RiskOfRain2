using Archipelago.MultiClient.Net.Helpers;
using Archipelago.RiskOfRain2.Extensions;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace Archipelago.RiskOfRain2.Handlers
{
    internal class ReceivedItemsHandler : IHandleSomething
    {
        private readonly ReceivedItemsHelper helper;

        public ReceivedItemsHandler(ReceivedItemsHelper helper)
        {
            this.helper = helper;
        }

        public void Hook()
        {
            helper.ItemReceived += Helper_ItemReceived;
        }

        public void Unhook()
        {
            helper.ItemReceived -= Helper_ItemReceived;
        }

        public string GetItemNameFromId(int id)
        {
            return helper.GetItemName(id);
        }

        private void Helper_ItemReceived(ReceivedItemsHelper helper)
        {
            var itemName = helper.PeekItemName();
            _ = helper.DequeueItem();

            switch (itemName)
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
                var pickupDef = PickupCatalog.GetPickupDef(lunar);
                if (pickupDef.itemIndex != ItemIndex.None)
                {
                    GiveItemToPlayers(lunar);
                }
                else if (pickupDef.equipmentIndex != EquipmentIndex.None)
                {
                    GiveEquipmentToPlayers(lunar);
                }
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
                var activeEquipment = inventory.GetEquipment(inventory.activeEquipmentSlot);
                if (!activeEquipment.Equals(EquipmentState.empty))
                {
                    var playerBody = player.master.GetBodyObject();

                    if (playerBody == null)
                    {
                        return;
                    }

                    var pickupInfo = new GenericPickupController.CreatePickupInfo()
                    {
                        pickupIndex = PickupCatalog.FindPickupIndex(activeEquipment.equipmentIndex),
                        position = playerBody.transform.position,
                        rotation = Quaternion.identity
                    };
                    GenericPickupController.CreatePickup(pickupInfo);
                }

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
    }
}