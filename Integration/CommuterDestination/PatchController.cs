// Adapted from Commuter Destination (MIT, Workshop 2475986859,
// github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
using ColossalFramework.UI;
using HarmonyLib;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace CommuterDestination
{
    /// <summary>
    /// Turns the integration on and off. Nothing else: the feature is a straight port of the
    /// original mod and drives itself - its own panel, its own stop-click patch, its own icons.
    ///
    /// The button IPT4 used to inject into its own stop panel, and the suppress-hide machinery
    /// that button needed, are gone. They existed to graft upstream's behaviour onto IPT4's panel,
    /// and that graft is what stopped the feature working.
    /// </summary>
    internal static class PatchController
    {
        private const string HarmonyModID = "com.IPT.CommuterDestination";

        private static Harmony _harmony;
        private static bool _active;
        private static GameObject _panelObject;

        private static Harmony GetHarmonyInstance() => _harmony ??= new Harmony(HarmonyModID);

        public static bool IsActive => _active;

        public static void Activate()
        {
            EnsurePanelExists();

            if (_active)
            {
                return;
            }

            try
            {
                // Only this integration's own patch class. PatchAll(assembly) would apply EVERY
                // [HarmonyPatch] type in the whole IPT4 assembly a second time, under this
                // integration's Harmony ID - double-patching every other integration, and
                // unpatching them all again on Deactivate.
                GetHarmonyInstance()
                    .CreateClassProcessor(typeof(OpenStopDestinationPanelPatch))
                    .Patch();
                _active = true;
                DestinationOverlayManager.EnsureRegistered();
            }
            catch (System.Exception ex)
            {
                Utils.LogError($"CommuterDestination: failed to apply patches: {ex.Message}");
            }
        }

        public static void Deactivate()
        {
            if (_active)
            {
                GetHarmonyInstance().UnpatchAll(HarmonyModID);
                _active = false;
            }

            // The overlay only draws while the panel is visible, so hiding it also clears the
            // icons - no separate overlay teardown needed.
            var panel = StopDestinationInfoPanel.instance;
            if (panel != null)
            {
                panel.Hide();
            }
        }

        private static void EnsurePanelExists()
        {
            if (StopDestinationInfoPanel.instance != null && _panelObject != null)
            {
                return;
            }

            try
            {
                var view = UIView.GetAView();
                if (view == null)
                {
                    return;
                }

                _panelObject = new GameObject("CommuterDestinationPanel");
                _panelObject.transform.parent = view.transform;
                _panelObject.AddComponent<StopDestinationInfoPanel>();
            }
            catch (System.Exception ex)
            {
                Utils.LogError($"CommuterDestination: failed to create destination panel: {ex.Message}");
            }
        }
    }
}
