using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OptimisedOutsideConnections
{
    [HarmonyPatch(typeof(CargoShipAI), nameof(CargoShipAI.SimulationStep),
        new[] { typeof(ushort), typeof(Vehicle), typeof(Vector3) },
        new[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
    internal static class Patch_CargoShipAI_SimulationStep
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) =>
            RandomizerRangeTranspiler.WidenGiveUpRoll(instructions, original, AccessTools.Method(typeof(Patch_CargoShipAI_SimulationStep), nameof(GetMultipliedRange)));

        // Matches upstream's own tuning: planes/ships scale the user-facing multiplier by 4.
        private static int GetMultipliedRange() =>
            ImprovedPublicTransport.ModSetting.Instance.EnableOptimisedOutsideConnections
                ? 4 * ImprovedPublicTransport.ModSetting.Instance.OutsideConnectionWaitMultiplier
                : 2;
    }
}
