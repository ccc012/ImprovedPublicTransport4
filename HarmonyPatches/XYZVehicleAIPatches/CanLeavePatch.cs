using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColossalFramework;
using HarmonyLib;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.Util;
using UnityEngine;
using static ImprovedPublicTransport.ImprovedPublicTransportMod;

namespace ImprovedPublicTransport.HarmonyPatches.XYZVehicleAIPatches
{
    public static class CanLeavePatch
    {
        private const string CanLeaveMethod = "CanLeave";
        // Smuggles per-call context from Prefix into the transpiler-injected wrapper call within
        // the same CanLeave invocation. [ThreadStatic] costs nothing here (vehicle AI simulation
        // runs on a single thread) but removes any risk if that ever changes.
        [ThreadStatic]
        private static ushort currentVehicleID;
        [ThreadStatic]
        private static ushort currentStop;
        
        
        public static void Apply()
        {
            PatchCanLeave(typeof(BusAI));
            PatchCanLeave(typeof(TrolleybusAI));
            PatchCanLeave(typeof(TramAI));
            PatchCanLeave(typeof(PassengerTrainAI));
            PatchCanLeave(typeof(PassengerPlaneAI));
            PatchCanLeave(typeof(PassengerHelicopterAI));
            PatchCanLeave(typeof(PassengerBlimpAI));
            PatchCanLeave(typeof(PassengerFerryAI));
            PatchCanLeave(typeof(PassengerShipAI));
        }

        public static void Undo()
        {
            UnpatchCanLeave(typeof(BusAI));
            UnpatchCanLeave(typeof(TrolleybusAI));
            UnpatchCanLeave(typeof(TramAI));
            UnpatchCanLeave(typeof(PassengerTrainAI));
            UnpatchCanLeave(typeof(PassengerPlaneAI));
            UnpatchCanLeave(typeof(PassengerHelicopterAI));
            UnpatchCanLeave(typeof(PassengerBlimpAI));
            UnpatchCanLeave(typeof(PassengerFerryAI));
            UnpatchCanLeave(typeof(PassengerShipAI));
        }
        
        private static void PatchCanLeave(Type type)
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(type, CanLeaveMethod),
                new PatchUtil.MethodDefinition(typeof(CanLeavePatch), nameof(Prefix), priority: Priority.Normal),
                new PatchUtil.MethodDefinition(typeof(CanLeavePatch), nameof(Postfix), priority: Priority.Normal),
                new PatchUtil.MethodDefinition(typeof(CanLeavePatch), nameof(Transpile), priority: Priority.Normal)
            );
        }

        private static void UnpatchCanLeave(Type type)
        {
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(type, CanLeaveMethod));
        }
        
        public static void Prefix(ushort vehicleID, ref Vehicle vehicleData)
        {
            currentVehicleID = vehicleID;
            currentStop = 0;

            // Prefer the live stop the vehicle is leaving (target building). Cached CurrentStop
            // was only set on passenger exchange and went stale on empty stop visits, so unbunching
            // used the wrong stop flag or was skipped entirely.
            if (vehicleData.m_targetBuilding != 0)
            {
                currentStop = vehicleData.m_targetBuilding;
            }

            var cache = CachedVehicleData.m_cachedVehicleData;
            if (cache != null && vehicleID != 0 && vehicleID < cache.Length)
            {
                if (currentStop == 0)
                {
                    currentStop = cache[vehicleID].CurrentStop;
                }
            }
        }


        public static void Postfix(ushort vehicleID, ref bool __result)
        {
            var cache = CachedVehicleData.m_cachedVehicleData;
            if (__result && cache != null && vehicleID != 0 && vehicleID < cache.Length)
            {
                // we don't know whether it was unbunching or just a traffic light or something else
                cache[vehicleID].IsUnbunchingInProgress = false;
            }

            currentVehicleID = 0;
            currentStop = 0;
        }
        
        public static IEnumerable<CodeInstruction> Transpile(MethodBase original,
            IEnumerable<CodeInstruction> instructions)
        {
            if (Diagnostics.VerboseTranspileLogs)
            {
                Debug.Log($"{ShortModName}: CanLeavePatch - Transpiling method: {original.DeclaringType}.{original}");
            }
            var replaced = false;
            foreach (var codeInstruction in instructions)
            {
                if (replaced || SkipInstruction(codeInstruction))
                {
                    yield return codeInstruction;
                    continue;
                }

                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(CanLeavePatch), nameof(TransportLineCanLeaveStopWrapper)))
                {
                    labels = codeInstruction.labels
                };
                if (Diagnostics.VerboseTranspileLogs)
                {
                    Debug.Log($"{ShortModName}: CanLeavePatch - Replaced a call to CanLeaveStop");
                }
                replaced = true;
            }
        }

        private static bool SkipInstruction(CodeInstruction codeInstruction)
        {
            return codeInstruction.opcode != OpCodes.Call || codeInstruction.operand  == null || !codeInstruction.operand.ToString().Contains("CanLeaveStop");
        }

        public static bool TransportLineCanLeaveStopWrapper(ref TransportLine transportLine, ushort nextStop, int waitTime) //the args are for the same signature
        {
            if (!ModSetting.Instance.EnableUnbunching || currentVehicleID == 0)
            {
                return true;
            }

            // no unbunching for evac buses or if only 1 bus on line!
            var vehicleData = VehicleManager.instance.m_vehicles.m_buffer[currentVehicleID];
            if (vehicleData.Info?.m_class?.m_service == ItemClass.Service.Disaster ||
                vehicleData.m_nextLineVehicle == 0 && Singleton<TransportManager>.instance.m_lines
                    .m_buffer[vehicleData.m_transportLine].m_vehicles == currentVehicleID)
            {
                return true;
            }

            var nodeCache = CachedNodeData.m_cachedNodeData;
            var vehicleCache = CachedVehicleData.m_cachedVehicleData;
            if (currentStop == 0
                || nodeCache == null
                || currentStop >= nodeCache.Length
                || !nodeCache[currentStop].Unbunching
                || !CachedTransportLineData.GetUnbunchingState(vehicleData.m_transportLine))
            {
                return true;
            }

            // ignore the provided waitTime as it's divided.
            // Don't divide m_waitCounter by 2^4! Because the call goes to our CanLeaveStop patch too where it expects a non-divided value..
            var canLeaveStop = Singleton<TransportManager>.instance.m_lines
                .m_buffer[vehicleData.m_transportLine]
                .CanLeaveStop(vehicleData.m_targetBuilding, vehicleData.m_waitCounter);
            if (vehicleCache != null && currentVehicleID < vehicleCache.Length)
            {
                vehicleCache[currentVehicleID].IsUnbunchingInProgress = !canLeaveStop;
            }

            return canLeaveStop;
        }
    }
}