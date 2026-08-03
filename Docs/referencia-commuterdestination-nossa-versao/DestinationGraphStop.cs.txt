// Adapted from Commuter Destination (MIT, Workshop 2475986859) - see LICENSE.txt.
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace CommuterDestination
{
    /// <summary>Legacy stop-grouped journey bag (kept for overlay compatibility).</summary>
    internal sealed class DestinationGraphStop
    {
        public readonly Vector3 Position;
        private readonly Dictionary<ushort, DestinationGraphJourney> _journeys =
            new Dictionary<ushort, DestinationGraphJourney>(8);

        public DestinationGraphStop(ushort stopId)
        {
            if (stopId != 0)
            {
                Position = Singleton<NetManager>.instance.m_nodes.m_buffer[stopId].m_position;
            }
            else
            {
                Position = Vector3.zero;
            }
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

        public void AddJourneyDirect(ushort buildingId, Vector3 position, int count)
        {
            if (_journeys.TryGetValue(buildingId, out var journey))
            {
                journey.Popularity += count;
                return;
            }

            var j = new DestinationGraphJourney(buildingId, position);
            j.Popularity = count;
            _journeys.Add(buildingId, j);
        }

        public IEnumerable<DestinationGraphJourney> Journeys => _journeys.Values;
    }
}
