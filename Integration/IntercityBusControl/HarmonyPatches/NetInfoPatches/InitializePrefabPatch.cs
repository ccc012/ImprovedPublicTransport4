using System;
using HarmonyLib;
using ImprovedPublicTransport.Util;

namespace IntercityBusControl.HarmonyPatches.NetInfoPatches
{
    [HarmonyPatch(typeof(NetInfo))]
    [HarmonyPatch(nameof(NetInfo.InitializePrefab))]
    internal static class InitializePrefabPatch
    {

        internal static void Postfix(NetInfo __instance)
        {
            try
            {
                if (__instance?.name != Mod.IntercityBusLine)
                {
                    return;
                }

                // Update m_transportLineInfo for all bus stations/hubs whose transport types
                // indicate they should support intercity buses. Uses the same detection logic
                // as StationPatcher — no hardcoded building names.
                uint count = (uint)PrefabCollection<BuildingInfo>.LoadedCount();
                for (uint i = 0; i < count; i++)
                {
                    var info = PrefabCollection<BuildingInfo>.GetLoaded(i);
                    if (info?.m_buildingAI is not TransportStationAI ai) continue;

                    var ti1 = ai.m_transportInfo;
                    var ti2 = ai.m_secondaryTransportInfo;

                    bool isShip  = SubService(ti1) == ItemClass.SubService.PublicTransportShip
                                || SubService(ti2) == ItemClass.SubService.PublicTransportShip;
                    bool isTrain = SubService(ti1) == ItemClass.SubService.PublicTransportTrain
                                || SubService(ti2) == ItemClass.SubService.PublicTransportTrain;
                    if (isShip || isTrain) continue;

                    bool isBusPrimary   = SubService(ti1) == ItemClass.SubService.PublicTransportBus;
                    bool isBusSecondary = SubService(ti2) == ItemClass.SubService.PublicTransportBus;
                    if (!(isBusPrimary ^ isBusSecondary)) continue;

                    // For pure bus stations: only update if m_transportLineInfo is null or already
                    // the intercity bus line — don't overwrite regular bus depots.
                    if (isBusPrimary && ai.m_transportLineInfo != null
                        && ai.m_transportLineInfo.name != Mod.IntercityBusLine) continue;

                    StationPatcher.RecordOriginalState(info, ai);
                    ai.m_transportLineInfo = __instance;
                }
            }
            catch (Exception e)
            {
                Utils.LogError($"Intercity Bus Control - NetInfoPatches.InitializePrefabPatch error: {e.Message}");
            }
        }

        private static ItemClass.SubService SubService(TransportInfo ti) =>
            ti?.m_class.m_subService ?? ItemClass.SubService.None;
    }
}