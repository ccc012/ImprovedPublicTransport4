using System.Collections.Generic;
using System.Linq;
using ColossalFramework;
using ImprovedPublicTransport.Query;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.Util;
using JetBrains.Annotations;

namespace ImprovedPublicTransport.Command
{
    public static class SelectVehicleTypesCommand
    {
        public static void Execute([NotNull] IEnumerable<PrefabData> selectedVehicleInfos)
        {
            if (selectedVehicleInfos == null)
            {
                return;
            }

            var lineId = WorldInfoCurrentLineIDQuery.Query(out _);
            if (lineId == 0)
            {
                return;
            }

            var selectedItems = new HashSet<string>(
                selectedVehicleInfos
                    .Where(v => v.Info != null && !string.IsNullOrEmpty(v.Info.name))
                    .Select(v => v.Info.name)
                    .Distinct()
                    .ToArray());

            CachedTransportLineData.SetPrefabs(lineId, selectedItems.Count == 0 ? null : selectedItems);
            Singleton<SimulationManager>.instance.AddAction(() => ReplaceVehicles(lineId));
        }

        // Roughly based on TransportLine.ReplaceVehicles() — removes active vehicles whose
        // prefab is no longer in the line's allowed set so the spawn queue can refill correctly.
        private static void ReplaceVehicles(ushort lineID)
        {
            var prefabs = CachedTransportLineData.GetPrefabs(lineID);
            // null prefabs = any vehicle type allowed; nothing to cull.
            if (prefabs == null)
            {
                return;
            }

            var instance = Singleton<VehicleManager>.instance;
            var buffer = instance.m_vehicles.m_buffer;
            // Snapshot IDs first: RemoveVehicle mutates the buffer mid-iteration if we walk live.
            var toRemove = new List<ushort>(16);
            for (var i = 1; i < buffer.Length; ++i)
            {
                ref var vehicle = ref buffer[i];
                if (vehicle.m_flags == 0 || vehicle.m_transportLine != lineID)
                {
                    continue;
                }

                var info = vehicle.Info;
                if (info == null || prefabs.Contains(info.name))
                {
                    continue;
                }

                toRemove.Add((ushort)i);
            }

            for (var i = 0; i < toRemove.Count; i++)
            {
                TransportLineUtil.RemoveVehicle(lineID, toRemove[i], false);
            }
        }
    }
}
