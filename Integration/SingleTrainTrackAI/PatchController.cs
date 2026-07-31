using System.Reflection;
using HarmonyLib;

namespace SingleTrainTrackAI
{
    internal static class PatchController
    {
        public const string HarmonyModID = "IPT4.SingleTrainTrackAI";

        private static Harmony _harmony;

        private static Harmony GetHarmonyInstance() => _harmony ??= new Harmony(HarmonyModID);

        public static void Activate()
        {
            GetHarmonyInstance().PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void Deactivate()
        {
            GetHarmonyInstance().UnpatchAll(HarmonyModID);
            TrackReservation.Clear();
            SegmentClassifier.Clear();
        }
    }
}
