using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace ImprovedPublicTransport.Util
{
    /// <summary>
    /// Shared "give this stop a name" logic for stop panels and Train Display.
    /// Prefers nearest building / street; avoids exact duplicate names by appending a
    /// house-number style suffix when the same base name is already used on another stop.
    /// </summary>
    internal static class StopAutoNamer
    {
        // Base name → how many stops already use it (or a numbered variant).
        private static readonly Dictionary<string, int> UsedBaseNameCounts =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Stops whose existing names we already folded into UsedBaseNameCounts (avoid
        // re-counting every Train Display poll / panel open).
        private static readonly HashSet<ushort> RememberedStopNodes = new HashSet<ushort>();

        // Nodes where auto-name already failed (no nearby building/street). Re-scanning
        // FindBuilding every Train Display poll for the same unnamed stop caused hitching.
        private static readonly HashSet<ushort> FailedAutoNameNodes = new HashSet<ushort>();

        // Services that usually have useful names near stops. Full enum×enum FindBuilding
        // was ~ServiceCount×SubServiceCount spatial queries per unnamed stop (UI click hitch).
        private static readonly ItemClass.Service[] NearbyNameServices =
        {
            ItemClass.Service.Residential,
            ItemClass.Service.Commercial,
            ItemClass.Service.Office,
            ItemClass.Service.Industrial,
            ItemClass.Service.Monument,
            ItemClass.Service.Tourism,
            ItemClass.Service.Education,
            ItemClass.Service.HealthCare,
            ItemClass.Service.PoliceDepartment,
            ItemClass.Service.FireDepartment,
            ItemClass.Service.PublicTransport,
            ItemClass.Service.Beautification,
            ItemClass.Service.Garbage,
            ItemClass.Service.Electricity,
            ItemClass.Service.Water,
            ItemClass.Service.Disaster,
        };

        /// <summary>
        /// Returns the stop's existing name, or auto-assigns and returns one.
        /// Returns null only if no nearby name source exists.
        /// </summary>
        public static string EnsureNamed(InstanceID instanceId)
        {
            if (!InstanceManager.IsValid(instanceId) || instanceId.NetNode == 0)
            {
                return null;
            }

            var instanceManager = Singleton<InstanceManager>.instance;
            var existing = instanceManager.GetName(instanceId);
            if (!string.IsNullOrEmpty(existing))
            {
                // Only count each stop once for uniqueness bookkeeping.
                if (RememberedStopNodes.Add(instanceId.NetNode))
                {
                    RememberName(existing);
                }

                return existing;
            }

            // Do not re-run spatial building scans every poll for stops that already failed.
            if (FailedAutoNameNodes.Contains(instanceId.NetNode))
            {
                return null;
            }

            var position = Singleton<NetManager>.instance.m_nodes.m_buffer[instanceId.NetNode].m_position;
            var baseName = FindNearestBuildingName(position);
            if (string.IsNullOrEmpty(baseName))
            {
                baseName = FindNearestStreetName(position);
            }

            if (string.IsNullOrEmpty(baseName))
            {
                FailedAutoNameNodes.Add(instanceId.NetNode);
                return null;
            }

            var unique = MakeUniqueName(baseName, position);
            instanceManager.SetName(instanceId, unique);
            RememberedStopNodes.Add(instanceId.NetNode);
            FailedAutoNameNodes.Remove(instanceId.NetNode);
            RememberName(unique);
            return unique;
        }

        /// <summary>
        /// Nearby named buildings for the stop dropdown (click path). Much cheaper than
        /// scanning every Service×SubService pair.
        /// </summary>
        public static ushort[] FindNearbyNamedBuildings(Vector3 position, float radius = 100f)
        {
            var buildingManager = Singleton<BuildingManager>.instance;
            if (buildingManager == null)
            {
                return new ushort[0];
            }

            // Small fixed set — enough for the dropdown without HashSet/List growth per service.
            var found = new ushort[24];
            var count = 0;
            var radiusSq = radius * radius;

            for (var i = 0; i < NearbyNameServices.Length && count < found.Length; i++)
            {
                // SubService.None = any sub-service under this service (one spatial query).
                var building = buildingManager.FindBuilding(
                    position,
                    radius,
                    NearbyNameServices[i],
                    ItemClass.SubService.None,
                    Building.Flags.Active,
                    Building.Flags.Untouchable);

                if (building == 0)
                {
                    continue;
                }

                // Dedupe
                var dupe = false;
                for (var j = 0; j < count; j++)
                {
                    if (found[j] == building)
                    {
                        dupe = true;
                        break;
                    }
                }

                if (dupe)
                {
                    continue;
                }

                var d = (buildingManager.m_buildings.m_buffer[building].m_position - position).sqrMagnitude;
                if (d > radiusSq)
                {
                    continue;
                }

                found[count++] = building;
            }

            if (count == 0)
            {
                return new ushort[0];
            }

            // Sort by distance (insertion sort; count is tiny).
            for (var i = 1; i < count; i++)
            {
                var id = found[i];
                var di = (buildingManager.m_buildings.m_buffer[id].m_position - position).sqrMagnitude;
                var j = i - 1;
                while (j >= 0)
                {
                    var dj = (buildingManager.m_buildings.m_buffer[found[j]].m_position - position).sqrMagnitude;
                    if (dj <= di)
                    {
                        break;
                    }

                    found[j + 1] = found[j];
                    j--;
                }

                found[j + 1] = id;
            }

            var result = new ushort[count];
            Array.Copy(found, result, count);
            return result;
        }

        public static void ClearSessionCaches()
        {
            UsedBaseNameCounts.Clear();
            RememberedStopNodes.Clear();
            FailedAutoNameNodes.Clear();
        }

        private static void RememberName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var bas = StripHouseSuffix(name);
            if (!UsedBaseNameCounts.TryGetValue(bas, out var n))
            {
                UsedBaseNameCounts[bas] = 1;
            }
            else
            {
                UsedBaseNameCounts[bas] = n + 1;
            }
        }

        /// <summary>
        /// "Residência Aspen" stays; "Residência Aspen 24" → base "Residência Aspen".
        /// </summary>
        private static string StripHouseSuffix(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            var i = name.Length - 1;
            while (i >= 0 && char.IsDigit(name[i]))
            {
                i--;
            }

            if (i < name.Length - 1 && i >= 0 && name[i] == ' ')
            {
                return name.Substring(0, i).TrimEnd();
            }

            return name;
        }

        private static string MakeUniqueName(string baseName, Vector3 position)
        {
            baseName = baseName.Trim();
            if (!UsedBaseNameCounts.TryGetValue(baseName, out var count) || count <= 0)
            {
                UsedBaseNameCounts[baseName] = 1;
                return baseName;
            }

            // Same street / building name already used — add a stable house-like number from position
            // so two stops on the same road don't share an identical label.
            var house = ComputeHouseNumber(position, count);
            UsedBaseNameCounts[baseName] = count + 1;
            return baseName + " " + house;
        }

        private static int ComputeHouseNumber(Vector3 position, int collisionIndex)
        {
            // Stable pseudo house number from map coords (even/odd sides like street numbers).
            var n = Mathf.Abs(Mathf.RoundToInt(position.x + position.z * 0.37f));
            n = (n % 180) + 2 + collisionIndex * 2;
            if (n < 2)
            {
                n = 2 + collisionIndex;
            }

            return n;
        }

        private static string FindNearestBuildingName(Vector3 position)
        {
            var nearby = FindNearbyNamedBuildings(position, 100f);
            if (nearby == null || nearby.Length == 0)
            {
                return null;
            }

            var buildingManager = Singleton<BuildingManager>.instance;
            // Already sorted by distance — first with a non-empty name wins.
            for (var i = 0; i < nearby.Length; i++)
            {
                var name = buildingManager.GetBuildingName(nearby[i], InstanceID.Empty);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }

            return null;
        }

        private static string FindNearestStreetName(Vector3 position)
        {
            try
            {
                var netManager = Singleton<NetManager>.instance;
                if (netManager == null)
                {
                    return null;
                }

                // Grid sample: pick the nearest named road segment within ~80 m.
                ushort bestSeg = 0;
                var bestDistSq = 80f * 80f;
                var gridX = Mathf.Clamp((int)(position.x / 64f + 135f), 0, 269);
                var gridZ = Mathf.Clamp((int)(position.z / 64f + 135f), 0, 269);
                for (var z = Math.Max(0, gridZ - 1); z <= Math.Min(269, gridZ + 1); z++)
                {
                    for (var x = Math.Max(0, gridX - 1); x <= Math.Min(269, gridX + 1); x++)
                    {
                        var segId = netManager.m_segmentGrid[z * 270 + x];
                        var guard = 0;
                        while (segId != 0 && guard++ < 64)
                        {
                            ref var seg = ref netManager.m_segments.m_buffer[segId];
                            if ((seg.m_flags & NetSegment.Flags.Created) != 0)
                            {
                                var mid = (netManager.m_nodes.m_buffer[seg.m_startNode].m_position
                                           + netManager.m_nodes.m_buffer[seg.m_endNode].m_position) * 0.5f;
                                var d = (mid - position).sqrMagnitude;
                                if (d < bestDistSq)
                                {
                                    var name = netManager.GetSegmentName(segId);
                                    if (!string.IsNullOrEmpty(name))
                                    {
                                        bestDistSq = d;
                                        bestSeg = segId;
                                    }
                                }
                            }

                            segId = seg.m_nextGridSegment;
                        }
                    }
                }

                return bestSeg != 0 ? netManager.GetSegmentName(bestSeg) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
