using HarmonyLib;
using System;
using System.Reflection;
using JetBrains.Annotations;

namespace ExpressBusServices
{
    [HarmonyPatch]
    [UsedImplicitly]
    public class Patch_BusStartPathFind
    {
        [UsedImplicitly]
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("BusAI:StartPathFind", new Type[] { typeof(ushort), typeof(Vehicle).MakeByRefType() });
        }

        /// <summary>
        /// Apply redeploy target when valid, and block vanilla StartPathFind when the target
        /// would IndexOutOfRange the node/building buffer (simulation popup spam).
        /// </summary>
        [HarmonyPrefix]
        [UsedImplicitly]
        public static bool AdjustPathfindTargetForRedeployment(ushort vehicleID, ref Vehicle vehicleData, ref bool __result)
        {
            try
            {
                if (ServiceBalancerUtil.ReadRedeploymentInstructions(vehicleID, out ushort redeploymentTarget))
                {
                    if (TransportStopSafety.IsLiveStopNode(redeploymentTarget))
                    {
                        vehicleData.m_targetBuilding = redeploymentTarget;
                        // Consume once pathfind is about to run with this target. Leaving the
                        // instruction sticky made every later StartPathFind re-pin the same stop
                        // → reverse-then-continue thrash after the bus already left that stop.
                        ServiceBalancerUtil.ReadRedeploymentInstructions(vehicleID, out _, removeEntry: true);
                    }
                    else
                    {
                        // Stale/corrupt redeploy target — drop it so it cannot poison pathfind.
                        ServiceBalancerUtil.ForgetVehicle(vehicleID);
                    }
                }

                if (!TransportStopSafety.IsSafePathfindTarget(ref vehicleData))
                {
                    __result = false;
                    return false;
                }

                return true;
            }
            catch
            {
                __result = false;
                return false;
            }
        }
    }
}
