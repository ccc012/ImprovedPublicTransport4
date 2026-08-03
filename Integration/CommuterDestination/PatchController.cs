// Adapted from Commuter Destination (MIT, Workshop 2475986859,
// github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
using ColossalFramework.UI;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace CommuterDestination
{
    /// <summary>
    /// Turns the integration on and off. No Harmony patching of its own any more: the stop-click
    /// hook (OpenStopDestinationPanelPatch.TryShowForStopClick) is called directly from IPT4's own
    /// PublicTransportStopButton.OnMouseDown prefix instead of being registered as a second,
    /// independent Harmony prefix on the same method - see that call site's comment for why a
    /// second prefix there never actually ran. All that is left to do here is create this
    /// integration's own floating panel and gate everything behind EnableCommuterDestination
    /// (checked at each call site - TryShowForStopClick, DestinationOverlayManager.RenderIcons).
    /// </summary>
    internal static class PatchController
    {
        private static bool _active;
        private static GameObject _panelObject;

        public static bool IsActive => _active;

        public static void Activate()
        {
            EnsurePanelExists();

            if (_active)
            {
                return;
            }

            _active = true;
            DestinationOverlayManager.EnsureRegistered();
            // LogWarning, not Log: Utils.Log is silent unless Diagnostics.VerboseRuntimeLogs is on
            // (see Utils.Log's own comment) - this integration has been hard to diagnose from a
            // normal log more than once now, so its one-time activation confirmation needs to be
            // visible by default, the same way PatchUtil's own patch-conflict warnings already are.
            Utils.LogWarning("CommuterDestination: integration applied.");
        }

        public static void Deactivate()
        {
            _active = false;

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
