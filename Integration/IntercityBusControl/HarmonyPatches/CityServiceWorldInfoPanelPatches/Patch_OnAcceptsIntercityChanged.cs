using System;
using HarmonyLib;
using ImprovedPublicTransport.Util;

namespace IntercityBusControl.HarmonyPatches.CityServiceWorldInfoPanelPatches
{
    /// <summary>
    /// For bus terminals we fully own acceptance via the IPT checkbox
    /// (<see cref="UpdateBindingsPatch"/>). Vanilla <c>OnAcceptsIntercityChanged</c> must never
    /// run for those buildings — it writes train <c>isEmptying</c> and used to fight our state.
    /// Train stations still use vanilla.
    /// </summary>
    [HarmonyPatch(typeof(CityServiceWorldInfoPanel), "OnAcceptsIntercityChanged")]
    internal static class Patch_OnAcceptsIntercityChanged
    {
        [HarmonyPrefix]
        public static bool Prefix(InstanceID ___m_InstanceID, bool value)
        {
            try
            {
                var buildingId = ___m_InstanceID.Building;
                if (buildingId == 0)
                {
                    return true;
                }

                var info = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info;
                if (info == null)
                {
                    return true;
                }

                var stationAi = info.m_buildingAI as TransportStationAI;
                if (stationAi == null
                    || !UpdateBindingsPatch.ShouldShowIntercityBusToggle(info, stationAi))
                {
                    return true; // train / non-bus — vanilla
                }

                // Bus terminal: swallow vanilla completely (IPT checkbox is the only commit path).
                return false;
            }
            catch (Exception ex)
            {
                Utils.LogError($"IntercityBusControl: failed in OnAcceptsIntercityChanged prefix: {ex.Message}");
                return true;
            }
        }
    }
}
