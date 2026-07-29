using System;
using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using ImprovedPublicTransport.HarmonyPatches.BuildingManagerPatches;
using ImprovedPublicTransport.HarmonyPatches.DepotAIPatches;
using ImprovedPublicTransport.HarmonyPatches.NetManagerPatches;
using ImprovedPublicTransport.HarmonyPatches.PublicTransportLineVehicleSelectorPatches;
using ImprovedPublicTransport.HarmonyPatches.TransportLinePatches;
using ImprovedPublicTransport.HarmonyPatches.TransportManagerPatches;
using ImprovedPublicTransport.HarmonyPatches.VehicleManagerPatches;
using ImprovedPublicTransport.HarmonyPatches.XYZVehicleAIPatches;
using ImprovedPublicTransport.HarmonyPatches.EconomyPanelPatches;
using ImprovedPublicTransport.RedirectionFramework;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.HarmonyPatches.PublicTransportVehicleButtonPatches;
using ImprovedPublicTransport.HarmonyPatches.PublicTransportWorldInfoPanelPatches;
using ImprovedPublicTransport.ReverseDetours;
using ImprovedPublicTransport.UI;
using ImprovedPublicTransport.UI.PanelExtenders;
using ImprovedPublicTransport.Integration.AdvancedStopSelection;
using ElevatedStopsEnabler;
// using SharedStopEnabler.Util;  // DISABLED: SharedStopsEnabler folder removed from Integration/
using MileageTaxiServices;
using RealisticWalkingSpeed;
using UnityEngine;
using Object = UnityEngine.Object;
using Utils = ImprovedPublicTransport.Util.Utils;
using AlgernonCommons.Patching;

namespace ImprovedPublicTransport
{
    // IUserMod (Name/Description/OnEnabled/OnSettingsUI) lives on Mod/IptModManager now
    // (CSLModsCommon's options UI). This class keeps only the LoadingExtensionBase half -
    // the game discovers both independently, they don't need to be the same class.
    public class ImprovedPublicTransportMod : LoadingExtensionBase
    {
        static ImprovedPublicTransportMod()
        {
            JsonNetBootstrap.EnsureLoaded();
        }
        public const string BaseModName = "Improved Public Transport 4 (local fork)";
        public const string ShortModName = "IPT";

        public static bool InGame;
        public static GameObject IptGameObject;
        private GameObject _worldInfoPanel;
        private const string Version = "4.0.0-dev";

        public override void OnCreated(ICities.ILoading loading)
        {
            JsonNetBootstrap.EnsureLoaded();
            base.OnCreated(loading);
            HarmonyHelper.EnsureHarmonyInstalled();
            // Ensure a sensible default HarmonyID for AlgernonCommons PatcherManager<TPatcher>
            // Some integrations (or other mods) may call PatcherManager<T>.Instance before
            // they set a specific HarmonyID; provide a safe default to avoid null-HarmonyID errors.
            try
            {
                AlgernonCommons.Patching.PatcherManager<AlgernonCommons.Patching.PatcherBase>.HarmonyID = "com.IPT";
            }
            catch { }

            // Initialise WhatsNew integration - sets ModBase.Instance for the AlgernonCommons notification system.
            new UI.AlgernonCommons.WhatsNewIntegration();
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            if (!HarmonyHelper.IsHarmonyInstalled)
            {
                return;
            }

            if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame && mode != LoadMode.NewGameFromScenario)
            {
                return;
            }

