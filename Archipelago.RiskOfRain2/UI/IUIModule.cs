using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Archipelago.RiskOfRain2.UI
{
    public interface IUIModule
    {
        void Enable(HUD hud);
        void Disable();
    }
}
