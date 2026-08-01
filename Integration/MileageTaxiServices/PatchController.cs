using HarmonyLib;
using ImprovedPublicTransport.Util;

namespace MileageTaxiServices
{
    internal class PatchController
    {
        public const string HarmonyModID = "com.ipt3.mileagetaxi";

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
            HarmonyScope.PatchNamespace(GetHarmonyInstance(), "MileageTaxiServices");
        }

        public static void Deactivate()
        {
            GetHarmonyInstance().UnpatchAll(HarmonyModID);
        }
    }
}
