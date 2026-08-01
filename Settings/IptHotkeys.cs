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

        // Defaults — also shown in Options; rebinding updates Combination on these instances.
        public static readonly KeyBinding TrainDisplayToggle =
            new KeyBinding(new KeyCombination(KeyCode.T, control: true, shift: true, alt: false));

        public static readonly KeyBinding LineColorRefresh =
            new KeyBinding(new KeyCombination(KeyCode.R, control: true, shift: true, alt: false));

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
