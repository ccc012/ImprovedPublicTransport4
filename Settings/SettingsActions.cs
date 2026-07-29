using System;
using System.Linq;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using ImprovedPublicTransport.Data;
using RealisticWalkingSpeed;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.Settings
{
    // Deliberately NOT part of ModSetting: this is a one-shot "pick types, click Delete" tool, not a
    // persistent preference - it must never be saved to disk or survive past the delete action itself.
    public static class DeleteLinesSelection
    {
        public static bool BusLines;
        public static bool SightseeingBusLines;
        public static bool TramLines;
        public static bool TrolleybusLines;
        public static bool TrainLines;
        public static bool MetroLines;
        public static bool MonorailLines;
        public static bool ShipLines;
        public static bool HelicopterLines;
        public static bool BlimpLines;

        public static bool Any() => BusLines || SightseeingBusLines || TramLines || TrolleybusLines
            || TrainLines || MetroLines || MonorailLines || ShipLines || HelicopterLines || BlimpLines;

        public static void Clear()
        {
            BusLines = SightseeingBusLines = TramLines = TrolleybusLines = TrainLines
                = MetroLines = MonorailLines = ShipLines = HelicopterLines = BlimpLines = false;
        }
    }

    public static class SettingsActions
    {
        // Reference to vehicle count slider for enabling/disabling when budget control state changes.
        // Its normal/disabled colors are already configured by Slider.SetBlueStyle() (see
        // CSLModsCommonOptionsPanel's AddSlider call) - toggling isEnabled alone re-renders it correctly.
        public static CSLModsCommon.UI.Sliders.Slider VehicleCountSlider { get; set; }

        public static void OnBudgetModeChanged(int mode)
        {
            var isBudgetOn = (mode == (int)ModSetting.BudgetControlModes.Enabled);

            if (VehicleCountSlider != null)
            {
                VehicleCountSlider.isEnabled = !isBudgetOn;
            }

            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }
            
            SimulationManager.instance.AddAction(() =>
            {
                var instance = Singleton<TransportManager>.instance;
                if (instance == null)
                {
                    Utils.LogWarning("SettingsActions: OnBudgetModeChanged called before TransportManager is available.");
                    return;
                }

                int length = instance.m_lines.m_buffer.Length;
                for (int index = 0; index < length; ++index)
                {
                    CachedTransportLineData.SetBudgetControlState((ushort) index, isBudgetOn);
                    if (isBudgetOn)
                        CachedTransportLineData.ClearEnqueuedVehicles((ushort) index);
                }
            });
        }

        public static void OnTicketPriceCustomizerChanged(int mode)
        {
            bool enabled = mode == (int)ModSetting.TicketPriceCustomizerModes.Enabled;

            // Update UI tab immediately on main thread (the dropdown callback runs on UI thread)
            ImprovedPublicTransport.Integration.TicketPriceCustomizer.TicketPricesTab.UpdateTabState();

            // Update day/night watcher immediately, on UI thread too (safe for component operations)
            if (ImprovedPublicTransportMod.IptGameObject != null)
            {
                var watcher = ImprovedPublicTransportMod.IptGameObject.GetComponent<ImprovedPublicTransport.Integration.TicketPriceCustomizer.DayNightPriceWatcher>();
                if (enabled)
                {
                    if (watcher == null)
                    {
                        ImprovedPublicTransportMod.IptGameObject.AddComponent<ImprovedPublicTransport.Integration.TicketPriceCustomizer.DayNightPriceWatcher>();
                    }
                }
                else
                {
                    if (watcher != null)
                    {
                        UnityEngine.Object.Destroy(watcher);
                    }
                }
            }

            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }

            // Apply ticket multipliers in simulation thread (game data manipulation)
            SimulationManager.instance.AddAction(() =>
            {
                try
                {
                    if (enabled)
                    {
                        ImprovedPublicTransport.Integration.TicketPriceCustomizer.PriceCustomization.SetPrices(ModSetting.Instance.TicketPriceCustomizer);
                        if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs) Utils.Log("SettingsActions: TicketPriceCustomizer enabled.");
                    }
                    else
                    {
                        // Revert to vanilla prices when disabling
                        ImprovedPublicTransport.Integration.TicketPriceCustomizer.PriceCustomization.ResetToVanilla();
                        if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs) Utils.Log("SettingsActions: TicketPriceCustomizer disabled and prices reset to vanilla.");
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"SettingsActions: OnTicketPriceCustomizerChanged failed: {ex.Message}");
                }
            });

        }

        public static void OnPublicTransportUnstuckerChanged(int value)
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }

            SimulationManager.instance.AddAction(() =>
            {
                if (value != 0)
                {
                    if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs) Utils.Log("SettingsActions: Enabling PublicTransportUnstucker");
                    PublicTransportUnstucker.PublicTransportUnstuckerIntegration.Activate();
                }
                else
                {
                    if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs) Utils.Log("SettingsActions: Disabling PublicTransportUnstucker");
                    PublicTransportUnstucker.PublicTransportUnstuckerIntegration.Deactivate();
                }
            });
        }

        public static void OnRealisticWalkingSpeedChanged(int walkingSpeedMode)
        {
            if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs) Utils.Log($"SettingsActions: OnRealisticWalkingSpeedChanged called with mode {walkingSpeedMode}");
            
            if (!ImprovedPublicTransportMod.InGame)
            {
                if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs) Utils.Log("SettingsActions: Not in-game, changes will be applied when game loads");
                return;
            }
            
            SimulationManager.instance.AddAction(() =>
            {
                try
                {
                    if (walkingSpeedMode == (int)ModSetting.WalkingSpeedModes.Realistic)
                    {
                        if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs) Utils.Log("SettingsActions: Enabling Realistic Walking Speed");
                        RealisticWalkingSpeedMod.EnableRealisticWalkingSpeedMod();
                    }
                    else
                    {
                        if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs) Utils.Log("SettingsActions: Disabling Realistic Walking Speed");
                        RealisticWalkingSpeedMod.DisableRealisticWalkingSpeedMod();
                    }
                }
                catch (System.Exception ex)
                {
                    Utils.LogError($"Failed to toggle Realistic Walking Speed: {ex.Message}\n{ex.StackTrace}");
                }
            });
        }

        public static void OnDefaultVehicleCountSubmitted(int count)
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }
            SimulationManager.instance.AddAction(() =>
            {
                TransportManager instance = Singleton<TransportManager>.instance;
                int length = instance.m_lines.m_buffer.Length;
                for (int index = 0; index < length; ++index)
                {
                    if (!instance.m_lines.m_buffer[index].Complete)
                        CachedTransportLineData.SetTargetVehicleCount((ushort) index, count);
                }
            });
        }


        public static void OnResetButtonClick()
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }
            SimulationManager.instance.AddAction(() =>
            {
                // Reset options to their defaults
                var options = ModSetting.Instance;
                options.IntervalAggressionFactor = 52;
                options.DefaultVehicleCount = 0;
                options.SpawnTimeInterval = 10;
                CSLModsCommon.Manager.Domain.DefaultDomain.GetOrCreateManager<CSLModsCommon.Manager.SettingManager>().SaveSettings();

                // Apply immediate effects to existing lines
                int length = Singleton<TransportManager>.instance.m_lines.m_buffer.Length;
                for (int index = 0; index < length; ++index)
                {
                    CachedTransportLineData.SetNextSpawnTime((ushort) index, 0.0f);
                    CachedTransportLineData.SetTargetVehicleCount((ushort) index, options.DefaultVehicleCount);
                }

                // Rebuild the Options panel so its sliders show the new values immediately,
                // instead of only picking them up the next time the panel is opened.
                CSLModsCommon.Manager.OptionsPanelManager.Refresh();


            });
        }


        public static void OnDeleteLinesClick()
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }
            if (!DeleteLinesSelection.Any())
            {
                return;
            }
            WorldInfoPanel.Hide<PublicTransportWorldInfoPanel>();
            ConfirmPanel.ShowModal(Localization.Get("SETTINGS_LINE_DELETION_TOOL_CONFIRM_TITLE"),
                Localization.Get("SETTINGS_LINE_DELETION_TOOL_CONFIRM_MSG"), (s, r) =>
                {
                    if (r != 1)
                        return;
                    Singleton<SimulationManager>.instance.AddAction(() =>
                    {
                        SimulationManager.instance.AddAction(DeleteLines);
                    });
                });
        }

        public static void SaveSettings()
        {
            CSLModsCommon.Manager.Domain.DefaultDomain.GetOrCreateManager<CSLModsCommon.Manager.SettingManager>().SaveSettings();
        }

        public static void ApplyTicketPriceSettings()
        {
            SaveSettings();

            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }

            SimulationManager.instance.AddAction(() =>
            {
                try
                {
                    ImprovedPublicTransport.Integration.TicketPriceCustomizer.PriceCustomization.ApplyForCurrentTime(ModSetting.Instance.TicketPriceCustomizer);
                }
                catch (System.Exception ex)
                {
                    Utils.LogError($"SettingsActions: ApplyTicketPriceSettings failed: {ex.Message}");
                }
            });
        }

        public static void ResetTicketPriceSettings()
        {
            var defaults = new ModSetting.TicketPriceCustomizerSettings();
            var current = ModSetting.Instance.TicketPriceCustomizer;
            if (current == null)
            {
                current = ModSetting.Instance.TicketPriceCustomizer = new ModSetting.TicketPriceCustomizerSettings();
            }

            foreach (var prop in typeof(ModSetting.TicketPriceCustomizerSettings).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.CanWrite && prop.PropertyType == typeof(float))
                {
                    prop.SetValue(current, prop.GetValue(defaults, null), null);
                }
            }

            ApplyTicketPriceSettings();

            // Rebuild the Options panel so its sliders show the new values immediately,
            // instead of only picking them up the next time the panel is opened.
            CSLModsCommon.Manager.OptionsPanelManager.Refresh();
        }

        private static void DeleteLines()
        {
            TransportManager instance = Singleton<TransportManager>.instance;
            int length = instance.m_lines.m_buffer.Length;
            for (int index = 0; index < length; ++index)
            {
                TransportInfo info = instance.m_lines.m_buffer[index].Info;
                if (info == null || instance.m_lines.m_buffer[index].m_flags == TransportLine.Flags.None)
                {
                    continue;
                }
                bool flag = false;
                var subService = info.GetSubService();
                var service = info.GetService();
                var level = info.GetClassLevel();
                if (service == ItemClass.Service.PublicTransport) //TODO(): handle evacuation buses
                {
                    if (level == ItemClass.Level.Level1)
                    {
                        switch (subService)
                        {
                            case ItemClass.SubService.PublicTransportBus:
                                flag = DeleteLinesSelection.BusLines;
                                break;
                            case ItemClass.SubService.PublicTransportMetro:
                                flag = DeleteLinesSelection.MetroLines;
                                break;
                            case ItemClass.SubService.PublicTransportTrain:
                                flag = DeleteLinesSelection.TrainLines;
                                break;
                            case ItemClass.SubService.PublicTransportShip:
                                flag = DeleteLinesSelection.ShipLines;
                                break;
                            case ItemClass.SubService.PublicTransportPlane:
                                if (info.m_vehicleType == VehicleInfo.VehicleType.Helicopter)
                                    flag = DeleteLinesSelection.HelicopterLines;
                                else if (info.m_vehicleType == VehicleInfo.VehicleType.Blimp)
                                    flag = DeleteLinesSelection.BlimpLines;
                                break;
                            case ItemClass.SubService.PublicTransportTram:
                                flag = DeleteLinesSelection.TramLines;
                                break;
                            case ItemClass.SubService.PublicTransportMonorail:
                                flag = DeleteLinesSelection.MonorailLines;
                                break;
                            case ItemClass.SubService.PublicTransportTrolleybus:
                                flag = DeleteLinesSelection.TrolleybusLines;
                                break;
                        }
                    }
                    else if (level == ItemClass.Level.Level2)
                    {
                        switch (subService)
                        {
                            case ItemClass.SubService.PublicTransportBus:
                                flag = DeleteLinesSelection.BusLines;
                                break;
                            case ItemClass.SubService.PublicTransportShip:
                                flag = DeleteLinesSelection.ShipLines;
                                break;
                            case ItemClass.SubService.PublicTransportPlane:
                                if (info.m_vehicleType == VehicleInfo.VehicleType.Helicopter)
                                    flag = DeleteLinesSelection.HelicopterLines;
                                else if (info.m_vehicleType == VehicleInfo.VehicleType.Blimp)
                                    flag = DeleteLinesSelection.BlimpLines;
                                break;
                            case ItemClass.SubService.PublicTransportTrain:
                                flag = DeleteLinesSelection.TrainLines;
                                break;
                        }
                    }
                    else if (level == ItemClass.Level.Level3)
                    {
                        switch (subService)
                        {
                            case ItemClass.SubService.PublicTransportTours:
                                flag = DeleteLinesSelection.SightseeingBusLines;
                                break;
                            case ItemClass.SubService.PublicTransportPlane:
                                if (info.m_vehicleType == VehicleInfo.VehicleType.Helicopter)
                                    flag = DeleteLinesSelection.HelicopterLines;
                                else if (info.m_vehicleType == VehicleInfo.VehicleType.Blimp)
                                    flag = DeleteLinesSelection.BlimpLines;
                                break;
                        }
                    }
                    if (flag)
                    {
                        instance.ReleaseLine((ushort) index); //TODO(): make sure that outside connection lines don't get deleted
                    }
                }
            }

            DeleteLinesSelection.Clear();
            CSLModsCommon.Manager.OptionsPanelManager.Refresh();
        }
    }
}



