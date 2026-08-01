using System;
using CitiesHarmony.API;
using ICities;
using IntercityBusControl.HarmonyPatches.BuildingInfoPatches;
using ImprovedPublicTransport.Util;

namespace IntercityBusControl
{
    public class LoadingExtension : LoadingExtensionBase
    {
        private static AppMode _loadMode;
        
        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            InitializePrefabPatch.Reset();
            _loadMode = loading.currentMode;
            try
            {
                if (_loadMode == AppMode.Game)
                {
                    if (!HarmonyHelper.IsHarmonyInstalled)
                    {
                        return;
                    }
                    if (!Mod.IsSunsetHarborInstalled())
                    {
                        if (Diagnostics.VerboseRuntimeLogs)
                        {
                            Utils.Log("Intercity Bus Control - Sunset Harbor DLC not found, skipping patches.");
                        }
                        return;
                    }
                    // Safe-default installs leave EnableIntercityBusControl off: do not register Harmony
                    // at all (CreateVehicle/UpdateBindings prefixes used to load even when the toggle
                    // was false). StationPatcher still only runs from ImprovedPublicTransportMod when on.
                    if (!ImprovedPublicTransport.ModSetting.Instance.EnableIntercityBusControl)
                    {
                        if (Diagnostics.VerboseRuntimeLogs)
                        {
                            Utils.Log("Intercity Bus Control - disabled by settings, skipping patches.");
                        }
                        return;
                    }
                    Patcher.PatchAll();
                }
            }
            catch (Exception e)
            {
                Utils.LogError($"Intercity Bus Control - OnCreated error: {e.Message}");
            }
        }

        public override void OnReleased()
        {
            base.OnReleased();
            InitializePrefabPatch.Reset();
            try
            {
                if (_loadMode == AppMode.Game)
                {
                    if (!HarmonyHelper.IsHarmonyInstalled)
                    {
                        return;
                    }
                    Patcher.UnpatchAll();
                }
            }
            catch (Exception e)
            {
                Utils.LogError($"Intercity Bus Control - OnReleased error: {e.Message}");
            }
        }
    }
}