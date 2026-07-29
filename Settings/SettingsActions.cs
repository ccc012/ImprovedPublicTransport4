using System;
using System.Linq;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using ImprovedPublicTransport.Data;
using RealisticWalkingSpeed;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.Settings
{
    public static class SettingsActions
    {
        // Reference to vehicle count slider for enabling/disabling when budget control state changes
        public static UISlider VehicleCountSlider { get; set; }

        public static void OnBudgetModeChanged(int mode)
        {
            var isBudgetOn = (mode == (int)ModSetting.BudgetControlModes.Enabled);
            
            // Update slider state immediately
            if (VehicleCountSlider != null)
            {
                var activeTrackColor = new Color32(100, 100, 100, 255);
                var inactiveTrackColor = new Color32(50, 50, 50, 255);
                var activeThumbColor = new Color32(255, 255, 255, 255);
                var inactiveThumbColor = new Color32(60, 60, 60, 255);

                // Set both normal and disabled colors because disabled rendering uses disabledColor.
                VehicleCountSlider.color = isBudgetOn ? inactiveTrackColor : activeTrackColor;
                VehicleCountSlider.disabledColor = inactiveTrackColor;

                if (VehicleCountSlider.thumbObject != null)
                {
                    VehicleCountSlider.thumbObject.color = isBudgetOn ? inactiveThumbColor : activeThumbColor;
                    VehicleCountSlider.thumbObject.disabledColor = inactiveThumbColor;
                }

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
                        Utils.Log("SettingsActions: TicketPriceCustomizer enabled.");
                    }
                    else
                    {
                        // Revert to vanilla prices when disabling
                        ImprovedPublicTransport.Integration.TicketPriceCustomizer.PriceCustomization.ResetToVanilla();
                        Utils.Log("SettingsActions: TicketPriceCustomizer disabled and prices reset to vanilla.");
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
                    Utils.Log("SettingsActions: Enabling PublicTransportUnstucker");
                    PublicTransportUnstucker.PublicTransportUnstuckerIntegration.Activate();
                }
                else
                {
                    Utils.Log("SettingsActions: Disabling PublicTransportUnstucker");
                    PublicTransportUnstucker.PublicTransportUnstuckerIntegration.Deactivate();
                }
            });
        }

        public static void OnRealisticWalkingSpeedChanged(int walkingSpeedMode)
        {
            Utils.Log($"SettingsActions: OnRealisticWalkingSpeedChanged called with mode {walkingSpeedMode}");
            
            if (!ImprovedPublicTransportMod.InGame)
            {
                Utils.Log("SettingsActions: Not in-game, changes will be applied when game loads");
                return;
            }
            
            SimulationManager.instance.AddAction(() =>
            {
                try
                {
                    if (walkingSpeedMode == (int)ModSetting.WalkingSpeedModes.Realistic)
                    {
                        Utils.Log("SettingsActions: Enabling Realistic Walking Speed");
                        RealisticWalkingSpeedMod.EnableRealisticWalkingSpeedMod();
                    }
                    else
                    {
                        Utils.Log("SettingsActions: Disabling Realistic Walking Speed");
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

                Localization.Get("Unbunching settings reset to defaults.");
            });
        }


        public static void OnDeleteLinesClick()
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }
            if (!ModSetting.Instance.DeleteBusLines &&
                !ModSetting.Instance.DeleteSightseeingBusLines &&
                !ModSetting.Instance.DeleteTramLines &&
                !ModSetting.Instance.DeleteTrolleybusLines &&
                !ModSetting.Instance.DeleteTrainLines &&
                !ModSetting.Instance.DeleteMetroLines &&
                !ModSetting.Instance.DeleteMonorailLines &&
                !ModSetting.Instance.DeleteShipLines &&
                !ModSetting.Instance.DeleteHelicopterLines &&
                !ModSetting.Instance.DeleteBlimpLines)
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
                                flag = ModSetting.Instance.DeleteBusLines;
                                break;
                            case ItemClass.SubService.PublicTransportMetro:
                                flag = ModSetting.Instance.DeleteMetroLines;
                                break;
                            case ItemClass.SubService.PublicTransportTrain:
                                flag = ModSetting.Instance.DeleteTrainLines;
                                break;
                            case ItemClass.SubService.PublicTransportShip:
                                flag = ModSetting.Instance.DeleteShipLines;
                                break;
                            case ItemClass.SubService.PublicTransportPlane:
                                if (info.m_vehicleType == VehicleInfo.VehicleType.Helicopter)
                                    flag = ModSetting.Instance.DeleteHelicopterLines;
                                else if (info.m_vehicleType == VehicleInfo.VehicleType.Blimp)
                                    flag = ModSetting.Instance.DeleteBlimpLines;
                                break;
                            case ItemClass.SubService.PublicTransportTram:
                                flag = ModSetting.Instance.DeleteTramLines;
                                break;
                            case ItemClass.SubService.PublicTransportMonorail:
                                flag = ModSetting.Instance.DeleteMonorailLines;
                                break;
                            case ItemClass.SubService.PublicTransportTrolleybus:
                                flag = ModSetting.Instance.DeleteTrolleybusLines;
                                break;
                        }
                    }
                    else if (level == ItemClass.Level.Level2)
                    {
                        switch (subService)
                        {
                            case ItemClass.SubService.PublicTransportBus:
                                flag = ModSetting.Instance.DeleteBusLines;
                                break;
                            case ItemClass.SubService.PublicTransportShip:
                                flag = ModSetting.Instance.DeleteShipLines;
                                break;
                            case ItemClass.SubService.PublicTransportPlane:
                                if (info.m_vehicleType == VehicleInfo.VehicleType.Helicopter)
                                    flag = ModSetting.Instance.DeleteHelicopterLines;
                                else if (info.m_vehicleType == VehicleInfo.VehicleType.Blimp)
                                    flag = ModSetting.Instance.DeleteBlimpLines;
                                break;
                            case ItemClass.SubService.PublicTransportTrain:
                                flag = ModSetting.Instance.DeleteTrainLines;
                                break;
                        }
                    }
                    else if (level == ItemClass.Level.Level3)
                    {
                        switch (subService)
                        {
                            case ItemClass.SubService.PublicTransportTours:
                                flag = ModSetting.Instance.DeleteSightseeingBusLines;
                                break;
                            case ItemClass.SubService.PublicTransportPlane:
                                if (info.m_vehicleType == VehicleInfo.VehicleType.Helicopter)
                                    flag = ModSetting.Instance.DeleteHelicopterLines;
                                else if (info.m_vehicleType == VehicleInfo.VehicleType.Blimp)
                                    flag = ModSetting.Instance.DeleteBlimpLines;
                                break;
                        }
                    }
                    if (flag)
                    {
                        instance.ReleaseLine((ushort) index); //TODO(): make sure that outside connection lines don't get deleted
                    }
                }
            }
        }
    }
}
