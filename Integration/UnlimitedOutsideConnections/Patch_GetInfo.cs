using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace UnlimitedOutsideConnections
{
    /// <summary>
    /// Clears the vanilla "too many outside connections" tool error (the hardcoded 4-connection cap)
    /// from the four network types that enforce it. Anchored on the actual enum value being loaded
    /// (Harmony's semantic <see cref="CodeInstruction.LoadsConstant(object)"/> match) rather than a
    /// raw opcode scan, so it finds the right constant regardless of which IL form the compiler chose
    /// for it and regardless of anything else nearby in the method.
    /// </summary>
    [HarmonyPatch]
    internal static class Patch_GetInfo
    {
        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(RoadAI), nameof(RoadAI.GetInfo));
            yield return AccessTools.Method(typeof(TrainTrackAI), nameof(TrainTrackAI.GetInfo));
            yield return AccessTools.Method(typeof(ShipPathAI), nameof(ShipPathAI.GetInfo));
            yield return AccessTools.Method(typeof(FlightPathAI), nameof(FlightPathAI.GetInfo));
        }

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!ImprovedPublicTransport.ModSetting.Instance.EnableUnlimitedOutsideConnections)
            {
                foreach (var instruction in instructions)
                {
                    yield return instruction;
                }
                yield break;
            }

            foreach (var instruction in instructions)
            {
                if (instruction.LoadsConstant(ToolBase.ToolErrors.TooManyConnections))
                {
                    // TooManyConnections is a large flag value, so the compiler always emits it as a
                    // full ldc.i4 with an explicit operand in practice - but LoadsConstant also
                    // matches the compact ldc.i4.0-8/ldc.i4.m1 opcodes, which bake their value into
                    // the opcode itself and ignore .operand entirely. Setting the opcode too (not
                    // just the operand) makes this correct either way instead of a silent no-op if a
                    // future game update ever changed how this constant is emitted.
                    instruction.opcode = OpCodes.Ldc_I4;
                    instruction.operand = 0;
                }

                yield return instruction;
            }
        }
    }
}
