// Adapted from Commuter Destination (MIT, Workshop 2475986859,
// github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
using System.Collections.Generic;

namespace CommuterDestination
{
    /// <summary>Where the citizens waiting at one stop are headed, grouped by alighting stop.</summary>
    internal sealed class DestinationGraph
    {
        public readonly List<DestinationGraphStop> Stops;

        public static DestinationGraph Empty => new DestinationGraph(new List<DestinationGraphStop>());

        public DestinationGraph(List<DestinationGraphStop> stops)
        {
            Stops = stops ?? new List<DestinationGraphStop>();
        }
    }
}
