using HarmonyLib;
using ImprovedPublicTransport.Util;
using UnityEngine;

namespace IntercityBusControl.HarmonyPatches.TransportStationAIPatches
{
    /// <summary>
    /// Actually enforces the per-building "accept intercity buses" toggle
    /// (<see cref="IntercityAcceptanceState"/>) - previously that state was saved and reflected back
    /// onto the checkbox, but nothing ever consulted it, so toggling it had zero effect on routing.
    /// </summary>
    /// <remarks>
    /// The obvious-looking fix - zeroing <c>TransportStationAI.m_maxVehicleCount</c>/
    /// <c>m_maxVehicleCount2</c> for the disabled building - does NOT work: those are fields on the
    /// building's <em>prefab</em> AI instance, shared by every placed copy of that same asset, so it
    /// would silently zero the cap for every other terminal using that asset too, not just the one
    /// the player disabled.
    /// <para>
    /// <see cref="TransportStationAI.CreateIncomingVehicle"/>/<c>CreateOutgoingVehicle</c> are private,
    /// per-<em>instance</em> methods (their first parameter is the specific building ID being
    /// evaluated, not shared prefab state) called from <c>ProduceGoods</c> whenever this terminal is
    /// due to spawn a bus to/from an outside connection. Skipping them for a disabled building ID
    /// blocks exactly - and only - that building's own intercity bus traffic, with no pathfinding
    /// changes involved.
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(TransportStationAI), "CreateIncomingVehicle")]
    internal static class Patch_CreateIncomingVehicle
    {
        // Rate-limit: Debug.Log is expensive. Old logic logged every different building every
        // tick → multi-station cities spammed Unity log and dropped FPS 40→20.
        private static float _lastLogRealtime;
        private const float LogIntervalSeconds = 30f;

        [HarmonyPrefix]
        public static bool Prefix(ushort buildingID, ref bool __result)
        {
            if (IntercityAcceptanceState.Accepts(buildingID))
            {
                return true;
            }

            LogBlockedOnce(buildingID, "incoming");
            __result = false;
            return false;
        }

        internal static void LogBlockedOnce(ushort buildingID, string direction)
        {
            if (!Diagnostics.VerboseRuntimeLogs)
            {
                return;
            }

            var now = Time.realtimeSinceStartup;
            // Global throttle — at most one line every 30s across all buildings/directions.
            if (now - _lastLogRealtime < LogIntervalSeconds)
            {
                return;
            }

            _lastLogRealtime = now;
            Utils.Log(
                $"IntercityBusControl: blocked {direction} vehicle spawn for disabled building {buildingID}.");
        }
    }

    [HarmonyPatch(typeof(TransportStationAI), "CreateOutgoingVehicle")]
    internal static class Patch_CreateOutgoingVehicle
    {
        [HarmonyPrefix]
        public static bool Prefix(ushort buildingID, ref bool __result)
        {
            if (IntercityAcceptanceState.Accepts(buildingID))
            {
                return true;
            }

            Patch_CreateIncomingVehicle.LogBlockedOnce(buildingID, "outgoing");
            __result = false;
            return false;
        }
    }
}
