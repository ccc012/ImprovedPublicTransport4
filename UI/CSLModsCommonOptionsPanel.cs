using System;
using System.Collections.Generic;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using CSLModsCommon.Common;
using CSLModsCommon.Compatibility;
using CSLModsCommon.Extension;
using CSLModsCommon.KeyBindings;
using CSLModsCommon.Manager;
using CSLModsCommon.UI.Containers;
using CSLModsCommon.UI.DropDown;
using CSLModsCommon.UI.OptionsPanel;
using CSLModsCommon.UI.SettingsCard;
using CSLModsCommon.UI.Sliders;
using CSLModsCommon.UI.Utilities;
using ImprovedPublicTransport.Settings;
using UnityEngine;

namespace ImprovedPublicTransport.UI
{
    // Slider cards don't show their current value by default (unlike the old attribute-based
    // OptionsFramework panel) - this appends it to the header and keeps it live as the slider moves.
    internal static class SliderValueLabelExtensions
    {
        public static SliderCard AddSliderWithValue(this OptionsPanelBase.SettingsSection section, string header, string description,
            float minValue, float maxValue, float stepValue, float defaultValue, Action<float> callback = null,
            string valueSuffix = "", float sliderWidth = 700, float sliderHeight = 16, bool gradientStyle = false,
            Action<SliderCard> beforeLayoutAction = null)
        {
            // Translated headers already end with ":" in most languages - avoid "Header:: value".
            var trimmedHeader = header?.TrimEnd().TrimEnd(':') ?? string.Empty;
            return section.AddSlider(header, description, minValue, maxValue, stepValue, defaultValue, callback,
                sliderWidth, sliderHeight, gradientStyle, card =>
                {
                    void UpdateHeader(float v) => card.Header = $"{trimmedHeader}: {(int)v}{valueSuffix}";
                    UpdateHeader(defaultValue);
                    card.Control.ValueChanged += (_, v) => UpdateHeader(v);
                    beforeLayoutAction?.Invoke(card);
                });
        }
    }

    // Top bar limit: General + 4 IPT mains + Advanced = 6 short tabs.
    // Nested sub-tabs group dense settings. Features live in categories (not a fat Integrations bag).
    public class CSLModsCommonOptionsPanel : OptionsPanelBase
    {
        private const string FleetTab = "Fleet";
        private const string StopsTab = "Stops";
        private const string OverlayTab = "Overlay";
        private const string SystemTab = "System";

        private CSLModsCommon.UI.Sliders.Slider _colorDiffSlider;
        private CSLModsCommon.UI.Sliders.Slider _colorPickAttemptsSlider;
        private CSLModsCommon.UI.SettingsCard.CheckBoxCard _expressBusSelfBalancingCard;
        private CSLModsCommon.UI.SettingsCard.CheckBoxCard _expressBusMiddleStopBalancingCard;
        private CSLModsCommon.UI.SettingsCard.CheckBoxCard _expressBusMinibusCard;
        private CheckBoxCard _ticketPathCostCard;
        private UIComponent _vehicleEditorPositionControl;
        private readonly List<UIComponent> _trainDisplayChildControls = new List<UIComponent>();
        private readonly List<UIComponent> _oocChildControls = new List<UIComponent>();

        private void UpdateColorDiffSlidersEnabled(ModSetting.AutoLineColorStrategy strategy)
        {
            var enabled = strategy != ModSetting.AutoLineColorStrategy.Disabled
                && strategy != ModSetting.AutoLineColorStrategy.CategorisedColor;
            if (_colorDiffSlider != null) _colorDiffSlider.isEnabled = enabled;
            if (_colorPickAttemptsSlider != null) _colorPickAttemptsSlider.isEnabled = enabled;
        }

        private void UpdateExpressBusControlsEnabled(ModSetting.ExpressBusServicesModes mode)
        {
            var enabled = mode != ModSetting.ExpressBusServicesModes.None;
            if (_expressBusSelfBalancingCard != null) _expressBusSelfBalancingCard.Control.isEnabled = enabled;
            if (_expressBusMiddleStopBalancingCard != null) _expressBusMiddleStopBalancingCard.Control.isEnabled = enabled;
            if (_expressBusMinibusCard != null) _expressBusMinibusCard.Control.isEnabled = enabled;
        }

        protected override void AddExtraPage()
        {
            // === Main tab bar (short names — translations must stay short) ===
            // 1 General (framework)  2 Fleet  3 Stops  4 Overlay  5 System  6 Advanced
            // Key bindings live under System (not a 7th top tab).

            var fleetPage = AddPage(FleetTab, Localization.Get("SETTINGS_TAB_FLEET_SHORT"));
            OptionsNestedTabs.Build(fleetPage,
                new OptionsNestedTabs.TabSpec("space", Localization.Get("SETTINGS_SUB_SPACING"), FillFleetSpacingSub),
                new OptionsNestedTabs.TabSpec("budget", Localization.Get("SETTINGS_SUB_BUDGET"), FillBudgetPage),
                new OptionsNestedTabs.TabSpec("color", Localization.Get("SETTINGS_SUB_COLOR"), FillLineColorsPage));

            var stopsPage = AddPage(StopsTab, Localization.Get("SETTINGS_TAB_STOPS_SHORT"));
            OptionsNestedTabs.Build(stopsPage,
                new OptionsNestedTabs.TabSpec("caps", Localization.Get("SETTINGS_SUB_CAPS"), FillStopsPage),
                new OptionsNestedTabs.TabSpec("net", Localization.Get("SETTINGS_SUB_NET"), FillStopsNetworkSub));

            var overlayPage = AddPage(OverlayTab, Localization.Get("SETTINGS_TAB_OVERLAY_SHORT"));
            OptionsNestedTabs.Build(overlayPage,
                new OptionsNestedTabs.TabSpec("display", Localization.Get("SETTINGS_SUB_DISPLAY"), FillTrainDisplayPage),
                new OptionsNestedTabs.TabSpec("info", Localization.Get("SETTINGS_SUB_INFO"), FillOverlayInfoSub));

            var systemPage = AddPage(SystemTab, Localization.Get("SETTINGS_TAB_SYSTEM_SHORT"));
            OptionsNestedTabs.Build(systemPage,
                new OptionsNestedTabs.TabSpec("perf", Localization.Get("SETTINGS_SUB_PERF"), FillPerformancePage),
                new OptionsNestedTabs.TabSpec("compat", Localization.Get("SETTINGS_SUB_COMPAT"), FillCompatibilityPage),
                new OptionsNestedTabs.TabSpec("keys", Localization.Get("SETTINGS_SUB_KEYS"), FillKeyBindingPage));

            // Do not call base.AddExtraPage() — that would add a 7th top-level Key Binding tab.
        }

        /// <summary>Global performance profile + short guidance (one place for heavy UI/scan caps).</summary>
        private void FillPerformancePage(ScrollContainer page)
        {
            var setting = ModSetting.Instance;
            var section = AddSection(page, Localization.Get("SETTINGS_PERFORMANCE_PROFILE"),
                Localization.Get("SETTINGS_PERFORMANCE_PROFILE_TOOLTIP"));
            section.AddDropDown<ModSetting.PerformanceProfiles>(
                Localization.Get("SETTINGS_PERFORMANCE_PROFILE"),
                Localization.Get("SETTINGS_PERFORMANCE_PROFILE_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.PerformanceProfiles>(e => Localization.Get(e switch
                {
                    ModSetting.PerformanceProfiles.Light => "SETTINGS_PERFORMANCE_PROFILE_LIGHT",
                    ModSetting.PerformanceProfiles.Maximum => "SETTINGS_PERFORMANCE_PROFILE_MAXIMUM",
                    _ => "SETTINGS_PERFORMANCE_PROFILE_NORMAL",
                })),
                item => item.Value == setting.PerformanceProfile,
                item => ModSetting.Instance.PerformanceProfile = item.Value,
                null);

            var tips = AddSection(page, Localization.Get("SETTINGS_PERFORMANCE_TIPS_GROUP"),
                Localization.Get("SETTINGS_PERFORMANCE_TIPS_DESCRIPTION"));
            // Description-only section (no extra controls) — tips live in the section blurb.
            _ = tips;
        }

