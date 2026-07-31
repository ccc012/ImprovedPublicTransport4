using ColossalFramework;
using HarmonyLib;
using ImprovedPublicTransport;
using UnityEngine;

namespace StopStacker
{
    [HarmonyPatch(typeof(BusAI))]
    internal static class Patch_BusAI_CalculateSegmentPosition
    {
        [HarmonyPostfix]
        [HarmonyPatch("CalculateSegmentPosition")]
        public static void Postfix(ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position position, uint laneID, byte offset, ref Vector3 pos, ref Vector3 dir)
        {
            Apply(vehicleID, ref vehicleData, position, laneID, offset, ref pos, ref dir);
        }

        internal static void Apply(ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position position, uint laneID, byte offset, ref Vector3 pos, ref Vector3 dir)
        {
            if (!ModSetting.Instance.EnableStopStacker)
            {
                return;
            }

            // Same gating condition vanilla/BetterBusStopPosition use: only vehicles
            // actually near a stop (about to load/unload) are candidates for a berth.
            if ((vehicleData.m_flags & (Vehicle.Flags.Leaving | Vehicle.Flags.Arriving)) == 0)
            {
                return;
            }

            var berthIndex = BerthRegistry.GetBerthIndex(laneID, vehicleID);
            if (berthIndex <= 0)
            {
                // Lead vehicle: leave the stop position exactly as vanilla (or
                // BetterBusStopPosition, if also enabled) computed it.
                return;
            }

            var netManager = Singleton<NetManager>.instance;
            var info = netManager.m_segments.m_buffer[position.m_segment].Info;
            if (info?.m_lanes == null || info.m_lanes.Length <= position.m_lane)
            {
                return;
            }

            var laneInfo = info.m_lanes[position.m_lane];
            ref var lane = ref netManager.m_lanes.m_buffer[laneID];
            var laneLength = lane.m_length;
            if (laneLength < 1f)
            {
                return;
            }

            var vehicleLength = vehicleData.Info?.m_generatedInfo != null ? vehicleData.Info.m_generatedInfo.m_size.z : 12f;
            var margin = vehicleLength + 2f;
            var maxBerths = Mathf.Max(1, Mathf.FloorToInt(laneLength / margin));
            if (berthIndex >= maxBerths)
            {
                // No room for another berth on this stop lane - fall back to
                // vanilla behavior (vehicle queues as ordinary traffic).
                return;
            }

            var stopOffset = laneInfo.m_stopOffset;
            var invert = (netManager.m_segments.m_buffer[position.m_segment].m_flags & NetSegment.Flags.Invert) != 0;
            if (invert)
            {
                stopOffset = -stopOffset;
            }

            var inverted = (laneInfo.m_finalDirection & NetInfo.Direction.Backward) != 0;
            if (invert)
            {
                inverted = !inverted;
            }

            var laneOffset = offset * 0.003921569f;
            var shift = berthIndex * margin / laneLength;
            // Berths are placed progressively earlier along the vehicle's direction
            // of travel than the lead vehicle's stop point, so following vehicles
            // never overlap/clip through it.
            var shiftedOffset = inverted
                ? Mathf.Clamp01(laneOffset + shift)
                : Mathf.Clamp01(laneOffset - shift);

            lane.CalculateStopPositionAndDirection(shiftedOffset, stopOffset, out pos, out dir);
        }
    }

    [HarmonyPatch(typeof(TrolleybusAI))]
    internal static class Patch_TrolleybusAI_CalculateSegmentPosition
    {
        [HarmonyPostfix]
        [HarmonyPatch("CalculateSegmentPosition")]
        public static void Postfix(ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position position, uint laneID, byte offset, ref Vector3 pos, ref Vector3 dir)
        {
            Patch_BusAI_CalculateSegmentPosition.Apply(vehicleID, ref vehicleData, position, laneID, offset, ref pos, ref dir);
        }
    }
}
