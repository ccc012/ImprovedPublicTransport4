// Adapted from Commuter Destination (MIT, Workshop 2475986859,
// github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace CommuterDestination
{
    /// <summary>
    /// Straight port of upstream's Bridge (GetCitizenDestinations / GetDestinationStopId /
    /// StopIsDestination / IsCitizenAtStop) and DestinationGraphGenerator.GenerateGraph.
    ///
    /// Deliberately unmodified otherwise - no caps, no performance-profile limits, no reordering.
    /// An earlier in-house rewrite diverged from this and ended up showing no icons at all, so the
    /// behaviour here is upstream's, including its rough edges. The only two departures are safety
    /// ones that cannot change which icons are produced, and both are called out where they happen.
    /// </summary>
    internal static class DestinationGraphGenerator
    {
        /// <summary>
        /// Upstream's GetStopRange(): flat 64f, with its own TODO noting the real values are 32 for
        /// bus/cable car and 64 for everything else.
        /// </summary>
        private const float TransitRange = 64f;

        /// <summary>
        /// DEPARTURE FROM UPSTREAM (safety). Upstream's GetDestinationStopId walks the line with
        /// `while (true)`, terminating only on `nextStop == 0` or on finding the destination. A
        /// circular line has no stop with `nextStop == 0`, so a citizen whose destination is never
        /// matched spins forever and hangs the simulation thread. Bounded by the game's own maximum
        /// stop count so a legitimate walk can never be cut short.
        /// </summary>
        private const int MaxStopWalk = 256;

        public static DestinationGraph GenerateGraph(ushort stopId)
        {
            if (stopId == 0)
            {
                return DestinationGraph.Empty;
            }

            var stops = new Dictionary<ushort, DestinationGraphStop>();

            foreach (var destination in GetCitizenDestinations(stopId))
            {
                if (destination.StopId == 0 || destination.BuildingId == 0)
                {
                    continue;
                }

                DestinationGraphStop stop;
                if (!stops.TryGetValue(destination.StopId, out stop))
                {
                    stop = new DestinationGraphStop(destination.StopId);
                    stops.Add(destination.StopId, stop);
                }

                stop.AddJourney(destination.BuildingId);
            }

            return new DestinationGraph(new List<DestinationGraphStop>(stops.Values));
        }

        /// <remarks>Upstream notes this logic is taken from LoadPassengers (e.g. in BusAI).</remarks>
        private static List<CitizenDestination> GetCitizenDestinations(ushort stopId)
        {
            var citizenDestinations = new List<CitizenDestination>();

            var citizenManager = Singleton<CitizenManager>.instance;
            var netManager = Singleton<NetManager>.instance;
            if (citizenManager == null || netManager == null)
            {
                return citizenDestinations;
            }

            var stopPosition = netManager.m_nodes.m_buffer[stopId].m_position;

            var lowerX = Mathf.Max((int)((stopPosition.x - TransitRange) / 8.0 + 1080.0), 0);
            var upperX = Mathf.Min((int)((stopPosition.x + TransitRange) / 8.0 + 1080.0), 2159);
            var lowerZ = Mathf.Max((int)((stopPosition.z - TransitRange) / 8.0 + 1080.0), 0);
            var upperZ = Mathf.Min((int)((stopPosition.z + TransitRange) / 8.0 + 1080.0), 2159);

            var instances = citizenManager.m_instances.m_buffer;
            var grid = citizenManager.m_citizenGrid;

            for (var z = lowerZ; z <= upperZ; z++)
            {
                for (var x = lowerX; x <= upperX; x++)
                {
                    var citizenInstanceId = grid[z * 2160 + x];
                    var guard = 0;
                    while (citizenInstanceId != 0 && ++guard <= 65536)
                    {
                        var citizen = instances[citizenInstanceId];
                        var nextGridInstance = citizen.m_nextGridInstance;

                        if (IsCitizenAtStop(ref citizen, citizenInstanceId, stopId, TransitRange))
                        {
                            citizenDestinations.Add(new CitizenDestination
                            {
                                StopId = GetDestinationStopId(stopId, citizenInstanceId),
                                BuildingId = citizen.m_targetBuilding,
                            });
                        }

                        citizenInstanceId = nextGridInstance;
                    }
                }
            }

            return citizenDestinations;
        }

        private static bool IsCitizenAtStop(
            ref CitizenInstance citizen, ushort citizenInstanceId, ushort stopId, float stopRange)
        {
            var netManager = Singleton<NetManager>.instance;
            var stopPosition = netManager.m_nodes.m_buffer[stopId].m_position;

            // Upstream's IsCitizenInRangeOfStop, inlined: it compares the citizen's *target*
            // position, not their current position.
            if (Vector3.SqrMagnitude((Vector3)citizen.m_targetPos - stopPosition) >= stopRange * stopRange)
            {
                return false;
            }

            var nextStop = global::TransportLine.GetNextStop(stopId);
            var nextStopPosition = netManager.m_nodes.m_buffer[nextStop].m_position;

            var info = citizen.Info;
            if (info == null || info.m_citizenAI == null)
            {
                return false;
            }

            return (citizen.m_flags & CitizenInstance.Flags.WaitingTransport) != CitizenInstance.Flags.None
                && info.m_citizenAI.TransportArriveAtSource(
                    citizenInstanceId, ref citizen, stopPosition, nextStopPosition);
        }

        /// <summary>
        /// The stop this citizen will get off at, given the stop they are waiting at.
        /// </summary>
        /// <remarks>Upstream notes most of this is ripped from TransportArriveAtTarget.</remarks>
        private static ushort GetDestinationStopId(ushort originalStopId, ushort citizenId)
        {
            var citizen = Singleton<CitizenManager>.instance.m_instances.m_buffer[citizenId];
            var currentStop = global::TransportLine.GetNextStop(originalStopId);

            for (var walked = 0; walked < MaxStopWalk; walked++)
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
            }

            // Loop guard tripped (see MaxStopWalk): fall back to wherever the walk got to, which is
            // what upstream would return for the equivalent "ran out of line" case.
            return currentStop;
        }

        private static bool StopIsDestination(ushort currentStop, ushort nextStop, CitizenInstance citizenData)
        {
            var pathManager = Singleton<PathManager>.instance;
            var netManager = Singleton<NetManager>.instance;

            var currentPosition = netManager.m_nodes.m_buffer[currentStop].m_position;
            var nextPosition = netManager.m_nodes.m_buffer[nextStop].m_position;

            if ((citizenData.m_flags & CitizenInstance.Flags.OnTour) == CitizenInstance.Flags.OnTour)
            {
                if ((citizenData.m_flags & CitizenInstance.Flags.TargetIsNode) == CitizenInstance.Flags.TargetIsNode)
                {
                    var targetStop = citizenData.m_targetBuilding;
                    if (targetStop != 0 &&
                        Vector3.SqrMagnitude(netManager.m_nodes.m_buffer[targetStop].m_position - currentPosition) < 4.0)
                    {
                        var stopAfterTarget = global::TransportLine.GetNextStop(targetStop);
                        if (stopAfterTarget != 0 &&
                            Vector3.SqrMagnitude(netManager.m_nodes.m_buffer[stopAfterTarget].m_position - nextPosition) < 4.0)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            // DEPARTURE FROM UPSTREAM (safety, same result). Upstream advances the path here and
            // calls PathManager.ReleaseFirstUnit(ref citizenData.m_path) when the index runs past
            // the unit - inherited from TransportArriveAtTarget, where consuming the path is
            // correct. Here it runs inside a read-only query, and although citizenData is a struct
            // copy (so the citizen's own m_path field is untouched), ReleaseFirstUnit frees the
            // path unit *globally*, leaving the real citizen pointing at freed memory. The lookup
            // below reads the same position without freeing anything, so it reaches the same
            // verdict without damaging the city.
            var path = citizenData.m_path;
            var pathPositionIndex = citizenData.m_pathPositionIndex + 2;
            if (path == 0U || pathPositionIndex >> 1 >= pathManager.m_pathUnits.m_buffer[(int)path].m_positionCount)
            {
                return true;
            }

            PathUnit.Position position;
            if (pathManager.m_pathUnits.m_buffer[(int)path].GetPosition(pathPositionIndex >> 1, out position))
            {
                var laneId = PathManager.GetLaneID(position);
                if (Vector3.SqrMagnitude(
                        netManager.m_lanes.m_buffer[(int)laneId].CalculatePosition(position.m_offset * 0.003921569f)
                        - nextPosition) < 4.0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
