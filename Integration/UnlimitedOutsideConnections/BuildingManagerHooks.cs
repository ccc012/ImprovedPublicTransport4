using ColossalFramework;
using ImprovedPublicTransport;
using ImprovedPublicTransport.Util;

namespace UnlimitedOutsideConnections
{
    /// <summary>
    /// Vanilla only wires a transport station to an outside connection when the station is built (or
    /// its connection line reassigned) - a connection added afterwards (which the 4-connection cap
    /// removal in <see cref="Patch_GetInfo"/> now makes possible) is otherwise invisible to stations
    /// that already existed. These event hooks retroactively create/remove that wiring as
    /// connections are built or demolished.
    /// </summary>
    internal static class BuildingManagerHooks
    {
        internal static void Deploy()
        {
            Revert();
            BuildingManager.instance.EventBuildingCreated += OnBuildingCreated;
            BuildingManager.instance.EventBuildingReleased += OnBuildingReleased;
        }

        internal static void Revert()
        {
            BuildingManager.instance.EventBuildingCreated -= OnBuildingCreated;
            BuildingManager.instance.EventBuildingReleased -= OnBuildingReleased;
        }

        // Upstream's own OnBuildingCreated indexes buildingBuffer[buildingID] (the outside
        // connection's own building) throughout this loop instead of buildingBuffer[serviceID] (the
        // actual station found by FindServiceBuildings) - an outside connection is never a
        // TransportStationAI, so that check can never pass and CreateConnectionLines never actually
        // runs there. Indexed by serviceID here instead, which is what makes the retroactive wiring
        // this hook exists for actually happen.
        private static void OnBuildingCreated(ushort buildingID)
        {
            if (!ModSetting.Instance.EnableUnlimitedOutsideConnections)
            {
                return;
            }

            var instance = Singleton<BuildingManager>.instance;
            if (!(instance.m_buildings.m_buffer[buildingID].Info?.m_buildingAI is OutsideConnectionAI))
            {
                return;
            }

            var buildingBuffer = instance.m_buildings.m_buffer;
            foreach (var serviceID in BuildingUtil.FindServiceBuildings(buildingID))
            {
                if (!(buildingBuffer[serviceID].Info?.GetAI() is TransportStationAI stationAI))
                {
                    continue;
                }

                if (stationAI.m_transportLineInfo == null || (buildingBuffer[serviceID].m_flags & Building.Flags.Downgrading) != 0)
                {
                    continue;
                }

                buildingBuffer[serviceID].m_flags |= Building.Flags.IncomingOutgoing;
                Patch_TransportStationAI_CreateConnectionLines.CreateConnectionLines(stationAI, serviceID, ref buildingBuffer[serviceID]);
                BuildingUtil.ReleaseOwnVehicles(serviceID);
            }
        }

        private static void OnBuildingReleased(ushort buildingID)
        {
            if (!ModSetting.Instance.EnableUnlimitedOutsideConnections)
            {
                return;
            }

            var instance = Singleton<BuildingManager>.instance;
            if (!(instance.m_buildings.m_buffer[buildingID].Info?.m_buildingAI is OutsideConnectionAI))
            {
                return;
            }

            foreach (var serviceID in BuildingUtil.FindServiceBuildings(buildingID))
            {
                if (instance.m_buildings.m_buffer[serviceID].Info?.GetAI() is TransportStationAI stationAI && stationAI.m_transportLineInfo != null)
                {
                    BuildingUtil.ReleaseOwnVehicles(serviceID);
                }
            }

            BuildingUtil.ReleaseTargetedVehicles(buildingID);
        }
    }
}
