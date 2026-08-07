using System.Collections.Generic;
using ICities;
using ImprovedPublicTransport.Util;

namespace ImprovedPublicTransport
{
    public class BuildingExtension : BuildingExtensionBase
    {
        public static event BuildingExtension.DepotAdded OnDepotAdded;
        public static event BuildingExtension.DepotRemoved OnDepotRemoved;

        private static Dictionary<ItemClassTriplet, HashSet<ushort>> _depotMap;
        private static readonly ushort[] _depotBuffer = new ushort[128];

        public static void Init()
        {
            _depotMap = new Dictionary<ItemClassTriplet, HashSet<ushort>>();
            int bufferLength = BuildingManager.instance.m_buildings.m_buffer.Length;
            for (int index = 0; index < bufferLength; ++index)
            {
                ObserveBuilding((ushort)index);
            }
        }

        public static void Deinit()
        {
            _depotMap = null;
            OnDepotAdded = null;
            OnDepotRemoved = null;
        }

        public override void OnBuildingCreated(ushort id)
        {
            base.OnBuildingCreated(id);
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }
            ObserveBuilding(id);
        }

        public override void OnBuildingReleased(ushort id)
        {
            base.OnBuildingReleased(id);
            if (!ImprovedPublicTransportMod.InGame || _depotMap == null)
            {
                return;
            }

            foreach (var depots in _depotMap)
            {
                if (!depots.Value.Remove(id))
                {
                    continue;
                }
                OnDepotRemoved?.Invoke(depots.Key.Service, depots.Key.SubService, depots.Key.Level);
            }
        }

        private static void ObserveBuilding(ushort buildingId)
        {
            if (_depotMap == null)
            {
                return;
            }

            DepotUtil.GetStats(ref BuildingManager.instance.m_buildings.m_buffer[buildingId],
                out TransportInfo primaryInfo, out TransportInfo secondaryInfo);

            ObserveForInfo(buildingId, primaryInfo);
            ObserveForInfo(buildingId, secondaryInfo);
        }

        private static void ObserveForInfo(ushort buildingId, TransportInfo transportInfo)
        {
            if (_depotMap == null || transportInfo == null || buildingId >= BuildingManager.instance.m_buildings.m_buffer.Length || !DepotUtil.IsValidDepot(buildingId, transportInfo))
            {
                return;
            }
            var itemClassTriplet = new ItemClassTriplet(transportInfo.GetService(), transportInfo.GetSubService(),
                transportInfo.GetClassLevel());
            if (!_depotMap.TryGetValue(itemClassTriplet, out HashSet<ushort> depots))
            {
                depots = new HashSet<ushort>();
                _depotMap.Add(itemClassTriplet, depots);
            }
            if (depots.Contains(buildingId))
            {
                return;
            }
            depots.Add(buildingId);
            OnDepotAdded?.Invoke(itemClassTriplet.Service, itemClassTriplet.SubService, itemClassTriplet.Level);
        }

        public static ushort[] GetDepots(TransportInfo transportInfo)
        {
            if (transportInfo == null || _depotMap == null)
            {
                return new ushort[0];
            }

            if (!_depotMap.TryGetValue(
                    new ItemClassTriplet(transportInfo.GetService(), transportInfo.GetSubService(),
                        transportInfo.GetClassLevel()),
                    out HashSet<ushort> source)
                || source == null
                || source.Count == 0)
            {
                return new ushort[0];
            }

            // Manual filter — LINQ Where/ToArray allocated every GetClosestDepot / StartTransfer path.
            var scratch = new ushort[source.Count];
            var n = 0;
            foreach (var d in source)
            {
                if (DepotUtil.IsValidDepot(d, transportInfo))
                {
                    scratch[n++] = d;
                }
            }

            if (n == 0)
            {
                return new ushort[0];
            }

            if (n == scratch.Length)
            {
                return scratch;
            }

            var result = new ushort[n];
            System.Array.Copy(scratch, result, n);
            return result;
            //we validate here to be compatible with MOM (if MOM sets max vehicle count later than this mod loads)
        }

        public delegate void DepotAdded(ItemClass.Service service, ItemClass.SubService subService,
            ItemClass.Level level);

        public delegate void DepotRemoved(ItemClass.Service service, ItemClass.SubService subService,
            ItemClass.Level level);
    }
}
