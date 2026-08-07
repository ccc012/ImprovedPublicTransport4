using System;
using AutoLineColor;
using ColossalFramework;
using CSLModsCommon.KeyBindings;
using CSLModsCommon.Manager;
using FlightTracker;
using ImprovedPublicTransport.Query;
using ImprovedPublicTransport.UI;
using UnityEngine;

namespace ImprovedPublicTransport.Settings
{
    /// <summary>
    /// In-game hotkeys (Options → Key bindings). Registered when a city is loaded.
    /// All rebindable; custom combinations persist across restarts via ModSetting.
    /// </summary>
    public static class IptHotkeys
    {
        public const string ToggleTrainDisplay = "IPT4.ToggleTrainDisplay";
        public const string RefreshLineColor = "IPT4.RefreshLineColor";
        public const string AdvancedStopSelectionAlternate = "IPT4.AdvancedStopSelectionAlternate";
        public const string OpenLinePanel = "IPT4.OpenLinePanel";
        public const string ToggleLineUnbunching = "IPT4.ToggleLineUnbunching";
        public const string CopyLineConfig = "IPT4.CopyLineConfig";
        public const string PasteLineConfig = "IPT4.PasteLineConfig";
        public const string CopyToBuildings = "IPT4.CopyToBuildings";
        public const string CopyToDistricts = "IPT4.CopyToDistricts";
        public const string SelectVehicleTypes = "IPT4.SelectVehicleTypes";
        public const string ToggleVehicleEditor = "IPT4.ToggleVehicleEditor";
        public const string OpenFlightTracker = "IPT4.OpenFlightTracker";
        public const string PrevVehicle = "IPT4.PrevVehicle";
        public const string NextVehicle = "IPT4.NextVehicle";

        // Defaults — also shown in Options; rebinding updates Combination on these instances.
        public static readonly KeyBinding TrainDisplayToggle =
            new KeyBinding(new KeyCombination(KeyCode.T, control: true, shift: true, alt: false));

        public static readonly KeyBinding LineColorRefresh =
            new KeyBinding(new KeyCombination(KeyCode.R, control: true, shift: true, alt: false));

        // Held, not pressed-once: Advanced Stop Selection samples Combination.IsPressed() every
        // frame while the player drags a stop connection, to offer the exact-track alternate mode.
        public static readonly KeyBinding AdvancedStopSelectionAlternateKey =
            new KeyBinding(new KeyCombination(KeyCode.LeftShift), KeyBindingTriggerMode.Hold);

        public static readonly KeyBinding LinePanelToggle =
            new KeyBinding(new KeyCombination(KeyCode.L, control: true, shift: true, alt: false));

        public static readonly KeyBinding LineUnbunchingToggle =
            new KeyBinding(new KeyCombination(KeyCode.U, control: true, shift: true, alt: false));

        public static readonly KeyBinding LineConfigCopy =
            new KeyBinding(new KeyCombination(KeyCode.C, control: true, shift: true, alt: false));

        public static readonly KeyBinding LineConfigPaste =
            new KeyBinding(new KeyCombination(KeyCode.V, control: true, shift: true, alt: false));

        public static readonly KeyBinding CopyToServedBuildings =
            new KeyBinding(new KeyCombination(KeyCode.B, control: true, shift: true, alt: false));

        public static readonly KeyBinding CopyToServedDistricts =
            new KeyBinding(new KeyCombination(KeyCode.D, control: true, shift: true, alt: false));

        public static readonly KeyBinding SelectVehicleTypesKey =
            new KeyBinding(new KeyCombination(KeyCode.P, control: true, shift: true, alt: false));

        public static readonly KeyBinding VehicleEditorToggle =
            new KeyBinding(new KeyCombination(KeyCode.E, control: true, shift: true, alt: false));

        public static readonly KeyBinding FlightTrackerToggle =
            new KeyBinding(new KeyCombination(KeyCode.F, control: true, shift: true, alt: false));

