using HarmonyLib;
using System;
using System.Reflection;
using JetBrains.Annotations;

namespace ExpressBusServices
{
    [HarmonyPatch]
    [UsedImplicitly]
    public class Patch_VehicleAI_SimulationStep
    {
        /// <summary>
        /// Pathfind is async: WaitingPath + path==0 is normal for many frames.
        /// Clearing WaitingPath every tick forced repath → mid-street U-turn then continue.
        /// Only intervene after this many consecutive stuck frames (~1s at 60 sim steps/s).
        /// </summary>
        private const byte StuckFramesBeforeRetry = 64;

        /// <summary>
        /// Sparse per-vehicle counter. A Dictionary.Remove on the fast path used to run for
        /// nearly every vehicle every SimulationStep and tanked FPS under large fleets.
        /// Array index is free; only the rare stuck path writes non-zero values.
        /// </summary>
        private static byte[] _stuckWaitingPathFrames = new byte[0];

        [HarmonyTargetMethod]
        [UsedImplicitly]
        public static MethodBase TargetRelevantMethod()
        {
            return AccessTools.Method(typeof(VehicleAI), "SimulationStep", new Type[] { typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(int) });
        }

        [HarmonyPrepare]
        [UsedImplicitly]
        public static bool DetermineIfShouldPatch()
        {
            return true;
        }

        public static void ForgetVehicle(ushort vehicleID)
        {
            var arr = _stuckWaitingPathFrames;
            if (arr != null && vehicleID < arr.Length)
            {
                arr[vehicleID] = 0;
            }
        }

        private static void EnsureCapacity(ushort vehicleID)
        {
            var arr = _stuckWaitingPathFrames;
            if (arr != null && vehicleID < arr.Length)
            {
                return;
            }

            // Vehicle buffer is typically 16384; grow to next power-of-two-ish size.
            int need = vehicleID + 1;
            int size = arr == null || arr.Length == 0 ? 16384 : arr.Length;
            while (size < need)
            {
                size *= 2;
            }

            var next = new byte[size];
            if (arr != null && arr.Length > 0)
            {
                Array.Copy(arr, next, arr.Length);
            }

            _stuckWaitingPathFrames = next;
        }

        /// <summary>
        /// Unstick vehicles that never got a path. Only drop the line when the target is already
        /// unusable — otherwise clear WaitingPath after a grace period so pathfind can complete
        /// without thrashing (U-turn spam) or forcing a depot return that poisons GetNextStop.
        /// </summary>
        [HarmonyPrefix]
        [UsedImplicitly]
        public static void PreSimulationStep(ushort vehicleID, ref Vehicle vehicleData)
        {
            try
            {
                // Ultra-fast reject: almost every vehicle hits this path. Zero allocations, no dictionary.
                if (vehicleData.m_transportLine == 0
                    || vehicleData.m_path != 0
                    || (vehicleData.m_flags & Vehicle.Flags.WaitingPath) == 0)
                {
                    var arr = _stuckWaitingPathFrames;
                    if (arr != null && vehicleID < arr.Length && arr[vehicleID] != 0)
                    {
                        arr[vehicleID] = 0;
                    }

                    return;
                }

                EnsureCapacity(vehicleID);
                var stuckArr = _stuckWaitingPathFrames;
                byte stuck = stuckArr[vehicleID];
                if (stuck < byte.MaxValue)
                {
                    stuck++;
                }

                stuckArr[vehicleID] = stuck;

                // Let in-flight pathfind finish; only retry after grace period.
                if (stuck < StuckFramesBeforeRetry)
                {
                    return;
                }

                stuckArr[vehicleID] = 0;
                vehicleData.m_flags &= ~Vehicle.Flags.WaitingPath;

                // Only send back to base when the stop/building target is already invalid —
                // valid targets should retry pathfind next tick instead of poisoning state.
                if (!TransportStopSafety.IsSafePathfindTarget(ref vehicleData)
                    && vehicleData.Info?.m_vehicleAI != null)
                {
                    vehicleData.Info.m_vehicleAI.SetTransportLine(vehicleID, ref vehicleData, 0);
                }
            }
            catch
            {
                // Never let unsticker logic take down the simulation thread.
            }
        }
    }
}
