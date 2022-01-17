using Archipelago.RiskOfRain2.Extensions;
using Archipelago.RiskOfRain2.Net;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2.UI;
using UnityEngine;

namespace Archipelago.RiskOfRain2.UI.ProgressBar
{
    internal class ArchipelagoLocationCheckProgressBarUI : IUIModule
    {
        public int ItemPickupStep { get; set; }
        public int CurrentItemCount { get; set; }

        private ArchipelagoClient client;
        private HUD hud;
        private ArchipelagoLocationCheckProgressBarController locationCheckBar;
        private GameObject container;


        public void Enable(HUD hud, ArchipelagoClient client)
        {
            this.client = client;
            this.hud = hud;
            SyncLocationCheckProgress.OnLocationSynced += SyncLocationCheckProgress_LocationSynced;

            if (!client.ClientSideMode)
            {
                ItemPickupStep = client.Locations.ItemPickupStep;
                client.Locations.OnItemDropProcessed += Locations_OnItemDropProcessed;
            }

            Log.LogDebug($"Building UI with accent color: {client.AccentColor.r} {client.AccentColor.g} {client.AccentColor.b} {client.AccentColor.a}");
            BuildUI(client.AccentColor);
        }

        public void Disable()
        {
            SyncLocationCheckProgress.OnLocationSynced -= SyncLocationCheckProgress_LocationSynced;

            if (!client.ClientSideMode)
            {
                client.Locations.OnItemDropProcessed -= Locations_OnItemDropProcessed;
            }

            container.SetActive(false);
            UnityEngine.Object.Destroy(container);
        }

        private void SyncLocationCheckProgress_LocationSynced(int count, int step)
        {
            ItemPickupStep = step;
            CurrentItemCount = count;

            SetProgressBar(count);

            if (locationCheckBar != null)
            {
                locationCheckBar.itemPickupStep = step;
                locationCheckBar.currentItemCount = count;
            }
        }

        private void Locations_OnItemDropProcessed(int pickedUpCount)
        {
            SetProgressBar(pickedUpCount);
            new SyncLocationCheckProgress(CurrentItemCount, ItemPickupStep).Send(NetworkDestination.Clients);
        }

        private void SetProgressBar(int pickedUpCount)
        {
            if (pickedUpCount % ItemPickupStep == 0)
            {
                Log.LogDebug("Current Item Count is a multiple of the pickup step. Resetting it to zero.");
                CurrentItemCount = 0;
            }
            else
            {
                CurrentItemCount = pickedUpCount % ItemPickupStep;
                Log.LogDebug($"Current Item Count is not an even multiple of the pickup step. Setting it to {CurrentItemCount}.");
            }

            locationCheckBar.currentItemCount = CurrentItemCount;
            Log.LogDebug($"Progress bar is at {CurrentItemCount} item count. It was told {pickedUpCount} items were picked up with a step of {ItemPickupStep}.");
        }

        private void BuildUI(Color accent)
        {
            var container = new GameObject("ArchipelagoHUD");

            var text = CreateTextLabel();
            text.transform.SetParent(container.transform);
            text.transform.ResetScaleAndRotation();

            var progressBar = CreateProgressBar();
            progressBar.transform.SetParent(container.transform);
            progressBar.transform.ResetScaleAndRotation();
            progressBar.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            var rectTransform = container.AddComponent<RectTransform>();
            container.transform.SetParent(hud.expBar.transform.parent.parent);
            rectTransform.ResetAnchorsAndOffsets();
            rectTransform.anchoredPosition = Vector2.zero;
            container.transform.ResetScaleAndRotation();

            Log.LogDebug($"Setting location check bar canvas to color: {accent.r} {accent.g} {accent.b} {accent.a}");
            locationCheckBar.canvas.SetColor(accent);
            this.container = container;
        }

        private GameObject CreateTextLabel()
        {
            var container = new GameObject("ArchipelagoTextLabel");
            var rect = container.AddComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.ResetAnchorsAndOffsets();

            var text = UnityEngine.Object.Instantiate(hud.levelText.targetText);
            text.text = "Location Check Progress: ";
            text.transform.SetParent(container.transform);
            text.transform.ResetScaleAndRotation();

            var textRect = text.GetComponent<RectTransform>();
            textRect.ResetAnchorsAndOffsets();
            textRect.anchoredPosition = new Vector2(-85f, -2f);

            return container;
        }

        private GameObject CreateProgressBar()
        {
            var progressBarGameObject = UnityEngine.Object.Instantiate(hud.expBar.gameObject);
            UnityEngine.Object.Destroy(progressBarGameObject.GetComponent<ExpBar>());

            var rectTransform = progressBarGameObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = Vector2.right;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.offsetMin = new Vector2(250f, 0f);
            rectTransform.offsetMax = new Vector2(0f, 4f);

            locationCheckBar = progressBarGameObject.AddComponent<ArchipelagoLocationCheckProgressBarController>();
            locationCheckBar.currentItemCount = CurrentItemCount;
            locationCheckBar.itemPickupStep = ItemPickupStep;

            var fillPanel = progressBarGameObject.transform.Find("ShrunkenRoot/FillPanel");
            locationCheckBar.fillRectTransform = fillPanel.GetComponent<RectTransform>();

            var canvas = fillPanel.GetComponent<CanvasRenderer>();
            locationCheckBar.canvas = canvas;

            return progressBarGameObject;
        }
    }
}
