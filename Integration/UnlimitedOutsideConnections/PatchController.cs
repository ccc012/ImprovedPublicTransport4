using System.Reflection;
using HarmonyLib;

namespace UnlimitedOutsideConnections
{
    internal static class PatchController
    {
        public const string HarmonyModID = "IPT4.UnlimitedOutsideConnections";

        private static Harmony _harmony;

        private static Harmony GetHarmonyInstance() => _harmony ??= new Harmony(HarmonyModID);

        public static void Activate()
        {
            GetHarmonyInstance().PatchAll(Assembly.GetExecutingAssembly());
            BuildingManagerHooks.Deploy();
        }

        public static void Deactivate()
        {
            BuildingManagerHooks.Revert();
            GetHarmonyInstance().UnpatchAll(HarmonyModID);
        }
    }
}
