using System;
using HarmonyLib;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.Util;
using UnityEngine;

namespace ImprovedPublicTransport.HarmonyPatches.XYZVehicleAIPatches
{
    public class LoadPassengersPatch
    {
        private const string LoadPassengersMethod = "LoadPassengers";

        /// <summary>
        /// Set by BetterBoarding when it already wrote BoardPassengers / PassengersIn so this
        /// postfix does not double-count the same boarding event.
        /// </summary>
        [ThreadStatic]
        public static bool SkipPostAccounting;

        public static void Apply()
        {
            PatchLoadPassengers(typeof(BusAI));
            PatchLoadPassengers(typeof(TrolleybusAI));
            PatchLoadPassengers(typeof(TramAI));
            PatchLoadPassengers(typeof(CableCarAI));
            PatchLoadPassengers(typeof(PassengerTrainAI));
            PatchLoadPassengers(typeof(PassengerPlaneAI));
            PatchLoadPassengers(typeof(PassengerHelicopterAI));
            PatchLoadPassengers(typeof(PassengerBlimpAI));
            PatchLoadPassengers(typeof(PassengerFerryAI));
            PatchLoadPassengers(typeof(PassengerShipAI));
        }

        public static void Undo()
        {
            UnpatchLoadPassengers(typeof(BusAI));
            UnpatchLoadPassengers(typeof(TrolleybusAI));
            UnpatchLoadPassengers(typeof(TramAI));
            UnpatchLoadPassengers(typeof(CableCarAI));
            UnpatchLoadPassengers(typeof(PassengerTrainAI));
            UnpatchLoadPassengers(typeof(PassengerPlaneAI));
            UnpatchLoadPassengers(typeof(PassengerHelicopterAI));
            UnpatchLoadPassengers(typeof(PassengerBlimpAI));
            UnpatchLoadPassengers(typeof(PassengerFerryAI));
            UnpatchLoadPassengers(typeof(PassengerShipAI));
        }


        public static bool LoadPassengersPre(ushort vehicleID, ushort currentStop, out State __state)
        {
            if (vehicleID == 0 || CachedVehicleData.m_cachedVehicleData == null)
            {
                __state = new State { vehicleID = 0 };
                return true;
            }

            var data = VehicleManager.instance.m_vehicles.m_buffer[vehicleID];
            if (data.m_leadingVehicle != 0)
            {
                __state = new State { vehicleID = 0 };  // Mark as trailer with 0
                return true;
            }

            __state = new State
            {
                vehicleID = vehicleID,
                currentStop = currentStop,
                currentPassengers = VehicleUtil.GetTotalPassengerCount(vehicleID, CachedVehicleData.MaxVehicleCount)
            };
            return true;
        }

        public static void LoadPassengersPost(State __state)
        {
            if (SkipPostAccounting)
            {
                SkipPostAccounting = false;
                return;
            }

            // Skip if this was a trailer (vehicleID == 0 from pre-function's marker)
            if (__state.vehicleID == 0)
            {
                return;
            }

            var vehicleCache = CachedVehicleData.m_cachedVehicleData;
            var nodeCache = CachedNodeData.m_cachedNodeData;
            if (vehicleCache == null || __state.vehicleID >= vehicleCache.Length)
            {
                return;
            }

            var data = VehicleManager.instance.m_vehicles.m_buffer[__state.vehicleID];
            if (data.m_leadingVehicle != 0)
            {
                return;
            }

            var currentPassengers =
                VehicleUtil.GetTotalPassengerCount(__state.vehicleID, CachedVehicleData.MaxVehicleCount);
            var passengersIn = Mathf.Max(0, currentPassengers - __state.currentPassengers);
            if (passengersIn <= 0)
            {
                return;
            }

            vehicleCache[__state.vehicleID]
                .BoardPassengers(passengersIn, VehicleUtil.GetTicketPrice(__state.vehicleID), __state.currentStop);
            if (nodeCache != null && __state.currentStop != 0 && __state.currentStop < nodeCache.Length)
            {
                nodeCache[__state.currentStop].PassengersIn += passengersIn;
            }
        }

        public struct State
        {
            public ushort vehicleID;
            public ushort currentPassengers;
            public ushort currentStop;
        }

        private static void PatchLoadPassengers(Type type)
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(type, LoadPassengersMethod),
                new PatchUtil.MethodDefinition(typeof(LoadPassengersPatch), nameof(LoadPassengersPre), priority: Priority.Normal),
                new PatchUtil.MethodDefinition(typeof(LoadPassengersPatch), nameof(LoadPassengersPost), priority: Priority.Normal)
            );
        }

        private static void UnpatchLoadPassengers(Type type)
        {
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(type, LoadPassengersMethod));
        }
    }
}