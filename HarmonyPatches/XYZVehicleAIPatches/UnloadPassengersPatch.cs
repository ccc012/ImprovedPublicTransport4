using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using HarmonyLib;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.Util;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

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
            PatchUnloadPassengers(typeof(CableCarAI));
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
            UnpatchUnloadPassengers(typeof(CableCarAI));
            UnpatchUnloadPassengers(typeof(PassengerTrainAI));
            UnpatchUnloadPassengers(typeof(PassengerPlaneAI));
            UnpatchUnloadPassengers(typeof(PassengerHelicopterAI));
            UnpatchUnloadPassengers(typeof(PassengerBlimpAI));
            UnpatchUnloadPassengers(typeof(PassengerFerryAI));
            UnpatchUnloadPassengers(typeof(PassengerShipAI));
        }

        // Field-name lookup, cached per concrete AI type, for the guard below. Several
        // "PassengerXAI" types (confirmed: PassengerFerryAI, PassengerShipAI - decompiled both)
        // each declare their own public TransportInfo m_transportInfo field (not inherited from a
        // shared base), so this is read by reflection once per type rather than hardcoding a cast
        // per type.
        private static readonly Dictionary<Type, FieldInfo> TransportInfoFields = new Dictionary<Type, FieldInfo>();

        public static bool UnloadPassengersPre(object __instance, ushort vehicleID, Vehicle data, ushort currentStop, out State __state)
        {
            // Guard against a real crash seen in the wild (Ferry03, vehicle still with
            // m_transportLine == 0): vanilla's own UnloadPassengers indexes
            // TransportManager.instance.m_passengers[(int)m_transportInfo.m_transportType] on the
            // "not currently on a line" branch. That throws IndexOutOfRangeException whenever
            // m_transportInfo's m_transportType does not correspond to a valid slot in that fixed-
            // size array - which happens for at least one real save with corrupted vehicle/asset
            // data (the same save this session already found a corrupted vehicle prefab name in).
            // This is vanilla's own code, not ours, and we cannot patch around the array itself -
            // but we CAN skip the call entirely when we can see in advance that it would crash,
            // rather than let it take the whole simulation step down.
            if (currentStop != 0 && data.m_transportLine == 0 && __instance != null)
            {
                var type = __instance.GetType();
                if (!TransportInfoFields.TryGetValue(type, out var field))
                {
                    field = type.GetField("m_transportInfo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    TransportInfoFields[type] = field;
                }

                var transportInfo = field?.GetValue(__instance) as TransportInfo;
                var passengersArray = Singleton<TransportManager>.instance?.m_passengers;
                var typeIndex = transportInfo != null ? (int)transportInfo.m_transportType : -1;
                if (transportInfo == null || passengersArray == null ||
                    typeIndex < 0 || typeIndex >= passengersArray.Length)
                {
                    Utils.LogError(
                        $"UnloadPassengersPatch: skipped {type.Name}.UnloadPassengers for vehicle {vehicleID} - "
                        + $"m_transportInfo would index out of range (transportInfo null={transportInfo == null}, "
                        + $"typeIndex={typeIndex}, arrayLength={passengersArray?.Length ?? -1}). "
                        + "Likely a corrupted vehicle/asset in this save, not an IPT4 bug.");
                    __state = new State { vehicleID = 0 };
                    return false;
                }
            }

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