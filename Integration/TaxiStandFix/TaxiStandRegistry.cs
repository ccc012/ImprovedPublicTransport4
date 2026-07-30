// Original implementation for IPT4 - not ported from any other mod. "Taxi Stand Fix" (Workshop
// 3712889232) ships no public source and no license file, so per the project's own licensing rules
// (Projeto/IPT4/03_LICENCIAMENTO_MODS_FONTE.md) only its high-level description may be used as
// inspiration, never its code or implementation details. Everything below was written independently
// against the vanilla TaxiStandAI/TaxiAI classes.
using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace TaxiStandFix
{
    /// <summary>
    /// Keeps a cheap, periodically-refreshed list of taxi stand buildings, so a per-vehicle
    /// nearest-stand lookup does not have to scan the whole building buffer every time.
    /// </summary>
    internal static class TaxiStandRegistry
    {
        // A full city very rarely places more than a handful of taxi stands, so a full rescan is
        // cheap even at this interval - and correctness (never being more than ~17 seconds stale)
        // matters more here than shaving a rescan that already runs a few times a minute.
        private const int RefreshIntervalFrames = 1024;

        private static readonly List<ushort> StandBuildingIds = new List<ushort>();
        private static uint _lastRefreshFrame = uint.MaxValue;

        public static ushort FindNearest(Vector3 position, float maxDistance)
        {
            RefreshIfStale();

            var buildingManager = BuildingManager.instance;
            ushort best = 0;
            var bestSqrDistance = maxDistance * maxDistance;

            for (var i = 0; i < StandBuildingIds.Count; i++)
            {
                var buildingId = StandBuildingIds[i];
                var building = buildingManager.m_buildings.m_buffer[buildingId];
                // The building could have been bulldozed since the last refresh; a zeroed-out
                // Info is the cheapest signal that the slot no longer holds a taxi stand.
                if (building.Info == null)
                {
                    continue;
                }

                var sqrDistance = (building.m_position - position).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    best = buildingId;
                }
            }

            return best;
        }

        private static void RefreshIfStale()
        {
            var currentFrame = Singleton<SimulationManager>.instance.m_currentFrameIndex;
            if (_lastRefreshFrame != uint.MaxValue && currentFrame - _lastRefreshFrame < RefreshIntervalFrames)
            {
                return;
            }

            _lastRefreshFrame = currentFrame;
            StandBuildingIds.Clear();

            var buildingManager = BuildingManager.instance;
            var buffer = buildingManager.m_buildings.m_buffer;
            for (var i = 1; i < buffer.Length; i++)
            {
                if (buffer[i].m_flags == Building.Flags.None)
                {
                    continue;
                }

                if (buffer[i].Info?.m_buildingAI is TaxiStandAI)
                {
                    StandBuildingIds.Add((ushort)i);
                }
            }
        }

        /// <summary>Forces the next FindNearest call to rescan, used on level unload so a stale
        /// list from a previous city never leaks into a newly loaded one.</summary>
        public static void Reset()
        {
            StandBuildingIds.Clear();
            _lastRefreshFrame = uint.MaxValue;
        }
    }
}
