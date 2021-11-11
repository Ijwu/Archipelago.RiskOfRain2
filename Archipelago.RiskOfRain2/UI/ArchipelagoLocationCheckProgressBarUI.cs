using System;
using System.Collections.Generic;
using System.Text;
using Archipelago.RiskOfRain2.Extensions;
using Archipelago.RiskOfRain2.Net;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Archipelago.RiskOfRain2.UI
{
    internal class ArchipelagoLocationCheckProgressBarUI : IUIModule
    {
        public int ItemPickupStep { get; set; }
        public int CurrentItemCount { get; set; }

        private HUD hud;
        private ArchipelagoLocationCheckProgressBarController locationCheckBar;
        private GameObject container;

        private void SyncLocationCheckProgress_LocationSynced(int count, int step)
        {
            ItemPickupStep = step;
            CurrentItemCount = count;

            if (locationCheckBar != null)
            {
                locationCheckBar.itemPickupStep = step;
                locationCheckBar.currentItemCount = count;
            }
        }

        public void Enable(HUD hud, ArchipelagoClient2 client)
        {
            this.hud = hud;
            SyncLocationCheckProgress.OnLocationSynced += SyncLocationCheckProgress_LocationSynced;
            ItemPickupStep = client.Locations.ItemPickupStep;
            CurrentItemCount = client.Locations.CurrentChecks * ItemPickupStep;
            client.Locations.OnItemDropProcessed += Locations_OnItemDropProcessed;

            BuildUI(client.AccentColor);
        }

        private void Locations_OnItemDropProcessed(int pickedUpCount)
        {
            CurrentItemCount = pickedUpCount;
            locationCheckBar.currentItemCount = CurrentItemCount;
        }

        public void Disable()
        {
            hud = null;
            SyncLocationCheckProgress.OnLocationSynced -= SyncLocationCheckProgress_LocationSynced;

            GameObject.Destroy(container);
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

            locationCheckBar.canvas.SetColor(accent);

            this.container = container;
        }

        private GameObject CreateTextLabel()
        {
            var container = new GameObject("ArchipelagoTextLabel");
            var rect = container.AddComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.ResetAnchorsAndOffsets();

            var text = GameObject.Instantiate(hud.levelText.targetText);
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
            var progressBarGameObject = GameObject.Instantiate(hud.expBar.gameObject);
            GameObject.Destroy(progressBarGameObject.GetComponent<ExpBar>());

            RectTransform rectTransform = progressBarGameObject.GetComponent<RectTransform>();
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
