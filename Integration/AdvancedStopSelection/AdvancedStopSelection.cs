using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ImprovedPublicTransport.Util;

namespace ImprovedPublicTransport.Integration.AdvancedStopSelection
{
    public static class Patcher
    {
        private static readonly FieldInfo StopFlagField = AccessTools.Field(typeof(TransportInfo), nameof(TransportInfo.m_stopFlag));
        private static readonly MethodInfo GetAlternateModeMethod = AccessTools.Method(typeof(Patcher), nameof(GetAlternateMode));
        private static readonly MethodInfo FilterStopFlagMethod = AccessTools.Method(typeof(Patcher), nameof(FilterStopFlag));

        public static IEnumerable<CodeInstruction> TransportToolGetStopPositionTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var codes = new List<CodeInstruction>(instructions);
            var alternateModeLocal = generator.DeclareLocal(typeof(bool));
            yield return new CodeInstruction(OpCodes.Call, GetAlternateModeMethod);
            yield return new CodeInstruction(OpCodes.Stloc, alternateModeLocal);

            bool segmentNotZeroPassed = false;
            bool buildingCheckPatched = false;
            bool transportLine1CheckPatched = false;
            bool transportLine2CheckPatched = false;
            int stopFlagsPatched = 0;
            Label segmentElseLabel = default;
            CodeInstruction previous = null;
            CodeInstruction previousPrevious = null;
            var segmentArg = GetLoadArgument(original, "segment");
            var buildingArg = GetLoadArgument(original, "building");
            var transportInfoLocals = FindTransportInfoLocals(codes);

