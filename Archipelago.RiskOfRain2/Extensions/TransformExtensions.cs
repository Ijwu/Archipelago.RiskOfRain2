using UnityEngine;

namespace Archipelago.RiskOfRain2.Extensions
{
    public static class TransformExtensions
    {
        public static void ResetScaleAndRotation(this Transform self)
        {
            self.localScale = Vector3.one;
            self.rotation = new Quaternion(0, 0, 0, 0);
        }
    }
}
