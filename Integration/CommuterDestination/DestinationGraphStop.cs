// Adapted from Commuter Destination (MIT, Workshop 2475986859,
// github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace CommuterDestination
{
    /// <summary>
    /// A stop passengers alight at, tracking which buildings they walk to afterwards.
    /// </summary>
    internal sealed class DestinationGraphStop
    {
        public readonly Vector3 Position;

        private readonly Dictionary<ushort, DestinationGraphJourney> _journeys =
            new Dictionary<ushort, DestinationGraphJourney>();

        public DestinationGraphStop(ushort stopId)
        {
            Position = Singleton<NetManager>.instance.m_nodes.m_buffer[stopId].m_position;
        }

        public void AddJourney(ushort buildingId)
        {
            DestinationGraphJourney journey;
            if (_journeys.TryGetValue(buildingId, out journey))
            {
                journey.IncreasePopularity();
                return;
            }

            var position = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId].m_position;
            _journeys.Add(buildingId, new DestinationGraphJourney(buildingId, position));
        }

        public IEnumerable<DestinationGraphJourney> GetJourneys() => _journeys.Values;
    }
}
