using System.Reflection;
using HarmonyLib;

namespace TaxiStandFix
{
    internal static class PatchController
    {
        public const string HarmonyModID = "IPT4.TaxiStandFix";

        private static Harmony _harmony;

        private static Harmony GetHarmonyInstance() => _harmony ??= new Harmony(HarmonyModID);

        public static void Activate()
        {
            GetHarmonyInstance().PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void Deactivate()
        {
            GetHarmonyInstance().UnpatchAll(HarmonyModID);
            TaxiStandRegistry.Reset();
        }
    }
}
