using System.Collections.Generic;
using ColossalFramework;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.Util;
using JetBrains.Annotations;

namespace ImprovedPublicTransport.Query
{
    public static class ActiveVehiclesQuery
    {
        [NotNull]
        public static List<VehicleQueryResult> Query(ushort lineID, ItemClassTriplet classTriplet)
        {
            var results = new List<VehicleQueryResult>();
            if (lineID == 0 || VehiclePrefabs.instance == null)
            {
                return results;
            }

            var transportLine = Singleton<TransportManager>.instance.m_lines.m_buffer[lineID];
            var activeVehicleCount = TransportLineUtil.CountLineActiveVehicles(lineID, out _);
            var prefabs =
                VehiclePrefabs.instance.GetPrefabs(classTriplet.Service, classTriplet.SubService, classTriplet.Level);
            if (prefabs == null || prefabs.Length == 0)
            {
                return results;
            }

            // O(1) name lookup instead of nested linear scan over prefabs per vehicle.
            var prefabByName = new Dictionary<string, PrefabData>(prefabs.Length);
            foreach (var data in prefabs)
            {
                if (data?.Name != null)
                {
                    prefabByName[data.Name] = data;
                }
            }

            for (var index1 = 0; index1 < activeVehicleCount; ++index1)
            {
                var vehicle = transportLine.GetVehicle(index1);
                if (vehicle == 0)
                {
                    continue;
                }
                // Skip vehicles that are heading back to depot (vanilla SimulationStep filter).
                if ((VehicleManager.instance.m_vehicles.m_buffer[vehicle].m_flags & Vehicle.Flags.GoingBack) != 0)
                {
                    continue;
                }
                var info = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicle].Info;
                if (info?.name == null) continue;
                if (prefabByName.TryGetValue(info.name, out var matched))
                {
                    results.Add(new VehicleQueryResult { PrefabData = matched, VehicleID = vehicle });
                }
            }

            return results;
        }
        
        public class VehicleQueryResult
        {
            public ushort VehicleID;
            public PrefabData PrefabData;
        }
    }
}