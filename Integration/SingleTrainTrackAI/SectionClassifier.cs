using System.Collections.Generic;
using ColossalFramework;

namespace SingleTrainTrackAI
{
    /// <summary>
    /// Groups contiguous single-track segments into one logical "section": the stretch of shared
    /// track between the two nearest points where it stops being a plain single-track pass-through
    /// (a switch, a junction, a station platform, a dead end, or the point double-track resumes).
    /// This is what actually gets reserved by <see cref="TrackReservation"/> - reserving one segment
    /// at a time let two opposing trains each claim a different segment of the same section and meet
    /// head-on in the middle; reserving the whole section up front closes that gap.
    /// </summary>
    internal static class SectionClassifier
    {
        // Safety net against a malformed/looping network (e.g. a single-track loop with no switches)
        // turning the walk below into an unbounded/infinite scan.
        private const int MaxSectionLength = 512;

        internal sealed class Section
        {
            internal ushort[] Segments;
        }

        // Every member segment of a section maps to the same shared Section instance, so
        // TrackReservation can use plain reference equality/identity as the reservation key.
        private static readonly Dictionary<ushort, Section> _cache = new Dictionary<ushort, Section>();

        internal static Section GetSection(ushort segmentId)
        {
            if (segmentId == 0)
            {
                return null;
            }

            if (_cache.TryGetValue(segmentId, out var cached))
            {
                return cached;
            }

            if (!SegmentClassifier.IsSingleTrainTrack(segmentId))
            {
                return null;
            }

            var section = BuildSection(segmentId);
            foreach (var seg in section.Segments)
            {
                _cache[seg] = section;
            }

            return section;
        }

        private static Section BuildSection(ushort startSegmentId)
        {
            var netManager = Singleton<NetManager>.instance;
            var segments = new List<ushort> { startSegmentId };

            var startSeg = netManager.m_segments.m_buffer[startSegmentId];
            ExtendFrom(segments, startSegmentId, startSeg.m_startNode, prepend: true);
            ExtendFrom(segments, startSegmentId, startSeg.m_endNode, prepend: false);

            return new Section { Segments = segments.ToArray() };
        }

        /// <summary>Walks outward from <paramref name="fromSegment"/> through <paramref name="throughNode"/>,
        /// adding each further single-track segment as long as the node it's reached through is a plain
        /// two-way pass-through (exactly one other segment). Stops at the first node that isn't.</summary>
        private static void ExtendFrom(List<ushort> segments, ushort fromSegment, ushort throughNode, bool prepend)
        {
            var netManager = Singleton<NetManager>.instance;
            var currentSegment = fromSegment;
            var currentNode = throughNode;

            while (segments.Count < MaxSectionLength)
            {
                var next = FindSinglePassThroughContinuation(currentSegment, currentNode);
                if (next == 0)
                {
                    break;
                }

                if (prepend)
                {
                    segments.Insert(0, next);
                }
                else
                {
                    segments.Add(next);
                }

                var nextSeg = netManager.m_segments.m_buffer[next];
                currentNode = nextSeg.m_startNode == currentNode ? nextSeg.m_endNode : nextSeg.m_startNode;
                currentSegment = next;
            }
        }

        /// <summary>Returns the single other single-track segment continuing straight through
        /// <paramref name="node"/> from <paramref name="fromSegment"/>, or 0 if the node is a switch,
        /// junction, station platform, dead end, or the track stops being single-track there.</summary>
        private static ushort FindSinglePassThroughContinuation(ushort fromSegment, ushort node)
        {
            var netManager = Singleton<NetManager>.instance;
            var netNode = netManager.m_nodes.m_buffer[node];

            ushort other = 0;
            var otherCount = 0;
            for (var i = 0; i < 8; i++)
            {
                var seg = netNode.GetSegment(i);
                if (seg == 0 || seg == fromSegment)
                {
                    continue;
                }

                other = seg;
                otherCount++;
            }

            if (otherCount != 1 || other == 0)
            {
                return 0;
            }

            return SegmentClassifier.IsSingleTrainTrack(other) ? other : (ushort)0;
        }

        internal static void Clear() => _cache.Clear();
    }
}
