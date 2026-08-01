using System;
using ImprovedPublicTransport.Util;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.HarmonyPatches.DepotAIPatches
{
    /// <summary>
    /// Vanilla's own DepotAI base class defaults <c>m_maxVehicleCount</c>/<c>m_maxVehicleCount2</c> to
    /// 100,000 (an effectively uncapped garage). There is no dedicated TramDepotAI/TaxiDepotAI class -
    /// ordinary tram and taxi depots are plain DepotAI too, told apart only by
    /// <c>m_transportInfo.m_transportType</c> - so this is not something any absorbed mod introduced,
    /// it is the game's own default for every plain depot. Separate from
    /// <see cref="IntercityBusControl.StationPatcher"/>, which covers TransportStationAI (line-
    /// connected terminals, e.g. intercity bus) specifically and is excluded here to avoid the two
    /// mechanisms fighting over the same building.
    /// <para>
    /// Ferry / trolleybus / blimp / helicopter / hot-air-balloon depots are plain DepotAI as well.
    /// They are resolved by transport type, vehicle type, and (as a last resort) the building's
    /// ItemClass subservice/level, because some DLC prefabs leave <c>m_transportInfo</c> null or
    /// put the useful type on the secondary slot only.
    /// </para>
    /// </summary>
    public static class DepotCapacityPatch
    {
        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(BuildingInfo), nameof(BuildingInfo.InitializePrefab)),
                null,
                new PatchUtil.MethodDefinition(typeof(DepotCapacityPatch), nameof(Postfix))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(BuildingInfo), nameof(BuildingInfo.InitializePrefab))
            );
        }

        /// <summary>
        /// Retroactively applies the current capacity mode to every already-loaded depot prefab,
        /// the same "sweep everything once at level load" belt-and-suspenders IntercityBusControl's
        /// own StationPatcher.PatchStations() uses - InitializePrefab firing once per prefab covers
        /// the normal case, but this also catches prefabs that were already resident in memory
        /// (e.g. loaded once by an earlier session and never re-initialized) or a mode changed in
        /// Options after the level already loaded once.
        /// </summary>
        public static void PatchAllLoaded()
        {
            var setting = ModSetting.Instance;
            Utils.Log(
                "DepotCapacityPatch: PatchAllLoaded modes " +
                $"bus={setting.BusDepotCapacityMode} tram={setting.TramDepotCapacityMode} " +
                $"taxi={setting.TaxiDepotCapacityMode} trolley={setting.TrolleybusDepotCapacityMode} " +
                $"ferry={setting.FerryDepotCapacityMode}");

            var count = (uint)PrefabCollection<BuildingInfo>.LoadedCount();
            var patched = 0;
            for (uint i = 0; i < count; i++)
            {
                BuildingInfo info = null;
                try
                {
                    info = PrefabCollection<BuildingInfo>.GetLoaded(i);
                    if (info != null && ApplyToPrefab(info))
                    {
                        patched++;
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError(
                        $"DepotCapacityPatch: failed on prefab '{info?.name ?? i.ToString()}': {ex.Message}");
                }
            }

            // Always log summary so playtests can confirm ferry/trolley/blimp/heli were hit.
            Utils.Log($"DepotCapacityPatch: reapplied capacity modes to {patched} depot prefab(s).");
        }

        private static void Postfix(BuildingInfo __instance)
        {
            try
            {
                ApplyToPrefab(__instance);
            }
            catch (Exception ex)
            {
                Utils.LogError(
                    $"DepotCapacityPatch: InitializePrefab postfix failed on '{__instance?.name}': {ex.Message}");
            }
        }

        /// <returns>True if at least one capacity field was written.</returns>
        private static bool ApplyToPrefab(BuildingInfo info)
        {
            // Prefer m_buildingAI; fall back to GetAI() for prefabs that only materialise AI on demand.
            var ai = info?.m_buildingAI ?? info?.GetAI();
            // TransportStationAI is itself a DepotAI subclass (line-connected terminals, incl.
            // intercity bus) - IntercityBusControl already manages its capacity, so it is excluded
            // here rather than having two independent mechanisms both writing to the same field.
            if (ai is not DepotAI depotAi || depotAi is TransportStationAI)
            {
                return false;
            }

            var wrote = false;

            // Primary transport type (most plain depots only use this).
            if (TryResolve(depotAi.m_transportInfo, out var primaryType, out var primaryMode))
            {
                var cap = GetCapacity(primaryType, primaryMode);
                depotAi.m_maxVehicleCount = cap;
                wrote = true;
                LogPrefabDetail(
                    $"DepotCapacityPatch: '{info.name}' primary type={primaryType} " +
                    $"mode={primaryMode} -> m_maxVehicleCount={cap}");
            }

            // Dual-purpose depots (primary + secondary transport) were only writing the primary
            // slot - secondary stayed at vanilla 100000, so e.g. a combined garage looked "uncapped"
            // no matter what mode the player picked for the secondary type.
            if (depotAi.m_secondaryTransportInfo != null &&
                TryResolve(depotAi.m_secondaryTransportInfo, out var secondaryType, out var secondaryMode))
            {
                var cap2 = GetCapacity(secondaryType, secondaryMode);
                depotAi.m_maxVehicleCount2 = cap2;
                wrote = true;
                LogPrefabDetail(
                    $"DepotCapacityPatch: '{info.name}' secondary type={secondaryType} " +
                    $"mode={secondaryMode} -> m_maxVehicleCount2={cap2}");
            }
            else if (wrote)
            {
                // Match secondary to primary when the depot only exposes one type but still has a
                // second vehicle counter (vanilla often mirrors the same unlimited default on both).
                depotAi.m_maxVehicleCount2 = depotAi.m_maxVehicleCount;
            }
            else if (TryResolveFromBuildingClass(info, out var fallbackType, out var fallbackMode))
            {
                // Last resort: transport infos null/unrecognised, but the building class still
                // identifies a public-transport depot (ferry L2 ship, blimp L2 plane, heli L3 plane…).
                var cap = GetCapacity(fallbackType, fallbackMode);
                depotAi.m_maxVehicleCount = cap;
                depotAi.m_maxVehicleCount2 = cap;
                wrote = true;
                LogPrefabDetail(
                    $"DepotCapacityPatch: '{info.name}' building-class fallback type={fallbackType} " +
                    $"mode={fallbackMode} -> m_maxVehicleCount={cap}");
            }

            return wrote;
        }

        /// <summary>
        /// Per-prefab detail is Verbose-only; PatchAllLoaded already logs a single summary line.
        /// </summary>
        private static void LogPrefabDetail(string message)
        {
            if (Diagnostics.VerboseRuntimeLogs)
            {
                Utils.Log(message);
            }
        }

        /// <summary>
        /// Resolves capacity mode + logical transport type from a TransportInfo via transport type,
        /// vehicle type (Ferry/Blimp/Helicopter), or known TransportInfo prefab name.
        /// </summary>
        private static bool TryResolve(
            TransportInfo transportInfo,
            out TransportInfo.TransportType type,
            out ModSetting.DepotCapacityModes mode)
        {
            type = default;
            mode = ModSetting.DepotCapacityModes.Disabled;

            if (transportInfo == null)
            {
                return false;
            }

            type = transportInfo.m_transportType;

            // Prefer vehicle type for DLC modes that share a TransportType with something else
            // (Ferry shares Ship; Blimp shares Airplane).
            if (TryGetModeFromVehicleType(transportInfo.m_vehicleType, out mode))
            {
                type = MapVehicleTypeToTransportType(transportInfo.m_vehicleType, type);
                return true;
            }

            if (TryGetModeFromTransportType(type, out mode))
            {
                return true;
            }

            // Named prefabs ("Ferry", "Blimp", "Passenger Helicopter") as a final transport-info clue.
            return TryGetModeFromTransportName(transportInfo.name, out mode, out type);
        }

        private static bool TryGetModeFromVehicleType(
            VehicleInfo.VehicleType vehicleType,
            out ModSetting.DepotCapacityModes mode)
        {
            var setting = ModSetting.Instance;
            switch (vehicleType)
            {
                case VehicleInfo.VehicleType.Ferry:
                    mode = setting.FerryDepotCapacityMode;
                    return true;
                case VehicleInfo.VehicleType.Blimp:
                    // No dedicated blimp option - follow bus mode (same scale as other "shared" air).
                    mode = setting.BusDepotCapacityMode;
                    return true;
                case VehicleInfo.VehicleType.Helicopter:
                    mode = setting.BusDepotCapacityMode;
                    return true;
                default:
                    mode = ModSetting.DepotCapacityModes.Disabled;
                    return false;
            }
        }

        private static TransportInfo.TransportType MapVehicleTypeToTransportType(
            VehicleInfo.VehicleType vehicleType,
            TransportInfo.TransportType fallback)
        {
            return vehicleType switch
            {
                VehicleInfo.VehicleType.Ferry => TransportInfo.TransportType.Ship,
                // Blimps must not use Airplane caps (would allow ~8–20); hangars hold 1–3 ships.
                VehicleInfo.VehicleType.Blimp => TransportInfo.TransportType.HotAirBalloon,
                VehicleInfo.VehicleType.Helicopter => TransportInfo.TransportType.Helicopter,
                _ => fallback,
            };
        }

        private static bool TryGetModeFromTransportType(
            TransportInfo.TransportType transportType,
            out ModSetting.DepotCapacityModes mode)
        {
            var setting = ModSetting.Instance;
            switch (transportType)
            {
                case TransportInfo.TransportType.Tram:
                    mode = setting.TramDepotCapacityMode;
                    return true;
                case TransportInfo.TransportType.Taxi:
                    mode = setting.TaxiDepotCapacityMode;
                    return true;
                case TransportInfo.TransportType.Bus:
                case TransportInfo.TransportType.TouristBus:
                    mode = setting.BusDepotCapacityMode;
                    return true;
                case TransportInfo.TransportType.Trolleybus:
                    mode = setting.TrolleybusDepotCapacityMode;
                    return true;
                case TransportInfo.TransportType.Ship:
                    // Ferry depots use Ship transport type on DepotAI (TransportInfo name is often "Ferry").
                    mode = setting.FerryDepotCapacityMode;
                    return true;
                case TransportInfo.TransportType.Metro:
                case TransportInfo.TransportType.Train:
                case TransportInfo.TransportType.Monorail:
                case TransportInfo.TransportType.CableCar:
                case TransportInfo.TransportType.Airplane:
                    // Airplane covers passenger-plane hangars and many blimp prefabs.
                    mode = setting.BusDepotCapacityMode;
                    return true;
                case TransportInfo.TransportType.HotAirBalloon:
                    mode = setting.BusDepotCapacityMode;
                    return true;
                case TransportInfo.TransportType.Helicopter:
                    mode = setting.BusDepotCapacityMode;
                    return true;
                default:
                    mode = ModSetting.DepotCapacityModes.Disabled;
                    return false;
            }
        }

        private static bool TryGetModeFromTransportName(
            string name,
            out ModSetting.DepotCapacityModes mode,
            out TransportInfo.TransportType type)
        {
            mode = ModSetting.DepotCapacityModes.Disabled;
            type = default;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            var setting = ModSetting.Instance;
            // Exact / contains checks against vanilla TransportInfo prefab names.
            if (name.Equals("Ferry", StringComparison.OrdinalIgnoreCase) ||
                name.IndexOf("Ferry", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                mode = setting.FerryDepotCapacityMode;
                type = TransportInfo.TransportType.Ship;
                return true;
            }

            if (name.Equals("Blimp", StringComparison.OrdinalIgnoreCase) ||
                name.IndexOf("Blimp", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Blimp hangars are small (1–3 ships) — use HotAirBalloon table (2/4), not Airplane (8/20).
                mode = setting.BusDepotCapacityMode;
                type = TransportInfo.TransportType.HotAirBalloon;
                return true;
            }

            if (name.IndexOf("Helicopter", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                mode = setting.BusDepotCapacityMode;
                type = TransportInfo.TransportType.Helicopter;
                return true;
            }

            if (name.IndexOf("Trolley", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                mode = setting.TrolleybusDepotCapacityMode;
                type = TransportInfo.TransportType.Trolleybus;
                return true;
            }

            if (name.IndexOf("Hot Air", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("HotAir", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Balloon", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                mode = setting.BusDepotCapacityMode;
                type = TransportInfo.TransportType.HotAirBalloon;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Building ItemClass fallback used when both transport infos are missing or unrecognised.
        /// Mirrors the level/subservice layout VehiclePrefabs already uses for ferry/blimp/heli.
        /// </summary>
        private static bool TryResolveFromBuildingClass(
            BuildingInfo buildingInfo,
            out TransportInfo.TransportType type,
            out ModSetting.DepotCapacityModes mode)
        {
            type = default;
            mode = ModSetting.DepotCapacityModes.Disabled;

            var itemClass = buildingInfo?.m_class;
            if (itemClass == null || itemClass.m_service != ItemClass.Service.PublicTransport)
            {
                return false;
            }

            var setting = ModSetting.Instance;
            var sub = itemClass.m_subService;
            var level = itemClass.m_level;

            switch (sub)
            {
                case ItemClass.SubService.PublicTransportTrolleybus:
                    type = TransportInfo.TransportType.Trolleybus;
                    mode = setting.TrolleybusDepotCapacityMode;
                    return true;
                case ItemClass.SubService.PublicTransportShip:
                    // Level1 = passenger ship harbour area; Level2 = ferry depot.
                    type = TransportInfo.TransportType.Ship;
                    mode = setting.FerryDepotCapacityMode;
                    return true;
                case ItemClass.SubService.PublicTransportPlane when level == ItemClass.Level.Level2:
                    // Blimp hangar (Level2 plane class) — HotAirBalloon caps (2/4), not Airplane (8/20).
                    type = TransportInfo.TransportType.HotAirBalloon;
                    mode = setting.BusDepotCapacityMode;
                    return true;
                case ItemClass.SubService.PublicTransportPlane when level == ItemClass.Level.Level3:
                    // Helicopter depot.
                    type = TransportInfo.TransportType.Helicopter;
                    mode = setting.BusDepotCapacityMode;
                    return true;
                case ItemClass.SubService.PublicTransportPlane:
                    type = TransportInfo.TransportType.Airplane;
                    mode = setting.BusDepotCapacityMode;
                    return true;
                case ItemClass.SubService.PublicTransportBus:
                case ItemClass.SubService.PublicTransportTours:
                    type = TransportInfo.TransportType.Bus;
                    mode = setting.BusDepotCapacityMode;
                    return true;
                case ItemClass.SubService.PublicTransportTram:
                    type = TransportInfo.TransportType.Tram;
                    mode = setting.TramDepotCapacityMode;
                    return true;
                case ItemClass.SubService.PublicTransportTaxi:
                    type = TransportInfo.TransportType.Taxi;
                    mode = setting.TaxiDepotCapacityMode;
                    return true;
                case ItemClass.SubService.PublicTransportMetro:
                    type = TransportInfo.TransportType.Metro;
                    mode = setting.BusDepotCapacityMode;
                    return true;
                case ItemClass.SubService.PublicTransportTrain:
                    type = TransportInfo.TransportType.Train;
                    mode = setting.BusDepotCapacityMode;
                    return true;
                case ItemClass.SubService.PublicTransportMonorail:
                    type = TransportInfo.TransportType.Monorail;
                    mode = setting.BusDepotCapacityMode;
                    return true;
                case ItemClass.SubService.PublicTransportCableCar:
                    type = TransportInfo.TransportType.CableCar;
                    mode = setting.BusDepotCapacityMode;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Real-world-ish fleet sizes for a single depot/hangar (not a whole city fleet).
        /// Realistic = tight single-facility cap; Intermediate = large regional facility.
        /// Sources of scale: urban bus garages ~30–80; tram sheds ~15–40; ferry berths ~2–8;
        /// blimp hangars historically 1–3 ships; heliports ~4–10 pads; plane hangars ~4–12.
        /// </summary>
        private static int GetCapacity(TransportInfo.TransportType type, ModSetting.DepotCapacityModes mode)
        {
            if (mode == ModSetting.DepotCapacityModes.Disabled)
            {
                return 100000; // preserves the behaviour every existing save already has
            }

            var realistic = mode == ModSetting.DepotCapacityModes.Realistic;

            return type switch
            {
                TransportInfo.TransportType.Tram => realistic ? 25 : 80,
                TransportInfo.TransportType.Taxi => realistic ? 40 : 120,
                TransportInfo.TransportType.Bus => realistic ? 40 : 120,
                TransportInfo.TransportType.TouristBus => realistic ? 30 : 90,
                TransportInfo.TransportType.Trolleybus => realistic ? 30 : 90,
                // Ferry: one depot rarely berths more than a handful of vessels.
                TransportInfo.TransportType.Ship => realistic ? 4 : 10,
                TransportInfo.TransportType.Metro => realistic ? 24 : 60,
                TransportInfo.TransportType.Train => realistic ? 20 : 50,
                TransportInfo.TransportType.Monorail => realistic ? 20 : 50,
                TransportInfo.TransportType.CableCar => realistic ? 24 : 60,
                // Passenger plane hangar (not blimp — blimps map to HotAirBalloon below).
                TransportInfo.TransportType.Airplane => realistic ? 8 : 20,
                TransportInfo.TransportType.Helicopter => realistic ? 6 : 12,
                // Blimp hangar (VehicleType.Blimp is remapped to HotAirBalloon for capacity).
                TransportInfo.TransportType.HotAirBalloon => realistic ? 2 : 4,
                _ => realistic ? 30 : 100,
            };
        }
    }
}

