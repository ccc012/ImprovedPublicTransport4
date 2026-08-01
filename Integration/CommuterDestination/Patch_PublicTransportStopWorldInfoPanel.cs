// Adapted from Commuter Destination (MIT, Workshop 2475986859) - see LICENSE.txt.
using System;
using HarmonyLib;
using ImprovedPublicTransport;
using ImprovedPublicTransport.UI;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace CommuterDestination
{
    /// <summary>
    /// When a stop is selected: inject Destinations button and refresh data/icons.
    /// Does NOT force-open the floating panel (that made clicks feel broken).
    /// </summary>
    [HarmonyPatch(typeof(PublicTransportStopWorldInfoPanel), "Show", new[] { typeof(UnityEngine.Vector3), typeof(InstanceID) })]
    internal static class Patch_PublicTransportStopWorldInfoPanel_Show
    {
        [HarmonyPostfix]
        public static void Postfix(PublicTransportStopWorldInfoPanel __instance, InstanceID instanceID)
        {
            try
            {
                if (!ModSetting.Instance.EnableCommuterDestination || !InstanceManager.IsValid(instanceID))
                {
                    return;
                }

                StopPanelButton.EnsureOnPanel(__instance);

                var stopId = instanceID.NetNode;
                if (stopId == 0)
                {
                    return;
                }

                // Map icons only while stop is open (secondary list is NOT auto-opened).
                try
                {
                    var graph = DestinationGraphGenerator.GenerateGraph(stopId);
                    DestinationOverlayManager.ActiveGraph = graph;
                    DestinationOverlayManager.ActiveStopId = stopId;
                    DestinationOverlayManager.EnsureRegistered();
                }
                catch (Exception ex)
                {
                    Utils.LogError($"CommuterDestination: graph on Show failed: {ex.Message}");
                }

                // Secondary panel: only retarget if user already opened it and did not dismiss with X.
                var panel = CommuterDestinationPanel.Instance;
                if (panel != null && panel.isVisible && !CommuterDestinationPanel.UserDismissed)
                {
                    panel.Show(stopId);
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"CommuterDestination: failed on stop Show: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(PublicTransportStopWorldInfoPanel), "SetupPanel")]
    internal static class Patch_PublicTransportStopWorldInfoPanel_SetupPanel
    {
        [HarmonyPostfix]
        public static void Postfix(PublicTransportStopWorldInfoPanel __instance)
        {
            try
            {
                if (!ModSetting.Instance.EnableCommuterDestination)
                {
                    return;
                }

                StopPanelButton.EnsureOnPanel(__instance);
            }
            catch (Exception ex)
            {
                Utils.LogError($"CommuterDestination: SetupPanel button inject failed: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(PublicTransportStopWorldInfoPanel), "ChangeInstanceID")]
    internal static class Patch_PublicTransportStopWorldInfoPanel_ChangeInstanceID
    {
        [HarmonyPostfix]
        public static void Postfix(InstanceID newID)
        {
            try
            {
                if (!ModSetting.Instance.EnableCommuterDestination || newID.NetNode == 0)
                {
                    return;
                }

                var graph = DestinationGraphGenerator.GenerateGraph(newID.NetNode);
                DestinationOverlayManager.ActiveGraph = graph;
                DestinationOverlayManager.ActiveStopId = newID.NetNode;

                var panel = CommuterDestinationPanel.Instance;
                if (panel != null && panel.isVisible && !CommuterDestinationPanel.UserDismissed)
                {
                    panel.Show(newID.NetNode);
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"CommuterDestination: failed to sync on stop navigation: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(PublicTransportStopWorldInfoPanel), "Hide")]
    internal static class Patch_PublicTransportStopWorldInfoPanel_Hide
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            // Destinations button click must not close the stop panel in the same frame.
            if (PatchController.SuppressStopPanelHideFrames > 0)
            {
                return false;
            }

            return true;
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                // Only clear when hide really happened (not suppressed).
                if (PatchController.SuppressStopPanelHideFrames > 0)
                {
                    return;
                }

                var stopPanel = PublicTransportStopWorldInfoPanel.instance;
                if (stopPanel != null && stopPanel.isVisible)
                {
                    return;
                }

                CommuterDestinationPanel.CloseIfOpen();
                DestinationOverlayManager.ClearOverlay();
            }
            catch (Exception ex)
            {
                Utils.LogError($"CommuterDestination: failed on stop Hide: {ex.Message}");
            }
        }
    }

    /// <summary>Decay the suppress-hide counter each frame the stop panel updates.</summary>
    [HarmonyPatch(typeof(PublicTransportStopWorldInfoPanel), "LateUpdate")]
    internal static class Patch_PublicTransportStopWorldInfoPanel_LateUpdate
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            PatchController.TickSuppressHide();
        }
    }
}
