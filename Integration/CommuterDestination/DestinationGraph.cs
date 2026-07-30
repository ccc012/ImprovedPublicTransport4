// Adapted from Commuter Destination (MIT, Workshop 2475986859, github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
using System.Collections.Generic;

namespace CommuterDestination
{
    /// <summary>A graph of destinations from a series of transport line stops.</summary>
    internal sealed class DestinationGraph
    {
        public readonly IEnumerable<DestinationGraphStop> Stops;

        public DestinationGraph(IEnumerable<DestinationGraphStop> stops)
        {
            Stops = stops;
        }
    }
}
