using System;
using System.Collections.Generic;
using System.Text;
using Archipelago.RiskOfRain2.Net;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Archipelago.RiskOfRain2.UI
{
    public class ArchipelagoHUDController : IDisposable
    {
        public int ItemPickupStep { get; set; }
        public int CurrentItemCount { get; set; }

        private HUD hud;
        private ArchipelagoLocationCheckProgressBarController locationCheckBar;

        public ArchipelagoHUDController()
        {
            On.RoR2.UI.HUD.Awake += HUD_Awake;
            SyncLocationCheckProgress.LocationSynced += SyncLocationCheckProgress_LocationSynced;
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
            hud = null;
            On.RoR2.UI.HUD.Awake -= HUD_Awake;
            SyncLocationCheckProgress.LocationSynced -= SyncLocationCheckProgress_LocationSynced;
        }

        private void HUD_Awake(On.RoR2.UI.HUD.orig_Awake orig, HUD self)
        {
            orig(self);
            hud = self;
            PopulateHUD();
        }

        private void PopulateHUD()
        {
            var container = new GameObject("ArchipelagoHUD");
            var rectTransform = container.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = new Vector3(-115f, -75f);
            rectTransform.rotation = new Quaternion(0, 0, 0, 0);
            rectTransform.localScale = Vector3.one;
            rectTransform.pivot = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            container.transform.SetParent(hud.expBar.transform.parent.parent);
            container.transform.rotation = new Quaternion(0, 0, 0, 0);
            container.transform.localScale = Vector3.one;

            var text = CreateTextLabel();
            text.transform.SetParent(container.transform);
            text.transform.localScale = Vector3.one;
            text.transform.rotation = new Quaternion(0, 0, 0, 0);

            var progressBar = CreateProgressBar();
            progressBar.transform.SetParent(container.transform);
            progressBar.transform.localScale = Vector3.one;
            progressBar.transform.rotation = new Quaternion(0, 0, 0, 0);

            locationCheckBar.canvas.SetColor(new Color(.8f, .5f, 1, 1));
        }

        private GameObject CreateTextLabel()
        {
            var container = new GameObject("ArchipelagoTextLabel");
            var rect = container.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(-85f, -115f);
            rect.anchorMax = Vector2.one;
            rect.anchorMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.pivot = Vector2.zero;

            var text = UnityEngine.Object.Instantiate(hud.levelText.targetText);
            text.text = "Location Check Progress: ";

            text.transform.SetParent(container.transform);
            text.transform.localScale = Vector3.one;
            text.transform.rotation = new Quaternion(0, 0, 0, 0);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(-105f, -2f);

            return container;
        }

        private GameObject CreateProgressBar()
        {
            var progressBarGameObject = UnityEngine.Object.Instantiate(hud.expBar.gameObject);
            UnityEngine.Object.Destroy(progressBarGameObject.GetComponent<ExpBar>());

            RectTransform rectTransform = progressBarGameObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = new Vector2(1f, 0.25f);
            rectTransform.pivot = Vector2.right;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.rotation = new Quaternion(0, 0, 0, 0);
            rectTransform.localScale = Vector3.one;
            rectTransform.rotation = new Quaternion(0, 0, 0, 0);
            rectTransform.offsetMin = new Vector2(250f, 0f);
            rectTransform.offsetMax = new Vector2(0f, 16f);

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
