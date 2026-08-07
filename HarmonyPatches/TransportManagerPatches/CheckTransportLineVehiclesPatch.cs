using System.Runtime.CompilerServices;
using ColossalFramework;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.Util;

namespace ImprovedPublicTransport.HarmonyPatches.TransportManagerPatches
{
    public static class CheckTransportLineVehiclesPatch
    {
        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(TransportManager),
                    nameof(TransportManager.CheckTransportLineVehicles)),
                new PatchUtil.MethodDefinition(typeof(CheckTransportLineVehiclesPatch),
                    nameof(Prefix))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(TransportManager),
                    nameof(TransportManager.CheckTransportLineVehicles))
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Prefix(TransportManager __instance)
        {
            bool hasFilteredLine = false;
            for (ushort lineID = 0; lineID < __instance.m_lines.m_size; lineID++)
            {
                if (CachedTransportLineData.GetPrefabs(lineID)?.Count > 0)
                {
                    hasFilteredLine = true;
                    break;
                }
            }

            if (!hasFilteredLine)
            {
                return true;
            }

            for (ushort lineID = 0; lineID < __instance.m_lines.m_size; lineID++)
            {
                ref var line = ref __instance.m_lines.m_buffer[lineID];
                if (line.m_vehicles == 0 || CachedTransportLineData.GetPrefabs(lineID) != null)
                {
                    continue;
                }


                if (!__instance.TryGetSelectedLineVehicle(lineID, out var prefabIndex))
                {
                    continue;
                }

                var selected = PrefabCollection<VehicleInfo>.GetPrefab((uint)prefabIndex);
                var current = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[line.m_vehicles].Info;
                if (selected?.m_class != null && current?.m_class != null
                    && selected != current
                    && Singleton<BuildingManager>.instance.GetDepotLevels(lineID).Includes(selected.m_class.m_level))
                {
                    line.ReleaseLineVehicles();
                }
            }

            return false;
        }
    }
}
