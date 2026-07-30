using System;
using System.Collections.Generic;
using ColossalFramework.UI;
using HarmonyLib;
using ImprovedPublicTransport;
using ImprovedPublicTransport.Util;

namespace SubBuildingsTabs
{
    /// <summary>
    /// Attaches (once) and refreshes a <see cref="SubBuildingsTabstrip"/> on any building info
    /// panel, so buildings with sub-buildings (e.g. an airport with a built-in metro station) get
    /// a tab strip to switch between them.
    /// </summary>
    /// <remarks>
    /// Same targeting approach as FlightTracker's BuildingWorldInfoPanelPatch: patching
    /// <see cref="BuildingWorldInfoPanel"/>.OnSetTarget covers every well-behaved subclass via the
    /// vtable, and ShelterWorldInfoPanel is patched explicitly because it overrides the method
    /// without calling base.OnSetTarget.
    /// </remarks>
    [HarmonyPatch]
    public static class Patch_BuildingWorldInfoPanel_OnSetTarget
    {
        [HarmonyTargetMethods]
        public static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(BuildingWorldInfoPanel), "OnSetTarget");

            var shelterType = AccessTools.TypeByName("ShelterWorldInfoPanel");
            if (shelterType != null)
            {
                yield return AccessTools.Method(shelterType, "OnSetTarget");
            }
        }

        [HarmonyPostfix]
        public static void Postfix(BuildingWorldInfoPanel __instance, InstanceID ___m_InstanceID)
        {
            try
            {
                if (!ModSetting.Instance.EnableSubBuildingsTabs)
                {
                    return;
                }

                var tabstrip = __instance.Find<SubBuildingsTabstrip>(SubBuildingsTabstrip.ComponentName)
                                ?? __instance.component.AddUIComponent<SubBuildingsTabstrip>();
                tabstrip.UpdateInfoPanelTabs(___m_InstanceID.Building);
            }
            catch (Exception ex)
            {
                Utils.LogError($"SubBuildingsTabs: failed to refresh tab strip: {ex.Message}");
            }
        }
    }
}
