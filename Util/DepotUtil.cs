using System;
using ColossalFramework;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.ReverseDetours;
using UnityEngine;
using static ImprovedPublicTransport.ImprovedPublicTransportMod;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.Util
{
    public static class DepotUtil
    {
        public static void GetStats(ref Building building,
            out TransportInfo primaryInfo, out TransportInfo secondaryInfo)
        {
            var depotAi = building.Info?.m_buildingAI as DepotAI;
            if (depotAi == null || (depotAi.m_transportInfo == null && depotAi.m_secondaryTransportInfo == null))
            {
                var shelterAi = building.Info?.m_buildingAI as ShelterAI;
                if (shelterAi == null || shelterAi.m_transportInfo == null)
                {
                    primaryInfo = null;
                    secondaryInfo = null;
                }
                else
                {
                    primaryInfo = shelterAi.m_transportInfo;
                    secondaryInfo = null;
                }
            }
            else
            {
                primaryInfo = depotAi.m_transportInfo;
                secondaryInfo = depotAi.m_secondaryTransportInfo;
            }
        }

        /// <summary>
        /// Whether a line's TransportInfo is served by a depot slot's TransportInfo.
        /// Exact vehicle-type match for the common case; ferry/blimp/heli also accept the
        /// DLC-shared aliases (Ferry↔Ship depot, Blimp↔Airplane hangar with Blimp vehicle, etc.).
        /// </summary>
        public static bool MatchesTransportSlot(TransportInfo lineInfo, TransportInfo depotSlotInfo)
        {
            if (lineInfo == null || depotSlotInfo == null)
            {
                return false;
            }

            if (lineInfo.m_vehicleType != VehicleInfo.VehicleType.None
                && lineInfo.m_vehicleType == depotSlotInfo.m_vehicleType)
            {
                return true;
            }

            if (IsFerryLike(lineInfo) && IsFerryLike(depotSlotInfo))
            {
                return true;
            }

            if (IsBlimpLike(lineInfo) && IsBlimpLike(depotSlotInfo))
            {
                return true;
            }

            if (IsHelicopterLike(lineInfo) && IsHelicopterLike(depotSlotInfo))
            {
                return true;
            }

            if (lineInfo.m_transportType == TransportInfo.TransportType.Trolleybus
                && depotSlotInfo.m_transportType == TransportInfo.TransportType.Trolleybus)
            {
                return true;
            }

            return false;
        }

        public static bool IsFerryLike(TransportInfo info)
        {
            if (info == null)
            {
                return false;
            }

            if (info.m_vehicleType == VehicleInfo.VehicleType.Ferry)
            {
                return true;
            }

            // Some ferry assets keep Ship (or another) transport type but are named Ferry.
            return !string.IsNullOrEmpty(info.name)
                   && info.name.IndexOf("Ferry", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsBlimpLike(TransportInfo info)
        {
            if (info == null)
            {
                return false;
            }

            if (info.m_vehicleType == VehicleInfo.VehicleType.Blimp)
            {
                return true;
            }

            if (info.m_transportType == TransportInfo.TransportType.HotAirBalloon)
            {
                return true;
            }

            // Vanilla blimp lines often use Airplane transport type + Blimp vehicle type.
            if (info.m_transportType == TransportInfo.TransportType.Airplane
                && info.m_vehicleType == VehicleInfo.VehicleType.Blimp)
            {
                return true;
            }

            return !string.IsNullOrEmpty(info.name)
                   && info.name.IndexOf("Blimp", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsHelicopterLike(TransportInfo info)
        {
            if (info == null)
            {
                return false;
            }

            return info.m_vehicleType == VehicleInfo.VehicleType.Helicopter
                   || info.m_transportType == TransportInfo.TransportType.Helicopter
                   || (!string.IsNullOrEmpty(info.name)
                       && info.name.IndexOf("Helicopter", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Resolves which depot vehicle-count field applies for <paramref name="transportInfo"/>
        /// (primary <c>m_maxVehicleCount</c> vs secondary <c>m_maxVehicleCount2</c>).
        /// </summary>
        public static bool TryResolveDepotMaxVehicles(
            DepotAI buildingAi,
            TransportInfo transportInfo,
            out int maxVehicles)
        {
            maxVehicles = 0;
            if (buildingAi == null || transportInfo == null)
            {
                return false;
            }

            if (MatchesTransportSlot(transportInfo, buildingAi.m_transportInfo))
            {
                maxVehicles = buildingAi.m_maxVehicleCount;
                return true;
            }

            if (buildingAi.m_secondaryTransportInfo != null
                && MatchesTransportSlot(transportInfo, buildingAi.m_secondaryTransportInfo)
                && transportInfo.m_vehicleType != VehicleInfo.VehicleType.None)
            {
                maxVehicles = buildingAi.m_maxVehicleCount2;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Budget-adjusted max vehicles for a depot slot (same formula vanilla/DepotAI uses).
        /// </summary>
        public static int GetBudgetAdjustedMax(DepotAI buildingAi, int maxVehicles)
        {
            if (buildingAi?.m_info?.m_class == null || maxVehicles <= 0)
            {
                return 0;
            }

            int budget = Singleton<EconomyManager>.instance.GetBudget(buildingAi.m_info.m_class);
            int productionRate = PlayerBuildingAI.GetProductionRate(100, budget);
            return (productionRate * maxVehicles + 99) / 100;
        }

        public static bool IsValidDepot(ushort depotID, TransportInfo transportInfo)
        {
            if (transportInfo == null || depotID == 0)
            {
                return false;
            }

            var building = BuildingManager.instance.m_buildings.m_buffer[depotID];
            if (building.Info?.m_class == null || (building.m_flags & Building.Flags.Created) == Building.Flags.None)
                return false;
            GetStats(ref building, out TransportInfo primaryInfo, out TransportInfo secondaryInfo);
            if (primaryInfo == null && secondaryInfo == null)
            {
                return false;
            }
            ItemClass.Service service;
            ItemClass.SubService subService;
            ItemClass.Level level;
            if (MatchesTransportSlot(transportInfo, primaryInfo))
            {
                service = primaryInfo.GetService();
                subService = primaryInfo.GetSubService();
                level = primaryInfo.GetClassLevel();
            }
            else if (MatchesTransportSlot(transportInfo, secondaryInfo)
                     && transportInfo.m_vehicleType != VehicleInfo.VehicleType.Car)
            {
                service = secondaryInfo.GetService();
                subService = secondaryInfo.GetSubService();
                level = secondaryInfo.GetClassLevel();
            }
            else
            {
                return false;
            }
            var depotAi = building.Info.m_buildingAI as DepotAI;
            if (depotAi != null)
            {
                var buildingAi = depotAi;
                if (buildingAi.m_maxVehicleCount == 0)
                {
                    return false;
                }
                if (service == ItemClass.Service.PublicTransport)
                {
                    if (level == ItemClass.Level.Level1)
                    {
                        switch (subService)
                        {
                            case ItemClass.SubService.PublicTransportBus:
                            case ItemClass.SubService.PublicTransportMetro:
                            case ItemClass.SubService.PublicTransportTrain:
                            case ItemClass.SubService.PublicTransportShip:
                            case ItemClass.SubService.PublicTransportPlane:
                            case ItemClass.SubService.PublicTransportTram:
                            case ItemClass.SubService.PublicTransportMonorail:
                            case ItemClass.SubService.PublicTransportTaxi:
                            case ItemClass.SubService.PublicTransportCableCar:
                            case ItemClass.SubService.PublicTransportTrolleybus:
                                return true;
                        }
                    }
                    else if (level == ItemClass.Level.Level2)
                    {
                        switch (subService)
                        {
                            case ItemClass.SubService.PublicTransportBus:
                            case ItemClass.SubService.PublicTransportShip:
                            case ItemClass.SubService.PublicTransportPlane:
                            case ItemClass.SubService.PublicTransportTrain:
                                return true;
                        }
                    }
                    else if (level == ItemClass.Level.Level3)
                    {
                        switch (subService)
                        {
                            case ItemClass.SubService.PublicTransportTours:
                            case ItemClass.SubService.PublicTransportPlane:
                                return true;
                        }
                    }
                }
            }
            else if (building.Info.m_buildingAI is ShelterAI)
            {
                if (service == ItemClass.Service.Disaster && subService == ItemClass.SubService.None &&
                    level == ItemClass.Level.Level4)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool ValidateDepotAndFindNewIfNeeded(ushort lineID, ref ushort depotID, TransportInfo transportInfo)
        {
            if (transportInfo == null)
            {
                return false;
            }
            if (depotID != 0 &&
                DepotUtil.IsValidDepot(depotID, transportInfo))
            {
                return true;
            }

            depotID = AutoAssignLineDepot(lineID, out _);
            return depotID != 0;
        }

        public static ushort AutoAssignLineDepot(ushort lineID, out Vector3 stopPosition)
        {
            stopPosition = Vector3.zero;
            ushort firstStop = TransportManager.instance.m_lines.m_buffer[lineID].GetStop(0);
            if (firstStop == 0)
            {
                return 0;
            }

            stopPosition = Singleton<NetManager>.instance.m_nodes.m_buffer[firstStop].m_position;
            ushort closestDepot = DepotUtil.GetClosestDepot(lineID, stopPosition);
            if ((int)closestDepot != 0)
            {
                CachedTransportLineData.SetDepot(lineID, closestDepot);
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log($"auto assigned depot {closestDepot} to line {lineID}");
                }
            }

            return closestDepot;
        }

        public static ushort GetClosestDepot(ushort lineID, Vector3 stopPosition)
        {
            ushort bestAvailableDepot = 0;
            float bestAvailableDistance = float.MaxValue;
            ushort bestFallbackDepot = 0;
            float bestFallbackDistance = float.MaxValue;

            var buildingManager = Singleton<BuildingManager>.instance;
            var info = Singleton<TransportManager>.instance.m_lines.m_buffer[lineID].Info;
            if (info == null)
            {
                return 0;
            }

            var depotIds = BuildingExtension.GetDepots(info);
            foreach (var depotId in depotIds)
            {
                ref Building depot = ref buildingManager.m_buildings.m_buffer[depotId];
                if (!IsValidDepot(depotId, info))
                {
                    continue;
                }

                float distance = Vector3.Distance(stopPosition, depot.m_position);
                if (distance < bestFallbackDistance)
                {
                    bestFallbackDepot = depotId;
                    bestFallbackDistance = distance;
                }

                if (!CanAddVehicle(depotId, ref depot, info))
                {
                    continue;
                }

                if (distance < bestAvailableDistance)
                {
                    bestAvailableDepot = depotId;
                    bestAvailableDistance = distance;
                }
            }

            return bestAvailableDepot != 0 ? bestAvailableDepot : bestFallbackDepot;
        }

        public static bool CanAddVehicle(ushort depotID, ref Building depot, TransportInfo transportInfo)
        {
            if (depotID == 0 || depot.Info == null || transportInfo == null)
            {
                return false;
            }
            if (depot.Info.m_buildingAI is DepotAI)
            {
                DepotAI buildingAi = depot.Info.m_buildingAI as DepotAI;
                // Use the vehicle counter that matches the line's transport slot. Dual-purpose
                // depots keep primary on m_maxVehicleCount and secondary on m_maxVehicleCount2;
                // always reading the primary counter left ferry/blimp-on-secondary (etc.) looking
                // uncapped even after DepotCapacityPatch wrote a realistic m_maxVehicleCount2.
                // Ferry/blimp/heli also match DLC-shared type aliases (see MatchesTransportSlot).
                if (!TryResolveDepotMaxVehicles(buildingAi, transportInfo, out int maxVehicles))
                {
                    return false;
                }

                int num = GetBudgetAdjustedMax(buildingAi, maxVehicles);
                return buildingAi.GetVehicleCount(depotID, ref depot) < num;
            }
            if (depot.Info.m_buildingAI is ShelterAI)
            {
                ShelterAI buildingAi = depot.Info.m_buildingAI as ShelterAI;
                int num = (PlayerBuildingAI.GetProductionRate(100, Singleton<EconomyManager>.instance.GetBudget(buildingAi.m_info.m_class)) * buildingAi.m_evacuationBusCount + 99) / 100;
                int count = 0;
                int cargo = 0;
                int capacity = 0;
                int outside = 0;
                CommonBuildingAIReverseDetour.CalculateOwnVehicles(buildingAi, depotID, ref depot, buildingAi.m_transportInfo.m_vehicleReason, ref count, ref cargo, ref capacity, ref outside);
                return count < num;
            }
            return false;
        }
    }
}
