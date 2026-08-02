using System;
using System.Reflection;
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
            PatchTransportToolGetStopPosition();
            _active = true;
            SharedStopRegistry.RecalculateSharedStopSegments();
        }

        public static void Deactivate()
        {
            if (_active)
            {
                GetHarmonyInstance().UnpatchAll(HarmonyModID);
                _active = false;
            }

            SharedStopRegistry.Reset();
            SharedStopElevated.ResetFlags();
        }

        /// <summary>
        /// TransportTool.GetStopPosition is private; patch manually like upstream.
        /// Soft-fail if the method signature changed.
        /// </summary>
        private static void PatchTransportToolGetStopPosition()
        {
            try
            {
                var gspMethod = typeof(TransportTool).GetMethod(
                    "GetStopPosition",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var gspTranspiler = typeof(TransportToolGetStopPositionTranspiler).GetMethod(
                    nameof(TransportToolGetStopPositionTranspiler.Transpiler),
                    BindingFlags.Static | BindingFlags.Public);
                if (gspMethod == null || gspTranspiler == null)
                {
                    Utils.LogWarning("SharedStopEnabler: GetStopPosition method not found; placement share may be limited.");
                    return;
                }

                GetHarmonyInstance().Patch(gspMethod, transpiler: new HarmonyMethod(gspTranspiler));
            }
            catch (Exception ex)
            {
                Utils.LogError($"SharedStopEnabler: failed to patch GetStopPosition: {ex.Message}");
            }
        }
    }
}
