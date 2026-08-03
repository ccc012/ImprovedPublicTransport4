// Adapted from Commuter Destination (MIT, Workshop 2475986859,
// github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
using System;
using ColossalFramework.UI;
using ImprovedPublicTransport;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace CommuterDestination
{
    /// <summary>
    /// Upstream's OpenStopDestinationPanelPatch: clicking a stop button opens the destination
    /// panel for that stop.
    ///
    /// NOT a separate Harmony patch, on purpose. It was one originally (a second [HarmonyPrefix]
    /// on PublicTransportStopButton.OnMouseDown, alongside IPT4's own prefix in
    /// HarmonyPatches/PublicTransportStopButtonPatches/OnMouseDownPatch.cs) - and it never ran.
    /// Per Harmony's own docs: "The first prefix that returns false will skip all remaining
    /// prefixes ... and the original method." IPT4's own prefix always returns false (it has real
    /// side effects - it shows IPT4's own stop panel), so whichever of the two prefixes Harmony
    /// happened to run first, if it was ours, this one's Harmony registration never executed at
    /// all - the panel and the map icons genuinely could never appear. Prefix ordering between two
    /// independently-registered Harmony patches on the same method is not something to depend on.
    /// Calling this directly from inside IPT4's own prefix removes that dependency entirely.
    /// </summary>
    internal static class OpenStopDestinationPanelPatch
    {
        public static void TryShowForStopClick(UIComponent component)
        {
            try
            {
                if (!ModSetting.Instance.EnableCommuterDestination)
                {
                    return;
                }

                var button = component as UIButton;
                if (button == null || !(button.objectUserData is ushort))
                {
                    return;
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
        }
    }
}
