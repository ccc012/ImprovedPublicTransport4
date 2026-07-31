using System.Collections.Generic;
using ColossalFramework;

namespace SingleTrainTrackAI
{
    /// <summary>
    /// Identifies road/rail segments that only have a single train-usable lane - trains travelling in
    /// both directions have to share it, unlike a normal double-tracked line where each direction gets
    /// its own lane. A segment with two or more train lanes is assumed double-tracked (or wider) and
    /// is never reserved, regardless of each lane's individual direction flags.
    /// </summary>
    internal static class SegmentClassifier
    {
        // The lane layout being classified here is entirely a property of the segment's NetInfo
        // PREFAB (m_lanes is shared by every segment instance using that prefab), never of the
        // segment instance itself - so caching by NetInfo instead of segmentId is both correct and
        // immune to per-instance mutation, and avoids re-walking m_lanes on every
        // CalculateTargetSpeed call for every train (up to twice per call: current + next segment).
        private static readonly Dictionary<NetInfo, bool> _cache = new Dictionary<NetInfo, bool>();

        internal static bool IsSingleTrainTrack(ushort segmentId)
        {
            if (segmentId == 0)
            {
                return false;
            }

            var info = Singleton<NetManager>.instance.m_segments.m_buffer[segmentId].Info;
            if (info == null)
            {
                return false;
            }

            if (_cache.TryGetValue(info, out var cached))
            {
                return cached;
            }

            var result = Classify(info);
            _cache[info] = result;
            return result;
        }

        private static bool Classify(NetInfo info)
        {
            if (info.m_lanes == null)
            {
                return false;
            }

            var trainLaneCount = 0;
            foreach (var lane in info.m_lanes)
            {
                if (lane.m_vehicleType == VehicleInfo.VehicleType.Train && lane.m_laneType == NetInfo.LaneType.Vehicle)
                {
                    trainLaneCount++;
                    if (trainLaneCount > 1)
                    {
                        return false;
                    }
                }
            }

            return trainLaneCount == 1;
        }

        internal static void Clear()
        {
            _cache.Clear();
        }
    }
}
