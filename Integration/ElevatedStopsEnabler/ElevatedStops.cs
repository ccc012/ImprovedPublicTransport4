using System;
using System.Collections.Generic;
using System.Linq;
using ImprovedPublicTransport.Util;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ElevatedStopsEnabler
{
    static class ElevatedStops
    {
        private struct LaneState
        {
            public VehicleInfo.VehicleType StopType;
            public float StopOffset;
        }

        // Prefab lanes/props are shared, session-wide objects (not per-city) — mutating them without
        // recording the original values means turning the toggle off can never restore vanilla state,
        // and any other mod reading the same NetInfo would keep seeing our changes regardless of the
        // setting. Record every value the first time it is touched so Revert() can undo it exactly.
        private static readonly Dictionary<NetInfo.Lane, LaneState> _originalLaneState =
            new Dictionary<NetInfo.Lane, LaneState>();
        private static readonly Dictionary<NetLaneProps.Prop, NetLane.Flags> _originalPropFlags =
            new Dictionary<NetLaneProps.Prop, NetLane.Flags>();

        public static void Revert()
        {
            foreach (var kv in _originalLaneState)
            {
                kv.Key.m_stopType = kv.Value.StopType;
                kv.Key.m_stopOffset = kv.Value.StopOffset;
            }
            _originalLaneState.Clear();

            foreach (var kv in _originalPropFlags)
            {
                kv.Key.m_flagsForbidden = kv.Value;
            }
            _originalPropFlags.Clear();
        }

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

                // NetInfo.m_sortedLanes is an int[] of indices INTO m_lanes, ordered by physical
                // (left-to-right) lane position, not by declaration order. Every access below goes
                // through m_lanes[m_sortedLanes[i]] to resolve position -> actual lane, which is the
                // same pattern vanilla game code (RoadBaseAI, NetInfo itself) uses. m_sortedLanes[i]
                // is never used as a raw m_lanes index on its own, so this is safe.
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
                    var midLane = info.m_lanes[info.m_sortedLanes[i]];
                    if (midLane.m_vehicleType == VehicleInfo.VehicleType.None)
                    {
                        RecordLane(midLane);
                        midLane.m_stopType = mediumStopType;
                    }
                }

                var firstLane = info.m_lanes[info.m_sortedLanes[0]];
                RecordLane(firstLane);
                firstLane.m_stopType = firstStopType;
                firstLane.m_stopOffset = 0f;

                var lastLane = info.m_lanes[info.m_sortedLanes[info.m_sortedLanes.Length - 1]];
                RecordLane(lastLane);
                lastLane.m_stopType = secondStopType;
                lastLane.m_stopOffset = 0f;
            }
            catch (Exception ex)
            {
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.LogWarning($"ElevatedStopsEnabler: EnableStops failed for '{info?.name}': {ex.Message}");
                }
            }
        }

        private static void RecordLane(NetInfo.Lane lane)
        {
            if (lane != null && !_originalLaneState.ContainsKey(lane))
            {
                _originalLaneState[lane] = new LaneState { StopType = lane.m_stopType, StopOffset = lane.m_stopOffset };
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
                            if (!_originalPropFlags.ContainsKey(laneProp))
                            {
                                _originalPropFlags[laneProp] = laneProp.m_flagsForbidden;
                            }
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
