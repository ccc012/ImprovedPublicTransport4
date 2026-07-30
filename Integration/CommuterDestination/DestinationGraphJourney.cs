// Adapted from Commuter Destination (MIT, Workshop 2475986859, github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
using UnityEngine;

namespace CommuterDestination
{
    /// <summary>One destination building reached from a stop, and how many waiting citizens are
    /// headed there (e.g. 100 citizens from "Maple Lane" to "High Cliff University" is a single
    /// journey with Popularity = 100).</summary>
    internal sealed class DestinationGraphJourney
    {
        public readonly ushort DestinationBuildingId;
        public readonly Vector3 Destination;
        public int Popularity = 1;

        public DestinationGraphJourney(ushort destinationBuildingId, Vector3 destination)
        {
            DestinationBuildingId = destinationBuildingId;
            Destination = destination;
        }

        public void IncreasePopularity() => Popularity++;
    }
}
