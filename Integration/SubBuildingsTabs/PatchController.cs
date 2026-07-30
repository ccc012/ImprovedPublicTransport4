using System.Reflection;
using HarmonyLib;

namespace SubBuildingsTabs
{
    internal static class PatchController
    {
        public const string HarmonyModID = "IPT4.SubBuildingsTabs";

        private static Harmony _harmony;

        private static Harmony GetHarmonyInstance() => _harmony ??= new Harmony(HarmonyModID);

        public static void Activate()
        {
            GetHarmonyInstance().PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void Deactivate()
        {
            GetHarmonyInstance().UnpatchAll(HarmonyModID);

            // The checkbox is live; remove an already displayed strip as well as its patch.
            foreach (var tabstrip in UnityEngine.Resources.FindObjectsOfTypeAll<SubBuildingsTabstrip>())
            {
                UnityEngine.Object.Destroy(tabstrip.gameObject);
            }
        }
    }
}
