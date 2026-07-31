using ImprovedPublicTransport.Util;

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
            var count = (uint)PrefabCollection<BuildingInfo>.LoadedCount();
            for (uint i = 0; i < count; i++)
            {
                var info = PrefabCollection<BuildingInfo>.GetLoaded(i);
                if (info != null)
                {
                    Postfix(info);
                }
            }
        }

        private static void Postfix(BuildingInfo __instance)
        {
            // TransportStationAI is itself a DepotAI subclass (line-connected terminals, incl.
            // intercity bus) - IntercityBusControl already manages its capacity, so it is excluded
            // here rather than having two independent mechanisms both writing to the same field.
            if (__instance.m_buildingAI is not DepotAI depotAi || depotAi is TransportStationAI)
            {
                return;
            }

            var mode = depotAi.m_transportInfo?.m_transportType switch
            {
                TransportInfo.TransportType.Tram => (ModSetting.DepotCapacityModes?)ModSetting.Instance.TramDepotCapacityMode,
                TransportInfo.TransportType.Taxi => ModSetting.Instance.TaxiDepotCapacityMode,
                TransportInfo.TransportType.Bus => ModSetting.Instance.BusDepotCapacityMode,
                TransportInfo.TransportType.TouristBus => ModSetting.Instance.BusDepotCapacityMode,
                TransportInfo.TransportType.Trolleybus => ModSetting.Instance.TrolleybusDepotCapacityMode,
                TransportInfo.TransportType.Ship => ModSetting.Instance.FerryDepotCapacityMode,
                _ => null,
            };

            if (mode == null)
            {
                return;
            }

            var cap = GetCapacity(depotAi.m_transportInfo.m_transportType, mode.Value);
            depotAi.m_maxVehicleCount = cap;
            depotAi.m_maxVehicleCount2 = cap;
        }

        // Estimates, not sourced from any particular real-world figure: a tram depot realistically
        // holds fewer, larger vehicles than a taxi garage holds small ones, so the two get separate
        // (if similarly-reasoned) numbers rather than sharing one constant.
        private static int GetCapacity(TransportInfo.TransportType type, ModSetting.DepotCapacityModes mode)
        {
            if (mode == ModSetting.DepotCapacityModes.Disabled)
            {
                return 100000; // preserves the behaviour every existing save already has
            }

            return type switch
            {
                TransportInfo.TransportType.Tram => mode == ModSetting.DepotCapacityModes.Realistic ? 30 : 100,
                TransportInfo.TransportType.Taxi => mode == ModSetting.DepotCapacityModes.Realistic ? 60 : 250,
                TransportInfo.TransportType.Bus => mode == ModSetting.DepotCapacityModes.Realistic ? 50 : 200,
                TransportInfo.TransportType.TouristBus => mode == ModSetting.DepotCapacityModes.Realistic ? 50 : 200,
                TransportInfo.TransportType.Trolleybus => mode == ModSetting.DepotCapacityModes.Realistic ? 40 : 150,
                TransportInfo.TransportType.Ship => mode == ModSetting.DepotCapacityModes.Realistic ? 15 : 50,
                _ => mode == ModSetting.DepotCapacityModes.Realistic ? 40 : 200,
            };
        }
    }
}
