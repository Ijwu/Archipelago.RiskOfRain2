using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Archipelago.RiskOfRain2.UI
{
    internal interface IUIModule
    {
        void Enable(HUD hud, ArchipelagoClient2 client);
        void Disable();
    }
}