            foreach (var instruction in codes)
            {
                yield return instruction;

                if (instruction.LoadsField(StopFlagField))
                {
                    yield return new CodeInstruction(OpCodes.Call, FilterStopFlagMethod);
                    stopFlagsPatched++;
                }

                if (!segmentNotZeroPassed)
                {
                    if (previousPrevious != null && previousPrevious.opcode == OpCodes.Ret
                        && SameInstruction(previous, segmentArg) && IsBrfalse(instruction))
                    {
                        segmentNotZeroPassed = true;
                        segmentElseLabel = (Label)instruction.operand;
                    }
                }
                else
                {
                    if (!transportLine1CheckPatched && transportInfoLocals.Count > 0
                        && LoadsLocal(previous, transportInfoLocals[0]) && IsBrfalse(instruction))
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc, alternateModeLocal);
                        yield return new CodeInstruction(OpCodes.Brtrue, instruction.operand);
                        transportLine1CheckPatched = true;
                    }
                    else if (!transportLine2CheckPatched && transportInfoLocals.Count > 1
                        && LoadsLocal(previous, transportInfoLocals[1]) && IsBrfalse(instruction))
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc, alternateModeLocal);
                        yield return new CodeInstruction(OpCodes.Brtrue, instruction.operand);
                        transportLine2CheckPatched = true;
                    }

                    if (!buildingCheckPatched && previous != null && previous.labels.Contains(segmentElseLabel)
                        && SameInstruction(previous, buildingArg) && IsBrfalse(instruction))
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc, alternateModeLocal);
                        yield return new CodeInstruction(OpCodes.Brtrue, instruction.operand);
                        buildingCheckPatched = true;
                    }
                }

                previousPrevious = previous;
                previous = instruction;
            }

            if (!transportLine1CheckPatched || !transportLine2CheckPatched || !buildingCheckPatched)
                Utils.LogError($"AdvancedStopSelection: GetStopPosition pattern incomplete (t1={transportLine1CheckPatched}, t2={transportLine2CheckPatched}, building={buildingCheckPatched}).");
            if (stopFlagsPatched == 0)
                Utils.LogError("SharedStopEnabler: GetStopPosition m_stopFlag pattern not found.");
        }

        public static NetLane.Flags FilterStopFlag(NetLane.Flags stopFlag)
        {
            return ModSetting.Instance.EnableSharedStopEnabler ? NetLane.Flags.None : stopFlag;
        }

        private static bool GetAlternateMode()
        {
            return ModSetting.Instance.EnableAdvancedStopSelection
                && Settings.IptHotkeys.AdvancedStopSelectionAlternateKey.Combination.IsPressed();
        }

        private static List<int> FindTransportInfoLocals(IEnumerable<CodeInstruction> instructions)
        {
            var result = new List<int>();
            CodeInstruction previous = null;
            foreach (var instruction in instructions)
            {
                if (previous != null && previous.opcode == OpCodes.Callvirt && previous.operand is MethodInfo method
                    && (method.Name == nameof(BuildingAI.GetTransportLineInfo) || method.Name == nameof(BuildingAI.GetSecondaryTransportLineInfo)))
                {
                    int index = GetStoredLocalIndex(instruction);
                    if (index >= 0 && !result.Contains(index))
                        result.Add(index);
                }
                previous = instruction;
            }
            return result;
        }

        private static int GetStoredLocalIndex(CodeInstruction instruction)
        {
            if (instruction.opcode == OpCodes.Stloc_0) return 0;
            if (instruction.opcode == OpCodes.Stloc_1) return 1;
            if (instruction.opcode == OpCodes.Stloc_2) return 2;
            if (instruction.opcode == OpCodes.Stloc_3) return 3;
            if (instruction.opcode != OpCodes.Stloc && instruction.opcode != OpCodes.Stloc_S) return -1;
            if (instruction.operand is LocalBuilder local) return local.LocalIndex;
            if (instruction.operand is byte value) return value;
            return instruction.operand is int index ? index : -1;
        }

        private static bool LoadsLocal(CodeInstruction instruction, int index)
        {
            if (instruction == null) return false;
            if (index == 0 && instruction.opcode == OpCodes.Ldloc_0) return true;
            if (index == 1 && instruction.opcode == OpCodes.Ldloc_1) return true;
            if (index == 2 && instruction.opcode == OpCodes.Ldloc_2) return true;
            if (index == 3 && instruction.opcode == OpCodes.Ldloc_3) return true;
            if (instruction.opcode != OpCodes.Ldloc && instruction.opcode != OpCodes.Ldloc_S) return false;
            if (instruction.operand is LocalBuilder local) return local.LocalIndex == index;
            if (instruction.operand is byte value) return value == index;
            return instruction.operand is int operand && operand == index;
        }

        private static CodeInstruction GetLoadArgument(MethodBase method, string name)
        {
            var parameters = method.GetParameters();
            int index = Array.FindIndex(parameters, parameter => parameter.Name == name);
            if (index < 0) return null;
            if (!method.IsStatic) index++;
            if (index == 0) return new CodeInstruction(OpCodes.Ldarg_0);
            if (index == 1) return new CodeInstruction(OpCodes.Ldarg_1);
            if (index == 2) return new CodeInstruction(OpCodes.Ldarg_2);
            if (index == 3) return new CodeInstruction(OpCodes.Ldarg_3);
            return new CodeInstruction(OpCodes.Ldarg_S, index);
        }

        private static bool SameInstruction(CodeInstruction left, CodeInstruction right)
        {
            return left != null && right != null && left.opcode == right.opcode && Equals(left.operand, right.operand);
        }

        private static bool IsBrfalse(CodeInstruction instruction)
        {
            return instruction.opcode == OpCodes.Brfalse || instruction.opcode == OpCodes.Brfalse_S;
        }
    }

    internal static class PatchController
    {
        private const string HarmonyId = "IPT4.TransportTool.GetStopPosition";
        private static Harmony _harmony;
        private static bool _advancedActive;
        private static bool _sharedActive;

        public static void Activate()
        {
            _advancedActive = true;
            UpdatePatch();
        }

        public static void Deactivate()
        {
            _advancedActive = false;
            UpdatePatch();
        }

        internal static void SetSharedStopEnablerActive(bool active)
        {
            _sharedActive = active;
            UpdatePatch();
        }

        private static void UpdatePatch()
        {
            if (_advancedActive || _sharedActive)
                EnsurePatched();
            else
                Unpatch();
        }

        private static void EnsurePatched()
        {
            if (_harmony != null) return;
            var original = AccessTools.Method(typeof(TransportTool), "GetStopPosition");
            var transpiler = AccessTools.Method(typeof(Patcher), nameof(Patcher.TransportToolGetStopPositionTranspiler));
            if (original == null || transpiler == null)
            {
                Utils.LogError("TransportTool.GetStopPosition unified patch method not found.");
                return;
            }

            try
            {
                _harmony = new Harmony(HarmonyId);
                _harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
            }
            catch (Exception ex)
            {
                Utils.LogError($"TransportTool.GetStopPosition unified patch failed: {ex.Message}");
                Unpatch();
            }
        }

        private static void Unpatch()
        {
            if (_harmony == null) return;
            try { _harmony.UnpatchAll(HarmonyId); } catch { }
            _harmony = null;
        }
    }
}
