using HarmonyLib;
using System;
using System.Reflection;
using JetBrains.Annotations;

namespace ExpressBusServices
{
    // set priority such that IPT2 can execute first; essentially this should execute last
    [HarmonyPatch]
    [HarmonyPriority(Priority.LowerThanNormal)]
    [UsedImplicitly]
    public class Patch_PublicTransportExtraSkip
    {
        private static readonly Type[] StartPathFindSig =
            { typeof(ushort), typeof(Vehicle).MakeByRefType() };

        private static readonly MethodInfo BusStartPathFind =
            AccessTools.Method(typeof(BusAI), "StartPathFind", StartPathFindSig);
        private static readonly MethodInfo BusUnload =
            AccessTools.Method(typeof(BusAI), "UnloadPassengers");
        private static readonly MethodInfo BusLoad =
            AccessTools.Method(typeof(BusAI), "LoadPassengers");
        private static readonly MethodInfo TrolleyStartPathFind =
            AccessTools.Method(typeof(TrolleybusAI), "StartPathFind", StartPathFindSig);
        private static readonly MethodInfo TrolleyUnload =
            AccessTools.Method(typeof(TrolleybusAI), "UnloadPassengers");
        private static readonly MethodInfo TrolleyLoad =
            AccessTools.Method(typeof(TrolleybusAI), "LoadPassengers");

        [HarmonyTargetMethod]
        [UsedImplicitly]
        public static MethodBase TargetRelevantMethod()
        {
            return AccessTools.Method(typeof(VehicleAI), "ArrivingToDestination");
        }

        [HarmonyPrepare]
        [UsedImplicitly]
        public static bool DetermineIfShouldPatch()
        {
            return true;
        }

        [HarmonyPrefix]
        [UsedImplicitly]
        public static bool ExtraSkippingLogic(VehicleAI __instance, ushort vehicleID, ref Vehicle vehicleData)
        {
            if (!(__instance is BusAI || __instance is TrolleybusAI))
            {
                // not bus or trolleybus; no
                return true;
            }
            if (ExtraSkippingIsDisallowed(__instance, vehicleID, ref vehicleData, out var currentStop))
            {
                // actually, cannot do extra skip, so we allow the original method to execute.
                return true;
            }

            // okay can do
            if (!TransportStopSafety.IsLiveStopNode(currentStop))
            {
                return true;
            }

            ushort nextStop = TransportLine.GetNextStop(currentStop);
            if (!TransportStopSafety.IsLiveStopNode(nextStop))
            {
                // Broken stop chain — fall back to vanilla arrive logic.
                return true;
            }

            vehicleData.m_targetBuilding = nextStop;
            BusStopSkippingLookupTable.Notify_BusShouldSkipLoading(vehicleID);
            var pathfindParams = new object[] { vehicleID, vehicleData };
            var unloadParams = new object[] { vehicleID, vehicleData, currentStop, nextStop };
            try
            {
                if (__instance is BusAI busAi)
                {
                    if (BusStartPathFind == null || BusUnload == null || BusLoad == null)
                    {
                        vehicleData.m_targetBuilding = currentStop;
                        return true;
                    }

                    if (!(bool)BusStartPathFind.Invoke(busAi, pathfindParams))
                    {
                        // something bad happened; cancel
                        vehicleData.m_targetBuilding = currentStop;
                        return true;
                    }

                    vehicleData = (Vehicle)pathfindParams[1];
                    BusUnload.Invoke(busAi, unloadParams);
                    BusLoad.Invoke(busAi, unloadParams);
                }
                else if (__instance is TrolleybusAI trolleyAi)
                {
                    if (TrolleyStartPathFind == null || TrolleyUnload == null || TrolleyLoad == null)
                    {
                        vehicleData.m_targetBuilding = currentStop;
                        return true;
                    }

                    if (!(bool)TrolleyStartPathFind.Invoke(trolleyAi, pathfindParams))
                    {
                        // something bad happened; cancel
                        vehicleData.m_targetBuilding = currentStop;
                        return true;
                    }

                    vehicleData = (Vehicle)pathfindParams[1];
                    TrolleyUnload.Invoke(trolleyAi, unloadParams);
                    TrolleyLoad.Invoke(trolleyAi, unloadParams);
                }
                else
                {
                    // we should have already filtered this...?
                    return true;
                }
            }
            catch
            {
                vehicleData.m_targetBuilding = currentStop;
                return true;
            }

            // Pathfind is async: path==0 + WaitingPath means the request is still in flight.
            // Clearing WaitingPath here cancelled the path we just started and forced a repath
            // (mid-street reverse then continue). Leave WaitingPath alone; do not drop the line.
            return false;
        }

        private static bool ExtraSkippingIsDisallowed(VehicleAI __instance, ushort vehicleID, ref Vehicle vehicleData, out ushort currentApproachingStop)
        {
            // note: we extract this to be its own method to narrow down the place that triggers the strange NullRefEx bug

            currentApproachingStop = 0;
            if ((int)EBSModConfig.CurrentExpressBusMode < (int)EBSModConfig.ExpressMode.AGGRESSIVE)
            {
                // settings not enabled; no
                return true;
            }

            currentApproachingStop = vehicleData.m_targetBuilding;
            if (currentApproachingStop == 0 || vehicleData.m_transportLine == 0)
            {
                // this can happen when e.g. the depot is forced to deactivate and the vehicles are therefore forced to return to base
                // in this case, don't do it
                return true;
            }

            if (!DepartureChecker.CanSkipNextStop(vehicleID, ref vehicleData))
            {
                // is arriving at terminus; dont do this!
                return true;
            }

            TransportLineUtil.CountPassengersWaiting(currentApproachingStop, out int residents, out int tourists);
            var unloadPredict = TransportLineUtil.GetQuantityPassengerUnloadOnNextStop(vehicleID, ref vehicleData, out bool full, out bool empty);
            if (unloadPredict > 0 || (!full && (residents + tourists) > 0))
            {
                // have someone dropping off OR have boardable passengers
                // dont do it
                return true;
            }

            // none of the checks disallowed it
            return false;
        }
    }
}
