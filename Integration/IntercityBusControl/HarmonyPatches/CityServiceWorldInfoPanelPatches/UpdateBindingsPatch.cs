using System;
using System.Reflection;
using ColossalFramework.UI;
using HarmonyLib;
using ImprovedPublicTransport.Util;
using UnityEngine;

namespace IntercityBusControl.HarmonyPatches.CityServiceWorldInfoPanelPatches
{
    /// <summary>
    /// Shows and drives the "Accept intercity buses" control on bus terminals.
    /// </summary>
    /// <remarks>
    /// Deep-dive (playtest 2026-07-31): hijacking vanilla <c>m_AcceptIntercityTrains</c> is
    /// unstable. Vanilla <c>UpdateBindings</c> rewrites that checkbox from <c>isEmptying</c> every
    /// frame (train semantics). Even with IsInUpdateBindings / mouse gates, real clicks either
    /// never reached <c>OnAcceptsIntercityChanged</c> or arrived as True→False flip pairs.
    /// <para>
    /// Fix: hide the vanilla checkbox on bus terminals and own a dedicated IPT checkbox with its
    /// own eventCheckChanged. No isEmptying, no vanilla handler, no race.
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(CityServiceWorldInfoPanel), "UpdateBindings")]
    internal static class UpdateBindingsPatch
    {
        private const string IptCheckboxName = "IPT4_AcceptIntercityBuses";

        private static string _originalLabel;
        private static string _originalTooltip;
        private static ushort _lastLoggedBuilding;

        /// <summary>Kept for OnAccepts prefix (still blocks vanilla for bus terminals).</summary>
        internal static bool IsSyncingDisplay;
        internal static bool IsInUpdateBindings;

        private static readonly FieldInfo IntercityPanelField =
            AccessTools.Field(typeof(CityServiceWorldInfoPanel), "m_intercityTrainsPanel");

        private static readonly FieldInfo AcceptCheckboxField =
            AccessTools.Field(typeof(CityServiceWorldInfoPanel), "m_AcceptIntercityTrains");

        private static readonly FieldInfo InstanceIdField =
            AccessTools.Field(typeof(CityServiceWorldInfoPanel), "m_InstanceID");

        private static readonly FieldInfo VehicleSelectorContainerField =
            AccessTools.Field(typeof(CityServiceWorldInfoPanel), "m_VehicleSelectorContainer");

        private static readonly FieldInfo AdditionalVehicleSelectorContainerField =
            AccessTools.Field(typeof(CityServiceWorldInfoPanel), "m_AdditionalVehicleSelectorContainer");

        private static UIPanel _cachedPanel;
        private static UILabel _cachedLabel;
        private static UICheckBox _vanillaCheckBox;
        private static UICheckBox _iptCheckBox;
        private static ushort _iptBuildingId;
        private static bool _iptHooked;

        /// <summary>No-op kept so older ForceCheckboxVisual call sites still compile.</summary>
        internal static void ForceCheckboxVisual(bool accepts)
        {
            if (_iptCheckBox == null)
            {
                return;
            }

            IsSyncingDisplay = true;
            try
            {
                _iptCheckBox.isChecked = accepts;
            }
            finally
            {
                IsSyncingDisplay = false;
            }
        }

        [HarmonyPrefix]
        internal static void Prefix()
        {
            IsInUpdateBindings = true;
        }

        [HarmonyFinalizer]
        internal static void Finalizer()
        {
            IsInUpdateBindings = false;
        }

        [HarmonyPostfix]
        internal static void Postfix(
            CityServiceWorldInfoPanel __instance,
            InstanceID ___m_InstanceID)
        {
            try
            {
                var panel = IntercityPanelField?.GetValue(__instance) as UIPanel;
                if (panel == null)
                {
                    return;
                }

                if (panel != _cachedPanel)
                {
                    _cachedPanel = panel;
                    _cachedLabel = panel.Find<UILabel>("Label");
                    _vanillaCheckBox = AcceptCheckboxField?.GetValue(__instance) as UICheckBox
                                       ?? panel.Find<UICheckBox>("AcceptIntercityTrains");
                    // Drop any previous IPT checkbox if the panel object was recreated.
                    _iptCheckBox = panel.Find<UICheckBox>(IptCheckboxName);
                    _iptHooked = false;
                }

                var label = _cachedLabel;
                if (label == null)
                {
                    return;
                }

                _originalLabel ??= label.text;
                _originalTooltip ??= label.tooltip;

                var buildingId = ___m_InstanceID.Building;
                if (buildingId == 0 && InstanceIdField != null)
                {
                    var id = (InstanceID)InstanceIdField.GetValue(__instance);
                    buildingId = id.Building;
                }

                if (buildingId == 0)
                {
                    return;
                }

                var info = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info;
                if (info == null)
                {
                    RestoreLabel(label);
                    HideIptCheckbox();
                    return;
                }

                var stationAi = info.m_buildingAI as TransportStationAI;
                if (stationAi == null)
                {
                    RestoreLabel(label);
                    HideIptCheckbox();
                    return;
                }

                var show = ShouldShowIntercityBusToggle(info, stationAi);

                if (_lastLoggedBuilding != buildingId && Diagnostics.VerboseRuntimeLogs)
                {
                    _lastLoggedBuilding = buildingId;
                    Utils.Log(
                        $"IntercityBusControl UI: building={buildingId} prefab='{info.name}' " +
                        $"show={show} inPatchedSet={StationPatcher.PatchedBuildingNames.Contains(info.name)} " +
                        $"line='{stationAi.m_transportLineInfo?.name}' " +
                        $"ti='{stationAi.m_transportInfo?.name}' ti2='{stationAi.m_secondaryTransportInfo?.name}' " +
                        $"panel=True vanillaCb={(_vanillaCheckBox != null)} iptCb={(_iptCheckBox != null)}");
                }
                else if (_lastLoggedBuilding != buildingId)
                {
                    _lastLoggedBuilding = buildingId;
                }

                if (!show)
                {
                    RestoreLabel(label);
                    // Train / non-bus: restore vanilla checkbox, hide ours.
                    if (_vanillaCheckBox != null)
                    {
                        _vanillaCheckBox.isVisible = true;
                    }

                    HideIptCheckbox();
                    return;
                }

                // Force the vanilla intercity row visible (vanilla hides it for non-train buildings).
                if (panel.parent != null)
                {
                    panel.parent.isVisible = true;
                    panel.parent.Show();
                }

                panel.isVisible = true;
                panel.Show();

                label.text = ImprovedPublicTransport.Localization.Get("CITYSERVICE_ACCEPTINTERCITYBUSES");
                label.tooltip = ImprovedPublicTransport.Localization.Get("CITYSERVICE_ACCEPTINTERCITYBUSES_TOOLTIP");

                StationPatcher.PatchedBuildingNames.Add(info.name);
                HideStationDepotSelectors(__instance);

                // Hide vanilla train-oriented checkbox — it fights isEmptying every frame.
                if (_vanillaCheckBox != null)
                {
                    _vanillaCheckBox.isVisible = false;
                    _vanillaCheckBox.Hide();
                    _vanillaCheckBox.isEnabled = false;
                }

                EnsureIptCheckbox(panel, buildingId);

                if (_iptCheckBox != null)
                {
                    _iptBuildingId = buildingId;
                    var accepts = IntercityAcceptanceState.Accepts(buildingId);
                    IsSyncingDisplay = true;
                    try
                    {
                        _iptCheckBox.isVisible = true;
                        _iptCheckBox.isEnabled = true;
                        _iptCheckBox.Show();
                        if (_iptCheckBox.isChecked != accepts)
                        {
                            _iptCheckBox.isChecked = accepts;
                        }
                    }
                    finally
                    {
                        IsSyncingDisplay = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"IntercityBusControl UpdateBindingsPatch: {ex.Message}");
            }
        }

        private static void EnsureIptCheckbox(UIPanel panel, ushort buildingId)
        {
            if (_iptCheckBox == null)
            {
                // Same construction as UIUtils.CreateCheckBox — independent of vanilla control.
                _iptCheckBox = panel.AddUIComponent<UICheckBox>();
                _iptCheckBox.name = IptCheckboxName;
                _iptCheckBox.size = new Vector2(16f, 16f);
                _iptCheckBox.clipChildren = true;
                _iptCheckBox.relativePosition = _vanillaCheckBox != null
                    ? _vanillaCheckBox.relativePosition
                    : new Vector3(0f, 2f);

                var uncheckedSprite = _iptCheckBox.AddUIComponent<UISprite>();
                uncheckedSprite.spriteName = "check-unchecked";
                uncheckedSprite.size = new Vector2(16f, 16f);
                uncheckedSprite.relativePosition = Vector3.zero;

                var checkedSprite = uncheckedSprite.AddUIComponent<UISprite>();
                checkedSprite.spriteName = "check-checked";
                checkedSprite.size = new Vector2(16f, 16f);
                checkedSprite.relativePosition = Vector3.zero;
                _iptCheckBox.checkedBoxObject = checkedSprite;

                // Match vanilla sprites/atlas when available so it looks native.
                try
                {
                    if (_vanillaCheckBox != null)
                    {
                        var vanillaSprites = _vanillaCheckBox.GetComponentsInChildren<UISprite>(true);
                        if (vanillaSprites != null)
                        {
                            foreach (var vs in vanillaSprites)
                            {
                                if (vs == null)
                                {
                                    continue;
                                }

                                if (vs.spriteName != null
                                    && vs.spriteName.IndexOf("uncheck", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    uncheckedSprite.atlas = vs.atlas;
                                    uncheckedSprite.spriteName = vs.spriteName;
                                }
                                else if (vs.spriteName != null
                                         && vs.spriteName.IndexOf("check", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    checkedSprite.atlas = vs.atlas;
                                    checkedSprite.spriteName = vs.spriteName;
                                }
                            }
                        }

                        if (_vanillaCheckBox.checkedBoxObject is UISprite vCheck)
                        {
                            checkedSprite.atlas = vCheck.atlas;
                            checkedSprite.spriteName = vCheck.spriteName;
                        }
                    }
                }
                catch
                {
                    // default InGame atlas sprites are fine
                }

                _iptHooked = false;
            }

            if (!_iptHooked)
            {
                _iptCheckBox.eventCheckChanged -= OnIptCheckChanged;
                _iptCheckBox.eventCheckChanged += OnIptCheckChanged;
                _iptHooked = true;
                Utils.Log("IntercityBusControl: IPT accept-intercity checkbox hook installed.");
            }

            _iptBuildingId = buildingId;
        }

        private static void HideIptCheckbox()
        {
            if (_iptCheckBox != null)
            {
                _iptCheckBox.isVisible = false;
                _iptCheckBox.Hide();
            }
        }

        private static void OnIptCheckChanged(UIComponent component, bool value)
        {
            // Only ignore our own programmatic writes — never use IsInUpdateBindings here:
            // the IPT checkbox is not touched by vanilla UpdateBindings.
            if (IsSyncingDisplay)
            {
                return;
            }

            Commit(value, "ipt-checkbox");
        }

        private static void Commit(bool value, string source)
        {
            if (_iptBuildingId == 0)
            {
                return;
            }

            var buildingId = _iptBuildingId;
            var info = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info;
            var name = info != null ? info.name : "?";

            if (IntercityAcceptanceState.Accepts(buildingId) == value)
            {
                return;
            }

            Utils.Log(
                $"IntercityBusControl: building {buildingId} ({name}) accept-intercity-buses " +
                $"-> {value} ({source}, defaultWouldBe={IntercityAcceptanceState.DefaultAccepts(buildingId)})");

            IntercityAcceptanceState.SetAccepts(buildingId, value);
            ForceCheckboxVisual(value);
        }

        internal static bool ShouldShowIntercityBusToggle(BuildingInfo info, TransportStationAI ai)
        {
            if (info == null || ai == null)
            {
                return false;
            }

            if (StationPatcher.PatchedBuildingNames.Contains(info.name))
            {
                return true;
            }

            var name = info.name ?? string.Empty;
            if (name.IndexOf("Intercity Bus Station", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("IntercityBusStation", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (ai.m_transportLineInfo != null
                && string.Equals(ai.m_transportLineInfo.name, Mod.IntercityBusLine, StringComparison.Ordinal))
            {
                return true;
            }

            if (IsIntercityBusTransport(ai.m_transportInfo)
                || IsIntercityBusTransport(ai.m_secondaryTransportInfo))
            {
                return true;
            }

            bool bus1 = IsBusTransport(ai.m_transportInfo);
            bool bus2 = IsBusTransport(ai.m_secondaryTransportInfo);
            if (bus1 ^ bus2)
            {
                if (ai.m_transportLineInfo == null
                    || string.Equals(ai.m_transportLineInfo.name, Mod.IntercityBusLine, StringComparison.Ordinal))
                {
                    return StationPatcher.PatchedBuildingNames.Contains(info.name);
                }
            }

            return false;
        }

        private static bool IsBusTransport(TransportInfo ti) =>
            ti?.m_class != null
            && ti.m_class.m_subService == ItemClass.SubService.PublicTransportBus;

        private static bool IsIntercityBusTransport(TransportInfo ti) =>
            ti != null && string.Equals(ti.name, "Intercity Bus", StringComparison.Ordinal);

        private static void RestoreLabel(UILabel label)
        {
            if (_originalLabel != null)
            {
                label.text = _originalLabel;
            }

            if (_originalTooltip != null)
            {
                label.tooltip = _originalTooltip;
            }
        }

        private static void HideStationDepotSelectors(CityServiceWorldInfoPanel panel)
        {
            try
            {
                if (VehicleSelectorContainerField?.GetValue(panel) is UIPanel primary)
                {
                    primary.isVisible = false;
                    primary.Hide();
                }

                if (AdditionalVehicleSelectorContainerField?.GetValue(panel) is UIPanel secondary)
                {
                    secondary.isVisible = false;
                    secondary.Hide();
                }
            }
            catch
            {
                // Non-fatal
            }
        }
    }
}
