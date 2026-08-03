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
            _active = true;
        }

        public static void Deactivate()
        {
            if (_active)
            {
                GetHarmonyInstance().UnpatchAll(HarmonyModID);
                _active = false;
            }

            TrackReservation.Clear();
            SectionClassifier.Clear();
            SegmentClassifier.Clear();
        }
    }
}