        public static readonly KeyBinding PreviousVehicle =
            new KeyBinding(new KeyCombination(KeyCode.LeftArrow, control: true, shift: true, alt: false));

        public static readonly KeyBinding NextVehicleBinding =
            new KeyBinding(new KeyCombination(KeyCode.RightArrow, control: true, shift: true, alt: false));

        public static void Register()
        {
            try
            {
                var mgr = Domain.DefaultDomain.GetOrCreateManager<KeyBindingManager>();
                if (mgr == null)
                {
                    return;
                }

                LoadSavedBindings();

                mgr.Register(ToggleTrainDisplay, TrainDisplayToggle, OnToggleTrainDisplay,
                    KeyBindingContext.Game, overwrite: true);
                mgr.Register(RefreshLineColor, LineColorRefresh, OnRefreshLineColor,
                    KeyBindingContext.Game, overwrite: true);
                mgr.Register(AdvancedStopSelectionAlternate, AdvancedStopSelectionAlternateKey, () => { },
                    KeyBindingContext.Game, overwrite: true);
                mgr.Register(OpenLinePanel, LinePanelToggle, OnOpenLinePanel,
                    KeyBindingContext.Game, overwrite: true);
                mgr.Register(ToggleLineUnbunching, LineUnbunchingToggle, OnToggleLineUnbunching,
                    KeyBindingContext.Game, overwrite: true);
                mgr.Register(CopyLineConfig, LineConfigCopy, OnCopyLineConfig,
                    KeyBindingContext.Game, overwrite: true);
                mgr.Register(PasteLineConfig, LineConfigPaste, OnPasteLineConfig,
                    KeyBindingContext.Game, overwrite: true);
                mgr.Register(CopyToBuildings, CopyToServedBuildings, OnCopyToBuildings,
                    KeyBindingContext.Game, overwrite: true);
                mgr.Register(CopyToDistricts, CopyToServedDistricts, OnCopyToDistricts,
                    KeyBindingContext.Game, overwrite: true);
                mgr.Register(SelectVehicleTypes, SelectVehicleTypesKey, OnSelectVehicleTypes,
                    KeyBindingContext.Game, overwrite: true);
                mgr.Register(ToggleVehicleEditor, VehicleEditorToggle, OnToggleVehicleEditor,
                    KeyBindingContext.Game, overwrite: true);
                mgr.Register(OpenFlightTracker, FlightTrackerToggle, OnOpenFlightTracker,
                    KeyBindingContext.Game, overwrite: true);
                mgr.Register(PrevVehicle, PreviousVehicle, OnPreviousVehicle,
                    KeyBindingContext.Game, overwrite: true);
                mgr.Register(NextVehicle, NextVehicleBinding, OnNextVehicle,
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
                SaveBindings();
                var mgr = Domain.DefaultDomain.GetOrCreateManager<KeyBindingManager>();
                mgr?.Unregister(ToggleTrainDisplay);
                mgr?.Unregister(RefreshLineColor);
                mgr?.Unregister(AdvancedStopSelectionAlternate);
                mgr?.Unregister(OpenLinePanel);
                mgr?.Unregister(ToggleLineUnbunching);
                mgr?.Unregister(CopyLineConfig);
                mgr?.Unregister(PasteLineConfig);
                mgr?.Unregister(CopyToBuildings);
                mgr?.Unregister(CopyToDistricts);
                mgr?.Unregister(SelectVehicleTypes);
                mgr?.Unregister(ToggleVehicleEditor);
                mgr?.Unregister(OpenFlightTracker);
                mgr?.Unregister(PrevVehicle);
                mgr?.Unregister(NextVehicle);
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

        private static ushort SelectedLine()
        {
            var lineId = WorldInfoCurrentLineIDQuery.Query(out _);
            return lineId;
        }

        private static void OnOpenLinePanel()
        {
            var lineId = SelectedLine();
            if (lineId == 0)
            {
                return;
            }

            var pos = Singleton<TransportManager>.instance.m_lines.m_buffer[lineId].m_stops != 0
                ? Singleton<NetManager>.instance.m_nodes.m_buffer[
                    Singleton<TransportManager>.instance.m_lines.m_buffer[lineId].m_stops].m_position
                : ToolsModifierControl.cameraController.transform.position;
            WorldInfoPanel.HideAllWorldInfoPanels();
            WorldInfoPanel.Show<PublicTransportWorldInfoPanel>(pos, new InstanceID { TransportLine = lineId });
        }

        private static void OnToggleLineUnbunching()
        {
            var lineId = SelectedLine();
            if (lineId == 0)
            {
                return;
            }

            var state = Data.CachedTransportLineData.GetUnbunchingState(lineId);
            Data.CachedTransportLineData.SetUnbunchingState(lineId, !state);
        }

        private static void OnCopyLineConfig()
        {
            var lineId = SelectedLine();
            if (lineId == 0)
            {
                return;
            }

            UI.CopyPaste.Instance.Copy(lineId);
        }

        private static void OnPasteLineConfig()
        {
            var lineId = SelectedLine();
            if (lineId == 0 || !UI.CopyPaste.Instance.HasData)
            {
                return;
            }

            UI.CopyPaste.Instance.Paste(lineId);
        }

        private static void OnCopyToBuildings()
        {
            var lineId = SelectedLine();
            if (lineId == 0)
            {
                return;
            }

            UI.CopyPaste.Instance.CopyToServedBuildings(lineId);
        }

        private static void OnCopyToDistricts()
        {
            var lineId = SelectedLine();
            if (lineId == 0)
            {
                return;
            }

            UI.CopyPaste.Instance.CopyToServedDistricts(lineId);
        }

        private static void OnSelectVehicleTypes()
        {
            var lineId = SelectedLine();
            if (lineId == 0)
            {
                return;
            }

            UI.PrefabPanelManager.SetTarget(lineId);
        }

        private static void OnToggleVehicleEditor()
        {
            var s = ModSetting.Instance;
            s.HideVehicleEditor = !s.HideVehicleEditor;
            s.EnableVehicleEditor = !s.HideVehicleEditor;
            if (VehicleEditor.Instance != null)
            {
                VehicleEditor.Instance.isVisible = !s.HideVehicleEditor;
            }
        }

        private static void OnOpenFlightTracker()
        {
            if (!ImprovedPublicTransportMod.InGame || !ModSetting.Instance.EnableFlightTracker)
            {
                return;
            }

            var buildingId = WorldInfoPanel.GetCurrentInstanceID().Building;
            if (buildingId != 0)
            {
                TrackerPanelManager.SetTarget(buildingId);
            }
        }

        private static void OnPreviousVehicle()
        {
            ChangeSelectedVehicle(-1);
        }

        private static void OnNextVehicle()
        {
            ChangeSelectedVehicle(1);
        }

        private static void ChangeSelectedVehicle(int direction)
        {
            var current = WorldInfoPanel.GetCurrentInstanceID();
            if (current.Type != InstanceType.Vehicle || current.Vehicle == 0)
            {
                return;
            }

            var lineId = SelectedLine();
            if (lineId == 0)
            {
                return;
            }

            var first = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[current.Vehicle].GetFirstVehicle(current.Vehicle);
            var target = direction < 0
                ? Util.TransportLineUtil.GetPreviousVehicle(lineId, first)
                : Util.TransportLineUtil.GetNextVehicle(lineId, first);
            if (target == 0 || target == first)
            {
                return;
            }

            var instanceId = new InstanceID { Vehicle = target };
            WorldInfoPanel.ChangeInstanceID(current, instanceId);
        }

        // ---------- Persistence ----------

        // Each key combination is persisted as "keycodeInt|modifierInt" (0|0 = unbound).
        private static string Serialize(KeyCombination c)
            => $"{(int)c.Key}|{(int)c.Modifiers}";

        private static KeyCombination Deserialize(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return KeyCombination.Unbound;
            }

            var parts = s.Split('|');
            if (parts.Length != 2
                || !int.TryParse(parts[0], out int key)
                || !int.TryParse(parts[1], out int mods))
            {
                return KeyCombination.Unbound;
            }

            return new KeyCombination((KeyCode)key, (ModifierFlags)mods);
        }

        private static void ApplySaved(KeyBinding binding, string saved)
        {
            // Empty string = default (never customised). Apply both real bindings AND Unbound
            // ("0|0"), so a deliberately unbound hotkey stays unbound across restarts.
            if (string.IsNullOrEmpty(saved))
            {
                return;
            }

            binding.Combination = Deserialize(saved);
        }

        private static void LoadSavedBindings()
        {
            var s = ModSetting.Instance;
            ApplySaved(TrainDisplayToggle, s.HotkeyTrainDisplay);
            ApplySaved(LineColorRefresh, s.HotkeyRefreshLineColor);
            ApplySaved(AdvancedStopSelectionAlternateKey, s.HotkeyAdvancedStopAlternate);
            ApplySaved(LinePanelToggle, s.HotkeyOpenLinePanel);
            ApplySaved(LineUnbunchingToggle, s.HotkeyToggleLineUnbunching);
            ApplySaved(LineConfigCopy, s.HotkeyCopyLineConfig);
            ApplySaved(LineConfigPaste, s.HotkeyPasteLineConfig);
            ApplySaved(CopyToServedBuildings, s.HotkeyCopyToBuildings);
            ApplySaved(CopyToServedDistricts, s.HotkeyCopyToDistricts);
            ApplySaved(SelectVehicleTypesKey, s.HotkeySelectVehicleTypes);
            ApplySaved(VehicleEditorToggle, s.HotkeyToggleVehicleEditor);
            ApplySaved(FlightTrackerToggle, s.HotkeyOpenFlightTracker);
            ApplySaved(PreviousVehicle, s.HotkeyPrevVehicle);
            ApplySaved(NextVehicleBinding, s.HotkeyNextVehicle);
        }

        private static void SaveBindings()
        {
            try
            {
                var s = ModSetting.Instance;
                s.HotkeyTrainDisplay = Serialize(TrainDisplayToggle.Combination);
                s.HotkeyRefreshLineColor = Serialize(LineColorRefresh.Combination);
                s.HotkeyAdvancedStopAlternate = Serialize(AdvancedStopSelectionAlternateKey.Combination);
                s.HotkeyOpenLinePanel = Serialize(LinePanelToggle.Combination);
                s.HotkeyToggleLineUnbunching = Serialize(LineUnbunchingToggle.Combination);
                s.HotkeyCopyLineConfig = Serialize(LineConfigCopy.Combination);
                s.HotkeyPasteLineConfig = Serialize(LineConfigPaste.Combination);
                s.HotkeyCopyToBuildings = Serialize(CopyToServedBuildings.Combination);
                s.HotkeyCopyToDistricts = Serialize(CopyToServedDistricts.Combination);
                s.HotkeySelectVehicleTypes = Serialize(SelectVehicleTypesKey.Combination);
                s.HotkeyToggleVehicleEditor = Serialize(VehicleEditorToggle.Combination);
                s.HotkeyOpenFlightTracker = Serialize(FlightTrackerToggle.Combination);
                s.HotkeyPrevVehicle = Serialize(PreviousVehicle.Combination);
                s.HotkeyNextVehicle = Serialize(NextVehicleBinding.Combination);
                SettingsActions.SaveSettings();
            }
            catch
            {
                // non-fatal
            }
        }
    }
}
