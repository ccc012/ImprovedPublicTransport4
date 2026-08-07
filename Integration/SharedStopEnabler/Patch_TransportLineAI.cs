// Adapted from SharedStopEnabler (GPL-3.0, Workshop 2096382380, github.com/CodeBardian/SharedStopEnabler) - see LICENSE.txt.
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
    /// Also refreshes elevated/bridge stop flags via <see cref="RoadBridgeAIExt"/>.
    /// </summary>
    [HarmonyPatch(typeof(TransportLineAI), "AddLaneConnection")]
    internal static class Patch_TransportLineAI_AddLaneConnection
    {
        [HarmonyPrefix]
        public static void Prefix(
            NetLane.Flags ___m_stopFlag,
            VehicleInfo.VehicleType ___m_vehicleType,
            ushort nodeID,
            uint laneID)
        {
            try
            {
                if (!ModSetting.Instance.EnableSharedStopEnabler || nodeID == 0
                                                                || !___m_vehicleType.IsSharedStopTransport()
                                                                || laneID == 0)
                {
                    return;
                }

                var netManager = Singleton<NetManager>.instance;
                var segment = netManager.m_lanes.m_buffer[laneID].m_segment;
                if (segment == 0)
                {
                    return;
                }

                // Elevated/bridge: force stop flag on the lane then recompute segment flags.
                if (netManager.m_segments.m_buffer[segment].Info?.m_netAI is RoadBridgeAI roadBridgeAI)
                {
                    var flags = (NetLane.Flags)netManager.m_lanes.m_buffer[laneID].m_flags;
                    flags |= ___m_stopFlag;
                    netManager.m_lanes.m_buffer[laneID].m_flags = (ushort)flags;
                    roadBridgeAI.UpdateSegmentStopFlags(
                        segment,
                        ref netManager.m_segments.m_buffer[segment]);
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"SharedStopEnabler: failed in AddLaneConnection prefix: {ex.Message}");
            }
        }

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

                SharedStopRegistry.RegisterStop(netManager, segment, lineID, laneID);
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
                if (stillSharing == null || stillSharing.Length == 0)
                {
                    return;
                }

                var flags = (NetLane.Flags)netManager.m_lanes.m_buffer[lane].m_flags;
                var lineBuffer = Singleton<TransportManager>.instance.m_lines.m_buffer;
                foreach (var line in stillSharing)
                {
                    var stopInfo = lineBuffer[line].Info;
                    if (stopInfo == null)
                    {
                        continue;
                    }

                    flags |= stopInfo.m_stopFlag;
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
