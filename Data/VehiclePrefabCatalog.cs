using System;
using System.Collections.Generic;
using System.Reflection;

namespace ImprovedPublicTransport.Data
{
    internal static class VehiclePrefabCatalog
    {
        private static readonly Dictionary<Type, FieldInfo> TransportInfoFields = new Dictionary<Type, FieldInfo>();

        internal static HashSet<VehicleInfo> FindComponents(IList<VehicleInfo> prefabs)
        {
            if (prefabs == null)
            {
                throw new ArgumentNullException("prefabs");
            }

            var incoming = new HashSet<VehicleInfo>();
            for (var i = 0; i < prefabs.Count; i++)
            {
                var prefab = prefabs[i];
                if (ReferenceEquals(prefab, null) || prefab.m_trailers == null)
                {
                    continue;
                }

                for (var trailerIndex = 0; trailerIndex < prefab.m_trailers.Length; trailerIndex++)
                {
                    var trailer = prefab.m_trailers[trailerIndex].m_info;
                    if (!ReferenceEquals(trailer, null) && !ReferenceEquals(trailer, prefab))
                    {
                        incoming.Add(trailer);
                    }
                }
            }

            return incoming;
        }

        internal static bool IsEligibleRoot(VehicleInfo prefab, HashSet<VehicleInfo> components)
        {
            return !ReferenceEquals(prefab, null) && !ReferenceEquals(prefab.m_class, null) &&
                   (components == null || !components.Contains(prefab) || HasHeadEvidence(prefab)) && MatchesTransport(prefab);
        }

        internal static bool IsCompatible(VehicleInfo prefab, TransportInfo lineInfo)
        {
            if (ReferenceEquals(prefab, null) || ReferenceEquals(prefab.m_class, null) ||
                ReferenceEquals(lineInfo, null) || ReferenceEquals(lineInfo.m_class, null))
            {
                return false;
            }

            var prefabInfo = GetTransportInfo(prefab.m_vehicleAI);
            return !ReferenceEquals(prefabInfo, null) &&
                   prefab.m_class.m_service == lineInfo.m_class.m_service &&
                   prefab.m_class.m_subService == lineInfo.m_class.m_subService &&
                   prefab.m_class.m_level == lineInfo.m_class.m_level &&
                   prefabInfo.m_transportType == lineInfo.m_transportType;
        }

