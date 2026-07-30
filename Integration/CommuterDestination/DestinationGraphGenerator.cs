// Adapted from Commuter Destination (MIT, Workshop 2475986859, github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
// Ported closely from Bridge.cs (GetCitizenDestinations/GetDestinationStopId/StopIsDestination) -
// this is the one part of the port worth reading carefully, since it walks the citizen grid and
// calls into vanilla AI methods. It is read-only: nothing here writes simulation state, it only
// inspects citizen flags/paths to work out where a waiting citizen will get off.
using System.Collections.Generic;
using System.Linq;
using ColossalFramework;
using UnityEngine;

namespace CommuterDestination
{
    internal static class DestinationGraphGenerator
    {
        public static DestinationGraph GenerateGraph(ushort stopId)
        {
            var stops = new Dictionary<ushort, DestinationGraphStop>();

            foreach (var destination in GetCitizenDestinations(stopId))
            {
                if (!stops.TryGetValue(destination.StopId, out var stop))
                {
                    stop = new DestinationGraphStop(destination.StopId);
                    stops.Add(destination.StopId, stop);
                }

                stop.AddJourney(destination.BuildingId);
            }

            return new DestinationGraph(stops.Values);
        }

        /// <summary>Upstream comment: "these values are taken from the LoadPassengers game
        /// methods" - 32 for bus/cable car, 64 for the rest. This reduced port always uses 64
        /// (matches upstream's own current behaviour, which does not yet vary this either).</summary>
        private const float StopRange = 64f;

        /// <remarks>Ripped from TransportArriveAtTarget, same as upstream.</remarks>
        private static IEnumerable<CitizenDestination> GetCitizenDestinations(ushort stopId)
        {
            var destinations = new List<CitizenDestination>();
            var netManager = Singleton<NetManager>.instance;
            var citizenManager = Singleton<CitizenManager>.instance;

            var stopPosition = netManager.m_nodes.m_buffer[stopId].m_position;

            var lowerX = Mathf.Max((int)((stopPosition.x - StopRange) / 8.0 + 1080.0), 0);
            var upperX = Mathf.Min((int)((stopPosition.x + StopRange) / 8.0 + 1080.0), 2159);
            var lowerZ = Mathf.Max((int)((stopPosition.z - StopRange) / 8.0 + 1080.0), 0);
            var upperZ = Mathf.Min((int)((stopPosition.z + StopRange) / 8.0 + 1080.0), 2159);

            for (var z = lowerZ; z <= upperZ; z++)
            {
                for (var x = lowerX; x <= upperX; x++)
                {
                    var citizenInstanceId = citizenManager.m_citizenGrid[z * 2160 + x];
                    while (citizenInstanceId != 0)
                    {
                        var citizen = citizenManager.m_instances.m_buffer[citizenInstanceId];
                        var nextGridInstance = citizen.m_nextGridInstance;

                        if (IsCitizenAtStop(ref citizen, citizenInstanceId, stopId, stopPosition))
                        {
                            destinations.Add(new CitizenDestination
                            {
                                StopId = GetDestinationStopId(stopId, citizenInstanceId),
                                BuildingId = citizen.m_targetBuilding
                            });
                        }

                        citizenInstanceId = nextGridInstance;
                    }
                }
            }

            return destinations;
        }

        private static bool IsCitizenAtStop(ref CitizenInstance citizen, ushort citizenInstanceId, ushort stopId, Vector3 stopPosition)
        {
            if (Vector3.SqrMagnitude((Vector3)citizen.m_targetPos - stopPosition) >= StopRange * StopRange)
            {
                return false;
            }

            var nextStop = global::TransportLine.GetNextStop(stopId);
            var nextStopPosition = Singleton<NetManager>.instance.m_nodes.m_buffer[nextStop].m_position;

            return (citizen.m_flags & CitizenInstance.Flags.WaitingTransport) != CitizenInstance.Flags.None
                   && citizen.Info.m_citizenAI.TransportArriveAtSource(citizenInstanceId, ref citizen, stopPosition, nextStopPosition);
        }

