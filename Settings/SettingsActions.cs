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

        // One-click cascade: sets a batch of independent settings at once to match a named preset.
        // Safe/Vanilla leave everything off (max compatibility). Recommended turns on IPT core only.
        // Realistic enables most absorbed integrations. SharedStop and Commuter Destination stay
        // opt-in even on Realistic (higher risk / not part of the core cascade).
        public static void OnGameplayProfileChanged(ModSetting.GameplayProfiles profile)
        {
            var settings = ModSetting.Instance;
            if (settings.GameplayProfile == profile)
            {
                return;
            }

            settings.GameplayProfile = profile;

            switch (profile)
            {
                case ModSetting.GameplayProfiles.Safe:
                    ApplyAllIntegrationsOff(settings, alsoDisableUiConveniences: false);
                    break;

                case ModSetting.GameplayProfiles.Vanilla:
                    ApplyAllIntegrationsOff(settings, alsoDisableUiConveniences: true);
                    break;

                case ModSetting.GameplayProfiles.Recommended:
                    ApplyAllIntegrationsOff(settings);
                    // IPT original / low-conflict core only.
                    settings.BudgetControl = ModSetting.BudgetControlModes.Enabled;
                    settings.Unbunching = true;
                    settings.ShowLineInfo = true;
                    settings.EnableSubBuildingsTabs = true;
                    settings.EnableIntercityBusControl = true;
                    settings.EnablePublicTransportUnstucker = true;
                    settings.EnableAdvancedStopSelection = true;
                    settings.EnableElevatedStops = true;
                    break;

                case ModSetting.GameplayProfiles.Realistic:
                    ApplyAllIntegrationsOff(settings);
                    settings.WalkingSpeedMode = ModSetting.WalkingSpeedModes.Realistic;
                    settings.BbspLogic = ModSetting.BbspLogicModes.OriginalLogic;
                    settings.BudgetControl = ModSetting.BudgetControlModes.Enabled;
                    settings.Unbunching = true;
                    settings.ShowLineInfo = true;
                    settings.TicketPriceCustomizerMode = ModSetting.TicketPriceCustomizerModes.Enabled;
                    settings.AutoLineBudgetMode = ModSetting.AutoLineBudgetModes.Enabled;
                    settings.TrainDisplayMode = ModSetting.TrainDisplayModes.Enabled;
                    settings.TrainDisplayColorTheme = ModSetting.TrainDisplayColorThemes.Original;
                    settings.TrainDisplayOverlayScale = 1.0f;
                    // IntercityTerminalCapacityMode is legacy/save-compat only (not written on
                    // TransportStationAI) — leave default; real caps are depot modes below.
                    settings.TramDepotCapacityMode = ModSetting.DepotCapacityModes.Realistic;
                    settings.TaxiDepotCapacityMode = ModSetting.DepotCapacityModes.Realistic;
                    settings.BusDepotCapacityMode = ModSetting.DepotCapacityModes.Realistic;
                    settings.TrolleybusDepotCapacityMode = ModSetting.DepotCapacityModes.Realistic;
                    settings.FerryDepotCapacityMode = ModSetting.DepotCapacityModes.Realistic;
                    settings.EnableOptimisedOutsideConnections = true;
                    settings.EnablePublicTransportUnstucker = true;
                    settings.EnableIntercityBusControl = true;
                    settings.EnableFlightTracker = true;
                    settings.EnableSubBuildingsTabs = true;
                    settings.EnableTaxiStandFix = true;
                    // EnableStopStacker deliberately NOT set here - legacy/locked toggle, see
                    // FillAdvancedPage's grandfather note. Profiles never turn it back on.
                    settings.EnableSingleTrainTrackAI = true;
                    settings.EnableAdvancedStopSelection = true;
                    settings.EnableBetterBoarding = true;
                    settings.EnableMileageTaxi = true;
                    settings.EnableElevatedStops = true;
                    settings.ExpressBusUnbunchingMode = ModSetting.ExpressBusServicesModes.Prudential;
                    settings.ExpressTramUnbunchingMode = ModSetting.ExpressTramServicesModes.LightRail;
                    settings.AutoLineColorColorStrategy = ModSetting.AutoLineColorStrategy.RandomColor;
                    settings.AutoLineColorNamingStrategyMode = ModSetting.AutoLineColorNamingStrategy.Roads;
                    settings.EnableEmptyBeforeReturnToDepot = true;
                    // SharedStop / UnlimitedOutside / Commuter Destination stay opt-in (higher risk /
                    // prefab rewrites / not part of the core cascade).
                    break;

                case ModSetting.GameplayProfiles.Custom:
                default:
                    // No-op by design - player owns every toggle.
                    break;
            }

            SaveSettings();

            // Every dropdown/checkbox this touches needs to show its new value immediately, not just
            // the next time the Options panel happens to be reopened.
            CSLModsCommon.Manager.OptionsPanelManager.Refresh();

            // Live-apply whatever handlers exist so the cascade matches flipping each control by hand.
            OnBudgetModeChanged((int)settings.BudgetControl);
            OnDepotCapacityModeChanged();
            OnRealisticWalkingSpeedChanged((int)settings.WalkingSpeedMode);
            OnExpressBusSettingsChanged();
            if (profile != ModSetting.GameplayProfiles.Custom)
            {
                OnTicketPriceCustomizerChanged((int)settings.TicketPriceCustomizerMode);
                OnAutoLineBudgetModeChanged((int)settings.AutoLineBudgetMode);
                OnPublicTransportUnstuckerChanged(settings.EnablePublicTransportUnstucker ? 1 : 0);
                OnFlightTrackerChanged(settings.EnableFlightTracker);
                OnSubBuildingsTabsChanged(settings.EnableSubBuildingsTabs);
                OnTaxiStandFixChanged(settings.EnableTaxiStandFix);
                OnIntercityBusControlChanged(settings.EnableIntercityBusControl);
                OnSingleTrainTrackAIChanged(settings.EnableSingleTrainTrackAI);
                OnStopStackerChanged(settings.EnableStopStacker);
                OnElevatedStopsChanged(settings.EnableElevatedStops);

                // Profile cascades can flip several load-time-only integrations at once.
                if (ImprovedPublicTransportMod.InGame)
                {
                    NotifyReloadRequired("Gameplay profile integrations");
                }
            }
        }

        /// <summary>Baseline: every optional integration and mode off (Safe install / profile reset).
        /// When alsoDisableUiConveniences is false, UI-only features like ShowLineInfo remain enabled for convenience (Safe profile).
        /// When true, all non-essential features are disabled including UI conveniences (Vanilla profile / full reset).</summary>
        private static void ApplyAllIntegrationsOff(ModSetting settings, bool alsoDisableUiConveniences = true)
        {
            settings.WalkingSpeedMode = ModSetting.WalkingSpeedModes.Vanilla;
            settings.BbspLogic = ModSetting.BbspLogicModes.Disabled;
            settings.BudgetControl = ModSetting.BudgetControlModes.Disabled;
            settings.Unbunching = false;
            // ShowLineInfo is a UI convenience: show/hide transit line info in the interface.
            // Safe profile keeps it ON (helps compatibility, doesn't affect simulation).
            // Vanilla profile disables it (matches base game feel with no IPT extras).
            if (alsoDisableUiConveniences)
            {
                settings.ShowLineInfo = false;
            }
            else
            {
                settings.ShowLineInfo = true;
            }
            settings.TicketPriceCustomizerMode = ModSetting.TicketPriceCustomizerModes.Disabled;
            settings.AutoLineBudgetMode = ModSetting.AutoLineBudgetModes.Disabled;
            settings.TrainDisplayMode = ModSetting.TrainDisplayModes.Disabled;
            // Keep clearing the legacy field on full reset so saves do not rehydrate a value
            // that no longer does anything in Options / Realistic profile.
            settings.IntercityTerminalCapacityMode = ModSetting.DepotCapacityModes.Disabled;
            settings.TramDepotCapacityMode = ModSetting.DepotCapacityModes.Disabled;
            settings.TaxiDepotCapacityMode = ModSetting.DepotCapacityModes.Disabled;
            settings.BusDepotCapacityMode = ModSetting.DepotCapacityModes.Disabled;
            settings.TrolleybusDepotCapacityMode = ModSetting.DepotCapacityModes.Disabled;
            settings.FerryDepotCapacityMode = ModSetting.DepotCapacityModes.Disabled;
            settings.EnableOptimisedOutsideConnections = false;
            settings.EnableUnlimitedOutsideConnections = false;
            settings.EnablePublicTransportUnstucker = false;
            settings.EnableIntercityBusControl = false;
            settings.EnableFlightTracker = false;
            settings.EnableSubBuildingsTabs = false;
            settings.EnableTaxiStandFix = false;
            settings.EnableCommuterDestination = false;
            settings.EnableSharedStopEnabler = false;
            settings.EnableEmptyBeforeReturnToDepot = false;
            settings.TrainDisplayEnabledVehicleTypes = ModSetting.TrainDisplayVehicleTypes.All;
            settings.EnableSingleTrainTrackAI = false;
            settings.EnableStopStacker = false;
            settings.EnableAdvancedStopSelection = false;
            settings.EnableBetterBoarding = false;
            settings.EnableMileageTaxi = false;
            settings.EnableElevatedStops = false;
            settings.ExpressBusUnbunchingMode = ModSetting.ExpressBusServicesModes.None;
            settings.ExpressTramUnbunchingMode = ModSetting.ExpressTramServicesModes.Disabled;
            settings.AutoLineColorColorStrategy = ModSetting.AutoLineColorStrategy.Disabled;
            settings.AutoLineColorNamingStrategyMode = ModSetting.AutoLineColorNamingStrategy.Disabled;
            settings.DisableRoadDummyTraffic = false;
            settings.DisableTrainDummyTraffic = false;
            settings.DisablePlaneDummyTraffic = false;
            settings.DisableShipDummyTraffic = false;
        }

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
                        ImprovedPublicTransport.Integration.TicketPriceCustomizer.TicketPathCost.ApplyFromSettings();
                        if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs) Utils.Log("SettingsActions: TicketPriceCustomizer enabled.");
                    }
                    else
                    {
                        // Revert to vanilla prices when disabling
                        ImprovedPublicTransport.Integration.TicketPriceCustomizer.PriceCustomization.ResetToVanilla();
                        ImprovedPublicTransport.Integration.TicketPriceCustomizer.TicketPathCost.ClearAllStopLaneCosts();
                        if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs) Utils.Log("SettingsActions: TicketPriceCustomizer disabled and prices reset to vanilla.");
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"SettingsActions: OnTicketPriceCustomizerChanged failed: {ex.Message}");
                }
            });

        }

        public static void OnElevatedStopsChanged(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    ElevatedStopsEnabler.ElevatedStops.AddElevatedStoptypes();
                    ElevatedStopsEnabler.ElevatedStops.AllowStreetLightsOnElevatedStops();
                }
                else
                {
                    ElevatedStopsEnabler.ElevatedStops.Revert();
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"SettingsActions: failed to update ElevatedStopsEnabler: {ex.Message}");
            }
        }

        public static void OnAutoLineBudgetModeChanged(int mode)
        {
            bool enabled = mode == (int)ModSetting.AutoLineBudgetModes.Enabled;

            if (ImprovedPublicTransportMod.IptGameObject == null)
            {
                return;
            }

            var component = ImprovedPublicTransportMod.IptGameObject
                .GetComponent<Integration.AutoLineBudget.AutoLineBudgetIntegration>();
            if (enabled)
            {
                if (component == null)
                {
                    ImprovedPublicTransportMod.IptGameObject
                        .AddComponent<Integration.AutoLineBudget.AutoLineBudgetIntegration>();
                }
            }
            else
            {
                if (component != null)
                {
                    // OnDestroy releases every line it had taken over back to vanilla budget control.
                    UnityEngine.Object.Destroy(component);
                }
            }
        }

        public static void OnTicketPathfindingCostChanged(bool enabled)
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }

            SimulationManager.instance.AddAction(() =>
            {
                try
                {
                    if (enabled)
                    {
                        ImprovedPublicTransport.Integration.TicketPriceCustomizer.TicketPathCost.ApplyFromSettings();
                    }
                    else
                    {
                        ImprovedPublicTransport.Integration.TicketPriceCustomizer.TicketPathCost.ClearAllStopLaneCosts();
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"SettingsActions: OnTicketPathfindingCostChanged failed: {ex.Message}");
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

        // Dropdown mode alone only takes effect for prefabs patched from now on (InitializePrefab)
        // or at the next level load (PatchAllLoaded in OnLevelLoaded) - depots already sitting in
        // the world need this re-swept immediately or the change looks like it silently did nothing
        // until the player reloads the save.
        public static void OnDepotCapacityModeChanged()
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }

            SimulationManager.instance.AddAction(() =>
            {
                try
                {
                    ImprovedPublicTransport.HarmonyPatches.DepotAIPatches.DepotCapacityPatch.PatchAllLoaded();
                    // Intercity bus terminals use TransportStationAI + StationPatcher, not DepotCapacityPatch.
                    // Without this, changing "Intercity terminal capacity" in Options looked like a no-op
                    // until the next full level load.
                    if (ModSetting.Instance.EnableIntercityBusControl &&
                        IntercityBusControl.Mod.IsSunsetHarborInstalled())
                    {
                        IntercityBusControl.StationPatcher.PatchStations();
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"SettingsActions: OnDepotCapacityModeChanged failed: {ex.Message}");
                }
            });
        }

        public static void OnFlightTrackerChanged(bool enabled)
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }

            try
            {
                if (enabled)
                {
                    FlightTracker.Patcher.PatchAll();
                }
                else
                {
                    FlightTracker.TrackerPanelManager.Close();
                    FlightTracker.Patcher.UnpatchAll();
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"SettingsActions: failed to update FlightTracker: {ex.Message}");
            }
        }

        public static void OnSubBuildingsTabsChanged(bool enabled)
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }

            try
            {
                if (enabled)
                {
                    SubBuildingsTabs.PatchController.Activate();
                }
                else
                {
                    SubBuildingsTabs.PatchController.Deactivate();
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"SettingsActions: failed to update SubBuildingsTabs: {ex.Message}");
            }
        }

        public static void OnTaxiStandFixChanged(bool enabled)
        {
            if (!ImprovedPublicTransportMod.InGame || !SteamHelper.IsDLCOwned(SteamHelper.DLC.AfterDarkDLC))
            {
                return;
            }

            try
            {
                if (enabled)
                {
                    TaxiStandFix.PatchController.Activate();
                }
                else
                {
                    TaxiStandFix.PatchController.Deactivate();
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"SettingsActions: failed to update TaxiStandFix: {ex.Message}");
            }
        }

        public static void OnSharedStopEnablerChanged(bool enabled)
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }

            try
            {
                if (enabled)
                {
                    SharedStopEnabler.PatchController.Activate();
                }
                else
                {
                    SharedStopEnabler.PatchController.Deactivate();
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"SettingsActions: failed to update SharedStopEnabler: {ex.Message}");
            }
        }

        /// <summary>Live-apply Single Train Track AI Harmony patches (toggle mid-session).</summary>
        public static void OnSingleTrainTrackAIChanged(bool enabled)
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }

            try
            {
                if (enabled)
                {
                    SingleTrainTrackAI.PatchController.Activate();
                }
                else
                {
                    SingleTrainTrackAI.PatchController.Deactivate();
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"SettingsActions: failed to update SingleTrainTrackAI: {ex.Message}");
            }
        }

        /// <summary>Live-apply Stop Stacker Harmony patches (toggle mid-session).</summary>
        public static void OnStopStackerChanged(bool enabled)
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }

            try
            {
                if (enabled)
                {
                    StopStacker.PatchController.Activate();
                }
                else
                {
                    StopStacker.PatchController.Deactivate();
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"SettingsActions: failed to update StopStacker: {ex.Message}");
            }
        }

        /// <summary>
        /// Hot-apply Express Bus / Express Tram options: sync runtime config, then activate or
        /// deactivate Harmony patches depending on whether any express mode is still on.
        /// Safe when already active (mode/self-bal changes are live via EBSModConfig).
        /// </summary>
        public static void OnExpressBusSettingsChanged()
        {
            try
            {
                var settings = ModSetting.Instance;
                ExpressBusServices.EBSModConfig.SyncFromSettings(settings);

                if (!ImprovedPublicTransportMod.InGame)
                {
                    return;
                }

                if (ExpressBusServices.EBSModConfig.IsFullyDisabled(settings))
                {
                    ExpressBusServices.PatchController.Deactivate();
                    if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs)
                    {
                        Utils.Log("SettingsActions: Express Bus Services fully disabled; patches deactivated.");
                    }
                }
                else
                {
                    ExpressBusServices.PatchController.Activate();
                    if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs)
                    {
                        Utils.Log(
                            $"SettingsActions: Express Bus Services active " +
                            $"(bus={settings.ExpressBusUnbunchingMode}, tram={settings.ExpressTramUnbunchingMode}).");
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"SettingsActions: OnExpressBusSettingsChanged failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Mid-session toggle for Intercity Bus Control: patch/unpatch Harmony and (re)init
        /// per-building acceptance state. Requires Sunset Harbor when enabling.
        /// </summary>
        public static void OnIntercityBusControlChanged(bool enabled)
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }

            try
            {
                if (enabled)
                {
                    if (!IntercityBusControl.Mod.IsSunsetHarborInstalled())
                    {
                        Utils.LogWarning("SettingsActions: IntercityBusControl enable skipped - Sunset Harbor DLC not found.");
                        return;
                    }

                    IntercityBusControl.Patcher.PatchAll();
                    IntercityBusControl.StationPatcher.PatchStations();
                    IntercityBusControl.IntercityAcceptanceState.Init();
                    if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs)
                    {
                        Utils.Log("SettingsActions: IntercityBusControl enabled mid-session.");
                    }
                }
                else
                {
                    IntercityBusControl.Patcher.UnpatchAll();
                    // Safe: Deinit is idempotent and only clears sparse sets / event hooks.
                    IntercityBusControl.IntercityAcceptanceState.Deinit();
                    if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs)
                    {
                        Utils.Log("SettingsActions: IntercityBusControl disabled mid-session.");
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"SettingsActions: OnIntercityBusControlChanged failed: {ex.Message}");
            }
        }

        public static void OnCommuterDestinationChanged(bool enabled)
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }

            try
            {
                if (enabled)
                {
                    CommuterDestination.PatchController.Activate();
                }
                else
                {
                    // Deactivate() already clears the map overlay; there is no separate panel to
                    // close any more (upstream's floating destination list was dropped in favour of
                    // IPT4's own stop panel).
                    CommuterDestination.PatchController.Deactivate();
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"SettingsActions: failed to update CommuterDestination: {ex.Message}");
            }
        }

        // Features whose Harmony/prefab work only fully lands on level load. Log once per feature
        // name per session so mid-game option flips do not spam the log.
        private static readonly System.Collections.Generic.HashSet<string> ReloadNoticesLogged =
            new System.Collections.Generic.HashSet<string>();

        /// <summary>
        /// When a load-time-only integration is toggled mid-session, log once that a city reload
        /// is needed for the change to take full effect.
        /// </summary>
        public static void NotifyReloadRequired(string featureName)
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }

            if (string.IsNullOrEmpty(featureName))
            {
                featureName = "This setting";
            }

            if (!ReloadNoticesLogged.Add(featureName))
            {
                return;
            }

            Utils.Log($"{featureName}: Reload the city for this change to take full effect.");
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


        // Matches the literal default values declared on the properties in ModSetting.cs - kept as a
        // small explicit table rather than reflecting a fresh ModSetting instance, since ModSetting
        // is tied into the SettingManager/persistence framework and constructing a throwaway one
        // just to read its defaults is a heavier, riskier way to get the same 14 numbers.
        public static void OnResetStopsButtonClick()
        {
            var settings = ModSetting.Instance;
            settings.MaxWaitingPassengersBus = 50;
            settings.MaxWaitingPassengersTrolleybus = 50;
            settings.MaxWaitingPassengersEvacuationBus = 100;
            settings.MaxWaitingPassengersTouristBus = 50;
            settings.MaxWaitingPassengersTram = 80;
            settings.MaxWaitingPassengersMetro = 250;
            settings.MaxWaitingPassengersTrain = 250;
            settings.MaxWaitingPassengersMonorail = 250;
            settings.MaxWaitingPassengersShip = 150;
            settings.MaxWaitingPassengersAirplane = 250;
            settings.MaxWaitingPassengersCableCar = 40;
            settings.MaxWaitingPassengersHotAirBalloon = 40;
            settings.MaxWaitingPassengersHelicopter = 40;

            SaveSettings();

            // Rebuild the Options panel so the sliders show the reset values immediately, instead of
            // only picking them up the next time the panel is opened.
            CSLModsCommon.Manager.OptionsPanelManager.Refresh();
        }

        public static void OnResetButtonClick()
        {
            // Settings must reset even from the main-menu Options panel (before a city is loaded).
            var options = ModSetting.Instance;
            options.IntervalAggressionFactor = 52;
            options.DefaultVehicleCount = 0;
            options.SpawnTimeInterval = 10;
            SaveSettings();

            if (ImprovedPublicTransportMod.InGame)
            {
                SimulationManager.instance.AddAction(() =>
                {
                    int length = Singleton<TransportManager>.instance.m_lines.m_buffer.Length;
                    for (int index = 0; index < length; ++index)
                    {
                        CachedTransportLineData.SetNextSpawnTime((ushort)index, 0.0f);
                        CachedTransportLineData.SetTargetVehicleCount((ushort)index, options.DefaultVehicleCount);
                    }
                });
            }

            // Rebuild the Options panel so its sliders show the new values immediately.
            CSLModsCommon.Manager.OptionsPanelManager.Refresh();
        }

        /// <summary>Reset outside-connection related options to Safe defaults.</summary>
        public static void OnResetOutsideConnectionsClick()
        {
            var s = ModSetting.Instance;
            s.EnableOptimisedOutsideConnections = false;
            s.OutsideConnectionWaitMultiplier = 4;
            s.OutsideConnectionPassengerWaitScope = ModSetting.PassengerWaitScopes.OutsideConnectionsOnly;
            s.DisableRoadDummyTraffic = false;
            s.DisableTrainDummyTraffic = false;
            s.DisablePlaneDummyTraffic = false;
            s.DisableShipDummyTraffic = false;
            s.EnableUnlimitedOutsideConnections = false;
            SaveSettings();
            CSLModsCommon.Manager.OptionsPanelManager.Refresh();
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



