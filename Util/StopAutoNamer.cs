using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace ImprovedPublicTransport.Util
{
    /// <summary>
    /// Shared "give this stop a name from the nearest building" logic, used both by
    /// <see cref="HarmonyPatches.PublicTransportStopWorldInfoPanelPatches.AutoNameStopPatch"/> (fires
    /// once, reactively, when the player opens a stop's world info panel) and by
    /// TrainDisplayUpdated's route-strip/next-stop resolution (proactive - a stop the player has
    /// never clicked on still needs a name the moment a vehicle's HUD tries to show it, or it always
    /// renders as a bare "?").
    /// </summary>
    internal static class StopAutoNamer
    {
        /// <summary>
        /// Returns the stop's existing name, or auto-assigns and returns one from the nearest
        /// building within range if it has none yet. Returns null only if the stop is genuinely
        /// unnamed AND no nearby building could be used to name it (open countryside, etc.) - callers
        /// should fall back to their own placeholder in that case.
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
                return existing;
            }

            var position = Singleton<NetManager>.instance.m_nodes.m_buffer[instanceId.NetNode].m_position;
            var suggestedName = FindNearestBuildingName(position);
            if (string.IsNullOrEmpty(suggestedName))
            {
                return null;
            }

            instanceManager.SetName(instanceId, suggestedName);
            return suggestedName;
        }

        // Same search PublicTransportStopWorldInfoPanel.FindBuildings/IDToName already use to
        // populate the "suggested names" dropdown (every service/sub-service within 100m) -
        // reimplemented rather than reflected into, since both are private instance methods and the
        // search itself is small and self-contained.
        private static string FindNearestBuildingName(Vector3 position)
        {
            var buildingManager = Singleton<BuildingManager>.instance;
            var candidates = new List<ushort>();
            foreach (ItemClass.Service service in Enum.GetValues(typeof(ItemClass.Service)))
            {
                foreach (ItemClass.SubService subService in Enum.GetValues(typeof(ItemClass.SubService)))
                {
                    var building = buildingManager.FindBuilding(position, 100f, service, subService, Building.Flags.Active, Building.Flags.Untouchable);
                    if (building != 0)
                    {
                        candidates.Add(building);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            // Closest candidate first, matching the dropdown's own implicit "whatever FindBuilding
            // returns" ordering closely enough - the dropdown itself does not sort by distance either,
            // it just lists everything found; picking the nearest for the automatic case is the more
            // sensible default of the two.
            var closestId = candidates[0];
            var closestDistanceSq = float.MaxValue;
            foreach (var id in candidates)
            {
                var buildingPosition = buildingManager.m_buildings.m_buffer[id].m_position;
                var distanceSq = (buildingPosition - position).sqrMagnitude;
                if (distanceSq < closestDistanceSq)
                {
                    closestDistanceSq = distanceSq;
                    closestId = id;
                }
            }

            return buildingManager.GetBuildingName(closestId, InstanceID.Empty);
        }
    }
}
