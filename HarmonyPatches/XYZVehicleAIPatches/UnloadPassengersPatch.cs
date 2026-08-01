using System;
using HarmonyLib;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.Util;
using UnityEngine;

namespace ImprovedPublicTransport.HarmonyPatches.XYZVehicleAIPatches
{
    public class UnloadPassengersPatch
    {
        private const string UnloadPassengersMethod = "UnloadPassengers";

        public static void Apply()
        {
            PatchUnloadPassengers(typeof(BusAI));
            PatchUnloadPassengers(typeof(TrolleybusAI));
            PatchUnloadPassengers(typeof(TramAI));
            PatchUnloadPassengers(typeof(PassengerTrainAI));
            PatchUnloadPassengers(typeof(PassengerPlaneAI));
            PatchUnloadPassengers(typeof(PassengerHelicopterAI));
            PatchUnloadPassengers(typeof(PassengerBlimpAI));
            PatchUnloadPassengers(typeof(PassengerFerryAI));
            PatchUnloadPassengers(typeof(PassengerShipAI));
        }

        public static void Undo()
        {
            UnpatchUnloadPassengers(typeof(BusAI));
            UnpatchUnloadPassengers(typeof(TrolleybusAI));
            UnpatchUnloadPassengers(typeof(TramAI));
            UnpatchUnloadPassengers(typeof(PassengerTrainAI));
            UnpatchUnloadPassengers(typeof(PassengerPlaneAI));
            UnpatchUnloadPassengers(typeof(PassengerHelicopterAI));
            UnpatchUnloadPassengers(typeof(PassengerBlimpAI));
            UnpatchUnloadPassengers(typeof(PassengerFerryAI));
            UnpatchUnloadPassengers(typeof(PassengerShipAI));
        }

        public static bool UnloadPassengersPre(ushort vehicleID, ushort currentStop, out State __state)
        {
            if (vehicleID == 0 || CachedVehicleData.m_cachedVehicleData == null ||
                VehicleManager.instance.m_vehicles.m_buffer[vehicleID].m_leadingVehicle != 0)
            {
                __state = new State { vehicleID = 0 };  // Mark as trailer / invalid with 0
                return true;
            }

            __state = new State()
            {
                vehicleID = vehicleID,
                currentStop = currentStop,
                currentPassengers = VehicleUtil.GetTotalPassengerCount(vehicleID, CachedVehicleData.MaxVehicleCount)
            };
            return true;
        }

        public static void UnloadPassengersPost(State __state)
        {
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

            if (VehicleManager.instance.m_vehicles.m_buffer[__state.vehicleID].m_leadingVehicle != 0)
            {
                return;
            }

            // Empty-before-depot: if this vehicle was waiting to go home, complete it now that unload ran.
            try
            {
                ref var v = ref VehicleManager.instance.m_vehicles.m_buffer[__state.vehicleID];
                VehicleAIPatches.EmptyBeforeDepotPatch.TryCompletePendingReturn(__state.vehicleID, ref v);
            }
            catch
            {
                // non-fatal
            }

            var currentPassengers =
                VehicleUtil.GetTotalPassengerCount(__state.vehicleID, CachedVehicleData.MaxVehicleCount);
            var passengersOut = Mathf.Max(0, __state.currentPassengers - currentPassengers);
            if (passengersOut <= 0)
            {
                return;
            }

            vehicleCache[__state.vehicleID]
                .DisembarkPassengers(passengersOut, __state.currentStop);
            if (nodeCache != null && __state.currentStop != 0 && __state.currentStop < nodeCache.Length)
            {
                nodeCache[__state.currentStop].PassengersOut += passengersOut;
            }
        }

        public struct State
        {
            public ushort vehicleID;
            public ushort currentPassengers;
            public ushort currentStop;
        }

        private static void PatchUnloadPassengers(Type type)
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(type, UnloadPassengersMethod),
                new PatchUtil.MethodDefinition(typeof(UnloadPassengersPatch), nameof(UnloadPassengersPre), priority: Priority.Normal),
                new PatchUtil.MethodDefinition(typeof(UnloadPassengersPatch), nameof(UnloadPassengersPost), priority: Priority.Normal)
            );
        }

        private static void UnpatchUnloadPassengers(Type type)
        {
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(type, UnloadPassengersMethod));
        }
    }
}