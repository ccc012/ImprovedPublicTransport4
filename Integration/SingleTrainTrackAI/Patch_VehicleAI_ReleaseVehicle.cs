using HarmonyLib;
using ImprovedPublicTransport;

namespace SingleTrainTrackAI
{
    /// <summary>
    /// Releases a despawned/deleted train's single-track hold immediately, instead of relying solely
    /// on TrackReservation's stale-after-timeout safety net (600 frames = 10 in-game seconds during
    /// which the segment would otherwise stay blocked to trains coming from the other direction).
    /// </summary>
    [HarmonyPatch(typeof(VehicleAI), "ReleaseVehicle")]
    internal static class Patch_VehicleAI_ReleaseVehicle
    {
        [HarmonyPostfix]
        public static void Postfix(VehicleAI __instance, ushort vehicleID)
        {
            if (!ModSetting.Instance.EnableSingleTrainTrackAI || !(__instance is TrainAI))
            {
                return;
            }

            TrackReservation.ReleaseVehicle(vehicleID);
        }
    }
}
