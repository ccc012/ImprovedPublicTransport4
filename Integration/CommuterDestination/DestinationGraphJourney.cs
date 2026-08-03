// Adapted from Commuter Destination (MIT, Workshop 2475986859,
// github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
using UnityEngine;

namespace CommuterDestination
{
    /// <summary>
    /// One destination building reached from a stop, and how many waiting citizens are headed
    /// there (100 citizens from "Maple Lane" to "High Cliff University" is one journey with
    /// Popularity 100).
    /// </summary>
    internal sealed class DestinationGraphJourney
    {
        public readonly ushort BuildingId;
        public readonly Vector3 Destination;
        public int Popularity = 1;

        public DestinationGraphJourney(ushort buildingId, Vector3 destination)
        {
            BuildingId = buildingId;
            Destination = destination;
        }

        public void IncreasePopularity() => Popularity++;
    }
}
