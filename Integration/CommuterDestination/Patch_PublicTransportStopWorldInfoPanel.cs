// Adapted from Commuter Destination (MIT, Workshop 2475986859, github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
// This is where the port diverges most from upstream: instead of an IL transpiler injecting a label
// into IPT2's panel (upstream's Harmony/Patches/IPT2/IPT2StopPanelLabelPatch.cs), this patches
// IPT4's own UI/PublicTransportStopWorldInfoPanel.cs directly, since that file is part of IPT4
// itself - a plain Harmony postfix that finds the existing "Buttons" panel by name and adds one
// more button. No IL pattern matching, nothing that can silently patch the wrong instruction range
// if the game or IPT4's own panel layout changes.
using System;
using ColossalFramework.UI;
using HarmonyLib;
using ImprovedPublicTransport;
using ImprovedPublicTransport.UI;
using UIUtils = ImprovedPublicTransport.Util.UIUtils;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace CommuterDestination
{
    [HarmonyPatch(typeof(PublicTransportStopWorldInfoPanel), "SetupPanel")]
    internal static class Patch_PublicTransportStopWorldInfoPanel_SetupPanel
    {
        private static readonly System.Reflection.FieldInfo InstanceIdField =
            AccessTools.Field(typeof(PublicTransportStopWorldInfoPanel), "m_InstanceID");

        [HarmonyPostfix]
        public static void Postfix(PublicTransportStopWorldInfoPanel __instance)
        {
            try
            {
                if (!ModSetting.Instance.EnableCommuterDestination)
                {
                    return;
                }

                var buttonsPanel = __instance.Find<UIPanel>("Buttons");
                if (buttonsPanel == null)
                {
                    Utils.LogWarning("CommuterDestination: \"Buttons\" panel not found on stop info panel, skipping button.");
                    return;
                }

                var button = UIUtils.CreateButton(buttonsPanel);
                button.name = "CommuterDestination";
                button.textPadding = new UnityEngine.RectOffset(10, 10, 4, 0);
                button.text = Localization.Get("COMMUTER_DESTINATION_BUTTON");
                button.tooltip = Localization.Get("COMMUTER_DESTINATION_BUTTON_TOOLTIP");
                button.textScale = 0.75f;
                button.size = new UnityEngine.Vector2(110f, 32f);
                button.wordWrap = true;
                // SetupPanel (and therefore this postfix) only runs once, when the panel GameObject
                // is first created (PublicTransportStopWorldInfoPanel.Start) - the same button is
                // then reused for every stop the player selects afterwards (Show() does not call
                // SetupPanel again). Capturing __instance's current stop at click time (via the
                // private m_InstanceID field, read fresh on every click) instead of at button-
                // creation time is required for the button to ever reflect anything but the first
                // stop the player happened to click.
                button.eventClick += (_, __) =>
                {
                    var currentInstanceId = (InstanceID)InstanceIdField.GetValue(__instance);
                    CommuterDestinationPanel.ShowForStop(currentInstanceId.NetNode);
                };
            }
            catch (Exception ex)
            {
                Utils.LogError($"CommuterDestination: failed to add button to stop panel: {ex.Message}");
            }
        }
    }

    /// <summary>Keeps the Commuter Destination panel following along when the player uses the
    /// "Previous"/"Next" stop buttons on IPT4's own panel while it is open.</summary>
    [HarmonyPatch(typeof(PublicTransportStopWorldInfoPanel), "ChangeInstanceID")]
    internal static class Patch_PublicTransportStopWorldInfoPanel_ChangeInstanceID
    {
        [HarmonyPostfix]
        public static void Postfix(InstanceID newID)
        {
            try
            {
                if (!ModSetting.Instance.EnableCommuterDestination || CommuterDestinationPanel.Instance == null || !CommuterDestinationPanel.Instance.isVisible)
                {
                    return;
                }

                CommuterDestinationPanel.Instance.Show(newID.NetNode);
            }
            catch (Exception ex)
            {
                Utils.LogError($"CommuterDestination: failed to sync panel on stop navigation: {ex.Message}");
            }
        }
    }

    /// <summary>Closes the satellite Commuter Destination panel when the main stop panel closes, so
    /// it never lingers open pointing at a stop the player is no longer looking at.</summary>
    [HarmonyPatch(typeof(PublicTransportStopWorldInfoPanel), "Hide")]
    internal static class Patch_PublicTransportStopWorldInfoPanel_Hide
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                CommuterDestinationPanel.CloseIfOpen();
            }
            catch (Exception ex)
            {
                Utils.LogError($"CommuterDestination: failed to close panel on stop panel hide: {ex.Message}");
            }
        }
    }
}
