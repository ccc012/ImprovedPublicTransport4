using HarmonyLib;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.HarmonyPatches.XYZVehicleAIPatches;
using ImprovedPublicTransport.Util;

namespace BetterBoarding
{
    [HarmonyPatch(typeof(BusAI))]
    [HarmonyPatch("LoadPassengers", MethodType.Normal)]
    // need to execute after our other mod, Express Bus Services
    [HarmonyAfter(new string[] { PatchController.ExpressBusServicesHarmonyID })]
    public class LoadPassengers_BusAI
    {
        [HarmonyPrefix]
        public static bool LoadPassengersBetter(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            int passengersBoarded = BoardingUtility.HandleBetterBoarding(vehicleID, ref data, currentStop, nextStop);
            if (passengersBoarded < 0)
            {
                // Fall back to game's default LoadPassengers for single-vehicle transports
                return true;
            }
            
            // Track boarding in IPT3's passenger data
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

            // IPT LoadPassengersPost also runs after this prefix — skip its delta accounting.
            LoadPassengersPatch.SkipPostAccounting = true;
            return false;
        }
    }
}
