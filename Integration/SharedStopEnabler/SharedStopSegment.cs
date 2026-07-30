// Adapted from SharedStopEnabler (GPL-3.0, Workshop 2096382380, github.com/CodeBardian/SharedStopEnabler) - see LICENSE.txt.
using System.Collections.Generic;

namespace SharedStopEnabler
{
    /// <summary>Bookkeeping for one road segment that has more than one transit line's stop
    /// connected to it: which lane, and which line IDs are using that lane.</summary>
    internal sealed class SharedStopSegment
    {
        public ushort Segment;
        public readonly Dictionary<uint, List<ushort>> Lanes = new Dictionary<uint, List<ushort>>();

        public SharedStopSegment(ushort segment, ushort line, uint lane)
        {
            Segment = segment;
            Lanes.Add(lane, new List<ushort> { line });
        }
    }
}