        /// <summary>
        /// Live incompatibility / dependency scan for players — surfaces CSLModsCommon's detector
        /// on a dedicated tab so it is not buried under Advanced.
        /// </summary>
        private void FillCompatibilityPage(ScrollContainer page)
        {
            var intro = AddSection(page, Localization.Get("SETTINGS_COMPAT_INTRO_GROUP"),
                Localization.Get("SETTINGS_COMPAT_INTRO_DESCRIPTION"));

            intro.AddButton(
                Localization.Get("SETTINGS_COMPAT_RUN_CHECK"),
                Localization.Get("SETTINGS_COMPAT_RUN_CHECK_TOOLTIP"),
                Localization.Get("SETTINGS_COMPAT_RUN_CHECK_BUTTON"),
                null,
                onButtonClicked: _ =>
                {
                    try
                    {
                        Domain.DefaultDomain.GetOrCreateManager<CompatibilityManager>().CheckAndShowDialog();
                    }
                    catch (Exception ex)
                    {
                        Util.Utils.LogError($"Compatibility check UI failed: {ex.Message}");
                    }
                });

            try
            {
                var mgr = Domain.DefaultDomain.GetOrCreateManager<CompatibilityManager>();
                mgr.RefreshQuiet();

                var status = mgr.CurrentStatus;
                var ok = status.IsOnlyFlag(CompatibilityStatus.Normal) && !mgr.ShouldRestartGame;

                var statusSection = AddSection(page, Localization.Get("SETTINGS_COMPAT_STATUS_GROUP"),
                    ok
                        ? Localization.Get("SETTINGS_COMPAT_STATUS_OK")
                        : Localization.Get("SETTINGS_COMPAT_STATUS_WARN"));

                var matched = 0;
                foreach (var item in mgr.IncompatibleRule.Lookup.Values)
                {
                    if (!item.IsMatched)
                    {
                        continue;
                    }

                    matched++;
                    var name = item.DisplayName ?? item.AssemblyName ?? "?";
                    var detail = string.IsNullOrEmpty(item.CustomWarningMessage)
                        ? Localization.Get("SETTINGS_COMPAT_MATCHED_GENERIC")
                        : item.CustomWarningMessage;
                    statusSection.AddLabel(name, Localization.Get("SETTINGS_COMPAT_BADGE_CONFLICT"), detail, null);
                }

                if (mgr.DependencyRule.IsMatched)
                {
                    foreach (var dep in mgr.DependencyRule.Lookup)
                    {
                        if (dep.IsIncluded)
                        {
                            continue;
                        }

                        statusSection.AddLabel(
                            dep.DisplayName ?? dep.AssemblyName,
                            Localization.Get("SETTINGS_COMPAT_BADGE_MISSING"),
                            Localization.Get("SETTINGS_COMPAT_MISSING_DEP"),
                            null);
                    }
                }

                if (matched == 0 && !mgr.DependencyRule.IsMatched)
                {
                    statusSection.AddLabel(
                        Localization.Get("SETTINGS_COMPAT_NO_ISSUES"),
                        Localization.Get("SETTINGS_COMPAT_BADGE_OK"),
                        null,
                        null);
                }
            }
            catch (Exception ex)
            {
                Util.Utils.LogError($"Compatibility status section failed: {ex.Message}");
            }

            var guide = AddSection(page, Localization.Get("SETTINGS_COMPAT_GUIDE_GROUP"),
                Localization.Get("SETTINGS_COMPAT_GUIDE_DESCRIPTION"));
            _ = guide;
        }

        protected override void FillKeyBindingPage(ScrollContainer page)
        {
            var section = AddSection(page, Localization.Get("SETTINGS_KEYBINDINGS_GROUP"),
                Localization.Get("SETTINGS_KEYBINDINGS_GROUP_DESCRIPTION"));

            // Bound actions live on IptHotkeys; rebinding updates the same KeyBinding instances
            // that KeyBindingManager dispatches (no save format change required for v1).
            section.AddKeyBinding(IptHotkeys.TrainDisplayToggle,
                Localization.Get("SETTINGS_HOTKEY_TRAIN_DISPLAY"),
                Localization.Get("SETTINGS_HOTKEY_TRAIN_DISPLAY_TOOLTIP"));
            section.AddKeyBinding(IptHotkeys.LineColorRefresh,
                Localization.Get("SETTINGS_HOTKEY_LINE_COLOR"),
                Localization.Get("SETTINGS_HOTKEY_LINE_COLOR_TOOLTIP"));
            section.AddKeyBinding(IptHotkeys.AdvancedStopSelectionAlternateKey,
                Localization.Get("SETTINGS_HOTKEY_ADVSTOPSELECT_ALT"),
                Localization.Get("SETTINGS_HOTKEY_ADVSTOPSELECT_ALT_TOOLTIP"));
        }

        protected override void FillAdvancedPage(ScrollContainer page)
        {
            var setting = ModSetting.Instance;

            // Links first (was always near the top of Advanced before the restructure).
            var links = AddSection(page, Localization.Get("SETTINGS_ADVANCED_LINKS_GROUP"));
            links.AddButton(Localization.Get("SETTINGS_GITHUB_REPO"), null, CSLModsCommon.Localization.SharedTranslations.Website, null,
                onButtonClicked: _ => CSLModsCommon.Utilities.URLHelper.OpenURL("https://github.com/ccc012/ImprovedPublicTransport4"));

            var future = AddSection(page, Localization.Get("SETTINGS_FUTURE_GROUP"),
                Localization.Get("SETTINGS_FUTURE_GROUP_DESCRIPTION"));
            AddFutureSpoiler(future, "SETTINGS_FUTURE_REVERSIBLE_TRAM", "SETTINGS_FUTURE_REVERSIBLE_TRAM_TIP");
            AddFutureSpoiler(future, "SETTINGS_FUTURE_BREAKDOWN", "SETTINGS_FUTURE_BREAKDOWN_TIP");
            AddFutureSpoiler(future, "SETTINGS_FUTURE_BUSWAYPOINT", "SETTINGS_FUTURE_BUSWAYPOINT_TIP");

            // Two behaviours that always ran with no way to turn them off - added here for
            // compatibility with other mods/players who prefer manual control over the same thing.
            var hidden = AddSection(page, Localization.Get("SETTINGS_HIDDENBEHAVIOUR_GROUP"),
                Localization.Get("SETTINGS_HIDDENBEHAVIOUR_GROUP_DESC"));
            hidden.AddCheckBox(setting.EnableAutoNameStops, Localization.Get("SETTINGS_AUTONAMESTOPS_ENABLE"), null, Localization.Get("SETTINGS_AUTONAMESTOPS_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableAutoNameStops = isChecked;
                    Settings.SettingsActions.NotifyReloadRequired("Auto-name stops");
                });
            hidden.AddCheckBox(setting.EnableRescueFullwidthDigits, Localization.Get("SETTINGS_RESCUEFULLWIDTHDIGITS_ENABLE"), null, Localization.Get("SETTINGS_RESCUEFULLWIDTHDIGITS_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableRescueFullwidthDigits = isChecked;
                    Settings.SettingsActions.NotifyReloadRequired("Rescue Fullwidth Digits");
                });

            // Dangerous tools last so casual players do not hit them first.
            FillDeleteLinesPage(page);
        }

        private static void AddFutureSpoiler(SettingsSection section, string titleKey, string tipKey)
        {
            var card = section.AddCheckBox(
                false,
                Localization.Get(titleKey),
                null,
                Localization.Get(tipKey),
                (_, __) => { /* intentional no-op — not implemented yet */ });
            if (card?.Control != null)
            {
                OptionsNestedTabs.SetEnabled(card.Control, false);
            }
        }

