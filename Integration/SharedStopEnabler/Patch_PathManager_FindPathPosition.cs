// Adapted from SharedStopEnabler (GPL-3.0, Workshop 2096382380, github.com/CodeBardian/SharedStopEnabler) - see LICENSE.txt.
using System;
using ColossalFramework;
using HarmonyLib;
using ImprovedPublicTransport;
using ImprovedPublicTransport.Util;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace SharedStopEnabler
{
    /// <summary>
    /// When pathfinding lands on a multi-lane shared stop for cars, nudge the stop lane
    /// toward the adjacent driving lane so citizens board the correct platform side.
    /// Hot path — early-outs when SharedStop is off.
    /// </summary>
    [HarmonyPatch(typeof(PathManager))]
    [HarmonyPatch("FindPathPosition")]
    [HarmonyPatch(
        new[]
        {
            typeof(Vector3), typeof(ItemClass.Service), typeof(ItemClass.Service), typeof(NetInfo.LaneType),
            typeof(VehicleInfo.VehicleType), typeof(VehicleInfo.VehicleCategory), typeof(VehicleInfo.VehicleType),
            typeof(bool), typeof(bool), typeof(float), typeof(bool), typeof(bool),
            typeof(PathUnit.Position), typeof(PathUnit.Position), typeof(float), typeof(float),
        },
        new[]
        {
            ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal,
            ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal,
            ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal,
            ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out,
        })]
    internal static class Patch_PathManager_FindPathPosition
    {
        [HarmonyPostfix]
        public static void Postfix(
            Vector3 position,
            ref PathUnit.Position pathPosA,
            VehicleInfo.VehicleType stopType,
            VehicleInfo.VehicleCategory vehicleCategory)
        {
            try
            {
                if (!ModSetting.Instance.EnableSharedStopEnabler)
                {
                    return;
                }

                if (stopType != VehicleInfo.VehicleType.Car || pathPosA.m_lane <= 1 || pathPosA.m_segment == 0)
                {
                    return;
                }

                var net = Singleton<NetManager>.instance;
                ref var segment = ref net.m_segments.m_buffer[pathPosA.m_segment];
                if (segment.Info?.m_lanes == null || segment.Info.m_sortedLanes == null)
                {
                    return;
                }

                var stop = pathPosA.m_lane;
                segment.GetClosestLanePosition(
                    position,
                    NetInfo.LaneType.Vehicle,
                    stopType,
                    vehicleCategory,
                    out _,
                    out _,
                    out var laneIndex,
                    out _);

                if (laneIndex < 0 || laneIndex >= segment.Info.m_lanes.Length)
                {
                    return;
                }

                var dir = segment.Info.m_lanes[laneIndex].m_direction;
                // stop/laneIndex are m_lanes indices; FindIndex looks them up inside m_sortedLanes to get
                // their physical (left-to-right) position, then +/-1 below walks to the spatially adjacent
                // lane and reads m_sortedLanes[pos] to convert that position back into an m_lanes index.
                // Both directions of the m_lanes <-> m_sortedLanes conversion are used correctly here.
                var stopLane = Array.FindIndex(segment.Info.m_sortedLanes, s => s == stop);
                var drivingLane = Array.FindIndex(segment.Info.m_sortedLanes, s => s == laneIndex);
                if (stopLane < 0 || drivingLane < 0)
                {
                    return;
                }

                if (dir == NetInfo.Direction.Backward && stopLane > drivingLane && drivingLane > 0)
                {
                    pathPosA.m_lane = (byte)segment.Info.m_sortedLanes[drivingLane - 1];
                }
                else if (dir == NetInfo.Direction.Forward
                         && stopLane < drivingLane
                         && drivingLane + 1 < segment.Info.m_sortedLanes.Length)
                {
                    pathPosA.m_lane = (byte)segment.Info.m_sortedLanes[drivingLane + 1];
                }
            }
            catch (Exception ex)
            {
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.LogError($"SharedStopEnabler: FindPathPosition postfix failed: {ex.Message}");
                }
            }
        }
    }
}
