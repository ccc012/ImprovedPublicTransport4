using System.Collections.Generic;
using ImprovedPublicTransport.Data;
using JetBrains.Annotations;

namespace ImprovedPublicTransport.Query
{
    public static class QueuedVehicleQuery
    {
        [NotNull]
        public static List<PrefabData> Query(ushort lineID, ItemClassTriplet classTriplet)
        {
            var result = new List<PrefabData>();
            var enqueuedVehicles = CachedTransportLineData.GetEnqueuedVehicles(lineID);
            if (enqueuedVehicles == null || enqueuedVehicles.Length == 0) return result;
            if (VehiclePrefabs.instance == null) return result;

            var prefabs = VehiclePrefabs.instance.GetPrefabs(classTriplet.Service, classTriplet.SubService, classTriplet.Level);
            if (prefabs == null || prefabs.Length == 0) return result;
            // Build a name-lookup dictionary to turn O(n*m) into O(n+m)
            var prefabByName = new Dictionary<string, PrefabData>(prefabs.Length);
            foreach (var data in prefabs)
                prefabByName[data.Name] = data;

            foreach (var str in enqueuedVehicles)
            {
                if (prefabByName.TryGetValue(str, out var found))
                    result.Add(found);
            }

            return result;
        }
    }
}