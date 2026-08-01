using ExpressBusServices.DataTypes;
using HarmonyLib;

namespace ExpressBusServices.PerformanceBoost
{
    [HarmonyPatch(typeof(Vehicle))]
    [HarmonyPatch("Unspawn", MethodType.Normal)]
    public class VehicleUnspawnListenerPatch
    {
        [HarmonyPostfix]
        public static void OnUnspawn(ushort vehicleID)
        {
            // Vehicle IDs are recycled after despawn - leave any per-vehicle table entry behind
            // and the next vehicle that reuses the slot inherits skip/pax/redeploy state.
            CachedVehicleProperties.UnsetCache(vehicleID);
            VehiclePaxDeltaInfo.Remove(vehicleID);
            BusStopSkippingLookupTable.ForgetBus(vehicleID);
            ServiceBalancerUtil.ForgetVehicle(vehicleID);
            Patch_VehicleAI_SimulationStep.ForgetVehicle(vehicleID);
            ImprovedPublicTransport.HarmonyPatches.VehicleAIPatches.EmptyBeforeDepotPatch.ForgetVehicle(vehicleID);
        }
    }
}
