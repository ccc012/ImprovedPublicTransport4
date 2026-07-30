using System;
using ColossalFramework.UI;
using HarmonyLib;
using ImprovedPublicTransport.Util;

namespace IntercityBusControl.HarmonyPatches.CityServiceWorldInfoPanelPatches
{
    /// <summary>
    /// Intercepts clicks on the checkbox <see cref="UpdateBindingsPatch"/> relabels to "Accept
    /// intercity buses" on a bus terminal. Vanilla's own handler
    /// (<c>CityServiceWorldInfoPanel.OnAcceptsIntercityChanged</c>) just sets
    /// <c>acceptsInterCityTrains</c>, which is a plain alias for <c>isEmptying</c> - so without this
    /// patch, unchecking the relabeled box on a bus terminal would put that building into vanilla's
    /// shutdown/evacuate state, not stop it from accepting buses.
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
                if (info == null || !StationPatcher.PatchedBuildingNames.Contains(info.name))
                {
                    // Not one of ours (a real train station, or an untouched building) - let
                    // vanilla's own accept-intercity-trains behaviour run as normal.
                    return true;
                }

                IntercityAcceptanceState.SetAccepts(buildingId, value);
                return false; // skip the original - never touch isEmptying for a bus terminal.
            }
            catch (Exception ex)
            {
                Utils.LogError($"IntercityBusControl: failed in OnAcceptsIntercityChanged prefix: {ex.Message}");
                return true;
            }
        }
    }
}
