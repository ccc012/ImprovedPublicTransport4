using CitiesHarmony.API;
using HarmonyLib;
using ImprovedPublicTransport.Util;

namespace IntercityBusControl
{
    /// <summary>
    /// Class to manage the mod's Harmony patches.
    /// </summary>
    internal static class Patcher
    {
        // Unique harmony identifier.
        private const string HarmonyId = "github.com/bloodypenguin/Skylines-IntercityBusController";

        // Flag.
        internal static bool Patched => _patched;
        private static bool _patched = false;


        /// <summary>
        /// Apply all Harmony patches.
        /// </summary>
        public static void PatchAll()
        {
            // Don't do anything if already patched.
            if (!_patched)
            {
                // Ensure Harmony is ready before patching.
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    // Only IntercityBusControl.* types - full-assembly PatchAll would register
                    // every other Integration/* [HarmonyPatch] under this Harmony ID.
                    Harmony harmonyInstance = new Harmony(HarmonyId);
                    HarmonyScope.PatchNamespace(harmonyInstance, "IntercityBusControl");

                    // Belt-and-suspenders: attribute Prefix on UpdateBindings was not reliably
                    // blocking OnAccepts echos in playtest (both False and True logged as player
                    // click). Re-assert Prefix/Finalizer via explicit Method patch.
                    var updateBindings = AccessTools.Method(typeof(CityServiceWorldInfoPanel), "UpdateBindings");
                    if (updateBindings != null)
                    {
                        var prefix = AccessTools.Method(
                            typeof(HarmonyPatches.CityServiceWorldInfoPanelPatches.UpdateBindingsPatch),
                            nameof(HarmonyPatches.CityServiceWorldInfoPanelPatches.UpdateBindingsPatch.Prefix));
                        var finalizer = AccessTools.Method(
                            typeof(HarmonyPatches.CityServiceWorldInfoPanelPatches.UpdateBindingsPatch),
                            nameof(HarmonyPatches.CityServiceWorldInfoPanelPatches.UpdateBindingsPatch.Finalizer));
                        if (prefix != null)
                        {
                            harmonyInstance.Patch(updateBindings, prefix: new HarmonyMethod(prefix));
                        }

                        if (finalizer != null)
                        {
                            harmonyInstance.Patch(updateBindings, finalizer: new HarmonyMethod(finalizer));
                        }

                        Utils.Log("IntercityBusControl: UpdateBindings Prefix/Finalizer explicitly patched for checkbox lock.");
                    }

                    _patched = true;
                }
            }
        }


        public static void UnpatchAll()
        {
            // Only unapply if patches appplied.
            if (_patched)
            {
                // Unapply patches, but only with our HarmonyID.
                Harmony harmonyInstance = new Harmony(HarmonyId);
                harmonyInstance.UnpatchAll(HarmonyId);
                _patched = false;
            }
        }
    }
}