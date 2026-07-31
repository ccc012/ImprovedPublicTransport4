using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using ImprovedPublicTransport.Util;

namespace UnlimitedOutsideConnections
{
    /// <summary>
    /// Harmony reverse patch giving <see cref="BuildingManagerHooks"/> access to
    /// <c>TransportStationAI.CreateConnectionLines</c>, which is private - vanilla only calls it when
    /// a station is built or a connection line is (re)assigned, never retroactively when a new
    /// outside connection appears later, which is exactly the gap this integration closes.
    /// </summary>
    [HarmonyPatch]
    internal static class Patch_TransportStationAI_CreateConnectionLines
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(TransportStationAI), "CreateConnectionLines",
            new[] { typeof(ushort), typeof(Building) },
            new[] { ArgumentType.Normal, ArgumentType.Ref })]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CreateConnectionLines(TransportStationAI buildingAI, ushort buildingID, ref Building data)
        {
            Utils.LogError($"UnlimitedOutsideConnections: reverse patch for TransportStationAI.CreateConnectionLines was not applied (buildingAI={buildingAI}, buildingID={buildingID}).");
            throw new NotImplementedException("TransportStationAI.CreateConnectionLines reverse Harmony patch wasn't applied");
        }
    }
}
