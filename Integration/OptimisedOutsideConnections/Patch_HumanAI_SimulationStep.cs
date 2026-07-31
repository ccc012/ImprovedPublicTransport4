using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using ImprovedPublicTransport;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace OptimisedOutsideConnections
{
    /// <summary>
    /// Widens the citizen "patience" roll while waiting for public transport, but only for citizens
    /// specifically waiting at an outside connection - normal in-city waiting is left at vanilla's own
    /// pacing. Patches <see cref="HumanAI"/> directly rather than <see cref="ResidentAI"/> /
    /// <see cref="TouristAI"/>: ResidentAI overrides this overload but always calls
    /// <c>base.SimulationStep(...)</c> at the end, and TouristAI never overrides it at all, so the
    /// base HumanAI implementation runs for every citizen type either way.
    /// </summary>
    [HarmonyPatch(typeof(HumanAI), nameof(HumanAI.SimulationStep),
        new[] { typeof(ushort), typeof(CitizenInstance), typeof(CitizenInstance.Frame), typeof(bool) },
        new[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal })]
    internal static class Patch_HumanAI_SimulationStep
    {
        private static readonly MethodInfo Int32RangeMethod =
            AccessTools.Method(typeof(Randomizer), nameof(Randomizer.Int32), new[] { typeof(uint) });

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var codes = new List<CodeInstruction>(instructions);

            // HumanAI.SimulationStep calls Randomizer.Int32(uint) three times for unrelated waiting
            // states (spawning a vehicle, waiting for transport, waiting for a taxi); the one this
            // integration targets - waiting for transport - is the only one of the three whose
            // argument is the constant 4, so that's enough to uniquely identify it without also
            // having to track which CitizenInstance.Flags branch the IL is currently inside.
            var callIndex = -1;
            for (var i = 1; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo mi && mi == Int32RangeMethod && codes[i - 1].opcode == OpCodes.Ldc_I4_4)
                {
                    callIndex = i;
                    break;
                }
            }

            if (callIndex < 0)
            {
                Utils.LogError($"OptimisedOutsideConnections: WaitingTransport Randomizer.Int32(4u) call not found in {original?.DeclaringType?.Name}.{original?.Name} - leaving vanilla behaviour unchanged.");
                return codes;
            }

            // Replace the ldc.i4.4 argument with (ldarg.2; call GetPassengerWaitRange) - citizenData
            // is argument 2 of this SimulationStep overload. Unlike the cargo-vehicle patches, no
            // comparison needs flipping: vanilla gates the wait-counter increment directly on
            // "roll == 0", so a larger divisor already makes that increment rarer on its own, which
            // is the correct direction for "wait longer".
            var argumentIndex = callIndex - 1;
            var originalLabels = codes[argumentIndex].labels;
            codes[argumentIndex] = new CodeInstruction(OpCodes.Ldarg_2).WithLabels(originalLabels);
            codes.Insert(argumentIndex + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_HumanAI_SimulationStep), nameof(GetPassengerWaitRange))));

            return codes;
        }

        // Vanilla's own constant is 4. Default scope (OutsideConnectionsOnly) preserves it unchanged
        // for every citizen NOT waiting at an outside connection. The CityWide scope is an explicit
        // opt-in reproducing upstream's own actual behaviour (Citizens.cs in the source mod always
        // widened this roll, regardless of location) - which also slows down ordinary domestic
        // public-transport patience for every citizen in the city, likely an unintended side effect
        // in the original given the mod's stated purpose, but offered here as a real choice rather
        // than silently either forced on or left out.
        private static uint GetPassengerWaitRange(ref CitizenInstance citizenData)
        {
            var scope = ModSetting.Instance.OutsideConnectionPassengerWaitScope;
            if (!ModSetting.Instance.EnableOptimisedOutsideConnections || scope == ModSetting.PassengerWaitScopes.Disabled)
            {
                return 4u;
            }

            if (scope == ModSetting.PassengerWaitScopes.CityWide)
            {
                return 4u * (uint)ModSetting.Instance.OutsideConnectionWaitMultiplier;
            }

            return IsWaitingAtOutsideConnection(ref citizenData)
                ? 4u * (uint)ModSetting.Instance.OutsideConnectionWaitMultiplier
                : 4u;
        }

        // True if this citizen's next path position is a node that isn't a stop on any transport
        // line - i.e. a plain road/track node at an outside connection, not a normal in-city stop.
        private static bool IsWaitingAtOutsideConnection(ref CitizenInstance citizenData)
        {
            if ((citizenData.m_flags & (CitizenInstance.Flags.TargetIsNode | CitizenInstance.Flags.OnTour)) != CitizenInstance.Flags.None)
            {
                return false;
            }

            var pathManager = Singleton<PathManager>.instance;
            var netManager = Singleton<NetManager>.instance;

            if (!pathManager.m_pathUnits.m_buffer[citizenData.m_path].GetPosition(citizenData.m_pathPositionIndex >> 1, out var position))
            {
                return false;
            }

            var nodeId = netManager.m_segments.m_buffer[position.m_segment].m_startNode;
            return netManager.m_nodes.m_buffer[nodeId].m_transportLine == 0;
        }
    }
}
