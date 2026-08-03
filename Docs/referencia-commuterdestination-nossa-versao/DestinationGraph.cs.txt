// Adapted from Commuter Destination (MIT, Workshop 2475986859) - see LICENSE.txt.
using System.Collections.Generic;
using UnityEngine;

namespace CommuterDestination
{
    /// <summary>
    /// Compact destination snapshot for the open stop panel + map overlay.
    /// Flattened by building (panel and icons only care about final destinations).
    /// </summary>
    internal sealed class DestinationGraph
    {
        public readonly DestinationEntry[] Entries;
        public readonly int WaitingCount;

        public static readonly DestinationGraph Empty = new DestinationGraph(new DestinationEntry[0], 0);

        public DestinationGraph(DestinationEntry[] entries, int waitingCount)
        {
            Entries = entries ?? new DestinationEntry[0];
            WaitingCount = waitingCount;
        }

        /// <summary>Legacy shape for any remaining stop-grouped iteration (overlay uses Entries).</summary>
        public IEnumerable<DestinationGraphStop> Stops
        {
            get
            {
                // Single synthetic stop so older loops still work if any remain.
                if (Entries.Length == 0)
                {
                    yield break;
                }

                var stop = new DestinationGraphStop(0);
                for (var i = 0; i < Entries.Length; i++)
                {
                    stop.AddJourneyDirect(Entries[i].BuildingId, Entries[i].Position, Entries[i].Count);
                }

                yield return stop;
            }
        }
    }

    /// <summary>One destination building and how many waiting citizens are headed there.</summary>
    internal struct DestinationEntry
    {
        public ushort BuildingId;
        public Vector3 Position;
        public int Count;

        public DestinationEntry(ushort buildingId, Vector3 position, int count)
        {
            BuildingId = buildingId;
            Position = position;
            Count = count;
        }
    }
}
