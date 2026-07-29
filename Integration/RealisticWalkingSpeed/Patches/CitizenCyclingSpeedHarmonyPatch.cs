using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ImprovedPublicTransport.Util;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace RealisticWalkingSpeed.Patches
{
    public class CitizenCyclingSpeedHarmonyPatch : IHarmonyPatch
    {
        private readonly Harmony _harmony;

        public CitizenCyclingSpeedHarmonyPatch(Harmony harmony)
        {
            _harmony = harmony ?? throw new ArgumentNullException(nameof(harmony));
        }

        public void Apply()
        {
            try
            {
                var simulationStepMethodInfo = typeof(HumanAI).GetMethod(
                    "SimulationStep",
                    BindingFlags.Instance | BindingFlags.Public,
                    Type.DefaultBinder,
                    new []
                    {
                        typeof(ushort),
                        typeof(CitizenInstance).MakeByRefType(),
                        typeof(CitizenInstance.Frame).MakeByRefType(),
                        typeof(bool)
                    },
                    null
                );
                
                if (simulationStepMethodInfo == null)
                {
                    Utils.LogError("CitizenCyclingSpeedHarmonyPatch: Could not find HumanAI.SimulationStep method");
                    return;
                }

                var simulationStepTranspilerMethodInfo = GetType()
                    .GetMethod(nameof(SimulationStepTranspiler), BindingFlags.Static | BindingFlags.NonPublic);
                var harmonyMethod = new HarmonyMethod(simulationStepTranspilerMethodInfo) { priority = Priority.Low };
                _harmony.Patch(simulationStepMethodInfo, null, null, harmonyMethod);
                if (Diagnostics.VerboseTranspileLogs)
                {
                    Utils.Log("CitizenCyclingSpeedHarmonyPatch: Successfully patched HumanAI.SimulationStep");
                }
            }
            catch (System.Exception ex)
            {
                Utils.LogError($"CitizenCyclingSpeedHarmonyPatch: Failed to apply patch: {ex.Message}\n{ex.StackTrace}");
            }
        }

        static IEnumerable<CodeInstruction> SimulationStepTranspiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var codes = new List<CodeInstruction>(codeInstructions);
            for (int i = 0; i < codes.Count; i++)
            {
                var firstCode = codes[i];
                if (firstCode.opcode != OpCodes.Ldloc_S)
                {
                    continue;
                }

                if (i + 7 >= codes.Count)
                    continue;

                var onBikeLaneFactor = codes[i + 1];
                var onBikeLaneMultiplication = codes[i + 2];
                var notOnBikeLaneFactor = codes[i + 6];
                var notOnBikeLaneMultiplication = codes[i + 7];

                if (!(onBikeLaneFactor.opcode == OpCodes.Ldc_R4
                    && onBikeLaneFactor.operand is float onBikeVal && onBikeVal == 2.0f
                    && onBikeLaneMultiplication.opcode == OpCodes.Mul
                    && notOnBikeLaneFactor.opcode == OpCodes.Ldc_R4
                    && notOnBikeLaneFactor.operand is float notOnBikeVal && notOnBikeVal == 1.5f
                    && notOnBikeLaneMultiplication.opcode == OpCodes.Mul))
                {
                    continue;
                }

                onBikeLaneFactor.operand = 3.5f;
                notOnBikeLaneFactor.operand = 2.5f;
                if (Diagnostics.VerboseTranspileLogs)
                {
                    Utils.Log("CitizenCyclingSpeedHarmonyPatch: Transpiler successfully modified cycling speeds");
                }

                break;
            }

            return codes;
        }
    }
}