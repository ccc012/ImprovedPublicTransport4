using ColossalFramework.UI;
using HarmonyLib;
using ImprovedPublicTransport.Util;

namespace IntercityBusControl.HarmonyPatches.CityServiceWorldInfoPanelPatches
{
    
    [HarmonyPatch(typeof(CityServiceWorldInfoPanel))]
    [HarmonyPatch("UpdateBindings")]
    internal static class UpdateBindingsPatch
    {
        private static string _originalLabel;
        private static string _originalTooltip;

        // This panel is a single persistent UI instance the game reuses for every city-service
        // building the player inspects (fire, police, health, transport...), so the child lookups
        // below are stable across calls - cache them instead of re-walking the UI tree every frame
        // UpdateBindings runs (i.e. every frame this or ANY city-service panel is open).
        private static UIPanel _cachedPanel;
        private static UILabel _cachedLabel;
        private static UICheckBox _cachedCheckBox;

        internal static void Postfix(CityServiceWorldInfoPanel __instance, InstanceID ___m_InstanceID, UIPanel ___m_intercityTrainsPanel)
        {
            // Defensive checks: panel or label might be unavailable in certain game versions or due to other mods
            if (___m_intercityTrainsPanel == null)
            {
                Utils.LogWarning("Intercity Bus Control - intercity trains panel not found");
                return;
            }

            if (___m_intercityTrainsPanel != _cachedPanel)
            {
                _cachedPanel = ___m_intercityTrainsPanel;
                _cachedLabel = _cachedPanel.Find<UILabel>("Label");
                _cachedCheckBox = _cachedPanel.Find<UICheckBox>("AcceptIntercityTrains");
            }

            var label = _cachedLabel;
            if (label == null)
            {
                Utils.LogWarning("Intercity Bus Control - label not found on intercity trains panel");
                return;
            }

            _originalLabel ??= label.text;
            _originalTooltip ??= label.tooltip;

            var building1 = ___m_InstanceID.Building;
            var instance = BuildingManager.instance;
            var building2 = instance.m_buildings.m_buffer[building1];
            var info = building2.Info;
            // The building may have just been bulldozed/changed while this panel is still open for
            // one more frame - Info can be null in that window. Without this guard the postfix
            // throws every frame until the panel closes, since Harmony does not swallow postfix
            // exceptions.
            if (info == null)
            {
                label.text = _originalLabel;
                label.tooltip = _originalTooltip;
                return;
            }

            var buildingAi = info.m_buildingAI;
            var transportStationAi = buildingAi as TransportStationAI;
            if (transportStationAi == null)
            {
                label.text = _originalLabel;
                label.tooltip = _originalTooltip;
                return;
            }

            var transportLineInfo1 = transportStationAi.GetTransportLineInfo();
            var transportLineInfo2 = transportStationAi.GetSecondaryTransportLineInfo();

            var ships =
                transportLineInfo1 != null && transportLineInfo1.m_class.m_subService ==
                ItemClass.SubService.PublicTransportShip || transportLineInfo2 != null &&
                transportLineInfo2.m_class.m_subService == ItemClass.SubService.PublicTransportShip;

            var intercityTrains = transportLineInfo1 != null && transportLineInfo1.m_class.m_subService == ItemClass.SubService.PublicTransportTrain || transportLineInfo2 != null && transportLineInfo2.m_class.m_subService == ItemClass.SubService.PublicTransportTrain;

            var intercityBus1 = transportLineInfo1 != null && transportLineInfo1.m_class.m_subService == ItemClass.SubService.PublicTransportBus && transportStationAi.m_maxVehicleCount > 0;
            var intercityBus2 = transportLineInfo2 != null && transportLineInfo2.m_class.m_subService == ItemClass.SubService.PublicTransportBus && transportStationAi.m_maxVehicleCount2 > 0;

            // Show the toggle only on buildings our mod explicitly patched (PatchedBuildingNames).
            // We do NOT gate on !ships/!intercityTrains here — mixed hubs (ferry+bus, train+bus)
            // are in PatchedBuildingNames and should show the toggle. Native intercity bus stations
            // were never added to PatchedBuildingNames so they are correctly excluded.
            var intercityBuses = (intercityBus1 || intercityBus2)
                && StationPatcher.PatchedBuildingNames.Contains(info.name);
            var isVisible = intercityBuses;

            if (isVisible)
            {
                ___m_intercityTrainsPanel.parent.isVisible = true;
                ___m_intercityTrainsPanel.isVisible = true;
            }

            if (intercityBuses)
            {
                label.text = ImprovedPublicTransport.Localization.Get("CITYSERVICE_ACCEPTINTERCITYBUSES");
                label.tooltip = ImprovedPublicTransport.Localization.Get("CITYSERVICE_ACCEPTINTERCITYBUSES_TOOLTIP");

                // The checkbox itself is vanilla's "AcceptIntercityTrains" - its click handler is
                // intercepted for these buildings (see Patch_OnAcceptsIntercityChanged) so it never
                // reaches acceptsInterCityTrains, which is really just an alias for isEmptying;
                // toggling it on a bus terminal would put the building into vanilla's shutdown/
                // evacuate state. Reflect OUR OWN per-building state here instead. Setting isChecked
                // does re-fire eventCheckChanged, but that is safe: the intercepting prefix below
                // just re-persists the same value for bus-patched buildings rather than touching
                // isEmptying, so no detach/reattach dance is needed to avoid it.
                if (_cachedCheckBox != null)
                {
                    _cachedCheckBox.isChecked = IntercityAcceptanceState.Accepts(building1);
                }
            }
            else
            {
                label.text = _originalLabel;
                label.tooltip = _originalTooltip;
            }
        }
    }
}