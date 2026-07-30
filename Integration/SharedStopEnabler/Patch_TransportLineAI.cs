// Adapted from SharedStopEnabler (GPL-3.0, Workshop 2096382380, github.com/CodeBardian/SharedStopEnabler) - see LICENSE.txt.
// The RoadBridgeAI branch from the upstream patches (elevated/bridge stop flag updates) was
// removed - this reduced port does not touch elevated stops, see LICENSE.txt for why.
using System;
using ColossalFramework;
using HarmonyLib;
using ImprovedPublicTransport;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace SharedStopEnabler
{
    /// <summary>
    /// Tracks which road segments have more than one transit line's stop wired to them, so
    /// <see cref="SharedStopRegistry"/> stays in sync as lines are built, edited and deleted.
    /// </summary>
    [HarmonyPatch(typeof(TransportLineAI), "AddLaneConnection")]
    internal static class Patch_TransportLineAI_AddLaneConnection
    {
        [HarmonyPostfix]
        public static void Postfix(VehicleInfo.VehicleType ___m_vehicleType, ushort nodeID, uint laneID)
        {
            try
            {
                if (!ModSetting.Instance.EnableSharedStopEnabler || nodeID == 0 || !___m_vehicleType.IsSharedStopTransport())
                {
                    return;
                }

                var netManager = Singleton<NetManager>.instance;
                var segment = netManager.m_lanes.m_buffer[laneID].m_segment;
                var lineID = netManager.m_nodes.m_buffer[nodeID].m_transportLine;
                if (segment == 0 || lineID == 0)
                {
                    return;
                }

                SharedStopRegistry.AddSharedStop(segment, lineID, laneID);
            }
            catch (Exception ex)
            {
                Utils.LogError($"SharedStopEnabler: failed in AddLaneConnection postfix: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(TransportLineAI), "RemoveLaneConnection")]
    internal static class Patch_TransportLineAI_RemoveLaneConnection
    {
        [HarmonyPrefix]
        public static void Prefix(VehicleInfo.VehicleType ___m_vehicleType, ushort nodeID, ref NetNode data, out uint __state)
        {
            __state = 0;
            try
            {
                if (!ModSetting.Instance.EnableSharedStopEnabler || nodeID == 0 || !___m_vehicleType.IsSharedStopTransport())
                {
                    return;
                }

                var netManager = Singleton<NetManager>.instance;
                var lane = data.m_lane;
                var segment = netManager.m_lanes.m_buffer[lane].m_segment;
                var lineID = netManager.m_nodes.m_buffer[nodeID].m_transportLine;
                if (lane == 0 || segment == 0 || lineID == 0 || !SharedStopRegistry.IsSharedStopSegment(segment))
                {
                    return;
                }

                SharedStopRegistry.RemoveSharedStop(segment, lineID, lane);
                __state = lane;
            }
            catch (Exception ex)
            {
                Utils.LogError($"SharedStopEnabler: failed in RemoveLaneConnection prefix: {ex.Message}");
            }
        }

        [HarmonyPostfix]
        public static void Postfix(uint __state)
        {
            try
            {
                if (__state == 0)
                {
                    return;
                }

                // The lane the departing line was using may still be a stop for the OTHER line(s)
                // still sharing this segment - vanilla's own RemoveLaneConnection can clear those
                // flags outright, so re-apply whatever every remaining line on this lane still needs.
                var netManager = Singleton<NetManager>.instance;
                var lane = __state;
                var segment = netManager.m_lanes.m_buffer[lane].m_segment;
                if (!SharedStopRegistry.IsSharedStopSegment(segment))
                {
                    return;
                }

                var stillSharing = SharedStopRegistry.LanesStillUsing(segment, lane);
                if (stillSharing == null || stillSharing.Count == 0)
                {
                    return;
                }

                var flags = (NetLane.Flags)netManager.m_lanes.m_buffer[lane].m_flags;
                foreach (var line in stillSharing)
                {
                    var stopFlag = Singleton<TransportManager>.instance.m_lines.m_buffer[line].Info.m_stopFlag;
                    flags |= stopFlag;
                }

                netManager.m_lanes.m_buffer[lane].m_flags = (ushort)flags;
                netManager.UpdateSegmentFlags(segment);
                netManager.UpdateSegmentRenderer(segment, true);
            }
            catch (Exception ex)
            {
                Utils.LogError($"SharedStopEnabler: failed in RemoveLaneConnection postfix: {ex.Message}");
            }
        }
    }
}
