using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.Util;
using UnityEngine;
using static ImprovedPublicTransport.ImprovedPublicTransportMod;

namespace ImprovedPublicTransport.HarmonyPatches.DepotAIPatches
{
    public class StartTransferPatch
    {
        private const string VehicleSelectorHarmonyID = "com.github.algernon-A.csl.vehicleselector";
        
        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(DepotAI), nameof(DepotAI.StartTransfer)),
                new PatchUtil.MethodDefinition(typeof(StartTransferPatch), nameof(StartTransferPre), before: new[] {VehicleSelectorHarmonyID}),
                null
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(DepotAI), nameof(DepotAI.StartTransfer))
            );
        }
        
        private static bool StartTransferPre(
            DepotAI __instance,
            ref ushort buildingID, ref Building data,
            TransferManager.TransferReason reason,
            TransferManager.TransferOffer offer)
        {
            var lineID = offer.TransportLine;
            var lines = TransportManager.instance.m_lines;
            if (lineID == 0 || lineID >= lines.m_size)
            {
                return true;
            }

            //TODO: fish boats?
            //TODO: also check reason? - see DepotAI
            var info = lines.m_buffer[lineID].Info;
            if (info?.m_class == null || info.m_class.m_service == ItemClass.Service.Disaster)
            {
                return true; //if it's not a proper transport line, let's not modify the behavior
            }

            if (!CachedTransportLineData._init)
            {
                return true; // cache not ready — leave vanilla spawn path alone
            }

            var depot = CachedTransportLineData.GetDepot(lineID);
            if (!DepotUtil.ValidateDepotAndFindNewIfNeeded(lineID, ref depot, info))
            {
                if (depot == 0)
                {
                    Debug.LogWarning($"{ShortModName}: No proper depot was found for line {lineID}!");
                    CachedTransportLineData.ClearEnqueuedVehicles(lineID);
                    return false;
                }

                Debug.LogWarning($"{ShortModName}: Invalid or no depot was selected for line {lineID}, resetting to : {depot}!");
                CachedTransportLineData.ClearEnqueuedVehicles(lineID);
                return false;
            }


            if (depot == buildingID)
            {
                if (SimHelper.SimulationTime < CachedTransportLineData.GetNextSpawnTime(lineID))
                {
                    return false; //if we need to wait before spawn, let's wait
                }

                if (!DepotUtil.CanAddVehicle(depot, ref BuildingManager.instance.m_buildings.m_buffer[depot], info))
                {
                    CachedTransportLineData.ClearEnqueuedVehicles(lineID);
                    return false;
                }

                CachedTransportLineData.SetNextSpawnTime(lineID, SimHelper.SimulationTime + ModSetting.Instance.SpawnTimeInterval);
            }
            else
            {
                // Validate target depot before redirecting to prevent infinite loops
                var targetInfo = depot != 0 ? BuildingManager.instance.m_buildings.m_buffer[depot].Info : null;
                if (targetInfo == null)
                {
                    Debug.LogWarning($"{ShortModName}: Invalid target depot {depot} for redirect from {buildingID}. Aborting redirect.");
                    CachedTransportLineData.ClearEnqueuedVehicles(lineID);
                    return false;
                }

                // Redirect through the TARGET building's own AI, not __instance. __instance is the
                // AI of the building vanilla originally offered the transfer to, and the two are
                // frequently different subclasses - notably TransportStationAI (bus/train stations,
                // which act as their own depot) derives from DepotAI, so a station and a plain depot
                // can each end up on either side of this redirect. Driving one building's transfer
                // through the other's AI mixes up m_transportInfo / m_vehicleAI / capacity, which
                // spawns vehicles that do not match the line and get culled again almost
                // immediately - the "buses appear at the station, move a little and vanish" report.
                var targetAi = targetInfo.m_buildingAI as DepotAI;
                if (targetAi == null)
                {
                    Debug.LogWarning($"{ShortModName}: Target depot {depot} for line {lineID} is not a depot ({targetInfo.m_buildingAI?.GetType().Name ?? "null AI"}). Aborting redirect.");
                    CachedTransportLineData.ClearEnqueuedVehicles(lineID);
                    return false;
                }

                if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs)
                {
                    Debug.Log($"{ShortModName}: Redirecting from {buildingID} to {depot}");
                }

                targetAi.StartTransfer(depot, ref BuildingManager.instance.m_buildings.m_buffer[depot], reason,
                    offer);
                return false;
            }

            return true;
        }
    }
}
