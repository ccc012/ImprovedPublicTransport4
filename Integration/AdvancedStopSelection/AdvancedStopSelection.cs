using HarmonyLib;
using ICities;
using ColossalFramework.PlatformServices;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using UnityEngine;
using ImprovedPublicTransport.Util;

namespace ImprovedPublicTransport.Integration.AdvancedStopSelection
{
    public static class Patcher
    {
        private static CodeInstruction GetLDArg(MethodBase method, string argName)
        {
            var idx = Array.FindIndex(method.GetParameters(), p => p.Name == argName);

            if (idx == -1)
                return null;
            else if (!method.IsStatic)
                idx += 1;

            return idx switch
            {
                0 => new CodeInstruction(OpCodes.Ldarg_0),
                1 => new CodeInstruction(OpCodes.Ldarg_1),
                2 => new CodeInstruction(OpCodes.Ldarg_2),
                3 => new CodeInstruction(OpCodes.Ldarg_3),
                _ => new CodeInstruction(OpCodes.Ldarg_S, idx)
            };
        }

        public static IEnumerable<CodeInstruction> TransportToolGetStopPositionTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var alternateModeLocal = generator.DeclareLocal(typeof(bool));
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patcher), nameof(Patcher.GetAlternateMode)));
            yield return new CodeInstruction(OpCodes.Stloc, alternateModeLocal);

            bool segmentNotZeroPassed = false;
            Label segmentElseLabel = default;
            bool buildingCheckPatched = false;
            bool transportLine1CheckPatched = false;
            bool transportLine2CheckPatched = false;
            CodeInstruction prevInstruction = null;
            CodeInstruction prevPrevInstruction = null;
            var segmentArg = GetLDArg(original, "segment");
            var buildingArg = GetLDArg(original, "building");
            foreach (var instruction in instructions)
            {
                yield return instruction;

                if(!segmentNotZeroPassed)
                {
                    if(prevPrevInstruction != null && prevPrevInstruction.opcode == OpCodes.Ret && prevInstruction != null && prevInstruction.opcode == segmentArg.opcode && prevInstruction.operand == segmentArg.operand && instruction.opcode == OpCodes.Brfalse)
                    {
                        segmentNotZeroPassed = true;
                        segmentElseLabel = (Label)instruction.operand;
                    }
                }
                else
                {
                    if (!transportLine1CheckPatched && prevInstruction != null && prevInstruction.opcode == OpCodes.Ldloc_S && prevInstruction.operand is LocalBuilder local1 && local1.LocalIndex == 12 && instruction.opcode == OpCodes.Brfalse)
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc, alternateModeLocal);
                        yield return new CodeInstruction(OpCodes.Brtrue, instruction.operand);
                        transportLine1CheckPatched = true;
                    }

                    if (!transportLine2CheckPatched && prevInstruction != null && prevInstruction.opcode == OpCodes.Ldloc_S && prevInstruction.operand is LocalBuilder local2 && local2.LocalIndex == 13 && instruction.opcode == OpCodes.Brfalse)
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc, alternateModeLocal);
                        yield return new CodeInstruction(OpCodes.Brtrue, instruction.operand);
                        transportLine2CheckPatched = true;
                    }

                    if (!buildingCheckPatched && prevInstruction != null && prevInstruction.labels.Contains(segmentElseLabel) && prevInstruction.opcode == buildingArg.opcode && prevInstruction.operand == buildingArg.operand && instruction.opcode == OpCodes.Brfalse)
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc, alternateModeLocal);
                        yield return new CodeInstruction(OpCodes.Brtrue, instruction.operand);
                        buildingCheckPatched = true;
                    }
                }

                prevPrevInstruction = prevInstruction;
                prevInstruction = instruction;
            }

            if (!transportLine1CheckPatched || !transportLine2CheckPatched || !buildingCheckPatched)
                Utils.LogError($"AdvancedStopSelection: transpiler did not find all expected IL patterns (t1={transportLine1CheckPatched}, t2={transportLine2CheckPatched}, bldg={buildingCheckPatched}). The patch may be incomplete — a game update may have changed local variable indices.");
        }
        private static bool GetAlternateMode()
        {
            return ImprovedPublicTransport.Settings.IptHotkeys.AdvancedStopSelectionAlternateKey.Combination.IsPressed();
        }
    }

    internal static class PatchController
    {
        private const string HarmonyId = "ipt3.advancedstopselection.mod";
        private static Harmony _harmony;

        public static void PatchAll()
        {
            if (_harmony != null)
                return; // already patched

            _harmony = new Harmony(HarmonyId);
            try
            {
                var original = AccessTools.Method(typeof(TransportTool), "GetStopPosition");
                var transpiler = new HarmonyMethod(AccessTools.Method(typeof(Patcher), nameof(Patcher.TransportToolGetStopPositionTranspiler)));
                _harmony.Patch(original, null, null, transpiler);
                if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("AdvancedStopSelection: patch applied.");
            }
            catch (Exception ex)
            {
                Utils.LogError($"AdvancedStopSelection: failed to apply patch: {ex.Message}");
                try { _harmony?.UnpatchAll(HarmonyId); } catch { }
                _harmony = null;
            }
        }

        public static void UnpatchAll()
        {
            try
            {
                _harmony?.UnpatchAll(HarmonyId);
            }
            catch { }
            _harmony = null;
            if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) Utils.Log("AdvancedStopSelection: patch removed.");
        }

        // Convenience aliases used by IPT lifecycle
        public static void Activate() => PatchAll();
        public static void Deactivate() => UnpatchAll();
    }

}