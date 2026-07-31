using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColossalFramework.Math;
using HarmonyLib;
using ImprovedPublicTransport.Util;

namespace OptimisedOutsideConnections
{
    /// <summary>
    /// Shared transpiler logic for CargoTrainAI/CargoPlaneAI/CargoShipAI.SimulationStep. Each of
    /// those vanilla methods, while a vehicle is WaitingCargo, rolls
    /// <c>Randomizer.Int32(2u) == 0</c> once per simulation tick; when true, the tick is skipped and
    /// the vehicle's "how full am I" wait counter does not advance. Replacing the constant 2 with a
    /// larger value alone is NOT enough - P(roll == 0) = 1/N, so a larger N makes the skip roll rarer
    /// and the vehicle would depart SOONER on average, the opposite of "wait for more cargo". The
    /// comparison also has to flip from == (ceq) to &gt; (cgt) against the same "compare to 0"
    /// operand, so the condition becomes "roll != 0", which gets MORE likely (not less) as N grows -
    /// this is exactly the transformation the original mod applies, just anchored on the specific
    /// Randomizer.Int32(uint) call site instead of the first raw ldc.i4.2 found anywhere in the
    /// method body, so it can't silently touch an unrelated "load constant 2" if an unrelated part of
    /// the method changes in a future game update.
    /// </summary>
    internal static class RandomizerRangeTranspiler
    {
        private static readonly MethodInfo Int32RangeMethod =
            AccessTools.Method(typeof(Randomizer), nameof(Randomizer.Int32), new[] { typeof(uint) });

        internal static IEnumerable<CodeInstruction> WidenGiveUpRoll(IEnumerable<CodeInstruction> instructions, MethodBase original, MethodInfo getMultipliedRange)
        {
            var codes = new List<CodeInstruction>(instructions);

            var callIndex = codes.FindIndex(c => c.opcode == OpCodes.Call && c.operand is MethodInfo mi && mi == Int32RangeMethod);
            if (callIndex <= 0)
            {
                Utils.LogError($"OptimisedOutsideConnections: Randomizer.Int32(uint) call not found in {original?.DeclaringType?.Name}.{original?.Name} - leaving vanilla behaviour unchanged.");
                return codes;
            }

            // The argument pushed immediately before the call is the "2" to widen.
            var argumentIndex = callIndex - 1;
            var originalArgument = codes[argumentIndex];
            if (!originalArgument.opcode.Name.StartsWith("ldc.i4", StringComparison.Ordinal))
            {
                Utils.LogError($"OptimisedOutsideConnections: unexpected instruction before Int32(uint) call in {original?.DeclaringType?.Name}.{original?.Name} ({originalArgument.opcode}) - leaving vanilla behaviour unchanged.");
                return codes;
            }

            codes[argumentIndex] = new CodeInstruction(OpCodes.Call, getMultipliedRange).WithLabels(originalArgument.labels);

            // Two instructions after the call: ldc.i4.0, then the comparison to flip from == to >.
            var ceqIndex = callIndex + 2;
            if (ceqIndex >= codes.Count || codes[ceqIndex].opcode != OpCodes.Ceq)
            {
                Utils.LogError($"OptimisedOutsideConnections: expected ceq two instructions after Int32(uint) call in {original?.DeclaringType?.Name}.{original?.Name}, found {(ceqIndex < codes.Count ? codes[ceqIndex].opcode.ToString() : "end of method")} - leaving vanilla behaviour unchanged.");
                // Revert the argument swap too, since a half-applied patch (widened range but still
                // "== 0") would make vehicles depart sooner, not later - the opposite of the setting.
                // Restores the actual original instruction (not a hardcoded Ldc_I4_2 guess), correct
                // regardless of which ldc.i4* form the compiler originally used.
                codes[argumentIndex] = originalArgument;
                return codes;
            }

            codes[ceqIndex] = new CodeInstruction(OpCodes.Cgt).WithLabels(codes[ceqIndex].labels);

            return codes;
        }
    }
}
