using ColossalFramework;
using HarmonyLib;
using ImprovedPublicTransport;
using UnityEngine;

namespace SingleTrainTrackAI
{
    /// <summary>
    /// Holds a train at speed 0 before it enters a single-track segment that another train is
    /// currently occupying, and claims/renews the segment for whichever train is already on it -
    /// giving each direction exclusive use of the shared track without touching the vehicle's actual
    /// path or position calculation (unlike vanilla same-direction following, which is already
    /// handled by TrainAI's own overlap checks - this only concerns opposing-direction sharing).
    /// </summary>
    [HarmonyPatch(typeof(TrainAI), "CalculateTargetSpeed")]
    internal static class Patch_TrainAI_CalculateTargetSpeed
    {
        [HarmonyPostfix]
        public static void Postfix(ushort vehicleID, ref Vehicle data, ref float __result)
        {
            if (!ModSetting.Instance.EnableSingleTrainTrackAI || data.m_path == 0)
            {
                return;
            }

            // Only the lead car of a multi-unit train owns reservations / brake decisions.
            var leadId = data.GetFirstVehicle(vehicleID);
            if (leadId != 0 && leadId != vehicleID)
            {
                return;
            }

            var pathManager = Singleton<PathManager>.instance;
            var pathIndex = data.m_pathPositionIndex >> 1;
            if (!pathManager.m_pathUnits.m_buffer[data.m_path].GetPosition(pathIndex, out var currentPos))
            {
                return;
            }

            var currentFrame = TrackReservation.CurrentFrame;

            if (SegmentClassifier.IsSingleTrainTrack(currentPos.m_segment))
            {
                // Already on the shared track - claim the whole section it belongs to (renews our
                // own hold, releases whatever section we held before if we've moved out of it since
                // last tick) and proceed at normal speed; we got here because nothing blocked us
                // from entering.
                TrackReservation.Occupy(SectionClassifier.GetSection(currentPos.m_segment), vehicleID, currentFrame);
                return;
            }

            // Not on a shared segment yet - if the very next one is single-track and its section is
            // held by a different train, brake rather than enter it.
            if (!pathManager.m_pathUnits.m_buffer[data.m_path].GetNextPosition(pathIndex, out var nextPos))
            {
                return;
            }

            if (SegmentClassifier.IsSingleTrainTrack(nextPos.m_segment) &&
                TrackReservation.IsHeldByOther(SectionClassifier.GetSection(nextPos.m_segment), vehicleID))
            {
                // Soft brake: hard 0 froze trains for long periods when reservations went stale or
                // when same-direction followers queued behind a slow train still on the segment.
                // Cap speed so the train creeps up and only fully stops when already nearly stopped.
                __result = Mathf.Min(__result, 8f);
            }
        }
    }
}
