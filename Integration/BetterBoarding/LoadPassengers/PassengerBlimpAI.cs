using HarmonyLib;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.HarmonyPatches.XYZVehicleAIPatches;
using ImprovedPublicTransport.Util;

namespace BetterBoarding
{
    [HarmonyPatch(typeof(PassengerBlimpAI))]
    [HarmonyPatch("LoadPassengers", MethodType.Normal)]
    // need to execute after our other mod, Express Bus Services
    [HarmonyAfter(new string[] { PatchController.ExpressBusServicesHarmonyID })]
    public class LoadPassengers_PassengerBlimpAI
    {
        [HarmonyPrefix]
        public static bool LoadPassengersBetter(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            int passengersBoarded = BoardingUtility.HandleBetterBoarding(vehicleID, ref data, currentStop, nextStop);
            if (passengersBoarded < 0)
            {
                return true;
            }
            
            if (passengersBoarded > 0 && vehicleID != 0 && currentStop != 0)
            {
                if (CachedVehicleData.m_cachedVehicleData != null && vehicleID < CachedVehicleData.m_cachedVehicleData.Length)
                {
                    CachedVehicleData.m_cachedVehicleData[vehicleID]
                        .BoardPassengers(passengersBoarded, VehicleUtil.GetTicketPrice(vehicleID), currentStop);
                }
                if (CachedNodeData.m_cachedNodeData != null && currentStop < CachedNodeData.m_cachedNodeData.Length)
                {
                    CachedNodeData.m_cachedNodeData[currentStop].PassengersIn += passengersBoarded;
                }
            }

            LoadPassengersPatch.SkipPostAccounting = true;
            return false;
        }
    }
}
