using RoR2.UI;

namespace Archipelago.RiskOfRain2.UI
{
    internal interface IUIModule
    {
        void Enable(HUD hud, ArchipelagoClient client);
        void Disable();
    }
}
