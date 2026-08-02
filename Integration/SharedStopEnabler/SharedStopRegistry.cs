// Adapted from SharedStopEnabler (GPL-3.0, Workshop 2096382380, github.com/CodeBardian/SharedStopEnabler) - see LICENSE.txt.
//
// Ported the MonoBehaviour/Singleton<SharedStopsTool> pattern to a plain static class instead -
// IPT4 already tracks similar per-line/per-segment state this way (Data/CachedTransportLineData.cs)
// and this avoids adding a second lifecycle model for essentially the same kind of bookkeeping.
using System;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework;
using ImprovedPublicTransport.Util;
using UnityEngine;

namespace SharedStopEnabler
{
    internal static class SharedStopRegistry
    {
        private static readonly List<SharedStopSegment> SharedStopSegments = new List<SharedStopSegment>();
        private static bool _segmentsInitialized;

        public static bool IsSharedStopSegment(ushort segment)
        {
            return SharedStopSegments.Any(s => s.Segment == segment);
        }

        public static void AddSharedStop(ushort segment, ushort line, uint lane)
        {
            var existing = SharedStopSegments.FirstOrDefault(s => s.Segment == segment);
            if (existing != null)
            {
                if (existing.Lanes.TryGetValue(lane, out var lines))
                {
                    if (!lines.Contains(line))
                    {
                        lines.Add(line);
                    }
                }
                else
                {
                    existing.Lanes.Add(lane, new List<ushort> { line });
                }
            }
            else
            {
                SharedStopSegments.Add(new SharedStopSegment(segment, line, lane));
            }
        }

        public static void RemoveSharedStop(ushort segment, ushort line, uint lane)
        {
            var existing = SharedStopSegments.FirstOrDefault(s => s.Segment == segment);
            if (existing == null)
            {
                return;
            }

            if (existing.Lanes.TryGetValue(lane, out var lines))
            {
                lines.Remove(line);
                if (lines.Count == 0)
                {
                    existing.Lanes.Remove(lane);
                }
            }

            if (existing.Lanes.Count == 0)
            {
                Singleton<NetManager>.instance.m_segments.m_buffer[existing.Segment].m_flags &= ~NetSegment.Flags.StopAll;
                SharedStopSegments.Remove(existing);
            }
        }

        /// <summary>Line IDs still recorded as using this lane, or null if the segment/lane is not
        /// tracked at all.</summary>
        public static List<ushort> LanesStillUsing(ushort segment, uint lane)
        {
            var existing = SharedStopSegments.FirstOrDefault(s => s.Segment == segment);
            if (existing == null || !existing.Lanes.TryGetValue(lane, out var lines))
            {
                return null;
            }

            return lines;
        }

        /// <summary>Keeps bookkeeping in sync when a segment is split by road editing (e.g. adding
        /// a node/intersection on top of an existing shared-stop segment reassigns its ID).</summary>
        public static void RenameSegment(ushort oldSegment, ushort newSegment)
        {
            var existing = SharedStopSegments.FirstOrDefault(s => s.Segment == oldSegment);
            if (existing != null)
            {
                existing.Segment = newSegment;
            }
        }

        public static void Reset()
        {
            SharedStopSegments.Clear();
            _segmentsInitialized = false;
        }

        /// <summary>
        /// Registers a single stop segment/lane/line combination and re-applies elevated/bridge
        /// stop flags for it. Shared by <see cref="Patch_TransportLineAI_AddLaneConnection"/>'s
        /// postfix (a single newly-wired stop) and <see cref="RecalculateSharedStopSegments"/>
        /// (every stop of every existing line, at once) so both paths run the exact same logic.
        /// </summary>
        public static void RegisterStop(NetManager netManager, ushort segment, ushort lineID, uint laneID)
        {
            AddSharedStop(segment, lineID, laneID);

            if (netManager.m_segments.m_buffer[segment].Info?.m_netAI is RoadBridgeAI roadBridgeAI)
            {
                roadBridgeAI.UpdateSegmentStopFlags(
                    segment,
                    ref netManager.m_segments.m_buffer[segment]);
            }
        }

