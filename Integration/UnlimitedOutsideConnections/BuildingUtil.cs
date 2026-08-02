using System.Collections.Generic;
using ColossalFramework;

namespace UnlimitedOutsideConnections
{
    internal static class BuildingUtil
    {
        /// <summary>
        /// Returns every public-transport service building whose sub-service matches the given
        /// outside connection (intercity bus routes match to roads instead, since there's no direct
        /// sub-service equivalent).
        /// </summary>
        internal static IEnumerable<ushort> FindServiceBuildings(ushort connectionBuildingID)
        {
            var buildingList = new List<ushort>();
            if (connectionBuildingID == 0)
            {
                return buildingList;
            }

            var buildingManager = Singleton<BuildingManager>.instance;
            var buildingBuffer = buildingManager.m_buildings.m_buffer;
            var connectionInfo = buildingBuffer[connectionBuildingID].Info;
            if (!(connectionInfo?.GetAI() is OutsideConnectionAI))
            {
                return buildingList;
            }
            // Intercity bus outside connections report Service.Road (no direct PublicTransport
            // sub-service equivalent) instead of Service.PublicTransport - both must be allowed
            // through here, or the Road-matching branch below can never be reached.
            var connectionService = connectionInfo.GetService();
            if (connectionService != ItemClass.Service.PublicTransport && connectionService != ItemClass.Service.Road)
            {
                return buildingList;
            }

            var serviceBuildings = buildingManager.GetServiceBuildings(ItemClass.Service.PublicTransport);
            if (serviceBuildings == null || serviceBuildings.m_size == 0)
            {
                return buildingList;
            }

            var subService = connectionInfo.GetSubService();
            foreach (var buildingID in serviceBuildings)
            {
                var serviceInfo = buildingBuffer[buildingID].Info;
                if (buildingBuffer[buildingID].m_flags != Building.Flags.None && serviceInfo != null &&
                    ((connectionService == ItemClass.Service.Road && serviceInfo.GetSubService() == ItemClass.SubService.PublicTransportBus) ||
                     serviceInfo.GetSubService() == subService))
                {
                    buildingList.Add(buildingID);
                }
            }

            return buildingList;
        }

        /// <summary>Releases any vehicle currently travelling to or from the given (now-demolished) building.</summary>
        internal static void ReleaseTargetedVehicles(ushort buildingID)
        {
            if (buildingID == 0 || BuildingManager.instance.m_buildings.m_buffer[buildingID].Info?.m_buildingAI == null)
            {
                return;
            }

            var vehicleManager = Singleton<VehicleManager>.instance;
            var vehicles = vehicleManager.m_vehicles.m_buffer;
            // int, not ushort: with an expanded vehicle buffer (this project already supports up to
            // 65536, see ImprovedPublicTransportMod.DetermineMaxVehicleCount) a ushort loop variable
            // wraps 65535->0 forever instead of exiting, hanging the game on every demolition.
            for (int i = 1; i < vehicles.Length; ++i)
            {
                if (vehicles[i].m_sourceBuilding == buildingID || vehicles[i].m_targetBuilding == buildingID)
                {
                    vehicleManager.ReleaseVehicle((ushort)i);
                }
            }
        }

        /// <summary>
        /// Clears the target of vehicles owned by the given building, forcing a recalculation (and
        /// possible despawn) instead of releasing them outright, so dummy/decorative traffic is
        /// handled correctly.
        /// </summary>
        internal static void ReleaseOwnVehicles(ushort buildingID)
        {
            if (buildingID == 0)
            {
                return;
            }

            var buildings = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
            var buildingInfo = buildings[buildingID].Info;
            if (buildingInfo?.m_buildingAI == null)
            {
                return;
            }

            var vehicles = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
            var vehicleID = buildings[buildingID].m_ownVehicles;
            var guard = 0;
            while (vehicleID != 0 && guard++ < 65536)
            {
                if (vehicles[vehicleID].m_transportLine == 0)
                {
                    var vehicleInfo = vehicles[vehicleID].Info;
                    if (vehicleInfo != null && vehicleInfo.m_class.m_service == buildingInfo.m_class.m_service && vehicleInfo.m_class.m_subService == buildingInfo.m_class.m_subService)
                    {
                        vehicleInfo.m_vehicleAI.SetTarget(vehicleID, ref vehicles[vehicleID], 0);
                    }
                }

                vehicleID = vehicles[vehicleID].m_nextOwnVehicle;
            }
        }
    }
}
