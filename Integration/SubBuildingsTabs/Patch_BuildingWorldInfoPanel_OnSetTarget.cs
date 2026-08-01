using System;
using System.Collections.Generic;
using ColossalFramework.UI;
using HarmonyLib;
using ImprovedPublicTransport;
using ImprovedPublicTransport.Util;
using UnityEngine;

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

                // UITabstrip.Start() sets this once, but Start() on a component added via
                // AddUIComponent() this frame is deferred by Unity to the next Update cycle - the
                // very first OnSetTarget for a given panel instance can therefore render one frame
                // (sometimes more, if the panel itself hasn't finished sizing for the same target
                // yet) before the offset takes effect, leaving the strip visibly adrift above the
                // panel instead of flush with its top edge. Re-asserting it here, every call, costs
                // nothing and matches the original mod's own approach (upstream re-applies this same
                // offset on every panel visibility-change event rather than trusting a one-time
                // Start() to have already run).
                //
                // Live tests: -50 (wrong) → 0 (gap) → +10 (still short). Bump +5 more → +15.
                // Sub-Buildings Tabs only — not related to Intercity checkbox / depot UI.
                tabstrip.relativePosition = new Vector2(13, 15);
                tabstrip.UpdateInfoPanelTabs(___m_InstanceID.Building);
            }
            catch (Exception ex)
            {
                Utils.LogError($"SubBuildingsTabs: failed to refresh tab strip: {ex.Message}");
            }
        }
    }
}
