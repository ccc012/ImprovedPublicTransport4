using System;
using System.Linq;
using ImprovedPublicTransport.Util;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ElevatedStopsEnabler
{
    static class ElevatedStops
    {
        public static void AddElevatedStoptypes()
        {
            var networks = UnityEngine.Resources.FindObjectsOfTypeAll<NetInfo>();
            foreach (var network in networks)
            {
                if (network == null || !network.m_hasPedestrianLanes)
                    continue;

                if (network.m_netAI is not RoadAI ai)
                    continue;

                if (network.m_lanes == null || network.m_sortedLanes == null || network.m_sortedLanes.Length == 0)
                    continue;

                bool hasStops = network.m_lanes.Any(lane => lane != null && lane.m_stopType != VehicleInfo.VehicleType.None);
                if (!hasStops)
                    continue;

                VehicleInfo.VehicleType firstStopType = network.m_lanes[network.m_sortedLanes[0]].m_stopType;
                VehicleInfo.VehicleType secondStopType = network.m_lanes[network.m_sortedLanes[network.m_sortedLanes.Length - 1]].m_stopType;
                VehicleInfo.VehicleType mediumStopType = VehicleInfo.VehicleType.None;

                for (int i = 1; i < network.m_sortedLanes.Length - 2; i++)
                {
                    if (network.m_lanes[network.m_sortedLanes[i]].m_stopType != VehicleInfo.VehicleType.None)
                    {
                        mediumStopType = network.m_lanes[network.m_sortedLanes[i]].m_stopType;
                        break;
                    }
                }

                EnableStops(ai.m_elevatedInfo, mediumStopType, firstStopType, secondStopType);
                EnableStops(ai.m_bridgeInfo, mediumStopType, firstStopType, secondStopType);
                EnableStops(ai.m_tunnelInfo, mediumStopType, firstStopType, secondStopType);
                EnableStops(ai.m_slopeInfo, mediumStopType, firstStopType, secondStopType);
            }
        }

        private static void EnableStops(NetInfo info, VehicleInfo.VehicleType mediumStopType, VehicleInfo.VehicleType firstStopType, VehicleInfo.VehicleType secondStopType)
        {
            try
            {
                if (info == null)
                    return;

                if (info.m_lanes == null || info.m_sortedLanes == null || info.m_lanes.Length == 0 || info.m_sortedLanes.Length == 0)
                    return;

                for (int i = 1; i < info.m_sortedLanes.Length - 2; i++)
                {
                    if (info.m_lanes[info.m_sortedLanes[i]].m_vehicleType == VehicleInfo.VehicleType.None)
                    {
                        info.m_lanes[info.m_sortedLanes[i]].m_stopType = mediumStopType;
                    }
                }

                info.m_lanes[info.m_sortedLanes[0]].m_stopType = firstStopType;
                info.m_lanes[info.m_sortedLanes[0]].m_stopOffset = 0f;
                info.m_lanes[info.m_sortedLanes[info.m_sortedLanes.Length - 1]].m_stopType = secondStopType;
                info.m_lanes[info.m_sortedLanes[info.m_sortedLanes.Length - 1]].m_stopOffset = 0f;
            }
            catch (Exception ex)
            {
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.LogWarning($"ElevatedStopsEnabler: EnableStops failed for '{info?.name}': {ex.Message}");
                }
            }
        }

        public static void AllowStreetLightsOnElevatedStops()
        {
            for (uint i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); i++)
            {
                var netInfo = PrefabCollection<NetInfo>.GetLoaded(i);

                if (netInfo == null || netInfo.m_lanes == null || !netInfo.m_hasPedestrianLanes) 
                    continue;

                if (!(netInfo.m_netAI is RoadBridgeAI)) 
                    continue;

                foreach (NetInfo.Lane lane in netInfo.m_lanes)
                {
                    if (lane == null || lane.m_laneType != NetInfo.LaneType.Pedestrian || lane.m_laneProps == null || lane.m_laneProps.m_props == null) continue;

                    foreach (NetLaneProps.Prop laneProp in lane.m_laneProps.m_props)
                    {
                        if (laneProp != null && laneProp.m_prop != null && laneProp.m_prop.name.Contains("Street Light"))
                        {
                            laneProp.m_flagsForbidden = NetLane.Flags.None;
                            break;
                        }
                    }
                }
            }
        }
    }

    public static class ElevatedStopsExtensions
    {
        public static bool IsValidTransport(this VehicleInfo.VehicleType vehicleType)
        {
            return vehicleType == VehicleInfo.VehicleType.Car
                || vehicleType == VehicleInfo.VehicleType.Tram
                || vehicleType == VehicleInfo.VehicleType.Trolleybus;
        }
    }
}
