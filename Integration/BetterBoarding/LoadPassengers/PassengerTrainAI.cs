using ColossalFramework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.HarmonyPatches.XYZVehicleAIPatches;
using ImprovedPublicTransport.Util;

namespace BetterBoarding
{
    [HarmonyPatch(typeof(PassengerTrainAI))]
    [HarmonyPatch("LoadPassengers", MethodType.Normal)]
    // Need to execute after IPT2; and IPT2 did not specify Priority => Priority = Normal
    [HarmonyPriority(Priority.LowerThanNormal)]
    public class LoadPassengers_PassengerTrainAI
    {
        [HarmonyPrefix]
        public static bool LoadPassengersBetter(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            int passengersBoarded = BoardingUtility.HandleBetterBoarding(vehicleID, ref data, currentStop, nextStop);
            if (passengersBoarded < 0)
            {
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

            LoadPassengersPatch.SkipPostAccounting = true;
            return false;
        }
    }
}
