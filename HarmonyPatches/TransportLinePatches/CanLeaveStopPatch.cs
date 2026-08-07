using ColossalFramework;
using HarmonyLib;
using ImprovedPublicTransport.Util;
using UnityEngine;

namespace ImprovedPublicTransport.HarmonyPatches.TransportLinePatches
{
    public static class CanLeaveStopPatch
    {
        public const byte BoardingTime = 12; //from the original TransportLine time
        public const byte AirplaneBoardingTime = 200;
        public const byte MaxUnbunchingTime = byte.MaxValue - BoardingTime;

        // Verbose logs used to fire every CanLeaveStop call (many times per vehicle per second)
        // and flooded output_log when Log Level = Verbose. Sample at most once every 64 sim frames.
        private const uint VerboseLogFrameMask = 63u;
        
        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(TransportLine),
                    nameof(TransportLine.CanLeaveStop), priority: Priority.Normal),
                new PatchUtil.MethodDefinition(typeof(CanLeaveStopPatch),
                    nameof(Prefix), priority: Priority.Normal)
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(TransportLine),
                    nameof(TransportLine.CanLeaveStop))
            );
        }

        public static bool Prefix(ref TransportLine __instance, out bool __result, ushort nextStop, int waitTime)
        {
            var lineId = __instance.m_lineNumber;

            // Master switch: when unbunching is disabled, fall back to vanilla CanLeaveStop.
            if (!ModSetting.Instance.EnableUnbunching)
            {
                __result = false;
                return true;
            }

            if (nextStop == 0)
            {
                __result = true;
                LogVerbose(lineId, $"nextStop=0 => allow leave");
                return false;
            }

            var prevSegment = TransportLine.GetPrevSegment(nextStop);
            var lowTrafficState = prevSegment == 0 || (__instance.m_averageInterval -
                Singleton<NetManager>.instance.m_segments.m_buffer[prevSegment].m_trafficLightState0 + 2) / 4 <= 0;
            if (lowTrafficState)
            {
                __result = true;
                LogVerbose(lineId, $"prevSegment={prevSegment} lowTrafficState=true => allow leave");
                return false;
            }

            //begin mod(*): compare with interval aggression setup instead of default 64
            var targetWaitTime = BoardingTime + Mathf.Min(ModSetting.Instance.IntervalAggressionFactor, MaxUnbunchingTime);
            __result = waitTime >= targetWaitTime; //4 * 16 = 64 is max waiting time in vanilla, 12 is min waiting time
            LogVerbose(lineId, $"nextStop={nextStop} waitTime={waitTime} avgInterval={__instance.m_averageInterval} targetWaitTime={targetWaitTime} result={__result}");
            //end mod
            return false;
        }

        private static void LogVerbose(ushort lineId, string detail)
        {
            if (!Diagnostics.VerboseRuntimeLogs)
            {
                return;
            }

            if ((Singleton<SimulationManager>.instance.m_currentFrameIndex & VerboseLogFrameMask) != 0)
            {
                return;
            }

            var lineName = TransportManager.instance.GetLineName(lineId);
            ImprovedPublicTransport.Util.Utils.Log($"CanLeaveStopPatch: line {lineId} ({lineName}) {detail}");
        }
    }
}