        /// <summary>Get the stop at which a waiting citizen will get off the line, i.e. simulate
        /// forward through the line's remaining stops until the one matching their path/target.</summary>
        private static ushort GetDestinationStopId(ushort originStopId, ushort citizenInstanceId)
        {
            var netManager = Singleton<NetManager>.instance;
            var citizen = Singleton<CitizenManager>.instance.m_instances.m_buffer[citizenInstanceId];
            var currentStop = global::TransportLine.GetNextStop(originStopId);
            var guard = 0;

            while (true)
            {
                var nextStop = global::TransportLine.GetNextStop(currentStop);
                if (nextStop == 0)
                {
                    return currentStop;
                }

                if (StopIsDestination(currentStop, nextStop, citizen))
                {
                    return currentStop;
                }

                currentStop = nextStop;
                // A transport line cannot legitimately have more stops than the node buffer; this
                // only trips on a corrupted stop-chain cycle, and bails instead of hanging.
                if (++guard > 32768)
                {
                    return currentStop;
                }
            }
        }

        private static bool StopIsDestination(ushort currentStop, ushort nextStop, CitizenInstance citizenData)
        {
            var netManager = Singleton<NetManager>.instance;
            var currentPosition = netManager.m_nodes.m_buffer[currentStop].m_position;
            var nextPosition = netManager.m_nodes.m_buffer[nextStop].m_position;
            var pathManager = Singleton<PathManager>.instance;

            if ((citizenData.m_flags & CitizenInstance.Flags.OnTour) == CitizenInstance.Flags.OnTour)
            {
                if ((citizenData.m_flags & CitizenInstance.Flags.TargetIsNode) == CitizenInstance.Flags.TargetIsNode)
                {
                    var targetStop = citizenData.m_targetBuilding;
                    if (targetStop != 0 && Vector3.SqrMagnitude(netManager.m_nodes.m_buffer[targetStop].m_position - currentPosition) < 4.0)
                    {
                        var stopAfterTarget = global::TransportLine.GetNextStop(targetStop);
                        if (stopAfterTarget != 0 && Vector3.SqrMagnitude(netManager.m_nodes.m_buffer[stopAfterTarget].m_position - nextPosition) < 4.0)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            // Deliberately NOT ported: upstream's version, when the lookahead position crosses into
            // the next PathUnit segment, calls PathManager.ReleaseFirstUnit(ref citizenData.m_path).
            // citizenData is a local struct copy (safe on its own), but m_path is just a uint index
            // into the SHARED, reference-counted PathManager.m_pathUnits buffer - ReleaseFirstUnit
            // takes the buffer lock and, when m_referenceCount <= 1 (the common case: a citizen's
            // own path is usually not shared, m_referenceCount starts at 1), actually frees that
            // path unit back to the pool. Calling that from a read-only display feature would
            // deallocate a citizen's real, in-use path segment while they are still walking it -
            // a likely crash or corrupted pathfinding state for an unrelated citizen later reusing
            // that freed slot. Confirmed by reading PathManager.ReleaseFirstUnit's decompiled body.
            //
            // Skipping this case just means a citizen whose immediate next path position happens to
            // fall exactly on a PathUnit boundary is treated as "not yet at this stop" and gets
            // checked against the next stop instead - a minor accuracy loss on a display-only
            // feature, not a correctness requirement worth the risk above.
            if (citizenData.m_path != 0u)
            {
                var positionIndex = citizenData.m_pathPositionIndex + 2;
                var pathUnit = pathManager.m_pathUnits.m_buffer[citizenData.m_path];
                if (positionIndex >> 1 < pathUnit.m_positionCount &&
                    pathUnit.GetPosition(positionIndex >> 1, out var position))
                {
                    var laneId = PathManager.GetLaneID(position);
                    var offset = position.m_offset;
                    if (Vector3.SqrMagnitude(netManager.m_lanes.m_buffer[laneId].CalculatePosition(offset * 0.003921569f) - nextPosition) < 4.0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
