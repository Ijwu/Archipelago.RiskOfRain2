using System;
using System.Collections.Generic;
using System.Text;
using Archipelago.RiskOfRain2.UI;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archipelago.RiskOfRain2
{
    public class ArchipelagoHUDController : IDisposable
    {
        public int ItemPickupStep { get; set; }
        public int CurrentItemCount 
        { 
            get
            {
                return locationCheckBar.currentItemCount;
            }
            set
            {
                locationCheckBar.currentItemCount = value;
            }
        }

        private HUD hud;
        private ArchipelagoLocationCheckProgressBarController locationCheckBar;

        public ArchipelagoHUDController()
        {
            On.RoR2.UI.HUD.Awake += HUD_Awake;
        }

        public void Dispose()
        {
            hud = null;
            On.RoR2.UI.HUD.Awake -= HUD_Awake;
        }

        private void HUD_Awake(On.RoR2.UI.HUD.orig_Awake orig, RoR2.UI.HUD self)
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
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector3(-50f, -80f);
            rectTransform.rotation = new Quaternion(0, 0, 0, 0);
            rectTransform.localScale = new Vector3(1f, 1f, 1f);
            rectTransform.pivot = Vector2.zero;
            container.transform.rotation = new Quaternion(0, 0, 0, 0);
            container.transform.localScale = new Vector3(1f, 1f, 1f);
            container.transform.SetParent(hud.expBar.transform.parent);

            var text = CreateTextLabel();
            text.transform.SetParent(container.transform);

            var progressBar = CreateProgressBar();
            progressBar.transform.SetParent(container.transform);
        }

        private GameObject CreateTextLabel()
        {
            var container = new GameObject("ArchipelagoTextLabel");
            var rect = container.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(25f, -65f);

            var text = GameObject.Instantiate(hud.levelText.targetText);
            text.text = "Location Check Progress: ";

            text.transform.SetParent(container.transform);

            return container;
        }

        private GameObject CreateProgressBar()
        {
            var progressBarGameObject = GameObject.Instantiate(hud.expBar.gameObject);
            GameObject.Destroy(progressBarGameObject.GetComponent<ExpBar>());

            RectTransform rectTransform = progressBarGameObject.GetComponent<RectTransform>();
            rectTransform.anchorMax = new Vector2(1f, 0f);

            locationCheckBar = progressBarGameObject.AddComponent<ArchipelagoLocationCheckProgressBarController>();
            locationCheckBar.currentItemCount = CurrentItemCount;
            locationCheckBar.itemPickupStep = ItemPickupStep;
            locationCheckBar.fillRectTransform = progressBarGameObject.transform.Find("ShrunkenRoot/FillPanel").GetComponent<RectTransform>();
            return progressBarGameObject;
        }
    }
}
