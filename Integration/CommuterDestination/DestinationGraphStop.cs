// Adapted from Commuter Destination (MIT, Workshop 2475986859, github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace CommuterDestination
{
    /// <summary>A stop on a transport line, and the buildings passengers head to after getting off
    /// there.</summary>
    internal sealed class DestinationGraphStop
    {
        public readonly Vector3 Position;
        private readonly Dictionary<ushort, DestinationGraphJourney> _journeys = new Dictionary<ushort, DestinationGraphJourney>();

        public DestinationGraphStop(ushort stopId)
        {
            Position = Singleton<NetManager>.instance.m_nodes.m_buffer[stopId].m_position;
        }

        public void AddJourney(ushort buildingId)
        {
            if (_journeys.TryGetValue(buildingId, out var journey))
            {
                journey.IncreasePopularity();
                return;
            }

            var buildingPosition = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId].m_position;
            _journeys.Add(buildingId, new DestinationGraphJourney(buildingId, buildingPosition));
        }

        public IEnumerable<DestinationGraphJourney> Journeys => _journeys.Values;
    }
}
