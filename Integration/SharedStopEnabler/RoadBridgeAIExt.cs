// Adapted from SharedStopEnabler (GPL-3.0, Workshop 2096382380, github.com/CodeBardian/SharedStopEnabler) - see LICENSE.txt.
using ColossalFramework;

namespace SharedStopEnabler
{
    /// <summary>
    /// Re-applies Stop/Stop2 segment flags on elevated/bridge roads after a lane gains a stop.
    /// Vanilla RoadBridgeAI.UpdateSegmentFlags does not fully propagate multi-type stop flags.
    /// </summary>
    internal static class RoadBridgeAIExt
    {
        public static void UpdateSegmentStopFlags(this RoadBridgeAI roadbridge, ushort segmentID, ref NetSegment data)
        {
            roadbridge.UpdateSegmentFlags(segmentID, ref data);
            var flags = data.m_flags & ~(NetSegment.Flags.StopRight | NetSegment.Flags.StopLeft
                                         | NetSegment.Flags.StopRight2 | NetSegment.Flags.StopLeft2);
            if (roadbridge.m_info?.m_lanes == null)
            {
                data.m_flags = flags;
                return;
            }

            var instance = Singleton<NetManager>.instance;
            var inverted = (data.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
            var lane = instance.m_segments.m_buffer[segmentID].m_lanes;
            var num2 = 0;
            while (num2 < roadbridge.m_info.m_lanes.Length && lane != 0U)
            {
                var laneFlags = (NetLane.Flags)instance.m_lanes.m_buffer[lane].m_flags;
                if ((laneFlags & NetLane.Flags.Stop) != 0)
                {
                    if (roadbridge.m_info.m_lanes[num2].m_position < 0f != inverted)
                    {
                        flags |= NetSegment.Flags.StopLeft;
                    }
                    else
                    {
                        flags |= NetSegment.Flags.StopRight;
                    }
                }

                if ((laneFlags & NetLane.Flags.Stop2) != 0)
                {
                    if (roadbridge.m_info.m_lanes[num2].m_position < 0f != inverted)
                    {
                        flags |= NetSegment.Flags.StopLeft2;
                    }
                    else
                    {
                        flags |= NetSegment.Flags.StopRight2;
                    }
                }

                lane = instance.m_lanes.m_buffer[lane].m_nextLane;
                num2++;
            }

            data.m_flags = flags;
        }
    }
}
