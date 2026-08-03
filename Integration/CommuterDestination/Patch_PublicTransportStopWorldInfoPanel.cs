// Adapted from Commuter Destination (MIT, Workshop 2475986859,
// github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
using System;
using ColossalFramework.UI;
using HarmonyLib;
using ImprovedPublicTransport;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace CommuterDestination
{
    /// <summary>
    /// Upstream's OpenStopDestinationPanelPatch: clicking a stop button opens the destination
    /// panel for that stop. Restored in place of the button IPT4 used to inject into its own stop
    /// panel - the feature now drives itself end to end, exactly as the original mod does.
    ///
    /// IPT4 also prefixes this same method (HarmonyPatches/PublicTransportStopButtonPatches) to
    /// show its own stop panel and skip vanilla's. Both prefixes still run; this one returns true
    /// and only opens a panel, so the two do not interfere.
    /// </summary>
    [HarmonyPatch(typeof(PublicTransportStopButton), "OnMouseDown")]
    internal static class OpenStopDestinationPanelPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(UIComponent component)
        {
            try
            {
                if (!ModSetting.Instance.EnableCommuterDestination)
                {
                    return true;
                }

                var button = component as UIButton;
                if (button == null || !(button.objectUserData is ushort))
                {
                    return true;
                }

                var stopId = (ushort)button.objectUserData;
                var panel = StopDestinationInfoPanel.instance;
                if (panel != null && stopId != 0)
                {
                    panel.Show(stopId);
                    DestinationOverlayManager.EnsureRegistered();
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"CommuterDestination: failed to open destination panel: {ex.Message}");
            }

            return true;
        }
    }
}
