using HarmonyLib;
using ImprovedPublicTransport.Util;

namespace SingleTrainTrackAI
{
    internal static class PatchController
    {
        public const string HarmonyModID = "IPT4.SingleTrainTrackAI";

        private static Harmony _harmony;
        private static bool _active;

        private static Harmony GetHarmonyInstance() => _harmony ??= new Harmony(HarmonyModID);

        public static void Activate()
        {
            if (_active)
            {
                return;
            }

            HarmonyScope.PatchNamespace(GetHarmonyInstance(), "SingleTrainTrackAI");
            // Cache invalidation for network edits isn't [HarmonyPatch]-annotated, so wire it
            // manually - without it, sections/segments built before a rail edit keep stale bounds.
            NetworkChangePatch.Apply();
            _active = true;
        }

        public static void Deactivate()
        {
            if (_active)
            {
                NetworkChangePatch.Undo();
                GetHarmonyInstance().UnpatchAll(HarmonyModID);
                _active = false;
            }

            TrackReservation.Clear();
            SectionClassifier.Clear();
            SegmentClassifier.Clear();
        }
    }
}
