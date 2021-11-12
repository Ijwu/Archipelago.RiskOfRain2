using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Archipelago.RiskOfRain2.UI;

namespace Archipelago.RiskOfRain2.Handlers
{
    internal class UIModuleHandler : IHandleSomething
    {
        private List<Type> uiModuleTypes = new List<Type>();
        private List<IUIModule> uiModules = new List<IUIModule>();
        private readonly ArchipelagoClient client;

        public UIModuleHandler(ArchipelagoClient client)
        {
            uiModuleTypes = Assembly.GetExecutingAssembly().GetTypes().ToList().Where(x => x != typeof(IUIModule) && typeof(IUIModule).IsAssignableFrom(x)).ToList();

            Log.LogDebug($"UI Modules Found: {string.Join(", ", uiModuleTypes.Select(x => x.Name))}");
            this.client = client;
        }

        public void Hook()
        {
            On.RoR2.UI.HUD.OnEnable += HUD_OnEnable;
            On.RoR2.UI.HUD.OnDisable += HUD_OnDisable;
        }

        public void Unhook()
        {
            On.RoR2.UI.HUD.OnEnable -= HUD_OnEnable;
            On.RoR2.UI.HUD.OnDisable -= HUD_OnDisable;
        }

        private void HUD_OnEnable(On.RoR2.UI.HUD.orig_OnEnable orig, RoR2.UI.HUD self)
        {
            Log.LogDebug("HUD was enabled.");
            foreach (var type in uiModuleTypes)
            {
                IUIModule item = (IUIModule)Activator.CreateInstance(type);
                item.Enable(self, client);
                uiModules.Add(item);
            }

            orig(self);
        }

        private void HUD_OnDisable(On.RoR2.UI.HUD.orig_OnDisable orig, RoR2.UI.HUD self)
        {
            Log.LogDebug("HUD was disabled.");
            foreach (var item in uiModules)
            {
                item.Disable();
            }

            uiModules.Clear();

            orig(self);
        }
    }
}