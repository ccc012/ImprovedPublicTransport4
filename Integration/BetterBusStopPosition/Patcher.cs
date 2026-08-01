using HarmonyLib;
using ImprovedPublicTransport.Util;

namespace BetterBusStopPosition
{

public static class Patcher
{
    private const string harmonyId = "llunak.BetterBusStopPosition";
    private static bool patched = false;

    public static void PatchAll()
    {
        if( patched )
            return;
        var harmony = new Harmony( harmonyId );
        // Scope to this integration only - full-assembly PatchAll would re-apply every
        // other Integration/* [HarmonyPatch] under this Harmony ID.
        HarmonyScope.PatchNamespace( harmony, "BetterBusStopPosition" );
        patched = true;
    }

    public static void UnpatchAll()
    {
        if( !patched )
            return;
        var harmony = new Harmony( harmonyId );
        harmony.UnpatchAll( harmonyId );
        patched = false;
    }
}

}
