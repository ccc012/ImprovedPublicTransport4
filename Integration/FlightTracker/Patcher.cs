using HarmonyLib;
using ImprovedPublicTransport.Util;

namespace FlightTracker
{
    /// <summary>
    /// Harmony patches for FlightTracker.
    /// </summary>
    public static class Patcher
    {
        private const string HarmonyID = "com.IPT.FlightTracker";
        private static bool _patched = false;

        /// <summary>
        /// Apply all Harmony patches.
        /// </summary>
        public static void PatchAll()
        {
            if (_patched)
            {
                return;
            }

            _patched = true;
            var harmony = new Harmony(HarmonyID);
            HarmonyScope.PatchNamespace(harmony, "FlightTracker");
        }

        /// <summary>
        /// Remove all Harmony patches.
        /// </summary>
        public static void UnpatchAll()
        {
            if (!_patched)
            {
                return;
            }

            var harmony = new Harmony(HarmonyID);
            harmony.UnpatchAll(HarmonyID);
            _patched = false;
        }
    }
}
