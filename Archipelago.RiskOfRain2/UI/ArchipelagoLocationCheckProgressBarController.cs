using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Archipelago.RiskOfRain2.UI
{
    public class ArchipelagoLocationCheckProgressBarController : MonoBehaviour
    {
        public int currentItemCount;

        public int itemPickupStep;

        public RectTransform fillRectTransform;

        public CanvasRenderer canvas;

        public void Update()
        {
            var progressPercent = Mathf.InverseLerp(0, itemPickupStep, currentItemCount);
            if (fillRectTransform)
            {
                fillRectTransform.anchorMin = new Vector2(0f, 0f);
                fillRectTransform.anchorMax = new Vector2(progressPercent, 1f);
                fillRectTransform.sizeDelta = new Vector2(1f, 1f);
            }
        }
    }
}
