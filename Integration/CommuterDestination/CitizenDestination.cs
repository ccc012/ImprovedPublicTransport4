// Adapted from Commuter Destination (MIT, Workshop 2475986859,
// github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.

namespace CommuterDestination
{
    /// <summary>One waiting citizen: the stop they will alight at, and the building they end at.</summary>
    internal struct CitizenDestination
    {
        public ushort StopId;
        public ushort BuildingId;
    }
}
