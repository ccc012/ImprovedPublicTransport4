using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OptimisedOutsideConnections
{
    [HarmonyPatch(typeof(CargoTrainAI), nameof(CargoTrainAI.SimulationStep),
        new[] { typeof(ushort), typeof(Vehicle), typeof(Vector3) },
        new[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
    internal static class Patch_CargoTrainAI_SimulationStep
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) =>
            RandomizerRangeTranspiler.WidenGiveUpRoll(instructions, original, AccessTools.Method(typeof(Patch_CargoTrainAI_SimulationStep), nameof(GetMultipliedRange)));

        // Matches upstream's own tuning: trains scale the user-facing multiplier by 2.
        private static int GetMultipliedRange() =>
            ImprovedPublicTransport.ModSetting.Instance.EnableOptimisedOutsideConnections
                ? 2 * ImprovedPublicTransport.ModSetting.Instance.OutsideConnectionWaitMultiplier
                : 2;
    }
}
