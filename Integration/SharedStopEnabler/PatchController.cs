using System.Reflection;
using HarmonyLib;

namespace SharedStopEnabler
{
    internal static class PatchController
    {
        public const string HarmonyModID = "IPT4.SharedStopEnabler";

        private static Harmony _harmony;

        private static Harmony GetHarmonyInstance() => _harmony ??= new Harmony(HarmonyModID);

        public static void Activate()
        {
            SharedStopRegistry.InitSegments();
            GetHarmonyInstance().PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void Deactivate()
        {
            GetHarmonyInstance().UnpatchAll(HarmonyModID);
            SharedStopRegistry.Reset();
        }
    }
}
