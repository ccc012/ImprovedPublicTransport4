using ColossalFramework;
using ColossalFramework.UI;
using HarmonyLib;
using ImprovedPublicTransport.Util;
using ImprovedPublicTransport.UI;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace CommuterDestination
{
    internal static class PatchController
    {
        public const string HarmonyModID = "IPT4.CommuterDestination";

        private static Harmony _harmony;
        private static bool _active;

        /// <summary>Ignore stop-panel CheckForClose for a few frames after Destinations click.</summary>
        public static int SuppressStopPanelHideFrames;

        /// <summary>
        /// Frame the suppression was armed. TickSuppressHide only runs from the stop panel's own
        /// LateUpdate, so if that never fires for any reason (panel pooling, a future game/engine
        /// change to the patched method), SuppressStopPanelHideFrames could stay stuck above zero
        /// forever - permanently blocking the X button. The Hide() prefix cross-checks this
        /// timestamp so the suppression always expires on its own even if the decrement never runs.
        /// </summary>
        private static int _suppressArmedFrame;

        public static void ArmSuppressHide()
        {
            SuppressStopPanelHideFrames = 5;
            _suppressArmedFrame = Time.frameCount;
        }

        /// <summary>True only while genuinely within the suppression window - self-heals stale state.</summary>
        public static bool IsSuppressHideActive()
        {
            if (SuppressStopPanelHideFrames <= 0)
            {
                return false;
            }

            // Generous margin over the 5-frame intent; anything beyond this is stuck state, not a
            // real suppression window (a real one always clears within a handful of frames).
            if (Time.frameCount - _suppressArmedFrame > 30)
            {
                SuppressStopPanelHideFrames = 0;
                return false;
            }

            return true;
        }

        private static Harmony GetHarmonyInstance() => _harmony ??= new Harmony(HarmonyModID);

        public static bool IsActive => _active;

        public static void Activate()
        {
            if (!_active)
            {
                HarmonyScope.PatchNamespace(GetHarmonyInstance(), "CommuterDestination");
                _active = true;
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log("CommuterDestination: Harmony patches applied.");
                }
            }

            DestinationOverlayManager.EnsureRegistered();
            StopPanelButton.EnsureOnExistingPanel();
        }

        public static void Deactivate()
        {
            if (_active)
            {
                GetHarmonyInstance().UnpatchAll(HarmonyModID);
                _active = false;
            }

            StopPanelButton.RemoveFromExistingPanel();
            CommuterDestinationPanel.CloseIfOpen();
            DestinationOverlayManager.ClearOverlay();
        }

        public static void TickSuppressHide()
        {
            if (SuppressStopPanelHideFrames > 0)
            {
                SuppressStopPanelHideFrames--;
            }
        }
    }

    /// <summary>
    /// Destinations button on the stop world-info panel.
    /// </summary>
    internal static class StopPanelButton
    {
        private const string ButtonName = "CommuterDestination";

        private static readonly System.Reflection.FieldInfo InstanceIdField =
            AccessTools.Field(typeof(PublicTransportStopWorldInfoPanel), "m_InstanceID");

        public static void EnsureOnExistingPanel()
        {
            try
            {
                var panel = PublicTransportStopWorldInfoPanel.instance;
                if (panel == null)
                {
                    return;
                }

                EnsureOnPanel(panel);
            }
            catch (System.Exception ex)
            {
                Utils.LogError($"CommuterDestination: EnsureOnExistingPanel failed: {ex.Message}");
            }
        }

        public static void EnsureOnPanel(PublicTransportStopWorldInfoPanel panel)
        {
            if (panel == null || !ImprovedPublicTransport.ModSetting.Instance.EnableCommuterDestination)
            {
                return;
            }

            var buttonsPanel = panel.Find<UIPanel>("Buttons");
            if (buttonsPanel == null)
            {
                Utils.LogWarning("CommuterDestination: Buttons panel not found on stop UI.");
                return;
            }

            // Room for a second row so the 4th button is not outside the raycast box.
            if (buttonsPanel.height < 70f)
            {
                buttonsPanel.height = 70f;
                buttonsPanel.wrapLayout = true;
                buttonsPanel.autoLayout = true;
                // Grow parent stop panel a bit so the extra row is visible.
                if (panel.height < 320f)
                {
                    panel.height = 320f;
                }
            }

            var existing = buttonsPanel.Find<UIButton>(ButtonName);
            if (existing != null)
            {
                existing.isVisible = true;
                existing.isEnabled = true;
                return;
            }

            var button = ImprovedPublicTransport.Util.UIUtils.CreateButton(buttonsPanel);
            button.name = ButtonName;
            button.textPadding = new RectOffset(8, 8, 4, 0);
            button.text = ImprovedPublicTransport.Localization.Get("COMMUTER_DESTINATION_BUTTON");
            button.tooltip = ImprovedPublicTransport.Localization.Get("COMMUTER_DESTINATION_BUTTON_TOOLTIP");
            button.textScale = 0.7f;
            button.size = new Vector2(110f, 32f);
            button.wordWrap = true;
            button.canFocus = false;

            // MouseDown first so we suppress Hide before LateUpdate CheckForClose runs.
            button.eventMouseDown += (_, __) =>
            {
                PatchController.ArmSuppressHide();
            };

            button.eventClick += (_, __) =>
            {
                try
                {
                    PatchController.ArmSuppressHide();
                    var stopId = ResolveCurrentStopId(panel);
                    if (stopId == 0)
                    {
                        return;
                    }

                    // Toggle secondary list (icons keep updating from stop Show).
                    var inst = CommuterDestinationPanel.Instance;
                    if (inst != null && inst.isVisible && inst.CurrentStopId == stopId)
                    {
                        // Soft hide — same as X for "I don't need the list", icons stay.
                        CommuterDestinationPanel.UserDismissedSet();
                        inst.Hide();
                        return;
                    }

                    CommuterDestinationPanel.ShowForStop(stopId);
                }
                catch (System.Exception ex)
                {
                    Utils.LogError($"CommuterDestination: button click failed: {ex.Message}");
                }
            };

            buttonsPanel.ResetLayout();
        }

        private static ushort ResolveCurrentStopId(PublicTransportStopWorldInfoPanel panel)
        {
            if (InstanceIdField != null)
            {
                try
                {
                    var id = (InstanceID)InstanceIdField.GetValue(panel);
                    if (id.NetNode != 0)
                    {
                        return id.NetNode;
                    }
                }
                catch
                {
                    // fall through
                }
            }

            // Fallback: WorldInfoPanel selection.
            var selected = WorldInfoPanel.GetCurrentInstanceID();
            return selected.NetNode;
        }

        public static void RemoveFromExistingPanel()
        {
            try
            {
                var panel = PublicTransportStopWorldInfoPanel.instance;
                if (panel == null)
                {
                    return;
                }

                var buttonsPanel = panel.Find<UIPanel>("Buttons");
                var button = buttonsPanel?.Find<UIButton>(ButtonName);
                if (button != null)
                {
                    buttonsPanel.RemoveUIComponent(button);
                    Object.Destroy(button.gameObject);
                    buttonsPanel.ResetLayout();
                }
            }
            catch
            {
                // non-fatal
            }
        }
    }
}
