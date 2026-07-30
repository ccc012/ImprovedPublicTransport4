// Adapted from Commuter Destination (MIT, Workshop 2475986859, github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
namespace CommuterDestination
{
    /// <summary>One citizen waiting at a stop and where they get off the line.</summary>
    internal struct CitizenDestination
    {
        public ushort StopId;
        public ushort BuildingId;
    }
}