        protected override void FillGeneralPage(ScrollContainer page)
        {
            base.FillGeneralPage(page);
            var setting = ModSetting.Instance;

            var profileSection = AddSection(page, Localization.Get("SETTINGS_GAMEPLAY_PROFILE"),
                Localization.Get("SETTINGS_GAMEPLAY_PROFILE_TOOLTIP") + "\n\n" +
                Localization.Get("SETTINGS_GAMEPLAY_PROFILE_DESC_BLOCK"));
            profileSection.AddDropDown<ModSetting.GameplayProfiles>(
                Localization.Get("SETTINGS_GAMEPLAY_PROFILE"),
                null,
                DropDownHelper.FromEnum<ModSetting.GameplayProfiles>(e => Localization.Get(e switch
                {
                    ModSetting.GameplayProfiles.Safe => "SETTINGS_GAMEPLAY_PROFILE_SAFE",
                    ModSetting.GameplayProfiles.Vanilla => "SETTINGS_GAMEPLAY_PROFILE_VANILLA",
                    ModSetting.GameplayProfiles.Recommended => "SETTINGS_GAMEPLAY_PROFILE_RECOMMENDED",
                    ModSetting.GameplayProfiles.Realistic => "SETTINGS_GAMEPLAY_PROFILE_REALISTIC",
                    _ => "SETTINGS_GAMEPLAY_PROFILE_CUSTOM",
                })),
                item => item.Value == setting.GameplayProfile,
                item => Settings.SettingsActions.OnGameplayProfileChanged(item.Value),
                null);

            // One multi-line block — LabelCard row layout was collapsing titles to single-character
            // columns on some locale/layout passes (see screenshots with "C/S/D/H/O").
            var tipsBody =
                "• " + Localization.Get("SETTINGS_QUICK_TIP_1_TITLE") + " — " + Localization.Get("SETTINGS_QUICK_TIP_1_BODY") + "\n"
                + "• " + Localization.Get("SETTINGS_QUICK_TIP_2_TITLE") + " — " + Localization.Get("SETTINGS_QUICK_TIP_2_BODY") + "\n"
                + "• " + Localization.Get("SETTINGS_QUICK_TIP_3_TITLE") + " — " + Localization.Get("SETTINGS_QUICK_TIP_3_BODY");
            var tips = AddSection(page, Localization.Get("SETTINGS_QUICK_TIPS_GROUP"),
                Localization.Get("SETTINGS_QUICK_TIPS_DESC") + "\n\n" + tipsBody);

            var commonSection = AddSection(page, Localization.Get("SETTINGS"));
            commonSection.AddDropDown<ModSetting.VehicleSpeedUnits>(Localization.Get("SETTINGS_SPEED"), Localization.Get("SETTINGS_SPEED_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.VehicleSpeedUnits>(e => Localization.Get(e == ModSetting.VehicleSpeedUnits.KPH ? "SETTINGS_SPEED_KPH" : "SETTINGS_SPEED_MPH")),
                item => item.Value == setting.SpeedUnit,
                item => ModSetting.Instance.SpeedUnit = item.Value, null);
        }

        /// <summary>Fleet → Spacing: unbunching + Express Bus/Tram.</summary>
        private void FillFleetSpacingSub(ScrollContainer page)
        {
            var setting = ModSetting.Instance;

            var intro = AddSection(page, Localization.Get("SETTINGS_SUB_SPACING"),
                Localization.Get("SETTINGS_SUB_SPACING_DESC"));
            _ = intro;

            var unbunchingSection = AddSection(page, Localization.Get("SETTINGS_UNBUNCHING"),
                Localization.Get("EXPLANATION_UNBUNCHING"));
            unbunchingSection.AddSliderWithValue(Localization.Get("SETTINGS_UNBUNCHING_AGGRESSION"),
                Localization.Get("SETTINGS_UNBUNCHING_AGGRESSION_TOOLTIP"),
                0f, 52f, 1f, setting.IntervalAggressionFactor,
                v => ModSetting.Instance.IntervalAggressionFactor = (byte)v);

            unbunchingSection.AddSliderWithValue(Localization.Get("SETTINGS_VEHICLE_COUNT"), Localization.Get("SETTINGS_VEHICLE_COUNT_TOOLTIP"),
                0f, 100f, 1f, setting.DefaultVehicleCount,
                v =>
                {
                    ModSetting.Instance.DefaultVehicleCount = (int)v;
                    SettingsActions.OnDefaultVehicleCountSubmitted((int)v);
                },
                beforeLayoutAction: card =>
                {
                    SettingsActions.VehicleCountSlider = card.Control;
                    SettingsActions.OnBudgetModeChanged((int)ModSetting.Instance.BudgetControl);
                });

            unbunchingSection.AddSliderWithValue(Localization.Get("SETTINGS_SPAWN_TIME_INTERVAL"), Localization.Get("SETTINGS_SPAWN_TIME_INTERVAL_TOOLTIP"),
                0f, 100f, 1f, setting.SpawnTimeInterval,
                v => ModSetting.Instance.SpawnTimeInterval = (int)v, "s");

            unbunchingSection.AddButton(
                Localization.Get("SETTINGS_RESET"),
                Localization.Get("SETTINGS_UNBUNCHING_RESET_BUTTON_TOOLTIP"),
                Localization.Get("SETTINGS_RESET"),
                () => SettingsActions.OnResetButtonClick());

            var ebsBusSection = AddSection(page, Localization.Get("SETTINGS_EBS_GROUP_BUS"),
                Localization.Get("SETTINGS_EBS_GROUP_BUS_DESC"));
            ebsBusSection.AddDropDown<ModSetting.ExpressBusServicesModes>(Localization.Get("SETTINGS_EBS_DROPDOWN_UNBUNCHING_MODE"), Localization.Get("SETTINGS_EBS_TOOLTIP_UNBUNCHING_MODE"),
                DropDownHelper.FromEnum<ModSetting.ExpressBusServicesModes>(e => Localization.Get("SETTINGS_EBS_MODE_" + (e == ModSetting.ExpressBusServicesModes.None ? "NONE" : e.ToString().ToUpperInvariant()))),
                item => item.Value == setting.ExpressBusUnbunchingMode,
                item =>
                {
                    ModSetting.Instance.ExpressBusUnbunchingMode = item.Value;
                    UpdateExpressBusControlsEnabled(item.Value);
                    SettingsActions.OnExpressBusSettingsChanged();
                }, null);
            _expressBusSelfBalancingCard = ebsBusSection.AddCheckBox(setting.ExpressBusEnableSelfBalancing, Localization.Get("SETTINGS_EBS_ENABLE_SELFBAL"), null, Localization.Get("SETTINGS_EBS_TOOLTIP_SELFBAL"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.ExpressBusEnableSelfBalancing = isChecked;
                    SettingsActions.OnExpressBusSettingsChanged();
                });
            _expressBusMiddleStopBalancingCard = ebsBusSection.AddCheckBox(setting.ExpressBusAllowMiddleStopBalancing, Localization.Get("SETTINGS_EBS_ENABLE_SELFBAL_TARGETMID"), null, Localization.Get("SETTINGS_EBS_TOOLTIP_SELFBAL_TARGETMID"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.ExpressBusAllowMiddleStopBalancing = isChecked;
                    SettingsActions.OnExpressBusSettingsChanged();
                });
            _expressBusMinibusCard = ebsBusSection.AddCheckBox(setting.ExpressBusEnableMinibusMode, Localization.Get("SETTINGS_EBS_ENABLE_MINIBUS"), null, Localization.Get("SETTINGS_EBS_TOOLTIP_MINIBUS"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.ExpressBusEnableMinibusMode = isChecked;
                    SettingsActions.OnExpressBusSettingsChanged();
                });
            UpdateExpressBusControlsEnabled(setting.ExpressBusUnbunchingMode);

            var ebsTramSection = AddSection(page, Localization.Get("SETTINGS_EBS_GROUP_TRAM"),
                Localization.Get("SETTINGS_EBS_TOOLTIP_TRAM_UNBUNCHING"));
            ebsTramSection.AddDropDown<ModSetting.ExpressTramServicesModes>(
                Localization.Get("SETTINGS_EBS_DROPDOWN_TRAM_UNBUNCHING_MODE"),
                null,
                DropDownHelper.FromEnum<ModSetting.ExpressTramServicesModes>(e => Localization.Get("SETTINGS_EBS_TRAM_MODE_" + (e == ModSetting.ExpressTramServicesModes.Disabled ? "NONE" : e == ModSetting.ExpressTramServicesModes.LightRail ? "LIGHT_RAIL" : "TRAM"))),
                item => item.Value == setting.ExpressTramUnbunchingMode,
                item =>
                {
                    ModSetting.Instance.ExpressTramUnbunchingMode = item.Value;
                    SettingsActions.OnExpressBusSettingsChanged();
                }, null);
        }

        /// <summary>Everything that touches money: budget control, ticket prices, demand-driven
        /// fleet sizing. Kept apart from Fleet - budget/pricing decisions and vehicle-spacing
        /// decisions are made by different people for different reasons even in the same city.</summary>
        private void FillBudgetPage(ScrollContainer page)
        {
            var setting = ModSetting.Instance;
            var budgetSection = AddSection(page, Localization.Get("SETTINGS_BUDGET"));

            budgetSection.AddDropDown<ModSetting.BudgetControlModes>(Localization.Get("SETTINGS_ENABLE_BUDGET_CONTROL"), Localization.Get("SETTINGS_BUDGET_CONTROL_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.BudgetControlModes>(e => Localization.Get(e == ModSetting.BudgetControlModes.Disabled ? "SETTINGS_BUDGET_CONTROL_DISABLED" : "SETTINGS_BUDGET_CONTROL_ENABLED")),
                item => item.Value == setting.BudgetControl,
                item =>
                {
                    ModSetting.Instance.BudgetControl = item.Value;
                    SettingsActions.OnBudgetModeChanged((int)item.Value);
                }, null);

            budgetSection.AddDropDown<ModSetting.TicketPriceCustomizerModes>(Localization.Get("SETTINGS_BUDGET_TICKET_PRICES"), Localization.Get("SETTINGS_BUDGET_TICKET_PRICES_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.TicketPriceCustomizerModes>(e => Localization.Get(e == ModSetting.TicketPriceCustomizerModes.Disabled ? "SETTINGS_BUDGET_TICKET_PRICES_DISABLED" : "SETTINGS_BUDGET_TICKET_PRICES_ENABLED")),
                item => item.Value == setting.TicketPriceCustomizerMode,
                item =>
                {
                    ModSetting.Instance.TicketPriceCustomizerMode = item.Value;
                    SettingsActions.OnTicketPriceCustomizerChanged((int)item.Value);
                    UpdateTicketPathCostEnabled(item.Value);
                }, null);

            // Optional “fare as toll” — only when ticket prices are on.
            _ticketPathCostCard = budgetSection.AddCheckBox(
                setting.EnableTicketPathfindingCost,
                Localization.Get("SETTINGS_TICKET_PATHFINDING_COST"),
                null,
                Localization.Get("SETTINGS_TICKET_PATHFINDING_COST_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableTicketPathfindingCost = isChecked;
                    SettingsActions.OnTicketPathfindingCostChanged(isChecked);
                });
            UpdateTicketPathCostEnabled(setting.TicketPriceCustomizerMode);

            budgetSection.AddDropDown<ModSetting.AutoLineBudgetModes>(Localization.Get("SETTINGS_AUTO_LINE_BUDGET"), Localization.Get("SETTINGS_AUTO_LINE_BUDGET_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.AutoLineBudgetModes>(e => Localization.Get(e == ModSetting.AutoLineBudgetModes.Disabled ? "SETTINGS_AUTO_LINE_BUDGET_DISABLED" : "SETTINGS_AUTO_LINE_BUDGET_ENABLED")),
                item => item.Value == setting.AutoLineBudgetMode,
                item =>
                {
                    ModSetting.Instance.AutoLineBudgetMode = item.Value;
                    SettingsActions.OnAutoLineBudgetModeChanged((int)item.Value);
                }, null);
        }

        /// <summary>AutoLineColor's own settings, split out of the old "Auto Line" tab so it is not
        /// buried under budget/fleet settings it has nothing to do with.</summary>
        private void FillLineColorsPage(ScrollContainer page)
        {
            var setting = ModSetting.Instance;
            var autoColorSection = AddSection(page, Localization.Get("SETTINGS_TAB_LINECOLORS"));
            autoColorSection.AddDropDown<ModSetting.AutoLineColorStrategy>(Localization.Get("AUTOLINECOLOR_COLOR_STRATEGY"), Localization.Get("AUTOLINECOLOR_COLOR_STRATEGY_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.AutoLineColorStrategy>(e => Localization.Get(GetAutoLineColorStrategyKey(e))),
                item => item.Value == setting.AutoLineColorColorStrategy,
                item =>
                {
                    ModSetting.Instance.AutoLineColorColorStrategy = item.Value;
                    UpdateColorDiffSlidersEnabled(item.Value);
                }, null);
            // Only RandomHue/RandomColor/NamedColors actually use these two settings (they go through
            // ColorSelector.DifferenceThreshold) - Disabled and CategorisedColor (ColorSelector.LeastUsed) ignore them.
            // Kept right under the strategy dropdown they belong to, rather than after the unrelated naming dropdown.
            autoColorSection.AddSliderWithValue(Localization.Get("AUTOLINECOLOR_MIN_COLOR_DIFF"), Localization.Get("AUTOLINECOLOR_MIN_COLOR_DIFF_TOOLTIP"),
                1f, 50f, 1f, setting.AutoLineColorMinColorDiffPercentage,
                v => ModSetting.Instance.AutoLineColorMinColorDiffPercentage = (int)v, "%",
                beforeLayoutAction: card => _colorDiffSlider = card.Control);
            autoColorSection.AddSliderWithValue(Localization.Get("AUTOLINECOLOR_MAX_COLOR_PICK"), Localization.Get("AUTOLINECOLOR_MAX_COLOR_PICK_TOOLTIP"),
                1f, 50f, 1f, setting.AutoLineColorMaxDiffColorPickAttempt,
                v => ModSetting.Instance.AutoLineColorMaxDiffColorPickAttempt = (int)v,
                beforeLayoutAction: card =>
                {
                    _colorPickAttemptsSlider = card.Control;
                    UpdateColorDiffSlidersEnabled(setting.AutoLineColorColorStrategy);
                });
            autoColorSection.AddDropDown<ModSetting.AutoLineColorNamingStrategy>(Localization.Get("AUTOLINECOLOR_NAMING_STRATEGY"), Localization.Get("AUTOLINECOLOR_NAMING_STRATEGY_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.AutoLineColorNamingStrategy>(e => Localization.Get("AUTOLINECOLOR_NAMING_" + (e == ModSetting.AutoLineColorNamingStrategy.NamedColors ? "COLORS" : e.ToString().ToUpperInvariant()))),
                item => item.Value == setting.AutoLineColorNamingStrategyMode,
                item => ModSetting.Instance.AutoLineColorNamingStrategyMode = item.Value, null);
        }

        /// <summary>Stops → Network: stop geometry / multi-type / elevated (ex-Integrations).</summary>
        private void FillStopsNetworkSub(ScrollContainer page)
        {
            var setting = ModSetting.Instance;
            var net = AddSection(page, Localization.Get("SETTINGS_SUB_NET"),
                Localization.Get("SETTINGS_SUB_NET_DESC") + "\n\n" + Localization.Get("SETTINGS_SUB_NET_BBSP_NOTE"));

            net.AddDropDown<ModSetting.BbspLogicModes>(Localization.Get("SETTINGS_BBSP"), Localization.Get("SETTINGS_BBSP_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.BbspLogicModes>(e => Localization.Get(e == ModSetting.BbspLogicModes.Disabled ? "SETTINGS_BBSP_MODE_DISABLED" : "SETTINGS_BBSP_MODE_ORIGINAL")),
                item => item.Value == setting.BbspLogic,
                item =>
                {
                    ModSetting.Instance.BbspLogic = item.Value;
                    SettingsActions.NotifyReloadRequired("Better Bus Stop Position");
                }, null);

            net.AddCheckBox(setting.EnableAdvancedStopSelection, Localization.Get("SETTINGS_ADVANCEDSTOPSELECTION_ENABLE"), null, Localization.Get("SETTINGS_ADVANCEDSTOPSELECTION_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableAdvancedStopSelection = isChecked;
                    SettingsActions.NotifyReloadRequired("Advanced Stop Selection");
                });
            net.AddCheckBox(setting.EnableElevatedStops, Localization.Get("SETTINGS_ELEVATEDSTOPS_ENABLE"), null, Localization.Get("SETTINGS_ELEVATEDSTOPS_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableElevatedStops = isChecked;
                    SettingsActions.OnElevatedStopsChanged(isChecked);
                });
            net.AddCheckBox(setting.EnableSharedStopEnabler, Localization.Get("SETTINGS_SHAREDSTOPENABLER_ENABLE"), null, Localization.Get("SETTINGS_SHAREDSTOPENABLER_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableSharedStopEnabler = isChecked;
                    SettingsActions.OnSharedStopEnablerChanged(isChecked);
                    SettingsActions.NotifyReloadRequired("Shared Stop Enabler");
                });
            net.AddCheckBox(setting.EnableStopStacker, Localization.Get("SETTINGS_STOPSTACKER_ENABLE"), null, Localization.Get("SETTINGS_STOPSTACKER_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableStopStacker = isChecked;
                    SettingsActions.OnStopStackerChanged(isChecked);
                });

            var vehicles = AddSection(page, Localization.Get("SETTINGS_SUB_VEHICLES"),
                Localization.Get("SETTINGS_SUB_VEHICLES_DESC"));
            vehicles.AddCheckBox(setting.EnableBetterBoarding, Localization.Get("SETTINGS_BETTERBOARDING_ENABLE"), null, Localization.Get("SETTINGS_BETTERBOARDING_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableBetterBoarding = isChecked;
                    SettingsActions.NotifyReloadRequired("Better Boarding");
                });
            vehicles.AddCheckBox(setting.EnableEmptyBeforeReturnToDepot, Localization.Get("SETTINGS_EMPTY_BEFORE_DEPOT"), null, Localization.Get("SETTINGS_EMPTY_BEFORE_DEPOT_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.EnableEmptyBeforeReturnToDepot = isChecked);
            vehicles.AddCheckBox(setting.EnableSingleTrainTrackAI, Localization.Get("SETTINGS_STTAI_ENABLE"), null, Localization.Get("SETTINGS_STTAI_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableSingleTrainTrackAI = isChecked;
                    SettingsActions.OnSingleTrainTrackAIChanged(isChecked);
                });
            vehicles.AddCheckBox(setting.EnableTaxiStandFix, Localization.Get("SETTINGS_TAXISTANDFIX_ENABLE"), null, Localization.Get("SETTINGS_TAXISTANDFIX_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableTaxiStandFix = isChecked;
                    SettingsActions.OnTaxiStandFixChanged(isChecked);
                });
            vehicles.AddCheckBox(setting.EnableMileageTaxi, Localization.Get("SETTINGS_MILEAGETAXI_ENABLE"), null, Localization.Get("SETTINGS_MILEAGETAXI_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableMileageTaxi = isChecked;
                    SettingsActions.NotifyReloadRequired("Mileage Taxi Services");
                });
            vehicles.AddCheckBox(setting.EnablePublicTransportUnstucker, Localization.Get("SETTINGS_PTU_ENABLE"), null, Localization.Get("SETTINGS_PTU_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnablePublicTransportUnstucker = isChecked;
                    SettingsActions.OnPublicTransportUnstuckerChanged(isChecked ? 1 : 0);
                });

            AddDepotCapacitySection(page, setting);
            AddOutsideWorldSection(page, setting);
        }

        private void AddDepotCapacitySection(ScrollContainer page, ModSetting setting)
        {
            var depots = AddSection(page, Localization.Get("SETTINGS_SUB_DEPOTS"),
                Localization.Get("SETTINGS_SUB_DEPOTS_DESC"));
            void DepotDrop(string headerKey, string tipKey, Func<ModSetting.DepotCapacityModes> get, Action<ModSetting.DepotCapacityModes> set)
            {
                depots.AddDropDown<ModSetting.DepotCapacityModes>(Localization.Get(headerKey), Localization.Get(tipKey),
                    DropDownHelper.FromEnum<ModSetting.DepotCapacityModes>(e => Localization.Get(e switch
                    {
                        ModSetting.DepotCapacityModes.Realistic => "SETTINGS_DEPOT_CAPACITY_REALISTIC",
                        ModSetting.DepotCapacityModes.Intermediate => "SETTINGS_DEPOT_CAPACITY_INTERMEDIATE",
                        _ => "SETTINGS_DEPOT_CAPACITY_DISABLED",
                    })),
                    item => item.Value == get(),
                    item => { set(item.Value); SettingsActions.OnDepotCapacityModeChanged(); }, null);
            }

            DepotDrop("SETTINGS_BUS_DEPOT_CAPACITY", "SETTINGS_BUS_DEPOT_CAPACITY_TOOLTIP",
                () => setting.BusDepotCapacityMode, v => ModSetting.Instance.BusDepotCapacityMode = v);
            DepotDrop("SETTINGS_TRAM_DEPOT_CAPACITY", "SETTINGS_TRAM_DEPOT_CAPACITY_TOOLTIP",
                () => setting.TramDepotCapacityMode, v => ModSetting.Instance.TramDepotCapacityMode = v);
            DepotDrop("SETTINGS_TAXI_DEPOT_CAPACITY", "SETTINGS_TAXI_DEPOT_CAPACITY_TOOLTIP",
                () => setting.TaxiDepotCapacityMode, v => ModSetting.Instance.TaxiDepotCapacityMode = v);
            DepotDrop("SETTINGS_TROLLEYBUS_DEPOT_CAPACITY", "SETTINGS_TROLLEYBUS_DEPOT_CAPACITY_TOOLTIP",
                () => setting.TrolleybusDepotCapacityMode, v => ModSetting.Instance.TrolleybusDepotCapacityMode = v);
            DepotDrop("SETTINGS_FERRY_DEPOT_CAPACITY", "SETTINGS_FERRY_DEPOT_CAPACITY_TOOLTIP",
                () => setting.FerryDepotCapacityMode, v => ModSetting.Instance.FerryDepotCapacityMode = v);
        }

        private void AddOutsideWorldSection(ScrollContainer page, ModSetting setting)
        {
            _oocChildControls.Clear();
            var outside = AddSection(page, Localization.Get("SETTINGS_SUB_OUTSIDE"),
                Localization.Get("SETTINGS_SUB_OUTSIDE_DESC"));
            outside.AddCheckBox(setting.EnableIntercityBusControl, Localization.Get("SETTINGS_INTERCITY_BUS_ENABLE"), null, Localization.Get("SETTINGS_INTERCITY_BUS_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableIntercityBusControl = isChecked;
                    SettingsActions.OnIntercityBusControlChanged(isChecked);
                });
            outside.AddCheckBox(setting.EnableOptimisedOutsideConnections, Localization.Get("SETTINGS_OOC_ENABLE"), null, Localization.Get("SETTINGS_OOC_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableOptimisedOutsideConnections = isChecked;
                    UpdateOocChildrenEnabled(isChecked);
                });

            void TrackOoc(UIComponent c)
            {
                if (c != null)
                {
                    _oocChildControls.Add(c);
                }
            }

            TrackOoc(outside.AddSliderWithValue(Localization.Get("SETTINGS_OOC_WAIT_MULTIPLIER"), Localization.Get("SETTINGS_OOC_WAIT_MULTIPLIER_TOOLTIP"),
                1f, 10f, 1f, setting.OutsideConnectionWaitMultiplier,
                v => ModSetting.Instance.OutsideConnectionWaitMultiplier = (int)v, "x")?.Control);
            TrackOoc(outside.AddDropDown<ModSetting.PassengerWaitScopes>(Localization.Get("SETTINGS_OOC_PASSENGER_SCOPE"), Localization.Get("SETTINGS_OOC_PASSENGER_SCOPE_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.PassengerWaitScopes>(e => Localization.Get(e switch
                {
                    ModSetting.PassengerWaitScopes.CityWide => "SETTINGS_OOC_PASSENGER_SCOPE_CITYWIDE",
                    ModSetting.PassengerWaitScopes.Disabled => "SETTINGS_OOC_PASSENGER_SCOPE_DISABLED",
                    _ => "SETTINGS_OOC_PASSENGER_SCOPE_OUTSIDE",
                })),
                item => item.Value == setting.OutsideConnectionPassengerWaitScope,
                item => ModSetting.Instance.OutsideConnectionPassengerWaitScope = item.Value, null)?.Control);
            TrackOoc(outside.AddCheckBox(setting.DisableRoadDummyTraffic, Localization.Get("SETTINGS_OOC_DISABLE_DUMMY_ROAD"), null, Localization.Get("SETTINGS_OOC_DISABLE_DUMMY_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.DisableRoadDummyTraffic = isChecked)?.Control);
            TrackOoc(outside.AddCheckBox(setting.DisableTrainDummyTraffic, Localization.Get("SETTINGS_OOC_DISABLE_DUMMY_TRAIN"), null, Localization.Get("SETTINGS_OOC_DISABLE_DUMMY_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.DisableTrainDummyTraffic = isChecked)?.Control);
            TrackOoc(outside.AddCheckBox(setting.DisablePlaneDummyTraffic, Localization.Get("SETTINGS_OOC_DISABLE_DUMMY_PLANE"), null, Localization.Get("SETTINGS_OOC_DISABLE_DUMMY_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.DisablePlaneDummyTraffic = isChecked)?.Control);
            TrackOoc(outside.AddCheckBox(setting.DisableShipDummyTraffic, Localization.Get("SETTINGS_OOC_DISABLE_DUMMY_SHIP"), null, Localization.Get("SETTINGS_OOC_DISABLE_DUMMY_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.DisableShipDummyTraffic = isChecked)?.Control);

            // Unlimited outside is independent of OOC — leave clickable.
            outside.AddCheckBox(setting.EnableUnlimitedOutsideConnections, Localization.Get("SETTINGS_UOC_ENABLE"), null, Localization.Get("SETTINGS_UOC_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableUnlimitedOutsideConnections = isChecked;
                    SettingsActions.NotifyReloadRequired("Unlimited Outside Connections");
                });

            outside.AddButton(
                Localization.Get("SETTINGS_RESET"),
                Localization.Get("SETTINGS_OOC_RESET_TOOLTIP"),
                Localization.Get("SETTINGS_RESET"),
                () => SettingsActions.OnResetOutsideConnectionsClick());

            UpdateOocChildrenEnabled(setting.EnableOptimisedOutsideConnections);
        }

        private void UpdateOocChildrenEnabled(bool enabled)
        {
            foreach (var c in _oocChildControls)
            {
                OptionsNestedTabs.SetEnabled(c, enabled);
            }
        }

        /// <summary>Overlay → Info: panels/tools that show city info (not fleet sim).</summary>
        private void FillOverlayInfoSub(ScrollContainer page)
        {
            var setting = ModSetting.Instance;
            var ui = AddSection(page, Localization.Get("SETTINGS_UI"), Localization.Get("SETTINGS_SUB_INFO_DESC"));
            ui.AddDropDown<ModSetting.VehicleEditorPositions>(
                Localization.Get("SETTINGS_VEHICLE_EDITOR_POSITION"),
                Localization.Get("SETTINGS_VEHICLE_EDITOR_POSITION_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.VehicleEditorPositions>(e => Localization.Get(e == ModSetting.VehicleEditorPositions.Bottom ? "SETTINGS_VEHICLE_EDITOR_POSITION_BOTTOM" : "SETTINGS_VEHICLE_EDITOR_POSITION_RIGHT")),
                item => item.Value == setting.VehicleEditorPosition,
                item => ModSetting.Instance.VehicleEditorPosition = item.Value,
                null,
                30f,
                card =>
                {
                    if (card?.Control != null)
                    {
                        _vehicleEditorPositionControl = card.Control;
                        OptionsNestedTabs.SetEnabled(card.Control, !setting.HideVehicleEditor);
                    }
                });
            ui.AddCheckBox(setting.HideVehicleEditor, Localization.Get("SETTINGS_VEHICLE_EDITOR_HIDE"), null, Localization.Get("SETTINGS_VEHICLE_EDITOR_HIDE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.HideVehicleEditor = isChecked;
                    if (_vehicleEditorPositionControl != null)
                    {
                        OptionsNestedTabs.SetEnabled(_vehicleEditorPositionControl, !isChecked);
                    }
                });
            ui.AddCheckBox(setting.ShowLineInfo, Localization.Get("SETTINGS_AUTOSHOW_LINE_INFO"), null, Localization.Get("SETTINGS_AUTOSHOW_LINE_INFO_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.ShowLineInfo = isChecked);

            ui.AddDropDown<ModSetting.WalkingSpeedModes>(Localization.Get("SETTINGS_WALKING_SPEED"), Localization.Get("SETTINGS_WALKING_SPEED_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.WalkingSpeedModes>(e => Localization.Get(e == ModSetting.WalkingSpeedModes.Vanilla ? "SETTINGS_WALKING_SPEED_MODE_VANILLA" : "SETTINGS_WALKING_SPEED_MODE_REALISTIC")),
                item => item.Value == setting.WalkingSpeedMode,
                item =>
                {
                    ModSetting.Instance.WalkingSpeedMode = item.Value;
                    SettingsActions.OnRealisticWalkingSpeedChanged((int)item.Value);
                }, null);

            var info = AddSection(page, Localization.Get("SETTINGS_SUB_INFO"), null);
            info.AddCheckBox(setting.EnableFlightTracker, Localization.Get("SETTINGS_FLIGHTTRACKER_ENABLE"), null, Localization.Get("SETTINGS_FLIGHTTRACKER_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableFlightTracker = isChecked;
                    SettingsActions.OnFlightTrackerChanged(isChecked);
                });
            info.AddCheckBox(setting.EnableSubBuildingsTabs, Localization.Get("SETTINGS_SUBBUILDINGSTABS_ENABLE"), null, Localization.Get("SETTINGS_SUBBUILDINGSTABS_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableSubBuildingsTabs = isChecked;
                    SettingsActions.OnSubBuildingsTabsChanged(isChecked);
                });
            info.AddCheckBox(setting.EnableCommuterDestination, Localization.Get("SETTINGS_COMMUTERDESTINATION_ENABLE"), null, Localization.Get("SETTINGS_COMMUTERDESTINATION_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableCommuterDestination = isChecked;
                    SettingsActions.OnCommuterDestinationChanged(isChecked);
                });
        }

        private void UpdateTicketPathCostEnabled(ModSetting.TicketPriceCustomizerModes mode)
        {
            var on = mode == ModSetting.TicketPriceCustomizerModes.Enabled;
            if (_ticketPathCostCard?.Control != null)
            {
                OptionsNestedTabs.SetEnabled(_ticketPathCostCard.Control, on);
            }
        }

        private void UpdateTrainDisplayChildrenEnabled(ModSetting.TrainDisplayModes mode)
        {
            var on = mode == ModSetting.TrainDisplayModes.Enabled;
            foreach (var c in _trainDisplayChildControls)
            {
                OptionsNestedTabs.SetEnabled(c, on);
            }
        }

        private void FillStopsPage(ScrollContainer page)
        {
            var setting = ModSetting.Instance;
            // One combined description at the top instead of repeating a full explanation under each
            // of the 14 sliders below (same pattern as the Delete Lines tab) - they are all the same
            // kind of setting (a per-vehicle-type waiting-passenger cap), so a single explanation up
            // front says everything the per-slider ones did, without the visual repetition.
            var section = AddSection(page, Localization.Get("SETTINGS_STOPS"), Localization.Get("SETTINGS_STOPSANDSTATIONS_DESCRIPTION"));

            void PassengerSlider(string headerKey, float min, float max, float step, int current, Action<int> setter)
                => section.AddSliderWithValue(Localization.Get(headerKey), null, min, max, step, current, v => setter((int)v));

            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_BUS", 10f, 500f, 5f, setting.MaxWaitingPassengersBus, v => ModSetting.Instance.MaxWaitingPassengersBus = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TROLLEYBUS", 10f, 500f, 5f, setting.MaxWaitingPassengersTrolleybus, v => ModSetting.Instance.MaxWaitingPassengersTrolleybus = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_EVACUATION_BUS", 10f, 500f, 5f, setting.MaxWaitingPassengersEvacuationBus, v => ModSetting.Instance.MaxWaitingPassengersEvacuationBus = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TOURIST_BUS", 10f, 500f, 5f, setting.MaxWaitingPassengersTouristBus, v => ModSetting.Instance.MaxWaitingPassengersTouristBus = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAM", 10f, 500f, 5f, setting.MaxWaitingPassengersTram, v => ModSetting.Instance.MaxWaitingPassengersTram = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_METRO", 50f, 2000f, 25f, setting.MaxWaitingPassengersMetro, v => ModSetting.Instance.MaxWaitingPassengersMetro = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAIN", 50f, 2000f, 25f, setting.MaxWaitingPassengersTrain, v => ModSetting.Instance.MaxWaitingPassengersTrain = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_MONORAIL", 50f, 2000f, 25f, setting.MaxWaitingPassengersMonorail, v => ModSetting.Instance.MaxWaitingPassengersMonorail = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_SHIP", 50f, 1000f, 10f, setting.MaxWaitingPassengersShip, v => ModSetting.Instance.MaxWaitingPassengersShip = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_AIRPLANE", 50f, 1000f, 10f, setting.MaxWaitingPassengersAirplane, v => ModSetting.Instance.MaxWaitingPassengersAirplane = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_CABLECAR", 10f, 500f, 5f, setting.MaxWaitingPassengersCableCar, v => ModSetting.Instance.MaxWaitingPassengersCableCar = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HOTAIRBALLOON", 10f, 500f, 5f, setting.MaxWaitingPassengersHotAirBalloon, v => ModSetting.Instance.MaxWaitingPassengersHotAirBalloon = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HELICOPTER", 10f, 500f, 5f, setting.MaxWaitingPassengersHelicopter, v => ModSetting.Instance.MaxWaitingPassengersHelicopter = v);

            section.AddButton(null, Localization.Get("SETTINGS_STOPSANDSTATIONS_RESET_TOOLTIP"), Localization.Get("SETTINGS_RESET"),
                () => SettingsActions.OnResetStopsButtonClick());
        }

        private void FillTrainDisplayPage(ScrollContainer page)
        {
            var setting = ModSetting.Instance;
            _trainDisplayChildControls.Clear();
            var section = AddSection(page, Localization.Get("SETTINGS_TRAINDISPLAY_GROUP"), Localization.Get("SETTINGS_TRAINDISPLAY_GROUP_DESCRIPTION"));
            section.AddDropDown<ModSetting.TrainDisplayModes>(Localization.Get("SETTINGS_TRAINDISPLAY_ENABLE"), Localization.Get("SETTINGS_TRAINDISPLAY_ENABLE_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.TrainDisplayModes>(e => Localization.Get(e == ModSetting.TrainDisplayModes.Disabled ? "SETTINGS_TRAINDISPLAY_MODE_DISABLED" : "SETTINGS_TRAINDISPLAY_MODE_ENABLED")),
                item => item.Value == setting.TrainDisplayMode,
                item =>
                {
                    ModSetting.Instance.TrainDisplayMode = item.Value;
                    UpdateTrainDisplayChildrenEnabled(item.Value);
                }, null);

            TrackTrainChild(section.AddDropDown<ModSetting.TrainDisplayOverlayPositions>(Localization.Get("SETTINGS_TRAINDISPLAY_OVERLAY_POSITION"), Localization.Get("SETTINGS_TRAINDISPLAY_OVERLAY_POSITION_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.TrainDisplayOverlayPositions>(e => Localization.Get("SETTINGS_TRAINDISPLAY_POS_" + e.ToString().ToUpperInvariant())),
                item => item.Value == setting.TrainDisplayOverlayPosition,
                item => ModSetting.Instance.TrainDisplayOverlayPosition = item.Value, null));
            TrackTrainChild(section.AddSliderWithValue(Localization.Get("SETTINGS_TRAINDISPLAY_OVERLAY_SCALE"), Localization.Get("SETTINGS_TRAINDISPLAY_OVERLAY_SCALE_TOOLTIP"),
                50f, 200f, 5f, setting.TrainDisplayOverlayScale * 100f,
                v => ModSetting.Instance.TrainDisplayOverlayScale = Mathf.Clamp(v / 100f, 0.5f, 2f), "%"));
            TrackTrainChild(section.AddSliderWithValue(Localization.Get("SETTINGS_TRAINDISPLAY_OVERLAY_OPACITY"), Localization.Get("SETTINGS_TRAINDISPLAY_OVERLAY_OPACITY_TOOLTIP"),
                25f, 100f, 5f, setting.TrainDisplayOverlayOpacity * 100f,
                v => ModSetting.Instance.TrainDisplayOverlayOpacity = v / 100f, "%"));
            TrackTrainChild(section.AddSliderWithValue(Localization.Get("SETTINGS_TRAINDISPLAY_UPDATE_INTERVAL"), Localization.Get("SETTINGS_TRAINDISPLAY_UPDATE_INTERVAL_TOOLTIP"),
                // Min 100ms — sub-100ms was part of the 4.8 hitch reports when the panel was "snappier".
                100f, 1000f, 50f, Mathf.Max(100f, setting.TrainDisplayUpdateInterval * 1000f),
                v => ModSetting.Instance.TrainDisplayUpdateInterval = Mathf.Max(0.1f, v / 1000f), " ms"));
            TrackTrainChild(section.AddDropDown<ModSetting.TrainDisplayColorThemes>(Localization.Get("SETTINGS_TRAINDISPLAY_THEME"), Localization.Get("SETTINGS_TRAINDISPLAY_THEME_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.TrainDisplayColorThemes>(e => Localization.Get("SETTINGS_TRAINDISPLAY_THEME_" + e.ToString().ToUpperInvariant())),
                item => item.Value == setting.TrainDisplayColorTheme,
                item => ModSetting.Instance.TrainDisplayColorTheme = item.Value, null));
            TrackTrainChild(section.AddCheckBox((setting.TrainDisplayVisibleFields & ModSetting.TrainDisplayFields.Line) != 0, Localization.Get("SETTINGS_TRAINDISPLAY_SHOW_LINE"), null, Localization.Get("SETTINGS_TRAINDISPLAY_SHOW_LINE_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.TrainDisplayVisibleFields = SetFlag(ModSetting.Instance.TrainDisplayVisibleFields, ModSetting.TrainDisplayFields.Line, isChecked)));
            TrackTrainChild(section.AddCheckBox((setting.TrainDisplayVisibleFields & ModSetting.TrainDisplayFields.Destination) != 0, Localization.Get("SETTINGS_TRAINDISPLAY_SHOW_DESTINATION"), null, Localization.Get("SETTINGS_TRAINDISPLAY_SHOW_DESTINATION_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.TrainDisplayVisibleFields = SetFlag(ModSetting.Instance.TrainDisplayVisibleFields, ModSetting.TrainDisplayFields.Destination, isChecked)));
            TrackTrainChild(section.AddCheckBox((setting.TrainDisplayVisibleFields & ModSetting.TrainDisplayFields.State) != 0, Localization.Get("SETTINGS_TRAINDISPLAY_SHOW_STATE"), null, Localization.Get("SETTINGS_TRAINDISPLAY_SHOW_STATE_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.TrainDisplayVisibleFields = SetFlag(ModSetting.Instance.TrainDisplayVisibleFields, ModSetting.TrainDisplayFields.State, isChecked)));

            // Shared description for speed / passengers / elapsed (one strip of extras).
            var extras = AddSection(page, Localization.Get("SETTINGS_TRAINDISPLAY_EXTRAS_GROUP"),
                Localization.Get("SETTINGS_TRAINDISPLAY_SHOW_EXTRAS_TOOLTIP"));
            TrackTrainChild(extras.AddCheckBox((setting.TrainDisplayVisibleFields & ModSetting.TrainDisplayFields.Speed) != 0, Localization.Get("SETTINGS_TRAINDISPLAY_SHOW_SPEED"), null, null,
                (_, isChecked) => ModSetting.Instance.TrainDisplayVisibleFields = SetFlag(ModSetting.Instance.TrainDisplayVisibleFields, ModSetting.TrainDisplayFields.Speed, isChecked)));
            TrackTrainChild(extras.AddCheckBox((setting.TrainDisplayVisibleFields & ModSetting.TrainDisplayFields.Passengers) != 0, Localization.Get("SETTINGS_TRAINDISPLAY_SHOW_PASSENGERS"), null, null,
                (_, isChecked) => ModSetting.Instance.TrainDisplayVisibleFields = SetFlag(ModSetting.Instance.TrainDisplayVisibleFields, ModSetting.TrainDisplayFields.Passengers, isChecked)));
            TrackTrainChild(extras.AddCheckBox((setting.TrainDisplayVisibleFields & ModSetting.TrainDisplayFields.Elapsed) != 0, Localization.Get("SETTINGS_TRAINDISPLAY_SHOW_ELAPSED"), null, null,
                (_, isChecked) => ModSetting.Instance.TrainDisplayVisibleFields = SetFlag(ModSetting.Instance.TrainDisplayVisibleFields, ModSetting.TrainDisplayFields.Elapsed, isChecked)));

            var typesSection = AddSection(page, Localization.Get("SETTINGS_TRAINDISPLAY_TYPES_GROUP"), Localization.Get("SETTINGS_TRAINDISPLAY_TYPES_GROUP_DESCRIPTION"));
            AddVehicleTypeToggle(typesSection, setting, ModSetting.TrainDisplayVehicleTypes.Bus, "SETTINGS_TRAINDISPLAY_TYPE_BUS");
            AddVehicleTypeToggle(typesSection, setting, ModSetting.TrainDisplayVehicleTypes.Trolleybus, "SETTINGS_TRAINDISPLAY_TYPE_TROLLEY");
            AddVehicleTypeToggle(typesSection, setting, ModSetting.TrainDisplayVehicleTypes.Tram, "SETTINGS_TRAINDISPLAY_TYPE_TRAM");
            AddVehicleTypeToggle(typesSection, setting, ModSetting.TrainDisplayVehicleTypes.Metro, "SETTINGS_TRAINDISPLAY_TYPE_METRO");
            AddVehicleTypeToggle(typesSection, setting, ModSetting.TrainDisplayVehicleTypes.Train, "SETTINGS_TRAINDISPLAY_TYPE_TRAIN");
            AddVehicleTypeToggle(typesSection, setting, ModSetting.TrainDisplayVehicleTypes.Monorail, "SETTINGS_TRAINDISPLAY_TYPE_MONORAIL");
            AddVehicleTypeToggle(typesSection, setting, ModSetting.TrainDisplayVehicleTypes.Ship, "SETTINGS_TRAINDISPLAY_TYPE_SHIP");
            AddVehicleTypeToggle(typesSection, setting, ModSetting.TrainDisplayVehicleTypes.Plane, "SETTINGS_TRAINDISPLAY_TYPE_PLANE");
            AddVehicleTypeToggle(typesSection, setting, ModSetting.TrainDisplayVehicleTypes.Taxi, "SETTINGS_TRAINDISPLAY_TYPE_TAXI");
            AddVehicleTypeToggle(typesSection, setting, ModSetting.TrainDisplayVehicleTypes.CableCar, "SETTINGS_TRAINDISPLAY_TYPE_CABLECAR");
            AddVehicleTypeToggle(typesSection, setting, ModSetting.TrainDisplayVehicleTypes.Tours, "SETTINGS_TRAINDISPLAY_TYPE_TOURS");
            UpdateTrainDisplayChildrenEnabled(setting.TrainDisplayMode);
        }

        private void TrackTrainChild(UIComponent control)
        {
            if (control != null)
            {
                _trainDisplayChildControls.Add(control);
            }
        }

        private void TrackTrainChild(CheckBoxCard card)
        {
            if (card?.Control != null)
            {
                _trainDisplayChildControls.Add(card.Control);
            }
        }

        private void TrackTrainChild(DropDownCard card)
        {
            if (card?.Control != null)
            {
                _trainDisplayChildControls.Add(card.Control);
            }
        }

        private void TrackTrainChild(SliderCard card)
        {
            if (card?.Control != null)
            {
                _trainDisplayChildControls.Add(card.Control);
            }
        }

        private void AddVehicleTypeToggle(SettingsSection section, ModSetting setting, ModSetting.TrainDisplayVehicleTypes flag, string labelKey)
        {
            var card = section.AddCheckBox(
                (setting.TrainDisplayEnabledVehicleTypes & flag) != 0,
                Localization.Get(labelKey),
                null,
                Localization.Get("SETTINGS_TRAINDISPLAY_TYPE_TOOLTIP"),
                (_, isChecked) =>
                {
                    var cur = ModSetting.Instance.TrainDisplayEnabledVehicleTypes;
                    ModSetting.Instance.TrainDisplayEnabledVehicleTypes = isChecked ? cur | flag : cur & ~flag;
                });
            TrackTrainChild(card);
        }

        private static ModSetting.TrainDisplayFields SetFlag(ModSetting.TrainDisplayFields current, ModSetting.TrainDisplayFields flag, bool isSet)
            => isSet ? current | flag : current & ~flag;

        private void FillDeleteLinesPage(ScrollContainer page)
        {
            // Deliberately not bound to ModSetting - see DeleteLinesSelection: this is a one-shot
            // "pick types, click Delete" tool, so selections always start unchecked and reset after use.
            var section = AddSection(page, Localization.Get("SETTINGS_LINE_DELETION_TOOL"), Localization.Get("SETTINGS_LINE_DELETION_TOOL_DESCRIPTION"));

            section.AddCheckBox(DeleteLinesSelection.BusLines, Localization.Get("INFO_PUBLICTRANSPORT_BUS"), null, Localization.Get("SETTINGS_DELETE_BUS_TOOLTIP"),
                (_, isChecked) => DeleteLinesSelection.BusLines = isChecked);
            section.AddCheckBox(DeleteLinesSelection.SightseeingBusLines, Localization.Get("SETTINGS_DELETE_SIGHTSEEING_BUS_LABEL"), null, Localization.Get("SETTINGS_DELETE_SIGHTSEEING_BUS_TOOLTIP"),
                (_, isChecked) => DeleteLinesSelection.SightseeingBusLines = isChecked);
            if (PlatformService.IsDlcInstalled(SteamHelper.kWinterDLCAppID))
            {
                section.AddCheckBox(DeleteLinesSelection.TramLines, Localization.Get("INFO_PUBLICTRANSPORT_TRAM"), null, Localization.Get("SETTINGS_DELETE_TRAM_TOOLTIP"),
                    (_, isChecked) => DeleteLinesSelection.TramLines = isChecked);
            }
            section.AddCheckBox(DeleteLinesSelection.TrolleybusLines, Localization.Get("INFO_PUBLICTRANSPORT_TROLLEYBUS"), null, Localization.Get("SETTINGS_DELETE_TROLLEYBUS_TOOLTIP"),
                (_, isChecked) => DeleteLinesSelection.TrolleybusLines = isChecked);
            section.AddCheckBox(DeleteLinesSelection.TrainLines, Localization.Get("INFO_PUBLICTRANSPORT_TRAIN"), null, Localization.Get("SETTINGS_DELETE_TRAIN_TOOLTIP"),
                (_, isChecked) => DeleteLinesSelection.TrainLines = isChecked);
            section.AddCheckBox(DeleteLinesSelection.MetroLines, Localization.Get("INFO_PUBLICTRANSPORT_METRO"), null, Localization.Get("SETTINGS_DELETE_METRO_TOOLTIP"),
                (_, isChecked) => DeleteLinesSelection.MetroLines = isChecked);
            if (PlatformService.IsDlcInstalled(SteamHelper.kMotionDLCAppID))
            {
                section.AddCheckBox(DeleteLinesSelection.MonorailLines, Localization.Get("INFO_PUBLICTRANSPORT_MONORAIL"), null, Localization.Get("SETTINGS_DELETE_MONORAIL_TOOLTIP"),
                    (_, isChecked) => DeleteLinesSelection.MonorailLines = isChecked);
            }
            section.AddCheckBox(DeleteLinesSelection.ShipLines, Localization.Get("SETTINGS_DELETE_FERRY_LABEL"), null, Localization.Get("SETTINGS_DELETE_SHIP_TOOLTIP"),
                (_, isChecked) => DeleteLinesSelection.ShipLines = isChecked);
            section.AddCheckBox(DeleteLinesSelection.HelicopterLines, Localization.Get("SETTINGS_DELETE_HELICOPTER_LABEL"), null, Localization.Get("SETTINGS_DELETE_HELICOPTER_TOOLTIP"),
                (_, isChecked) => DeleteLinesSelection.HelicopterLines = isChecked);
            section.AddCheckBox(DeleteLinesSelection.BlimpLines, Localization.Get("SETTINGS_DELETE_BLIMP_LABEL"), null, Localization.Get("SETTINGS_DELETE_BLIMP_TOOLTIP"),
                (_, isChecked) => DeleteLinesSelection.BlimpLines = isChecked);

            section.AddButton(null, Localization.Get("SETTINGS_LINE_DELETION_TOOL_BUTTON_TOOLTIP"), Localization.Get("SETTINGS_DELETE"),
                () => SettingsActions.OnDeleteLinesClick());
        }

        private static string GetAutoLineColorStrategyKey(ModSetting.AutoLineColorStrategy strategy)
        {
            switch (strategy)
            {
                case ModSetting.AutoLineColorStrategy.Disabled: return "AUTOLINECOLOR_STRATEGY_DISABLED";
                case ModSetting.AutoLineColorStrategy.RandomHue: return "AUTOLINECOLOR_STRATEGY_RANDOM_HUE";
                case ModSetting.AutoLineColorStrategy.RandomColor: return "AUTOLINECOLOR_STRATEGY_RANDOM_COLOR";
                case ModSetting.AutoLineColorStrategy.CategorisedColor: return "AUTOLINECOLOR_STRATEGY_CATEGORISED";
                case ModSetting.AutoLineColorStrategy.NamedColors: return "AUTOLINECOLOR_STRATEGY_NAMED";
                default: return "AUTOLINECOLOR_STRATEGY_DISABLED";
            }
        }

    }
}
