using System.Collections.Generic;
using ColossalFramework;

namespace StopStacker
{
    /// <summary>
    /// Tracks, per stop lane, the arrival order of vehicles currently in the
    /// Arriving/Leaving state on that lane. Index 0 is always the lead
    /// vehicle (vanilla stop position); index N &gt;= 1 gets its own berth.
    /// Queues are compacted lazily against live vehicle state instead of a
    /// frame-timeout, since a stale entry can only be a despawned/no-longer-
    /// arriving vehicle and both are cheap to detect directly.
    /// </summary>
    internal static class BerthRegistry
    {
        private static readonly Dictionary<uint, List<ushort>> LaneQueues = new Dictionary<uint, List<ushort>>();

        public static int GetBerthIndex(uint laneId, ushort vehicleId)
        {
            if (!LaneQueues.TryGetValue(laneId, out var queue))
            {
                queue = new List<ushort>();
                LaneQueues[laneId] = queue;
            }

            CompactQueue(queue);

            var index = queue.IndexOf(vehicleId);
            if (index < 0)
            {
                queue.Add(vehicleId);
                index = queue.Count - 1;
            }

            return index;
        }

        private static void CompactQueue(List<ushort> queue)
        {
            var vehicles = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
            for (var i = queue.Count - 1; i >= 0; i--)
            {
                var flags = vehicles[queue[i]].m_flags;
                var stillTracked = (flags & Vehicle.Flags.Created) != 0 &&
                                    (flags & (Vehicle.Flags.Arriving | Vehicle.Flags.Leaving)) != 0;
                if (!stillTracked)
                {
                    queue.RemoveAt(i);
                }
            }
        }

        public static void Clear()
        {
            LaneQueues.Clear();
        }
    }
}
