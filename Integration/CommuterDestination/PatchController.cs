using System.Reflection;
using HarmonyLib;

namespace CommuterDestination
{
    internal static class PatchController
    {
        public const string HarmonyModID = "IPT4.CommuterDestination";

        private static Harmony _harmony;

        private static Harmony GetHarmonyInstance() => _harmony ??= new Harmony(HarmonyModID);

        public static void Activate()
        {
            GetHarmonyInstance().PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void Deactivate()
        {
            GetHarmonyInstance().UnpatchAll(HarmonyModID);
            CommuterDestinationPanel.CloseIfOpen();
        }
    }
}
