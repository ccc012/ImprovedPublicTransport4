using HarmonyLib;
using ImprovedPublicTransport.Util;

namespace TaxiStandFix
{
    internal static class PatchController
    {
        public const string HarmonyModID = "IPT4.TaxiStandFix";

        private static Harmony _harmony;

        private static Harmony GetHarmonyInstance() => _harmony ??= new Harmony(HarmonyModID);

        public static void Activate()
        {
            HarmonyScope.PatchNamespace(GetHarmonyInstance(), "TaxiStandFix");
        }

        public static void Deactivate()
        {
            GetHarmonyInstance().UnpatchAll(HarmonyModID);
            TaxiStandRegistry.Reset();
        }
    }
}
