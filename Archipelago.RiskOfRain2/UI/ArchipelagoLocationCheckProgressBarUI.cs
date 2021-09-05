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
    public class ArchipelagoLocationCheckProgressBarUI : IDisposable
    {
        public int ItemPickupStep { get; set; }
        public int CurrentItemCount { get; set; }

        private HUD hud;
        private ArchipelagoLocationCheckProgressBarController locationCheckBar;
        private GameObject container;

        public ArchipelagoLocationCheckProgressBarUI()
        {
            Log.LogInfo("ArchipelagoLocationCheckProgressBarUI.ctor()");
            SyncLocationCheckProgress.OnLocationSynced += SyncLocationCheckProgress_LocationSynced;
            On.RoR2.UI.HUD.Awake += HUD_Awake;
        }

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

        public void Dispose()
        {
            Log.LogInfo("HUD Stuff Dispose()");
            hud = null;
            On.RoR2.UI.HUD.Awake -= HUD_Awake;
            SyncLocationCheckProgress.OnLocationSynced -= SyncLocationCheckProgress_LocationSynced;

            GameObject.Destroy(container);
        }

        private void HUD_Awake(On.RoR2.UI.HUD.orig_Awake orig, HUD self)
        {
            Log.LogInfo("HUD_Awake()");
            orig(self);
            hud = self;
            PopulateHUD();
        }

        private void PopulateHUD()
        {
            Log.LogInfo("PopulateHUD()");
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

            locationCheckBar.canvas.SetColor(new Color(.8f, .5f, 1, 1));

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
