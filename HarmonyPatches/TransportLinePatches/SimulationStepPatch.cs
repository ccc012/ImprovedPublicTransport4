using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ColossalFramework;
using HarmonyLib;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.Util;
using UnityEngine;
using static ImprovedPublicTransport.ImprovedPublicTransportMod;

namespace ImprovedPublicTransport.HarmonyPatches.TransportLinePatches
{
    public class SimulationStepPatch
    {
        // EconomyManager accepts an int per transaction. Keep each call well below the signed
        // limit so economy extensions that scale the amount cannot overflow it either.
        private const int MaxMaintenanceTransaction = 100_000_000;

        public static void Apply()
        {
            if (Diagnostics.VerboseTranspileLogs)
            {
                ImprovedPublicTransport.Util.Utils.Log($"{ShortModName}: +++ SimulationStepPatch: check existing patches for TransportLine.SimulationStep");
                PatchUtil.LogExistingPatches(typeof(TransportLine).GetMethod(nameof(TransportLine.SimulationStep), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic));
            }

            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(TransportLine), nameof(TransportLine.SimulationStep)),
                new PatchUtil.MethodDefinition(typeof(SimulationStepPatch), nameof(Prefix), priority: Priority.Normal),
                new PatchUtil.MethodDefinition(typeof(SimulationStepPatch), nameof(Postfix), priority: Priority.Normal),
                new PatchUtil.MethodDefinition(typeof(SimulationStepPatch), nameof(Transpile), priority: Priority.Normal)
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(TransportLine), nameof(TransportLine.SimulationStep))
            );
        }

        public static IEnumerable<CodeInstruction> Transpile(MethodBase original,
            IEnumerable<CodeInstruction> instructions)
        {
            if (Diagnostics.VerboseTranspileLogs)
            {
                Debug.Log($"{ShortModName}: Transpiling method: {original.DeclaringType}.{original}");
            }

            var codes = new List<CodeInstruction>(instructions);
            var newCodes = new List<CodeInstruction>();
            foreach (var codeInstruction in codes)
            {
                if (SkipInstruction(codeInstruction))
                {
                    newCodes.Add(codeInstruction);
                    continue;
                }

                if (codeInstruction.operand.ToString().Contains(nameof(EconomyManager.FetchResource)))
                {
                    if (Diagnostics.VerboseTranspileLogs)
                    {
                        Debug.Log($"{ShortModName}: Replacing call to FetchResourceStub()");
                    }
                    newCodes.Add(new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(SimulationStepPatch), nameof(FetchResourceStub))));
                    continue;
                }

                if (Diagnostics.VerboseTranspileLogs)
                {
                    Debug.Log($"{ShortModName}: Replacing call to CalculateTargetVehicleCount()");
                }
                var thisInstruction = newCodes[newCodes.Count - 1];
                newCodes.RemoveAt(newCodes.Count - 1);

                newCodes.Add(new CodeInstruction(OpCodes.Ldarg_1)
                {
                    labels = thisInstruction.labels //need to preserve the label
                });
                newCodes.Add(new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(SimulationStepPatch), nameof(CalculateTargetVehicleCount))));
            }

            return newCodes.AsEnumerable();
        }

        private static bool SkipInstruction(CodeInstruction codeInstruction)
        {
            return codeInstruction.operand == null ||
                   !(codeInstruction.opcode == OpCodes.Callvirt && codeInstruction.operand.ToString()
                       .Contains(nameof(EconomyManager.FetchResource))) &&
                   !(codeInstruction.opcode == OpCodes.Call && codeInstruction.operand.ToString()
                       .Contains(nameof(TransportLine.CalculateTargetVehicleCount)));
        }

        public static bool Prefix(ushort lineID, out ushort __state)
        {
            __state = lineID;
            return true;
        }

        public static void Postfix(ushort __state)
        {
            if (!CachedTransportLineData._init || !((SimulationManager.instance.m_currentFrameIndex & 4095U) >= 3840U) ||
                !TransportManager.instance.m_lines.m_buffer[__state].Complete)
            {
                return;
            }

            var nodeCache = CachedNodeData.m_cachedNodeData;
            var vehicleCache = CachedVehicleData.m_cachedVehicleData;
            if (nodeCache == null || vehicleCache == null)
            {
                return;
            }

            var stops1 = TransportManager.instance.m_lines.m_buffer[__state].m_stops;
            var stop1 = stops1;
            var nodeGuard = 0;
            do
            {
                if (stop1 != 0 && stop1 < nodeCache.Length)
                {
                    nodeCache[stop1].StartNewWeek();
                }

                stop1 = TransportLine.GetNextStop(stop1);
                if (++nodeGuard > 32768)
                {
                    ImprovedPublicTransport.Util.Utils.LogError(
                        $"SimulationStepPatch: stop loop guard tripped for line {__state}");
                    break;
                }
            } while (stops1 != stop1 && stop1 != 0);

            long amount = 0;
            var activeVehicles = TransportLineUtil.CountLineActiveVehicles(__state, out _, (num3) =>
            {
                if (num3 == 0 || num3 >= vehicleCache.Length)
                {
                    return;
                }

                var vInfo = VehicleManager.instance.m_vehicles.m_buffer[num3].Info;
                if (vInfo == null) return;
                if (VehiclePrefabs.instance == null) return;
                var prefabData = VehiclePrefabs.instance.FindByIndex(vInfo.m_prefabDataIndex);
                if (prefabData == null) return;
                var maintenanceCost = prefabData.MaintenanceCost;
                if (maintenanceCost < 0)
                {
                    ImprovedPublicTransport.Util.Utils.LogWarning(
                        $"SimulationStepPatch: ignored negative maintenance cost {maintenanceCost} " +
                        $"for vehicle prefab '{vInfo.name}'.");
                    maintenanceCost = 0;
                }

                amount += maintenanceCost;
                vehicleCache[num3].StartNewWeek(maintenanceCost);
            });
            if (amount > 0)
            {
                var line = TransportManager.instance.m_lines.m_buffer[__state];
                if (line.Info?.m_class == null)
                {
                    return;
                }

                // Never log per-line weekly maintenance — this runs for every PT line on the
                // week-rollover path and Debug.Log hitchs the main thread hard under Verbose.

                ChargeMaintenance(amount, line.Info.m_class);
            }
        }

        private static void ChargeMaintenance(long amount, ItemClass itemClass)
        {
            var economyManager = Singleton<EconomyManager>.instance;
            while (amount > 0)
            {
                var transaction = (int)Math.Min(amount, MaxMaintenanceTransaction);
                economyManager.FetchResource(
                    EconomyManager.Resource.Maintenance,
                    transaction,
                    itemClass);
                amount -= transaction;
            }
        }

        public static int CalculateTargetVehicleCount(ushort lineID)
        {
            var instance = TransportManager.instance;
            if (!CachedTransportLineData._init)
            {
                // Never report 0 while cache is still loading — that despawns the whole fleet.
                return instance.m_lines.m_buffer[lineID].CalculateTargetVehicleCount();
            }

            var lineInfo = instance.m_lines.m_buffer[lineID].Info;
            int targetVehicleCount;
            if (CachedTransportLineData.GetBudgetControlState(lineID) ||
                (lineInfo?.m_class != null && lineInfo.m_class.m_service == ItemClass.Service.Disaster))
            {
                targetVehicleCount = instance.m_lines.m_buffer[lineID].CalculateTargetVehicleCount();
                CachedTransportLineData.SetTargetVehicleCount(lineID, targetVehicleCount);
            }
            else
            {
                targetVehicleCount = CachedTransportLineData.GetTargetVehicleCount(lineID);
            }

            var activeVehicles = TransportLineUtil.CountLineActiveVehicles(lineID, out _);
            var enqueued = CachedTransportLineData.EnqueuedVehiclesCount(lineID);
            var need = targetVehicleCount - enqueued - activeVehicles;
            for (var i = 0; i < need; i++)
            {
                var prefab = CachedTransportLineData.GetRandomPrefab(lineID);
                if (prefab == null)
                {
                    break;
                }

                CachedTransportLineData.EnqueueVehicle(lineID, prefab);
            }
            return targetVehicleCount;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int FetchResourceStub(EconomyManager economyManager, EconomyManager.Resource resource,
            int amount,
            ItemClass itemClass)
        {
            return 0;
        }
    }
}

