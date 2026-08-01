using ColossalFramework;
using ImprovedPublicTransport.HarmonyPatches.TransportLinePatches;

namespace ExpressBusServices
{
    /// <summary>
    /// Shared guards for Express Bus pathfind / redeploy so bad stop or building IDs
    /// never reach vanilla array indexing (simulation "Array index is out of range").
    /// </summary>
    internal static class TransportStopSafety
    {
        public static bool IsLiveStopNode(ushort stopId)
            => TransportLineStopSafetyPatch.IsLiveStopNode(stopId);

        public static bool IsInNodeBuffer(ushort stopId)
            => TransportLineStopSafetyPatch.IsInNodeBuffer(stopId);

        public static bool IsInBuildingBuffer(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return false;
            }

            var buildings = Singleton<BuildingManager>.instance?.m_buildings.m_buffer;
            return buildings != null && buildingId < buildings.Length;
        }

        /// <summary>
        /// Whether <see cref="Vehicle.m_targetBuilding"/> is safe for BusAI/TrolleybusAI.StartPathFind.
        /// On-line vehicles treat the field as a stop node; GoingBack vehicles treat it as a building.
        /// </summary>
        public static bool IsSafePathfindTarget(ref Vehicle vehicleData)
        {
            var target = vehicleData.m_targetBuilding;
            if (target == 0)
            {
                return false;
            }

            if ((vehicleData.m_flags & Vehicle.Flags.GoingBack) != 0)
            {
                return IsInBuildingBuffer(target);
            }

            // Depot spawn / on-route: target is a stop node.
            return IsLiveStopNode(target);
        }
    }
}
