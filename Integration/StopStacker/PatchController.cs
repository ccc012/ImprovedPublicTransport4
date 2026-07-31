using System.Reflection;
using HarmonyLib;

namespace StopStacker
{
    internal static class PatchController
    {
        public const string HarmonyModID = "IPT4.StopStacker";

        private static Harmony _harmony;

        private static Harmony GetHarmonyInstance() => _harmony ??= new Harmony(HarmonyModID);

        public static void Activate()
        {
            GetHarmonyInstance().PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void Deactivate()
        {
            GetHarmonyInstance().UnpatchAll(HarmonyModID);
            BerthRegistry.Clear();
        }
    }
}
