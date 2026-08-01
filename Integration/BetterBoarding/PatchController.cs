using HarmonyLib;
using ImprovedPublicTransport.Util;

namespace BetterBoarding
{
    internal static class PatchController
    {
        public const string HarmonyModID = "IPT3.BetterBoarding";

        /*
         * The "singleton" design is pretty straight-forward.
         */

        private static Harmony harmony;

        public static Harmony GetHarmonyInstance()
        {
            if (harmony == null)
            {
                harmony = new Harmony(HarmonyModID);
            }

            return harmony;
        }

        public static void Activate()
        {
            HarmonyScope.PatchNamespace(GetHarmonyInstance(), "BetterBoarding");
        }

        public static void Deactivate()
        {
            GetHarmonyInstance().UnpatchAll(HarmonyModID);
        }

        // Must match ExpressBusServices.PatchController.HarmonyModID so [HarmonyAfter] orders
        // LoadPassengers correctly. The old "IPT3.ExpressBusServices" string never existed as a
        // live Harmony owner after the EBS port kept the upstream ID.
        public const string ExpressBusServicesHarmonyID = ExpressBusServices.PatchController.HarmonyModID;
    }
}
