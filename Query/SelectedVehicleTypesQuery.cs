using System.Collections.Generic;
using System.Linq;
using ImprovedPublicTransport.Data;
using JetBrains.Annotations;

namespace ImprovedPublicTransport.Query
{
    public static class SelectedVehicleTypesQuery
    {

        [NotNull]
        public static List<PrefabData> Query(ushort lineID)
        {
            var prefabs = CachedTransportLineData.GetPrefabs(lineID);
            if (prefabs == null || VehiclePrefabs.instance == null)
            {
                return new List<PrefabData>();
            }
            return prefabs.Select(name => VehiclePrefabs.instance.FindByName(name)).Where(p => p != null)
                .ToList();
        }
    }
}