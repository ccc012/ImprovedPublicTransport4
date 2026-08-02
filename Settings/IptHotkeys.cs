using System;
using AutoLineColor;
using CSLModsCommon.KeyBindings;
using CSLModsCommon.Manager;
using ImprovedPublicTransport.Query;
using UnityEngine;

namespace ImprovedPublicTransport.Settings
{
    /// <summary>
    /// In-game hotkeys (Options → Key bindings). Registered when a city is loaded.
    /// </summary>
    public static class IptHotkeys
    {
        public const string ToggleTrainDisplay = "IPT4.ToggleTrainDisplay";
        public const string RefreshLineColor = "IPT4.RefreshLineColor";
        public const string AdvancedStopSelectionAlternate = "IPT4.AdvancedStopSelectionAlternate";

        // Defaults — also shown in Options; rebinding updates Combination on these instances.
        public static readonly KeyBinding TrainDisplayToggle =
            new KeyBinding(new KeyCombination(KeyCode.T, control: true, shift: true, alt: false));

        public static readonly KeyBinding LineColorRefresh =
            new KeyBinding(new KeyCombination(KeyCode.R, control: true, shift: true, alt: false));

        // Held, not pressed-once: Advanced Stop Selection samples Combination.IsPressed() every
        // frame while the player drags a stop connection, to offer the exact-track alternate mode.
        // Previously hardcoded to LeftShift OR RightShift; a single rebindable key covers one side
        // only, but lets it be moved off Shift entirely if that conflicts with something else.
        public static readonly KeyBinding AdvancedStopSelectionAlternateKey =
            new KeyBinding(new KeyCombination(KeyCode.LeftShift), KeyBindingTriggerMode.Hold);

        public static void Register()
        {
            try
            {
                var mgr = Domain.DefaultDomain.GetOrCreateManager<KeyBindingManager>();
                if (mgr == null)
                {
                    return;
                }

                mgr.Register(ToggleTrainDisplay, TrainDisplayToggle, OnToggleTrainDisplay,
                    KeyBindingContext.Game, overwrite: true);
                mgr.Register(RefreshLineColor, LineColorRefresh, OnRefreshLineColor,
                    KeyBindingContext.Game, overwrite: true);
                // No action needed - AdvancedStopSelection reads Combination.IsPressed() directly
                // every frame instead of reacting to a dispatched trigger. Still registered so it
                // gets a slot in the rebind UI, conflict detection, and save/load like the others.
                mgr.Register(AdvancedStopSelectionAlternate, AdvancedStopSelectionAlternateKey, () => { },
                    KeyBindingContext.Game, overwrite: true);
            }
            catch (Exception ex)
            {
                Util.Utils.LogError($"IptHotkeys.Register failed: {ex.Message}");
            }
        }

        public static void Unregister()
        {
            try
            {
                var mgr = Domain.DefaultDomain.GetOrCreateManager<KeyBindingManager>();
                mgr?.Unregister(ToggleTrainDisplay);
                mgr?.Unregister(RefreshLineColor);
                mgr?.Unregister(AdvancedStopSelectionAlternate);
            }
            catch
            {
                // non-fatal
            }
        }

        private static void OnToggleTrainDisplay()
        {
            var s = ModSetting.Instance;
            s.TrainDisplayMode = s.TrainDisplayMode == ModSetting.TrainDisplayModes.Enabled
                ? ModSetting.TrainDisplayModes.Disabled
                : ModSetting.TrainDisplayModes.Enabled;
        }

        private static void OnRefreshLineColor()
        {
            var lineId = WorldInfoCurrentLineIDQuery.Query(out _);
            if (lineId == 0)
            {
                return;
            }

            ColorMonitor.ForceRefreshLineNow(lineId);
        }
    }
}
