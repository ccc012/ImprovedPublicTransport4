// Adapted from SharedStopEnabler (GPL-3.0, Workshop 2096382380, github.com/CodeBardian/SharedStopEnabler) - see LICENSE.txt.
// IL match is fragile across game updates — if the pattern is not found, the rest of SharedStop still works.
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using ImprovedPublicTransport.Util;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace SharedStopEnabler
{
    /// <summary>
    /// Relaxes RoadAI.UpdateSegmentFlags so secondary stop flags (Stop*2) can coexist with primary ones.
    /// </summary>
    [HarmonyPatch(typeof(RoadAI), "UpdateSegmentFlags")]
    internal static class Patch_RoadAI_UpdateSegmentFlags
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var foundJump = false;
            var index = -1;

            for (var i = 0; i < codes.Count; i++)
            {
                if (foundJump)
                {
                    break;
                }

                if (codes[i].opcode != OpCodes.Br)
                {
                    continue;
                }

                for (var j = i + 1; j < codes.Count; j++)
                {
                    if (codes[j].opcode == OpCodes.Br)
                    {
                        break;
                    }

                    var strOperand = codes[j].operand;
                    if (strOperand != null && strOperand.ToString().Contains("65536"))
                    {
                        index = codes.FindIndex(j, c => c.opcode == OpCodes.Br);
                        foundJump = true;
                        break;
                    }
                }
            }

            if (index > -1)
            {
                codes[index].opcode = OpCodes.Nop;
                codes[index].operand = null;
            }
            else if (Diagnostics.VerboseTranspileLogs)
            {
                Utils.LogWarning("SharedStopEnabler: RoadAI.UpdateSegmentFlags transpiler pattern not found (game update?).");
            }

            return codes.AsEnumerable();
        }
    }
}
