// Adapted from SharedStopEnabler (GPL-3.0, Workshop 2096382380, github.com/CodeBardian/SharedStopEnabler) - see LICENSE.txt.
// Manual Harmony apply (private instance method). Removes the exclusive m_stopFlag gate so a second
// transport type can snap a stop onto a segment that already has another type's stop.
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using ImprovedPublicTransport.Util;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace SharedStopEnabler
{
    internal static class TransportToolGetStopPositionTranspiler
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var foundStopFlagAccess = false;
            var startIndex = -1;
            var endIndex = -1;

            for (var i = 0; i < codes.Count; i++)
            {
                if (foundStopFlagAccess)
                {
                    break;
                }

                if (codes[i].opcode != OpCodes.Call)
                {
                    continue;
                }

                var strOperand = codes[i].operand;
                if (strOperand == null || !strOperand.ToString().Contains("GetClosestLanePosition"))
                {
                    continue;
                }

                var stopFlagCount = 0;
                startIndex = i + 2;
                for (var j = startIndex + 1; j < codes.Count; j++)
                {
                    if (codes[j].opcode == OpCodes.Ret)
                    {
                        break;
                    }

                    strOperand = codes[j].operand;
                    if (strOperand != null && strOperand.ToString().Contains("m_stopFlag"))
                    {
                        stopFlagCount++;
                    }

                    if (stopFlagCount == 2)
                    {
                        endIndex = codes.FindIndex(j, c => c.opcode == OpCodes.Ret);
                        foundStopFlagAccess = true;
                        break;
                    }
                }
            }

            if (startIndex > -1 && endIndex > -1 && endIndex >= startIndex)
            {
                codes.RemoveRange(startIndex, endIndex - startIndex + 1);
                if (Diagnostics.VerboseTranspileLogs)
                {
                    Utils.Log("SharedStopEnabler: TransportTool.GetStopPosition stop-flag gate removed.");
                }
            }
            else if (Diagnostics.VerboseTranspileLogs)
            {
                Utils.LogWarning("SharedStopEnabler: TransportTool.GetStopPosition transpiler pattern not found.");
            }

            return codes.AsEnumerable();
        }
    }
}
