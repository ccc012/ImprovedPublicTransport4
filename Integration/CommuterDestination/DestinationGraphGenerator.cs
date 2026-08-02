// Adapted from Commuter Destination (MIT, Workshop 2475986859) - see LICENSE.txt.
using System.Collections.Generic;
using ColossalFramework;
using ImprovedPublicTransport;
using ImprovedPublicTransport.Util;
using UnityEngine;

namespace CommuterDestination
{
    internal static class DestinationGraphGenerator
    {
        // TEMP (testing): widened from 64f so we're not also fighting a too-narrow search radius
        // while confirming whether the feature produces any entries at all. Narrow back down once
        // confirmed working.
        private const float StopRange = 300f;
        private const float StopRangeSq = StopRange * StopRange;

        private static readonly Dictionary<ushort, int> s_buildingCounts = new Dictionary<ushort, int>(64);
        private static readonly List<DestinationEntry> s_scratchEntries = new List<DestinationEntry>(32);

        public static bool IsFullMapMode =>
            ModSetting.Instance.CommuterDestinationMapDetail
            == ModSetting.CommuterDestinationMapDetails.FullMap;

        public static int OverlayIconLimit =>
            PerformanceProfile.CommuterMaxDestinationsEffective(IsFullMapMode);

        public static DestinationGraph GenerateGraph(ushort stopId)
        {
            if (stopId == 0)
            {
                return DestinationGraph.Empty;
            }

            s_buildingCounts.Clear();

            var netManager = Singleton<NetManager>.instance;
            var citizenManager = Singleton<CitizenManager>.instance;
            if (netManager == null || citizenManager == null)
            {
                return DestinationGraph.Empty;
            }

            var fullMap = IsFullMapMode;
            var maxCitizens = PerformanceProfile.CommuterMaxCitizensEffective(fullMap);
            var maxDest = PerformanceProfile.CommuterMaxDestinationsEffective(fullMap);

            ref var stopNode = ref netManager.m_nodes.m_buffer[stopId];
            var stopPosition = stopNode.m_position;

            var nextStop = global::TransportLine.GetNextStop(stopId);
            Vector3 nextStopPosition = Vector3.zero;
            if (nextStop != 0)
            {
                nextStopPosition = netManager.m_nodes.m_buffer[nextStop].m_position;
            }

            var lowerX = Mathf.Max((int)((stopPosition.x - StopRange) / 8f + 1080f), 0);
            var upperX = Mathf.Min((int)((stopPosition.x + StopRange) / 8f + 1080f), 2159);
            var lowerZ = Mathf.Max((int)((stopPosition.z - StopRange) / 8f + 1080f), 0);
            var upperZ = Mathf.Min((int)((stopPosition.z + StopRange) / 8f + 1080f), 2159);

            var instances = citizenManager.m_instances.m_buffer;
            var grid = citizenManager.m_citizenGrid;
            var scanned = 0;
            var waiting = 0;

            for (var z = lowerZ; z <= upperZ && scanned < maxCitizens; z++)
            {
                var row = z * 2160;
                for (var x = lowerX; x <= upperX && scanned < maxCitizens; x++)
                {
                    var citizenInstanceId = grid[row + x];
                    var guard = 0;
                    while (citizenInstanceId != 0 && scanned < maxCitizens && guard++ < 64)
                    {
                        ref var citizen = ref instances[citizenInstanceId];
                        var nextGridInstance = citizen.m_nextGridInstance;
                        scanned++;

                        if ((citizen.m_flags & CitizenInstance.Flags.WaitingTransport) == 0)
                        {
                            citizenInstanceId = nextGridInstance;
                            continue;
                        }

                        // NOTE: used to also reject here when citizen.m_targetPos was outside
                        // StopRange of the stop - but m_targetPos is the citizen's CURRENT PATH
                        // SEGMENT target (the next waypoint), not their present position, and once
                        // WaitingTransport is set that waypoint has typically already advanced past
                        // the stop toward wherever the transport leg goes next - often far outside
                        // StopRange. That silently rejected nearly every legitimately-waiting
                        // citizen with no error ever logged, which is consistent with destination
                        // icons never appearing. The citizen grid iteration above (lowerX/upperX/
                        // lowerZ/upperZ, all derived from stopPosition +/- StopRange) already does
                        // the real proximity filtering by cell - this was a redundant, and wrong,
                        // second check.

                        // Light profile: skip expensive AI gate (WaitingTransport + grid cell is enough).
                        if (!PerformanceProfile.IsLight
                            && nextStop != 0
                            && citizen.Info?.m_citizenAI != null)
                        {
                            if (!citizen.Info.m_citizenAI.TransportArriveAtSource(
                                    citizenInstanceId, ref citizen, stopPosition, nextStopPosition))
                            {
                                citizenInstanceId = nextGridInstance;
                                continue;
                            }
                        }

                        var buildingId = citizen.m_targetBuilding;
                        if (buildingId == 0
                            || (citizen.m_flags & CitizenInstance.Flags.TargetIsNode) != 0)
                        {
                            citizenInstanceId = nextGridInstance;
                            continue;
                        }

                        waiting++;
                        if (s_buildingCounts.TryGetValue(buildingId, out var n))
                        {
                            s_buildingCounts[buildingId] = n + 1;
                        }
                        else
                        {
                            s_buildingCounts[buildingId] = 1;
                        }

                        citizenInstanceId = nextGridInstance;
                    }
                }
            }

            if (s_buildingCounts.Count == 0)
            {
                return new DestinationGraph(new DestinationEntry[0], waiting);
            }

            s_scratchEntries.Clear();
            var buildings = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
            foreach (var kv in s_buildingCounts)
            {
                var id = kv.Key;
                var count = kv.Value;
                if (id == 0 || count <= 0)
                {
                    continue;
                }

                InsertTopN(s_scratchEntries, new DestinationEntry(id, buildings[id].m_position, count), maxDest);
            }

            return new DestinationGraph(s_scratchEntries.ToArray(), waiting);
        }

        private static void InsertTopN(List<DestinationEntry> list, DestinationEntry entry, int maxN)
        {
            var insertAt = list.Count;
            for (var i = 0; i < list.Count; i++)
            {
                if (entry.Count > list[i].Count)
                {
                    insertAt = i;
                    break;
                }
            }

            if (insertAt >= maxN)
            {
                return;
            }

            list.Insert(insertAt, entry);
            while (list.Count > maxN)
            {
                list.RemoveAt(list.Count - 1);
            }
        }
    }
}
