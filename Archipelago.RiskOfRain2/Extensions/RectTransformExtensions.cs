using UnityEngine;

namespace Archipelago.RiskOfRain2.Extensions
{
    public static class RectTransformExtensions
    {
        public static void ResetAnchorsAndOffsets(this RectTransform self)
        {
            self.anchorMin = Vector2.zero;
            self.anchorMax = Vector2.one;
            self.pivot = Vector2.zero;
            self.offsetMin = Vector2.zero;
            self.offsetMax = Vector2.zero;
            self.sizeDelta = Vector2.zero;
        }
    }
}
