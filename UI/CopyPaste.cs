using System.Collections.Generic;
using ColossalFramework;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.UI.AlgernonCommons;
using ImprovedPublicTransport.Util;
using UnityEngine;

namespace ImprovedPublicTransport.UI
{
    /// <summary>
    /// Handles copying and pasting of line settings.
    /// </summary>
    public class CopyPaste
    {
        private const float NearbyBuildingSearchRadius = 100f;

        // Instance reference.
        private static CopyPaste s_instance;

        private HashSet<string> _copiedPrefabs;
        private bool _hasData;
        private int _targetVehicleCount;
        private bool _budgetControl;
        private ushort _depot;
        private ushort _ticketPrice;
        private Color _color;

        /// <summary>
        /// Gets the active instance.
        /// </summary>
        internal static CopyPaste Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new CopyPaste();
                }

                return s_instance;
            }
        }

        internal bool HasData => _hasData;

        internal Color CopiedColor => _color;

        internal ushort CopiedTicketPrice => _ticketPrice;

        /// <summary>
        /// Copies vehicle data from the given line to the copy buffer.
        /// </summary>
        /// <param name="transportLineID">Source line ID.</param>
        internal void Copy(ushort transportLineID)
        {
            if (transportLineID == 0)
            {
                return;
            }

            _targetVehicleCount = CachedTransportLineData.GetTargetVehicleCount(transportLineID);
            _budgetControl = CachedTransportLineData.GetBudgetControlState(transportLineID);
            _depot = CachedTransportLineData.GetDepot(transportLineID);
            _ticketPrice = Singleton<TransportManager>.instance.m_lines.m_buffer[transportLineID].m_ticketPrice;
            _color = Singleton<TransportManager>.instance.GetLineColor(transportLineID);

            HashSet<string> sourcePrefabs = CachedTransportLineData.GetPrefabs(transportLineID);
            _copiedPrefabs = sourcePrefabs != null ? new HashSet<string>(sourcePrefabs) : null;
            _hasData = true;
        }

        /// <summary>
        /// Copies the current line settings to all other lines that serve the same nearby buildings/stations.
        /// </summary>
        /// <param name="lineID">Source line ID.</param>
        internal void CopyToServedBuildings(ushort lineID)
        {
            if (!PrepareBatchCopy(lineID))
            {
                return;
            }

            var sourceBuildings = GetNearbyBuildingsForLine(lineID);
            if (sourceBuildings.Count == 0)
            {
                Logging.Message("CopyPaste.CopyToServedBuildings: no nearby served buildings found for this line.");
                return;
            }

            var targetLines = new List<ushort>();
            TransportManager manager = Singleton<TransportManager>.instance;
            for (int otherLine = 1; otherLine < manager.m_lines.m_buffer.Length; otherLine++)
            {
                if (otherLine == lineID)
                {
                    continue;
                }

                TransportLine line = manager.m_lines.m_buffer[otherLine];
                if (line.m_flags == TransportLine.Flags.None || line.Info == null)
                {
                    continue;
                }

                if (LineServesAnyBuilding((ushort)otherLine, sourceBuildings))
                {
                    targetLines.Add((ushort)otherLine);
                }
            }

            ApplyCopiedSettingsToLines(targetLines);
        }

        /// <summary>
        /// Copies the current line settings to all other lines that serve any of the same districts or park areas.
        /// </summary>
        /// <param name="lineID">Source line ID.</param>
        internal void CopyToServedDistricts(ushort lineID)
        {
            if (!PrepareBatchCopy(lineID))
            {
                return;
            }

            GetServedAreas(lineID, out HashSet<byte> districts, out HashSet<byte> parks);
            if (districts.Count == 0 && parks.Count == 0)
            {
                Logging.Message("CopyPaste.CopyToServedDistricts: no served districts or park areas found for this line.");
                return;
            }

            var targetLines = new List<ushort>();
            TransportManager manager = Singleton<TransportManager>.instance;
            for (int otherLine = 1; otherLine < manager.m_lines.m_buffer.Length; otherLine++)
            {
                if (otherLine == lineID)
                {
                    continue;
                }

                TransportLine line = manager.m_lines.m_buffer[otherLine];
                if (line.m_flags == TransportLine.Flags.None || line.Info == null)
                {
                    continue;
                }

                if (LineServesAnyArea((ushort)otherLine, districts, parks))
                {
                    targetLines.Add((ushort)otherLine);
                }
            }

            ApplyCopiedSettingsToLines(targetLines);
        }

        /// <summary>
        /// Checks if the given line is a valid target for the current copy buffer.
        /// </summary>
        internal bool IsValidTarget(ushort lineID)
        {
            return _hasData && lineID != 0;
        }

        /// <summary>
        /// Attempts to paste line data from the copy buffer to the given line.
        /// </summary>
        internal bool Paste(ushort lineID)
        {
            if (!_hasData || lineID == 0)
            {
                return false;
            }

            ApplyCopiedSettingsToLines(new[] { lineID });
            return true;
        }

        internal bool HasServedBuildings(ushort lineID) => GetNearbyBuildingsForLine(lineID).Count > 0;

        internal bool HasServedAreas(ushort lineID)
        {
            GetServedAreas(lineID, out HashSet<byte> districts, out HashSet<byte> parks);
            return districts.Count > 0 || parks.Count > 0;
        }

        private bool PrepareBatchCopy(ushort lineID)
        {
            if (lineID == 0)
            {
                Logging.Error("zero lineID passed to CopyPaste batch copy");
                return false;
            }

            Copy(lineID);
            return _hasData;
        }

        private void ApplyCopiedSettingsToLines(IEnumerable<ushort> lineIDs)
        {
            int copiedVehicleCount = _targetVehicleCount;
            bool copiedBudgetControl = _budgetControl;
            ushort copiedDepot = _depot;
            ushort copiedTicketPrice = _ticketPrice;
            HashSet<string> copiedPrefabs = _copiedPrefabs != null ? new HashSet<string>(_copiedPrefabs) : null;

            Singleton<SimulationManager>.instance.AddAction(() =>
            {
                foreach (ushort lineID in lineIDs)
                {
                    if (lineID == 0)
                    {
                        continue;
                    }

                    LineWatcher.instance?.MarkKnown(lineID);

                    TransportInfo info = Singleton<TransportManager>.instance.m_lines.m_buffer[lineID].Info;
                    if (info == null)
                    {
                        continue;
                    }

                    ushort targetDepot = copiedDepot != 0 && Util.DepotUtil.IsValidDepot(copiedDepot, info)
                        ? copiedDepot
                        : (ushort)0;

                    CachedTransportLineData.SetBudgetControlState(lineID, copiedBudgetControl);
                    CachedTransportLineData.SetTargetVehicleCount(lineID, copiedVehicleCount);
                    CachedTransportLineData.SetDepot(lineID, targetDepot);
                    CachedTransportLineData.SetPrefabs(lineID, copiedPrefabs != null ? new HashSet<string>(copiedPrefabs) : null);
                    Singleton<TransportManager>.instance.m_lines.m_buffer[lineID].m_ticketPrice = copiedTicketPrice;
                    Singleton<TransportManager>.instance.SetLineColor(lineID, _color);
                }
            });
        }

        private static bool LineServesAnyBuilding(ushort lineID, HashSet<ushort> sourceBuildings)
        {
            if (sourceBuildings.Count == 0)
            {
                return false;
            }

            var lineBuildings = GetNearbyBuildingsForLine(lineID);
            foreach (ushort buildingID in lineBuildings)
            {
                if (sourceBuildings.Contains(buildingID))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool LineServesAnyArea(ushort lineID, HashSet<byte> sourceDistricts, HashSet<byte> sourceParks)
        {
            GetServedAreas(lineID, out HashSet<byte> districts, out HashSet<byte> parks);
            foreach (byte district in districts)
            {
                if (sourceDistricts.Contains(district))
                {
                    return true;
                }
            }

            foreach (byte park in parks)
            {
                if (sourceParks.Contains(park))
                {
                    return true;
                }
            }

            return false;
        }

        private static HashSet<ushort> GetNearbyBuildingsForLine(ushort lineID)
        {
            var buildings = new HashSet<ushort>();
            foreach (ushort stopID in EnumerateLineStops(lineID))
            {
                Vector3 position = Singleton<NetManager>.instance.m_nodes.m_buffer[stopID].m_position;
                AddNearbyBuildings(position, buildings);
            }

            return buildings;
        }

        private static void GetServedAreas(ushort lineID, out HashSet<byte> districts, out HashSet<byte> parks)
        {
            districts = new HashSet<byte>();
            parks = new HashSet<byte>();
            DistrictManager districtManager = Singleton<DistrictManager>.instance;

            foreach (ushort stopID in EnumerateLineStops(lineID))
            {
                Vector3 position = Singleton<NetManager>.instance.m_nodes.m_buffer[stopID].m_position;
                byte district = districtManager.GetDistrict(position);
                byte park = districtManager.GetPark(position);
                if (district != 0)
                {
                    districts.Add(district);
                }
                if (park != 0)
                {
                    parks.Add(park);
                }
            }
        }

        private static IEnumerable<ushort> EnumerateLineStops(ushort lineID)
        {
            TransportLine line = Singleton<TransportManager>.instance.m_lines.m_buffer[lineID];
            ushort firstStop = line.m_stops;
            if (firstStop == 0)
            {
                yield break;
            }

            ushort stop = firstStop;
            int loopGuard = 0;
            do
            {
                yield return stop;
                stop = TransportLine.GetNextStop(stop);
                loopGuard++;
            }
            while (stop != 0 && stop != firstStop && loopGuard < 512);
        }

        private static void AddNearbyBuildings(Vector3 position, HashSet<ushort> result)
        {
            // Shared fast path with StopAutoNamer (priority services only — not Service×SubService).
            var nearby = StopAutoNamer.FindNearbyNamedBuildings(position, NearbyBuildingSearchRadius);
            if (nearby == null)
            {
                return;
            }

            for (var i = 0; i < nearby.Length; i++)
            {
                result.Add(nearby[i]);
            }
        }
    }
}