        private static bool HasHeadEvidence(VehicleInfo prefab)
        {
            if (prefab.m_trailers == null)
            {
                return false;
            }

            for (var i = 0; i < prefab.m_trailers.Length; i++)
            {
                var component = prefab.m_trailers[i].m_info;
                if (ReferenceEquals(component, prefab) || References(component, prefab))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool References(VehicleInfo prefab, VehicleInfo target)
        {
            if (ReferenceEquals(prefab, null) || prefab.m_trailers == null)
            {
                return false;
            }

            for (var i = 0; i < prefab.m_trailers.Length; i++)
            {
                if (ReferenceEquals(prefab.m_trailers[i].m_info, target))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesTransport(VehicleInfo prefab)
        {
            var ai = prefab.m_vehicleAI;
            if (ReferenceEquals(ai, null))
            {
                return false;
            }

            var transportInfo = GetTransportInfo(ai);
            if (ReferenceEquals(transportInfo, null) || ReferenceEquals(transportInfo.m_class, null))
            {
                return false;
            }

            if (transportInfo.m_class.m_service != prefab.m_class.m_service ||
                transportInfo.m_class.m_subService != prefab.m_class.m_subService ||
                transportInfo.m_class.m_level != prefab.m_class.m_level)
            {
                return false;
            }

            if (transportInfo.m_vehicleType != VehicleInfo.VehicleType.None &&
                transportInfo.m_vehicleType != prefab.m_vehicleType)
            {
                return false;
            }

            return MatchesTriplet(prefab, transportInfo) && MatchesAi(ai, transportInfo.m_transportType);
        }

        private static bool MatchesTriplet(VehicleInfo prefab, TransportInfo info)
        {
            if (prefab.m_class.m_service == ItemClass.Service.Disaster)
            {
                return prefab.m_class.m_subService == ItemClass.SubService.None &&
                       prefab.m_class.m_level == ItemClass.Level.Level4 &&
                       info.m_transportType == TransportInfo.TransportType.EvacuationBus;
            }

            switch (info.m_transportType)
            {
                case TransportInfo.TransportType.Bus:
                    return prefab.m_class.m_subService == ItemClass.SubService.PublicTransportBus;
                case TransportInfo.TransportType.TouristBus:
                    return prefab.m_class.m_subService == ItemClass.SubService.PublicTransportTours;
                case TransportInfo.TransportType.Tram:
                    return prefab.m_class.m_subService == ItemClass.SubService.PublicTransportTram;
                case TransportInfo.TransportType.Trolleybus:
                    return prefab.m_class.m_subService == ItemClass.SubService.PublicTransportTrolleybus;
                case TransportInfo.TransportType.Metro:
                    return prefab.m_class.m_subService == ItemClass.SubService.PublicTransportMetro;
                case TransportInfo.TransportType.Train:
                    return prefab.m_class.m_subService == ItemClass.SubService.PublicTransportTrain;
                case TransportInfo.TransportType.Monorail:
                    return prefab.m_class.m_subService == ItemClass.SubService.PublicTransportMonorail;
                case TransportInfo.TransportType.CableCar:
                    return prefab.m_class.m_subService == ItemClass.SubService.PublicTransportCableCar;
                case TransportInfo.TransportType.Ship:
                    return prefab.m_class.m_subService == ItemClass.SubService.PublicTransportShip;
                case TransportInfo.TransportType.Airplane:
                case TransportInfo.TransportType.HotAirBalloon:
                case TransportInfo.TransportType.Helicopter:
                    return prefab.m_class.m_subService == ItemClass.SubService.PublicTransportPlane;
                case TransportInfo.TransportType.Taxi:
                    return prefab.m_class.m_subService == ItemClass.SubService.PublicTransportTaxi;
                default:
                    return false;
            }
        }

        private static TransportInfo GetTransportInfo(VehicleAI ai)
        {
            var type = ai.GetType();
            FieldInfo field;
            lock (TransportInfoFields)
            {
                if (!TransportInfoFields.TryGetValue(type, out field))
                {
                    for (var current = type; current != null && field == null; current = current.BaseType)
                    {
                        field = current.GetField("m_transportInfo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    }
                    TransportInfoFields[type] = field;
                }
            }

            try
            {
                return field?.GetValue(ai) as TransportInfo;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool MatchesAi(VehicleAI ai, TransportInfo.TransportType type)
        {
            if (ai is CargoTrainAI || ai is CargoShipAI || ai is CargoPlaneAI)
            {
                return false;
            }

            if (ai is BusAI)
            {
                return type == TransportInfo.TransportType.Bus || type == TransportInfo.TransportType.TouristBus ||
                       type == TransportInfo.TransportType.EvacuationBus;
            }
            if (ai is TramBaseAI)
            {
                return type == TransportInfo.TransportType.Tram;
            }
            if (ai is TrolleybusAI)
            {
                return type == TransportInfo.TransportType.Trolleybus;
            }
            if (ai is TrainAI)
            {
                return type == TransportInfo.TransportType.Train || type == TransportInfo.TransportType.Metro || type == TransportInfo.TransportType.Monorail;
            }
            if (ai is ShipAI)
            {
                return type == TransportInfo.TransportType.Ship;
            }
            if (ai is AircraftAI)
            {
                return type == TransportInfo.TransportType.Airplane || type == TransportInfo.TransportType.HotAirBalloon || type == TransportInfo.TransportType.Helicopter;
            }

            return true;
        }
    }
}
