using HarmonyLib;
using System;
using System.Reflection;
using JetBrains.Annotations;

namespace ExpressBusServices
{
    [HarmonyPatch]
    [UsedImplicitly]
    public class Patch_TrolleyBusStartPathFind
    {
        [UsedImplicitly]
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("TrolleybusAI:StartPathFind", new Type[] { typeof(ushort), typeof(Vehicle).MakeByRefType() });
        }

        /// <summary>
        /// Same guards as <see cref="Patch_BusStartPathFind"/> for trolleybuses.
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
                        // Consume once — sticky redeploy targets caused mid-street U-turn thrash.
                        ServiceBalancerUtil.ReadRedeploymentInstructions(vehicleID, out _, removeEntry: true);
                    }
                    else
                    {
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