            InGame = true;
            try
            {
                Utils.Log($"{ShortModName}: Begin init version: {Version}");
                ReleaseUnusedCitizenUnits();
                UIView objectOfType = Object.FindObjectOfType<UIView>();
                if (objectOfType != null)
                {
                    IptGameObject = new GameObject("IptGameObject");
                    IptGameObject.transform.parent = objectOfType.transform;
                    IptGameObject.AddComponent<SimHelper>();
                    IptGameObject.AddComponent<LineWatcher>();
                    if (ModSetting.Instance.TicketPriceCustomizerMode == ModSetting.TicketPriceCustomizerModes.Enabled)
                    {
                        IptGameObject.AddComponent<Integration.TicketPriceCustomizer.DayNightPriceWatcher>();
                    }
                    else
                    {
                        if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("TicketPriceCustomizer: disabled by settings, skipping DayNightPriceWatcher.");
                    }

                    // Train Display - Updated: lightweight overlay for followed transport vehicles.
                    try
                    {
                        IptGameObject.AddComponent<Integration.TrainDisplayUpdated.TrainDisplayWatcher>();
                        if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("TrainDisplayUpdated: watcher attached.");
                    }
                    catch (Exception ex)
                    {
                        Utils.LogError($"TrainDisplayUpdated: failed to attach watcher: {ex.Message}");
                    }

                    // AutoLineBudget: automatic fleet sizing by passenger demand for lines in Budget mode
                    try
                    {
                        if (ModSetting.Instance.AutoLineBudgetMode == ModSetting.AutoLineBudgetModes.Enabled)
                        {
                            IptGameObject.AddComponent<Integration.AutoLineBudget.AutoLineBudgetIntegration>();
                            if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("AutoLineBudget: integration applied.");
                        }
                        else
                        {
                            if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("AutoLineBudget: disabled by settings, skipping integration.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Utils.LogError($"AutoLineBudget: failed to apply integration: {ex.Message}");
                    }

                    _worldInfoPanel = new GameObject("PublicTransportStopWorldInfoPanel");
                    _worldInfoPanel.transform.parent = objectOfType.transform;
                    _worldInfoPanel.AddComponent<PublicTransportStopWorldInfoPanel>();

                    CachedNodeData.Init();

                    int maxVehicleCount = DetermineMaxVehicleCount();
                    CachedVehicleData.Init(maxVehicleCount);

                    LoadPassengersPatch.Apply();
                    UnloadPassengersPatch.Apply();
                    StartTransferPatch.Apply();
                    ReleaseNodePatch.Apply();
                    ReleaseWaterSourcePatch.Apply();
                    GetVehicleInfoPatch.Apply();
                    CheckTransportLineVehiclesPatch.Apply();
                    ClassMatchesPatch.Apply();
                    CanLeavePatch.Apply();
                    // Rescue corrupt saves that contain fullwidth digits in transport line custom names.
                    NormalizeFullwidthLineNamesPatch.Apply();

                    // BetterBusStopPosition integration
                    try
                    {
                        Utils.Log("BetterBusStopPosition: Attempting to apply patches...");
                        BetterBusStopPosition.Patcher.PatchAll();
                        Utils.Log("BetterBusStopPosition: Patches applied successfully!");
                        if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("BetterBusStopPosition: integration applied.");
                    }
                    catch (Exception ex)
                    {
                        Utils.LogError($"BetterBusStopPosition: CRITICAL FAILURE - {ex.Message}\n{ex.StackTrace}");
                    }

                    Redirector<CommonBuildingAIReverseDetour>.Deploy();
                    HarmonyPatches.PublicTransportStopButtonPatches.OnMouseDownPatch.Apply();
                    HarmonyPatches.PublicTransportVehicleButtonPatches.OnMouseDownPatch.Apply();
                    RefreshVehicleButtonsPatch.Apply();
                    UpdateStopButtonsPatch.Apply();
                    BuildingExtension.Init();
                    LineWatcher.instance.Init();

                    CachedTransportLineData.Init();
                    Redirector<TransportLineReverseDetour>.Deploy();
                    SimulationStepPatch.Apply();
                    GetLineVehiclePatch.Apply();
                    CanLeaveStopPatch.Apply();

                    EconomyPanelAwakePatch.Apply();

                    VehiclePrefabs.Init();
                    SerializableDataExtension.instance.Loaded = true;
                    LocaleModifier.Init();

                    // Integration: enable elevated stops and street lights on elevated bridges
                    try
                    {
                        ElevatedStops.AddElevatedStoptypes();
                        ElevatedStops.AllowStreetLightsOnElevatedStops();
                        if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("ElevatedStopsEnabler: integration applied.");
                    }
                    catch (Exception ex)
                    {
                        Utils.LogError($"ElevatedStopsEnabler: failed to apply integration: {ex.Message}");
                    }

                    IptGameObject.AddComponent<VehicleEditor>();
                    IptGameObject.AddComponent<PanelExtenderLine>();
                    IptGameObject.AddComponent<PanelExtenderVehicle>();
                    IptGameObject.AddComponent<PanelExtenderCityService>();

                    // Stops and Stations: initialise integration and wire configuration into the threaded limiter
                    try
                    {
                        var sas = new StopsAndStations.StopsAndStationsIntegration();
                        sas.OnLevelLoaded(mode);
                        if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("StopsAndStations: integration loaded.");
                    }
                    catch (Exception ex)
                    {
                        Utils.LogError($"StopsAndStations: failed to initialise: {ex.Message}");
                    }

                    Utils.Log("Loading done!");
                    // Activate integration patches
                    ImprovedPublicTransport.Integration.AdvancedStopSelection.PatchController.Activate();

                    // SharedStopsEnabler (patches TransportTool to enable shared stops)
                    // DISABLED: Shared Stops Enabler integration folder removed from Integration/
                    // Uncomment to restore if re-adding the mod
                    /*
                    try
                    {
                        SharedStopEnabler.Util.Patcher.PatchAll();
                        if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("SharedStopsEnabler: integration applied.");
                    }
                    catch (Exception ex)
                    {
                        Utils.LogError($"SharedStopsEnabler: failed to apply integration: {ex.Message}");
                    }
                    */

                    // BetterBoarding integration (enhanced boarding decisions)
                    try
                    {
                        BetterBoarding.PatchController.Activate();
                        if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("BetterBoarding: integration applied.");
                    }
                    catch (Exception ex)
                    {
                        Utils.LogError($"BetterBoarding: failed to apply integration: {ex.Message}");
                    }

                    // MileageTaxiServices integration (generate fare income from taxi mileage)
                    // Requires After Dark DLC (taxis are an After Dark feature)
                    if (SteamHelper.IsDLCOwned(SteamHelper.DLC.AfterDarkDLC))
                    {
                        try
                        {
                            MileageTaxiServices.PatchController.Activate();
                            if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("MileageTaxiServices: integration applied.");
                        }
                        catch (Exception ex)
                        {
                            Utils.LogError($"MileageTaxiServices: failed to apply integration: {ex.Message}");
                        }
                    }
                    else
                    {
                        if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("MileageTaxiServices: After Dark DLC not detected, skipping integration.");
                    }

                    // RealisticWalkingSpeed integration
                    if (ModSetting.Instance.WalkingSpeedMode != ModSetting.WalkingSpeedModes.Vanilla)
                    {
                        try
                        {
                            RealisticWalkingSpeedMod.EnableRealisticWalkingSpeedMod();
                            RealisticWalkingSpeedMod.OnLevelLoaded(mode);
                            if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("RealisticWalkingSpeed: integration applied.");
                        }
                        catch (Exception ex)
                        {
                            Utils.LogError($"RealisticWalkingSpeed: failed to apply integration: {ex.Message}");
                        }
                    }
                    else
                    {
                        if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("RealisticWalkingSpeed: integration disabled (toggle is off).");
                    }

                    // PublicTransportUnstucker integration
                    try
                    {
                        if (ModSetting.Instance.EnablePublicTransportUnstucker)
                        {
                            PublicTransportUnstucker.PublicTransportUnstuckerIntegration.Activate();
                            if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("PublicTransportUnstucker: integration applied.");
                        }
                        else
                        {
                            if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("PublicTransportUnstucker: integration is disabled by settings.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Utils.LogError($"PublicTransportUnstucker: failed to apply integration: {ex.Message}");
                    }

                    // TicketPriceCustomizer: apply configured ticket multipliers on load (only if enabled)
                    try
                    {
                        if (ModSetting.Instance.TicketPriceCustomizerMode == ModSetting.TicketPriceCustomizerModes.Enabled)
                        {
                            ImprovedPublicTransport.Integration.TicketPriceCustomizer.PriceCustomization.SetPrices(ModSetting.Instance.TicketPriceCustomizer);
                            if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("TicketPriceCustomizer: Prices applied on load.");

                            // Ensure Ticket Prices tab exists (cover cases where EconomyPanel patching order/detection missed initial panel creation).
                            ImprovedPublicTransport.Integration.TicketPriceCustomizer.TicketPricesTab.UpdateTabState();
                        }
                        else
                        {
                            if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("TicketPriceCustomizer: disabled by settings, skipping initial price application.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Utils.LogError($"TicketPriceCustomizer: failed to apply prices on load: {ex.Message}");
                    }

                    // FlightTracker: FlightTracker manages its own patches (PatcherMod); IPT no longer forces patching here.
                    // This avoids redundant patching and Harmony warnings.

                    // IntercityBusControl integration (patches)
                    try
                    {
                        if (!ModSetting.Instance.EnableIntercityBusControl)
                        {
                            if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("IntercityBusControl: integration disabled (toggle is off).");
                        }
                        else if (!IntercityBusControl.Mod.IsSunsetHarborInstalled())
                        {
                            if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("IntercityBusControl: Sunset Harbor DLC not detected, skipping patches.");
                        }
                        else
                        {
                            IntercityBusControl.HarmonyPatches.BuildingInfoPatches.InitializePrefabPatch.Reset();
                            IntercityBusControl.Patcher.PatchAll();
                            // Prefabs are already loaded by the time OnLevelLoaded fires, so the
                            // InitializePrefab Harmony patches never ran for vanilla/workshop assets.
                            // PatchStations() handles those retroactively.
                            IntercityBusControl.StationPatcher.PatchStations();
                            if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("IntercityBusControl: integration applied.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Utils.LogError($"IntercityBusControl: failed to apply integration: {ex.Message}");
                    }

                    // FlightTracker integration (patches)
                    try
                    {
                        if (ModSetting.Instance.EnableFlightTracker)
                        {
                            FlightTracker.Patcher.PatchAll();
                            if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("FlightTracker: integration applied.");
                        }
                        else
                        {
                            if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("FlightTracker: integration disabled (toggle is off).");
                        }
                    }
                    catch (Exception ex)
                    {
                        Utils.LogError($"FlightTracker: failed to apply integration: {ex.Message}");
                    }

                    // Show What's New notification now that the in-game UI is fully initialized.
                    AlgernonCommons.Notifications.WhatsNew.ShowWhatsNew();
                }
                else
                    Utils.LogError("UIView not found, aborting!");
            }
            catch (Exception ex)
            {
                Utils.LogError(
                    $"{ShortModName}: Error during initialization, IPT disables itself.{Environment.NewLine}" +
                    $"Please try again without any other mod.{Environment.NewLine}" +
                    $"Please upload your log file and post the link here if that didn't help:{Environment.NewLine}" +
                    $"http://steamcommunity.com/workshop/filedetails/discussion/424106600/615086038663282271/{Environment.NewLine}" +
                    $"{ex.Message}{Environment.NewLine}" +
                    $"{ex.InnerException}{Environment.NewLine}" +
                    $"{ex.StackTrace}");
                Deinit();
            }
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            if (!HarmonyHelper.IsHarmonyInstalled)
            {
                return;
            }

            if (!InGame)
                return;
            InGame = false;
            // Reset ticket prices to defaults on unload (only if feature enabled)
            try
            {
                if (ModSetting.Instance.TicketPriceCustomizerMode == ModSetting.TicketPriceCustomizerModes.Enabled)
                {
                    ImprovedPublicTransport.Integration.TicketPriceCustomizer.PriceCustomization.SetPrices(new ModSetting.TicketPriceCustomizerSettings());
                    if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("TicketPriceCustomizer: Prices reset on unload.");
                }
                else
                {
                    if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("TicketPriceCustomizer: disabled by settings, skipping reset on unload.");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPriceCustomizer: Failed to reset prices on unload: {ex.Message}");
            }
            // Stops and Stations: unload
            try
            {
                StopsAndStations.StopsAndStationsIntegration.Instance?.OnLevelUnloading();
            }
            catch (Exception ex)
            {
                Utils.LogError($"StopsAndStations: failed to unload: {ex.Message}");
            }

            // PublicTransportUnstucker integration unload
            try
            {
                PublicTransportUnstucker.PublicTransportUnstuckerIntegration.Deactivate();
                if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("PublicTransportUnstucker: integration unloaded.");
            }
            catch (Exception ex)
            {
                Utils.LogError($"PublicTransportUnstucker: failed to unload integration: {ex.Message}");
            }

            Deinit();
            Utils.Log("Unloading done!" + Environment.NewLine);
        }

        /// <summary>
        /// Determines the maximum vehicle count by checking the actual VehicleManager state.
        /// This is more robust than checking for specific mod IDs, as it works regardless of
        /// which workshop ID a vehicle-expanding mod uses.
        /// </summary>
        private static int DetermineMaxVehicleCount()
        {
            try
            {
                // Check if VehicleManager is available and has been initialized
                if (VehicleManager.instance != null && VehicleManager.instance.m_vehicles != null)
                {
                    // Get the actual capacity of the vehicle array buffer in the game
                    int actualArraySize = VehicleManager.instance.m_vehicles.m_buffer.Length;

                    // Standard CS:L vehicle limit is 16384
                    // If the array is larger, it means a mod has expanded the vehicle limit
                    if (actualArraySize > VehicleManager.MAX_VEHICLE_COUNT)
                    {
                        Debug.LogWarning(
                            $"{ShortModName}: Detected expanded vehicle array size ({actualArraySize}). " +
                            $"This suggests a vehicle-expanding mod (like More Vehicles) is active. " +
                            $"Using expanded cache size.");
                        return actualArraySize;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{ShortModName}: Error checking vehicle manager state: {ex.Message}");
            }

            // Fallback: check for known vehicle-expanding mods by their workshop ID
            // More Vehicles (original workshop ID)
            if (Utils.IsModActive(1764208250))
            {
                Debug.LogWarning(
                    $"{ShortModName}: More Vehicles is enabled (ID: 1764208250), " +
                    $"applying compatibility workaround for expanded vehicle limits");
                return ushort.MaxValue + 1;
            }

            // More Vehicles (new workshop version)
            if (Utils.IsModActive(3684679695))
            {
                Debug.LogWarning(
                    $"{ShortModName}: More Vehicles is enabled (ID: 3684679695), " +
                    $"applying compatibility workaround for expanded vehicle limits");
                return ushort.MaxValue + 1;
            }

            // Default: use vanilla vehicle limit
            Debug.Log($"{ShortModName}: Using standard vehicle limit ({VehicleManager.MAX_VEHICLE_COUNT})");
            return VehicleManager.MAX_VEHICLE_COUNT;
        }

        private void ReleaseUnusedCitizenUnits()
        {
            Utils.Log("Find and clear unused citizen units.");
            CitizenManager instance = Singleton<CitizenManager>.instance;
            int num = 0;
            for (int index = 0; index < instance.m_units.m_buffer.Length; ++index)
            {
                CitizenUnit citizenUnit = instance.m_units.m_buffer[index];
                if (citizenUnit.m_flags != CitizenUnit.Flags.None && citizenUnit.m_building == 0 &&
                    (citizenUnit.m_vehicle == 0 && citizenUnit.m_goods == 0))
                {
                    ++num;
                    instance.m_units.m_buffer[index] = new CitizenUnit();
                    instance.m_units.ReleaseItem((uint)index);
                    Utils.LogToTxt(
                        $"CitizenUnit #{index} - Flags: {citizenUnit.m_flags} - Citizens: #{citizenUnit.m_citizen0} | #{citizenUnit.m_citizen1} | #{citizenUnit.m_citizen2} | #{citizenUnit.m_citizen3} | #{citizenUnit.m_citizen4}");
                }
            }

            Utils.Log("Cleared " + num + " unused citizen units.");
        }

        private void Deinit()
        {
            LoadPassengersPatch.Undo();
            UnloadPassengersPatch.Undo();
            StartTransferPatch.Undo();
            ReleaseNodePatch.Undo();
            ReleaseWaterSourcePatch.Undo();
            GetVehicleInfoPatch.Undo();
            ClassMatchesPatch.Undo();
            CheckTransportLineVehiclesPatch.Undo();
            NormalizeFullwidthLineNamesPatch.Undo();
            GetDepotLevelsPatch.Undo();
            CanLeavePatch.Undo();
            EconomyPanelAwakePatch.Undo();
            Integration.TicketPriceCustomizer.TicketPricesTab.Cleanup();

            Redirector<CommonBuildingAIReverseDetour>.Revert();
            HarmonyPatches.PublicTransportStopButtonPatches.OnMouseDownPatch.Undo();
            HarmonyPatches.PublicTransportVehicleButtonPatches.OnMouseDownPatch.Undo();
            RefreshVehicleButtonsPatch.Undo();
            UpdateStopButtonsPatch.Undo();

            Redirector<TransportLineReverseDetour>.Revert();
            SimulationStepPatch.Undo();
            GetLineVehiclePatch.Undo();
            CanLeaveStopPatch.Undo();
            CachedTransportLineData.Deinit();

            BuildingExtension.Deinit();
            CachedNodeData.Deinit();
            CachedVehicleData.Deinit();
            SerializableDataExtension.instance.Loaded = false;
            LocaleModifier.Deinit();

            if (IptGameObject != null)
                Object.Destroy(IptGameObject);
            if (_worldInfoPanel != null)
                Object.Destroy(_worldInfoPanel);

            // Deactivate integration patches
            ImprovedPublicTransport.Integration.AdvancedStopSelection.PatchController.Deactivate();

            // BetterBusStopPosition cleanup
            try
            {
                BetterBusStopPosition.Patcher.UnpatchAll();
                if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("BetterBusStopPosition: integration removed.");
            }
            catch (Exception ex)
            {
                Utils.LogError($"BetterBusStopPosition: failed to remove integration: {ex.Message}");
            }

            // SharedStopsEnabler cleanup
            // DISABLED: Shared Stops Enabler integration folder removed from Integration/
            // Uncomment to restore if re-adding the mod
            /*
            try
            {
                SharedStopEnabler.Util.Patcher.UnpatchAll();
                if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("SharedStopsEnabler: integration removed.");
            }
            catch (Exception ex)
            {
                Utils.LogError($"SharedStopsEnabler: failed to remove integration: {ex.Message}");
            }
            */

            // BetterBoarding cleanup
            try
            {
                BetterBoarding.PatchController.Deactivate();
                if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("BetterBoarding: integration removed.");
            }
            catch (Exception ex)
            {
                Utils.LogError($"BetterBoarding: failed to remove integration: {ex.Message}");
            }

            // MileageTaxiServices cleanup (only if After Dark DLC present)
            if (SteamHelper.IsDLCOwned(SteamHelper.DLC.AfterDarkDLC))
            {
                try
                {
                    MileageTaxiServices.PatchController.Deactivate();
                    if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("MileageTaxiServices: integration removed.");
                }
                catch (Exception ex)
                {
                    Utils.LogError($"MileageTaxiServices: failed to remove integration: {ex.Message}");
                }
            }

            // RealisticWalkingSpeed cleanup
            if (ModSetting.Instance.WalkingSpeedMode != ModSetting.WalkingSpeedModes.Vanilla)
            {
                try
                {
                    RealisticWalkingSpeedMod.DisableRealisticWalkingSpeedMod();
                    if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("RealisticWalkingSpeed: integration removed.");
                }
                catch (Exception ex)
                {
                    Utils.LogError($"RealisticWalkingSpeed: failed to remove integration: {ex.Message}");
                }
            }

            // IntercityBusControl cleanup (only if Sunset Harbor DLC present)
            if (IntercityBusControl.Mod.IsSunsetHarborInstalled())
            {
                try
                {
                    IntercityBusControl.Patcher.UnpatchAll();
                    IntercityBusControl.HarmonyPatches.BuildingInfoPatches.InitializePrefabPatch.Reset();
                    if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("IntercityBusControl: integration removed.");
                }
                catch (Exception ex)
                {
                    Utils.LogError($"IntercityBusControl: failed to remove integration: {ex.Message}");
                }
            }

            // FlightTracker cleanup
            try
            {
                FlightTracker.Patcher.UnpatchAll();
                if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("FlightTracker: integration removed.");
            }
            catch (Exception ex)
            {
                Utils.LogError($"FlightTracker: failed to remove integration: {ex.Message}");
            }

        }

    }
}
