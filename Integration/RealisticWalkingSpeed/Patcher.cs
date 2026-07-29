using HarmonyLib;
using RealisticWalkingSpeed.Patches;
using ColossalFramework;
using ImprovedPublicTransport.Util;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace RealisticWalkingSpeed
{
    public static class Patcher
    {
        private const string _harmonyId = "egi.citiesskylinesmods.realisticwalkingspeed";
        private static bool _patched;

        public static void PatchAll()
        {
            if (_patched)
            {
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log("RealisticWalkingSpeed: Patches already applied, skipping.");
                }
                return;
            }

            try
            {
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log("RealisticWalkingSpeed: Applying Harmony patches...");
                }
                var harmony = new Harmony(_harmonyId);

                new CitizenAnimationSpeedHarmonyPatch(harmony).Apply();
                if (Diagnostics.VerboseTranspileLogs)
                {
                    Utils.Log("RealisticWalkingSpeed: Applied CitizenAnimationSpeedHarmonyPatch");
                }
                
                if (SteamHelper.IsDLCOwned(SteamHelper.DLC.AfterDarkDLC))
                {
                    new CitizenCyclingSpeedHarmonyPatch(harmony).Apply();
                    if (Diagnostics.VerboseTranspileLogs)
                    {
                        Utils.Log("RealisticWalkingSpeed: Applied CitizenCyclingSpeedHarmonyPatch");
                    }
                }

                _patched = true;
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log("RealisticWalkingSpeed: Harmony patches applied successfully");
                }
            }
            catch (System.Exception ex)
            {
                Utils.LogError($"RealisticWalkingSpeed: Failed to apply Harmony patches: {ex.Message}\n{ex.StackTrace}");
                _patched = false;
            }
        }

        public static void UnpatchAll()
        {
            if (!_patched)
            {
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log("RealisticWalkingSpeed: No patches to remove, skipping.");
                }
                return;
            }

            try
            {
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log("RealisticWalkingSpeed: Removing Harmony patches...");
                }
                var harmony = new Harmony(_harmonyId);
                harmony.UnpatchAll(_harmonyId);
                _patched = false;
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log("RealisticWalkingSpeed: Harmony patches removed successfully");
                }
            }
            catch (System.Exception ex)
            {
                Utils.LogError($"RealisticWalkingSpeed: Failed to remove Harmony patches: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}