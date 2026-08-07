using System;
using HarmonyLib;
using ImprovedPublicTransport.Util;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace SharedStopEnabler
{
    internal static class PatchController
    {
        public const string HarmonyModID = "IPT4.SharedStopEnabler";

        private static Harmony _harmony;
        private static bool _active;

        private static Harmony GetHarmonyInstance() => _harmony ??= new Harmony(HarmonyModID);

        public static void Activate()
        {
            if (_active)
            {
                SharedStopRegistry.InitSegments();
                SharedStopElevated.Apply();
                SharedStopRegistry.RecalculateSharedStopSegments();
                return;
            }

            SharedStopRegistry.InitSegments();
            SharedStopElevated.Apply();
            HarmonyScope.PatchNamespace(GetHarmonyInstance(), "SharedStopEnabler");
            ImprovedPublicTransport.Integration.AdvancedStopSelection.PatchController.SetSharedStopEnablerActive(true);
            _active = true;
            SharedStopRegistry.RecalculateSharedStopSegments();
        }

        public static void Deactivate()
        {
            ImprovedPublicTransport.Integration.AdvancedStopSelection.PatchController.SetSharedStopEnablerActive(false);
            if (_active)
            {
                GetHarmonyInstance().UnpatchAll(HarmonyModID);
                _active = false;
            }

            SharedStopRegistry.RestoreSegmentFlags();
            SharedStopRegistry.Reset();
            SharedStopElevated.ResetFlags();
        }

    }
}
