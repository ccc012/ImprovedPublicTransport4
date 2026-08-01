using HarmonyLib;
using ImprovedPublicTransport.Util;

namespace UnlimitedOutsideConnections
{
    internal static class PatchController
    {
        public const string HarmonyModID = "IPT4.UnlimitedOutsideConnections";

        private static Harmony _harmony;

        private static Harmony GetHarmonyInstance() => _harmony ??= new Harmony(HarmonyModID);

        public static void Activate()
        {
            HarmonyScope.PatchNamespace(GetHarmonyInstance(), "UnlimitedOutsideConnections");
            BuildingManagerHooks.Deploy();
        }

        public static void Deactivate()
        {
            BuildingManagerHooks.Revert();
            GetHarmonyInstance().UnpatchAll(HarmonyModID);
        }
    }
}