        /// <summary>
        /// Equivalent of the original mod's SharedStopsTool.Start() -> RecalculateSharedStopSegments():
        /// walks every existing, complete TransportLine and re-registers every stop it uses via
        /// <see cref="RegisterStop"/>, the same call <see cref="Patch_TransportLineAI_AddLaneConnection"/>'s
        /// postfix makes for a newly-wired stop. Without this, the in-memory registry starts empty on
        /// every (re)activation and only learns about a shared stop the next time a line touching it is
        /// built or edited - so a segment already shared by 2+ lines from a previous session, or from
        /// before the toggle was turned on, is invisible until then. Cheap to call repeatedly (each
        /// registration is idempotent), so it is safe to run on every <c>PatchController.Activate()</c>,
        /// not just the first one per level load.
        /// </summary>
        public static void RecalculateSharedStopSegments()
        {
            var lineBuffer = Singleton<TransportManager>.instance.m_lines.m_buffer;
            var netManager = Singleton<NetManager>.instance;
            var lineCount = lineBuffer.Length;

            for (ushort lineID = 1; lineID < lineCount; lineID++)
            {
                var line = lineBuffer[lineID];
                if (!line.Complete || line.Info == null || !line.Info.m_vehicleType.IsSharedStopTransport())
                {
                    continue;
                }

                var stops = line.m_stops;
                if (stops == 0)
                {
                    continue;
                }

                var stopCount = line.CountStops(lineID);
                for (var i = 0; i < stopCount; i++)
                {
                    stops = global::TransportLine.GetNextStop(stops);
                    var lane = netManager.m_nodes.m_buffer[stops].m_lane;
                    if (lane == 0)
                    {
                        continue;
                    }

                    var segment = netManager.m_lanes.m_buffer[lane].m_segment;
                    if (segment == 0)
                    {
                        continue;
                    }

                    RegisterStop(netManager, segment, lineID, lane);
                }
            }
        }

        /// <summary>
        /// The core mechanism: vanilla forbids stacking more than one "secondary" stop type
        /// (Bus/Tram/Trolleybus stop, encoded as *2 flags) per side of a road segment. This relaxes
        /// exactly that restriction on every loaded road/track prefab, which is what lets a bus and
        /// a tram stop coexist on the same segment. Runs once per level load, not per segment
        /// instance - it edits the shared prefab definition, which every placed instance of that
        /// road type then inherits.
        /// </summary>
        public static void InitSegments()
        {
            if (_segmentsInitialized)
            {
                return;
            }

            _segmentsInitialized = true;

            for (uint i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); i++)
            {
                var netInfo = PrefabCollection<NetInfo>.GetLoaded(i);
                if (netInfo == null || netInfo.m_segments == null || !netInfo.m_hasPedestrianLanes)
                {
                    continue;
                }

                foreach (var segment in netInfo.m_segments)
                {
                    if (segment == null)
                    {
                        continue;
                    }

                    if (segment.m_forwardRequired == NetSegment.Flags.StopLeft && segment.m_backwardRequired == NetSegment.Flags.StopRight)
                    {
                        segment.m_backwardForbidden &= ~NetSegment.Flags.StopRight2;
                        segment.m_forwardForbidden &= ~NetSegment.Flags.StopLeft2;
                    }
                    else if (segment.m_forwardRequired == (NetSegment.Flags.StopRight | NetSegment.Flags.StopLeft2) && segment.m_backwardRequired == (NetSegment.Flags.StopLeft | NetSegment.Flags.StopRight2))
                    {
                        segment.m_backwardForbidden &= ~NetSegment.Flags.StopLeft2;
                        segment.m_forwardForbidden &= ~NetSegment.Flags.StopRight2;
                    }
                    else if (segment.m_forwardRequired == NetSegment.Flags.StopBoth && segment.m_backwardRequired == NetSegment.Flags.StopBoth)
                    {
                        segment.m_backwardForbidden &= ~NetSegment.Flags.StopBoth2;
                        segment.m_forwardForbidden &= ~NetSegment.Flags.StopBoth2;
                    }
                    else if (segment.m_forwardRequired == NetSegment.Flags.StopBoth2 && segment.m_backwardRequired == NetSegment.Flags.StopBoth2 && netInfo.m_connectGroup != NetInfo.ConnectGroup.NarrowTram)
                    {
                        segment.m_backwardForbidden &= ~NetSegment.Flags.StopBoth;
                        segment.m_forwardForbidden &= ~NetSegment.Flags.StopBoth;
                    }
                }

                foreach (var lane in netInfo.m_lanes)
                {
                    if (lane == null || lane.m_laneType != NetInfo.LaneType.Pedestrian || lane.m_laneProps?.m_props == null)
                    {
                        continue;
                    }

                    foreach (var laneProp in lane.m_laneProps.m_props)
                    {
                        if (laneProp?.m_prop == null)
                        {
                            continue;
                        }

                        if (laneProp.m_prop.name == "Tram Stop")
                        {
                            laneProp.m_flagsForbidden |= NetLane.Flags.Stop;
                        }
                        else if (laneProp.m_prop.name == "Bus Stop Large" || laneProp.m_prop.name == "Bus Stop Small")
                        {
                            laneProp.m_flagsForbidden &= ~NetLane.Flags.Stop2;
                        }
                    }
                }
            }
        }
    }
}
