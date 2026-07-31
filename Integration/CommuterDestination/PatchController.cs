using System.Reflection;
using ColossalFramework;
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

            // SimulationManagerBase<T, U> singletons only register for the game's render/simulation
            // callbacks (BeginOverlayImpl here) once something touches .instance - nothing else in
            // this integration ever did, so the map icons this manager exists to draw never rendered
            // at all, regardless of the panel/patches above working correctly. Forcing creation here
            // is the actual fix, not the Harmony patches themselves.
            _ = Singleton<DestinationOverlayManager>.instance;
        }

        public static void Deactivate()
        {
            GetHarmonyInstance().UnpatchAll(HarmonyModID);
            CommuterDestinationPanel.CloseIfOpen();
        }
    }
}
