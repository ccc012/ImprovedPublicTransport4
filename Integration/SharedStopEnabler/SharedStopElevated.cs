// Adapted from SharedStopEnabler (GPL-3.0, Workshop 2096382380, github.com/CodeBardian/SharedStopEnabler) - see LICENSE.txt.
using System;
using ColossalFramework;
using ImprovedPublicTransport.Util;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace SharedStopEnabler
{
    /// <summary>
    /// Prefab-level elevated/bridge stop enablement + middle-lane Car stop types so shared
    /// bus/tram/trolley stops work on elevated roads (complements ElevatedStopsEnabler).
    /// </summary>
    internal static class SharedStopElevated
    {
        private static bool _elevatedDone;
        private static bool _stopTypesDone;

        public static void Apply()
        {
            try
            {
                EnableElevatedStops();
                InitStopTypes();
            }
            catch (Exception ex)
            {
                Utils.LogError($"SharedStopEnabler: elevated/stop-type init failed: {ex.Message}");
            }
        }

        public static void ResetFlags()
        {
            _elevatedDone = false;
            _stopTypesDone = false;
        }

        private static void EnableElevatedStops()
        {
            if (_elevatedDone)
            {
                return;
            }

            _elevatedDone = true;
            var networks = Resources.FindObjectsOfTypeAll<NetInfo>();
            if (networks == null)
            {
                return;
            }

            foreach (var network in networks)
            {
                if (network == null || !network.m_hasPedestrianLanes)
                {
                    continue;
                }

                if (!(network.m_netAI is RoadAI ai))
                {
                    continue;
                }

                if (network.m_lanes == null || network.m_sortedLanes == null || network.m_sortedLanes.Length == 0)
                {
                    continue;
                }

                var hasStops = false;
                foreach (var lane in network.m_lanes)
                {
                    if (lane != null && lane.m_stopType != VehicleInfo.VehicleType.None)
                    {
                        hasStops = true;
                        break;
                    }
                }

                if (!hasStops)
                {
                    continue;
                }

                var firstStopType = network.m_lanes[network.m_sortedLanes[0]].m_stopType;
                var secondStopType = network.m_lanes[network.m_sortedLanes[network.m_sortedLanes.Length - 1]].m_stopType;
                var middleStopType = VehicleInfo.VehicleType.None;
                for (var i = 1; i < network.m_sortedLanes.Length - 2; i++)
                {
                    var li = network.m_sortedLanes[i];
                    if (li < 0 || li >= network.m_lanes.Length)
                    {
                        continue;
                    }

                    if (network.m_lanes[li].m_laneType == NetInfo.LaneType.Pedestrian)
                    {
                        middleStopType = network.m_lanes[li].m_stopType;
                        break;
                    }
                }

                EnableStops(ai.m_elevatedInfo, firstStopType, middleStopType, secondStopType);
                EnableStops(ai.m_bridgeInfo, firstStopType, middleStopType, secondStopType);
                EnableStops(ai.m_slopeInfo, firstStopType, middleStopType, secondStopType);
                EnableStops(ai.m_tunnelInfo, firstStopType, middleStopType, secondStopType);
            }
        }

        private static void EnableStops(
            NetInfo info,
            VehicleInfo.VehicleType firstStopType,
            VehicleInfo.VehicleType middleStopType,
            VehicleInfo.VehicleType secondStopType)
        {
            if (info?.m_lanes == null || info.m_sortedLanes == null
                                      || info.m_lanes.Length == 0 || info.m_sortedLanes.Length == 0)
            {
                return;
            }

            for (var i = 1; i < info.m_sortedLanes.Length - 2; i++)
            {
                var idx = info.m_sortedLanes[i];
                if (idx < 0 || idx >= info.m_lanes.Length)
                {
                    continue;
                }

                if (info.m_lanes[idx].m_vehicleType == VehicleInfo.VehicleType.None)
                {
                    info.m_lanes[idx].m_stopType = middleStopType;
                }
            }

            var first = info.m_sortedLanes[0];
            var last = info.m_sortedLanes[info.m_sortedLanes.Length - 1];
            if (first >= 0 && first < info.m_lanes.Length)
            {
                info.m_lanes[first].m_stopType = firstStopType;
                info.m_lanes[first].m_stopOffset = 0f;
            }

            if (last >= 0 && last < info.m_lanes.Length)
            {
                info.m_lanes[last].m_stopType = secondStopType;
                info.m_lanes[last].m_stopOffset = 0f;
            }
        }

        /// <summary>Allow bus (Car) stops on middle pedestrian lanes used for shared platforms.</summary>
        private static void InitStopTypes()
        {
            if (_stopTypesDone)
            {
                return;
            }

            _stopTypesDone = true;
            var networks = Resources.FindObjectsOfTypeAll<NetInfo>();
            if (networks == null)
            {
                return;
            }

            foreach (var segment in networks)
            {
                if (segment == null || !segment.m_hasPedestrianLanes
                                    || segment.m_lanes == null || segment.m_sortedLanes == null
                                    || segment.m_sortedLanes.Length < 3)
                {
                    continue;
                }

                for (var i = 1; i < segment.m_sortedLanes.Length - 2; i++)
                {
                    if (!IsValidMiddleLane(segment, (uint)i))
                    {
                        continue;
                    }

                    var index = (uint)segment.m_sortedLanes[i];
                    if (index < segment.m_lanes.Length)
                    {
                        segment.m_lanes[index].m_stopType |= VehicleInfo.VehicleType.Car;
                    }
                }
            }
        }

        private static bool IsValidMiddleLane(NetInfo segment, uint laneIndex)
        {
            if (laneIndex + 1 >= segment.m_sortedLanes.Length || laneIndex < 1)
            {
                return false;
            }

            var index = (uint)segment.m_sortedLanes[laneIndex];
            if (index >= segment.m_lanes.Length)
            {
                return false;
            }

            var lane = segment.m_lanes[index];
            if (lane.m_vehicleType != VehicleInfo.VehicleType.None
                || (lane.m_stopType & VehicleInfo.VehicleType.Car) == VehicleInfo.VehicleType.Car)
            {
                return false;
            }

            var index1 = segment.m_sortedLanes[laneIndex + 1];
            var index2 = segment.m_sortedLanes[laneIndex - 1];
            if (index1 < 0 || index1 >= segment.m_lanes.Length || index2 < 0 || index2 >= segment.m_lanes.Length)
            {
                return false;
            }

            if ((segment.m_lanes[index1].m_vehicleType & VehicleInfo.VehicleType.Car) != VehicleInfo.VehicleType.Car
                || (segment.m_lanes[index2].m_vehicleType & VehicleInfo.VehicleType.Car) != VehicleInfo.VehicleType.Car)
            {
                return false;
            }

            return true;
        }
    }
}
