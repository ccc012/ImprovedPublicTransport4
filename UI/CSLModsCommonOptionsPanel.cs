using System;
using System.Collections.Generic;
using CSLModsCommon.UI.OptionsPanel;
using CSLModsCommon.UI.Containers;
using CSLModsCommon.UI.SettingsCard;
using CSLModsCommon.UI.Utilities;
using ColossalFramework.PlatformServices;
using ImprovedPublicTransport.Settings;

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

    // Tab layout (reorganized so each tab's name actually matches what is on it - the previous
    // "Auto Line" tab had grown to hold seven unrelated sections, including a generic
    // "Integrations" grab-bag mixing six unrelated absorbed mods). New integrations belong on the
    // Integrations tab; nothing else here should need touching to add one.
    public class CSLModsCommonOptionsPanel : OptionsPanelBase
    {
        private const string FleetTab = "Fleet";
        private const string BudgetTab = "Budget";
        private const string LineColorsTab = "LineColors";
        private const string StopsTab = "Stops";
        private const string DeleteLinesTab = "DeleteLines";
        private const string TrainDisplayTab = "TrainDisplay";
        private const string IntegrationsTab = "Integrations";

        private CSLModsCommon.UI.Sliders.Slider _colorDiffSlider;
        private CSLModsCommon.UI.Sliders.Slider _colorPickAttemptsSlider;
        private CSLModsCommon.UI.SettingsCard.CheckBoxCard _expressBusSelfBalancingCard;
        private CSLModsCommon.UI.SettingsCard.CheckBoxCard _expressBusMiddleStopBalancingCard;
        private CSLModsCommon.UI.SettingsCard.CheckBoxCard _expressBusMinibusCard;

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
            var fleetPage = AddPage(FleetTab, Localization.Get("SETTINGS_TAB_FLEET"));
            FillFleetPage(fleetPage);
            var budgetPage = AddPage(BudgetTab, Localization.Get("SETTINGS_TAB_BUDGET"));
            FillBudgetPage(budgetPage);
            var lineColorsPage = AddPage(LineColorsTab, Localization.Get("SETTINGS_TAB_LINECOLORS"));
            FillLineColorsPage(lineColorsPage);
            var stopsPage = AddPage(StopsTab, Localization.Get("SETTINGS_TAB_STOPS"));
            FillStopsPage(stopsPage);
            var deletePage = AddPage(DeleteLinesTab, Localization.Get("SETTINGS_TAB_DELETE"));
            FillDeleteLinesPage(deletePage);
            var trainDisplayPage = AddPage(TrainDisplayTab, Localization.Get("SETTINGS_TRAINDISPLAY_GROUP"));
            FillTrainDisplayPage(trainDisplayPage);
            var integrationsPage = AddPage(IntegrationsTab, Localization.Get("SETTINGS_TAB_INTEGRATIONS"));
            FillIntegrationsPage(integrationsPage);

            // Restored: the base KeyBinding tab was removed earlier because nothing used it. With
            // more integrations landing, some may want a hotkey later - keep the tab available
            // (currently empty; FillKeyBindingPage has nothing to add yet) rather than removing the
            // whole mechanism again.
            base.AddExtraPage();
        }

        protected override void FillKeyBindingPage(ScrollContainer page)
        {
            // No keybindings defined yet - restored as a ready scaffold, not a specific request.
        }

        protected override void FillAdvancedPage(ScrollContainer page)
        {
            // The base Advanced page already has a link section (translation/Steam/Discord) gated
            // on ModManagerBase properties IPT4 doesn't set - CSLModsCommonShared has no built-in
            // "source repository" property, so this is added here instead of there.
            var section = AddSection(page, Localization.Get("SETTINGS_ADVANCED_LINKS_GROUP"));
            section.AddButton(Localization.Get("SETTINGS_GITHUB_REPO"), null, CSLModsCommon.Localization.SharedTranslations.Website, null,
                onButtonClicked: _ => CSLModsCommon.Utilities.URLHelper.OpenURL("https://github.com/ccc012/ImprovedPublicTransport4"));
        }

        protected override void FillGeneralPage(ScrollContainer page)
        {
            base.FillGeneralPage(page);
            var setting = ModSetting.Instance;

            var commonSection = AddSection(page, Localization.Get("SETTINGS"));
            commonSection.AddDropDown<ModSetting.VehicleSpeedUnits>(Localization.Get("SETTINGS_SPEED"), Localization.Get("SETTINGS_SPEED_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.VehicleSpeedUnits>(e => Localization.Get(e == ModSetting.VehicleSpeedUnits.KPH ? "SETTINGS_SPEED_KPH" : "SETTINGS_SPEED_MPH")),
                item => item.Value == setting.SpeedUnit,
                item => ModSetting.Instance.SpeedUnit = item.Value, null);

            commonSection.AddDropDown<ModSetting.BbspLogicModes>(Localization.Get("SETTINGS_BBSP"), Localization.Get("SETTINGS_BBSP_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.BbspLogicModes>(e => Localization.Get(e == ModSetting.BbspLogicModes.Disabled ? "SETTINGS_BBSP_MODE_DISABLED" : "SETTINGS_BBSP_MODE_ORIGINAL")),
                item => item.Value == setting.BbspLogic,
                item => ModSetting.Instance.BbspLogic = item.Value, null);

            commonSection.AddDropDown<ModSetting.WalkingSpeedModes>(Localization.Get("SETTINGS_WALKING_SPEED"), Localization.Get("SETTINGS_WALKING_SPEED_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.WalkingSpeedModes>(e => Localization.Get(e == ModSetting.WalkingSpeedModes.Vanilla ? "SETTINGS_WALKING_SPEED_MODE_VANILLA" : "SETTINGS_WALKING_SPEED_MODE_REALISTIC")),
                item => item.Value == setting.WalkingSpeedMode,
                item =>
                {
                    ModSetting.Instance.WalkingSpeedMode = item.Value;
                    SettingsActions.OnRealisticWalkingSpeedChanged((int)item.Value);
                }, null);

            var uiSection = AddSection(page, Localization.Get("SETTINGS_UI"));
            uiSection.AddDropDown<ModSetting.VehicleEditorPositions>(Localization.Get("SETTINGS_VEHICLE_EDITOR_POSITION"), Localization.Get("SETTINGS_VEHICLE_EDITOR_POSITION_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.VehicleEditorPositions>(e => Localization.Get(e == ModSetting.VehicleEditorPositions.Bottom ? "SETTINGS_VEHICLE_EDITOR_POSITION_BOTTOM" : "SETTINGS_VEHICLE_EDITOR_POSITION_RIGHT")),
                item => item.Value == setting.VehicleEditorPosition,
                item => ModSetting.Instance.VehicleEditorPosition = item.Value, null);
            uiSection.AddCheckBox(setting.HideVehicleEditor, Localization.Get("SETTINGS_VEHICLE_EDITOR_HIDE"), null, Localization.Get("SETTINGS_VEHICLE_EDITOR_HIDE_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.HideVehicleEditor = isChecked);
        }

        /// <summary>Fleet sizing and vehicle spacing: everything about how many vehicles run on a
        /// line and how evenly spaced they stay. Unbunching (native) and Express Bus/Tram
        /// (an alternate, absorbed-mod spacing approach) are two ways to solve the same problem, so
        /// they live together rather than in separate tabs.</summary>
        private void FillFleetPage(ScrollContainer page)
        {
            var setting = ModSetting.Instance;

            var autoLineSection = AddSection(page, Localization.Get("SETTINGS_AUTO_LINE"));
            autoLineSection.AddCheckBox(setting.ShowLineInfo, Localization.Get("SETTINGS_AUTOSHOW_LINE_INFO"), null, Localization.Get("SETTINGS_AUTOSHOW_LINE_INFO_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.ShowLineInfo = isChecked);

            var unbunchingSection = AddSection(page, Localization.Get("SETTINGS_UNBUNCHING"));
            unbunchingSection.AddSliderWithValue(Localization.Get("SETTINGS_UNBUNCHING_AGGRESSION"),
                Localization.Get("SETTINGS_UNBUNCHING_AGGRESSION_TOOLTIP") + "\n" + Localization.Get("EXPLANATION_UNBUNCHING"),
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

            unbunchingSection.AddButton(null, Localization.Get("SETTINGS_UNBUNCHING_RESET_BUTTON_TOOLTIP"), Localization.Get("SETTINGS_RESET"),
                () => SettingsActions.OnResetButtonClick());

            var ebsBusSection = AddSection(page, Localization.Get("SETTINGS_EBS_GROUP_BUS"));
            ebsBusSection.AddDropDown<ModSetting.ExpressBusServicesModes>(Localization.Get("SETTINGS_EBS_DROPDOWN_UNBUNCHING_MODE"), Localization.Get("SETTINGS_EBS_TOOLTIP_UNBUNCHING_MODE"),
                DropDownHelper.FromEnum<ModSetting.ExpressBusServicesModes>(e => Localization.Get("SETTINGS_EBS_MODE_" + (e == ModSetting.ExpressBusServicesModes.None ? "NONE" : e.ToString().ToUpperInvariant()))),
                item => item.Value == setting.ExpressBusUnbunchingMode,
                item =>
                {
                    ModSetting.Instance.ExpressBusUnbunchingMode = item.Value;
                    UpdateExpressBusControlsEnabled(item.Value);
                }, null);
            _expressBusSelfBalancingCard = ebsBusSection.AddCheckBox(setting.ExpressBusEnableSelfBalancing, Localization.Get("SETTINGS_EBS_ENABLE_SELFBAL"), null, Localization.Get("SETTINGS_EBS_TOOLTIP_SELFBAL"),
                (_, isChecked) => ModSetting.Instance.ExpressBusEnableSelfBalancing = isChecked);
            _expressBusMiddleStopBalancingCard = ebsBusSection.AddCheckBox(setting.ExpressBusAllowMiddleStopBalancing, Localization.Get("SETTINGS_EBS_ENABLE_SELFBAL_TARGETMID"), null, Localization.Get("SETTINGS_EBS_TOOLTIP_SELFBAL_TARGETMID"),
                (_, isChecked) => ModSetting.Instance.ExpressBusAllowMiddleStopBalancing = isChecked);
            _expressBusMinibusCard = ebsBusSection.AddCheckBox(setting.ExpressBusEnableMinibusMode, Localization.Get("SETTINGS_EBS_ENABLE_MINIBUS"), null, Localization.Get("SETTINGS_EBS_TOOLTIP_MINIBUS"),
                (_, isChecked) => ModSetting.Instance.ExpressBusEnableMinibusMode = isChecked);
            UpdateExpressBusControlsEnabled(setting.ExpressBusUnbunchingMode);

            var ebsTramSection = AddSection(page, Localization.Get("SETTINGS_EBS_GROUP_TRAM"));
            ebsTramSection.AddDropDown<ModSetting.ExpressTramServicesModes>(Localization.Get("SETTINGS_EBS_DROPDOWN_TRAM_UNBUNCHING_MODE"), Localization.Get("SETTINGS_EBS_TOOLTIP_TRAM_UNBUNCHING"),
                DropDownHelper.FromEnum<ModSetting.ExpressTramServicesModes>(e => Localization.Get("SETTINGS_EBS_TRAM_MODE_" + (e == ModSetting.ExpressTramServicesModes.Disabled ? "NONE" : e == ModSetting.ExpressTramServicesModes.LightRail ? "LIGHT_RAIL" : "TRAM"))),
                item => item.Value == setting.ExpressTramUnbunchingMode,
                item => ModSetting.Instance.ExpressTramUnbunchingMode = item.Value, null);
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
                }, null);

            budgetSection.AddDropDown<ModSetting.AutoLineBudgetModes>(Localization.Get("SETTINGS_AUTO_LINE_BUDGET"), Localization.Get("SETTINGS_AUTO_LINE_BUDGET_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.AutoLineBudgetModes>(e => Localization.Get(e == ModSetting.AutoLineBudgetModes.Disabled ? "SETTINGS_AUTO_LINE_BUDGET_DISABLED" : "SETTINGS_AUTO_LINE_BUDGET_ENABLED")),
                item => item.Value == setting.AutoLineBudgetMode,
                item => ModSetting.Instance.AutoLineBudgetMode = item.Value, null);
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

        /// <summary>Every absorbed third-party mod's on/off switch, in one place. This is the tab a
        /// newly-absorbed mod's toggle should land on by default - see README.md "Adding an
        /// integration" for the rest of the checklist.</summary>
        private void FillIntegrationsPage(ScrollContainer page)
        {
            var setting = ModSetting.Instance;
            var ptuSection = AddSection(page, Localization.Get("SETTINGS_PTU_GROUP"));
            ptuSection.AddCheckBox(setting.EnablePublicTransportUnstucker, Localization.Get("SETTINGS_PTU_ENABLE"), null, Localization.Get("SETTINGS_PTU_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnablePublicTransportUnstucker = isChecked;
                    SettingsActions.OnPublicTransportUnstuckerChanged(isChecked ? 1 : 0);
                });

            var integrationSection = AddSection(page, Localization.Get("SETTINGS_INTEGRATIONS_GROUP"));
            integrationSection.AddCheckBox(setting.EnableIntercityBusControl, Localization.Get("SETTINGS_INTERCITY_BUS_ENABLE"), null, Localization.Get("SETTINGS_INTERCITY_BUS_ENABLE_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.EnableIntercityBusControl = isChecked);
            // Takes effect on next level load - the terminal cap is applied once, when the station
            // prefab is first patched (see Integration/IntercityBusControl/StationPatcher.cs).
            integrationSection.AddDropDown<ModSetting.DepotCapacityModes>(Localization.Get("SETTINGS_INTERCITY_BUS_CAPACITY"), Localization.Get("SETTINGS_INTERCITY_BUS_CAPACITY_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.DepotCapacityModes>(e => Localization.Get(e switch
                {
                    ModSetting.DepotCapacityModes.Realistic => "SETTINGS_DEPOT_CAPACITY_REALISTIC",
                    ModSetting.DepotCapacityModes.Intermediate => "SETTINGS_DEPOT_CAPACITY_INTERMEDIATE",
                    _ => "SETTINGS_DEPOT_CAPACITY_DISABLED",
                })),
                item => item.Value == setting.IntercityTerminalCapacityMode,
                item => ModSetting.Instance.IntercityTerminalCapacityMode = item.Value, null);
            integrationSection.AddCheckBox(setting.EnableFlightTracker, Localization.Get("SETTINGS_FLIGHTTRACKER_ENABLE"), null, Localization.Get("SETTINGS_FLIGHTTRACKER_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableFlightTracker = isChecked;
                    SettingsActions.OnFlightTrackerChanged(isChecked);
                });
            integrationSection.AddCheckBox(setting.EnableSubBuildingsTabs, Localization.Get("SETTINGS_SUBBUILDINGSTABS_ENABLE"), null, Localization.Get("SETTINGS_SUBBUILDINGSTABS_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableSubBuildingsTabs = isChecked;
                    SettingsActions.OnSubBuildingsTabsChanged(isChecked);
                });
            integrationSection.AddCheckBox(setting.EnableTaxiStandFix, Localization.Get("SETTINGS_TAXISTANDFIX_ENABLE"), null, Localization.Get("SETTINGS_TAXISTANDFIX_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableTaxiStandFix = isChecked;
                    SettingsActions.OnTaxiStandFixChanged(isChecked);
                });
            // Off by default (see ModSetting.EnableSharedStopEnabler) - this is a reduced port,
            // see Integration/SharedStopEnabler/LICENSE.txt for exactly what was left out and why.
            integrationSection.AddCheckBox(setting.EnableSharedStopEnabler, Localization.Get("SETTINGS_SHAREDSTOPENABLER_ENABLE"), null, Localization.Get("SETTINGS_SHAREDSTOPENABLER_ENABLE_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnableSharedStopEnabler = isChecked;
                    SettingsActions.OnSharedStopEnablerChanged(isChecked);
                });
            // No live-toggle handler: the button this integration adds is built once, in
            // PublicTransportStopWorldInfoPanel.SetupPanel, which only ever runs once per game
            // session (see Integration/CommuterDestination/Patch_PublicTransportStopWorldInfoPanel.cs).
            // Toggling this takes effect on the next level load, same as most integrations above
            // that predate the live-toggle pattern (e.g. EnableIntercityBusControl).
            integrationSection.AddCheckBox(setting.EnableCommuterDestination, Localization.Get("SETTINGS_COMMUTERDESTINATION_ENABLE"), null, Localization.Get("SETTINGS_COMMUTERDESTINATION_ENABLE_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.EnableCommuterDestination = isChecked);
        }

        private void FillStopsPage(ScrollContainer page)
        {
            var setting = ModSetting.Instance;
            var section = AddSection(page, Localization.Get("SETTINGS_STOPS"));

            void PassengerSlider(string headerKey, string tooltipKey, float min, float max, float step, int current, Action<int> setter)
                => section.AddSliderWithValue(Localization.Get(headerKey), Localization.Get(tooltipKey), min, max, step, current, v => setter((int)v));

            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_BUS", "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_BUS_TOOLTIP", 10f, 500f, 5f, setting.MaxWaitingPassengersBus, v => ModSetting.Instance.MaxWaitingPassengersBus = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TROLLEYBUS", "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TROLLEYBUS_TOOLTIP", 10f, 500f, 5f, setting.MaxWaitingPassengersTrolleybus, v => ModSetting.Instance.MaxWaitingPassengersTrolleybus = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_EVACUATION_BUS", "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_EVACUATION_BUS_TOOLTIP", 10f, 500f, 5f, setting.MaxWaitingPassengersEvacuationBus, v => ModSetting.Instance.MaxWaitingPassengersEvacuationBus = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TOURIST_BUS", "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TOURIST_BUS_TOOLTIP", 10f, 500f, 5f, setting.MaxWaitingPassengersTouristBus, v => ModSetting.Instance.MaxWaitingPassengersTouristBus = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAM", "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAM_TOOLTIP", 10f, 500f, 5f, setting.MaxWaitingPassengersTram, v => ModSetting.Instance.MaxWaitingPassengersTram = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_METRO", "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_METRO_TOOLTIP", 50f, 2000f, 25f, setting.MaxWaitingPassengersMetro, v => ModSetting.Instance.MaxWaitingPassengersMetro = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAIN", "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAIN_TOOLTIP", 50f, 2000f, 25f, setting.MaxWaitingPassengersTrain, v => ModSetting.Instance.MaxWaitingPassengersTrain = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_MONORAIL", "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_MONORAIL_TOOLTIP", 50f, 2000f, 25f, setting.MaxWaitingPassengersMonorail, v => ModSetting.Instance.MaxWaitingPassengersMonorail = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_SHIP", "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_SHIP_TOOLTIP", 50f, 1000f, 10f, setting.MaxWaitingPassengersShip, v => ModSetting.Instance.MaxWaitingPassengersShip = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_AIRPLANE", "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_AIRPLANE_TOOLTIP", 50f, 1000f, 10f, setting.MaxWaitingPassengersAirplane, v => ModSetting.Instance.MaxWaitingPassengersAirplane = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_CABLECAR", "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_CABLECAR_TOOLTIP", 10f, 500f, 5f, setting.MaxWaitingPassengersCableCar, v => ModSetting.Instance.MaxWaitingPassengersCableCar = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HOTAIRBALLOON", "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HOTAIRBALLOON_TOOLTIP", 10f, 500f, 5f, setting.MaxWaitingPassengersHotAirBalloon, v => ModSetting.Instance.MaxWaitingPassengersHotAirBalloon = v);
            PassengerSlider("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HELICOPTER", "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HELICOPTER_TOOLTIP", 10f, 500f, 5f, setting.MaxWaitingPassengersHelicopter, v => ModSetting.Instance.MaxWaitingPassengersHelicopter = v);
        }

        private void FillTrainDisplayPage(ScrollContainer page)
        {
            var setting = ModSetting.Instance;
            var section = AddSection(page, Localization.Get("SETTINGS_TRAINDISPLAY_GROUP"), Localization.Get("SETTINGS_TRAINDISPLAY_GROUP_DESCRIPTION"));
            section.AddDropDown<ModSetting.TrainDisplayModes>(Localization.Get("SETTINGS_TRAINDISPLAY_ENABLE"), Localization.Get("SETTINGS_TRAINDISPLAY_ENABLE_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.TrainDisplayModes>(e => Localization.Get(e == ModSetting.TrainDisplayModes.Disabled ? "SETTINGS_TRAINDISPLAY_MODE_DISABLED" : "SETTINGS_TRAINDISPLAY_MODE_ENABLED")),
                item => item.Value == setting.TrainDisplayMode,
                item => ModSetting.Instance.TrainDisplayMode = item.Value, null);
            section.AddDropDown<ModSetting.TrainDisplayOverlayPositions>(Localization.Get("SETTINGS_TRAINDISPLAY_OVERLAY_POSITION"), Localization.Get("SETTINGS_TRAINDISPLAY_OVERLAY_POSITION_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.TrainDisplayOverlayPositions>(e => Localization.Get("SETTINGS_TRAINDISPLAY_POS_" + e.ToString().ToUpperInvariant())),
                item => item.Value == setting.TrainDisplayOverlayPosition,
                item => ModSetting.Instance.TrainDisplayOverlayPosition = item.Value, null);
            section.AddSliderWithValue(Localization.Get("SETTINGS_TRAINDISPLAY_OVERLAY_SCALE"), Localization.Get("SETTINGS_TRAINDISPLAY_OVERLAY_SCALE_TOOLTIP"),
                75f, 200f, 5f, setting.TrainDisplayOverlayScale * 100f,
                v => ModSetting.Instance.TrainDisplayOverlayScale = v / 100f, "%");
            section.AddSliderWithValue(Localization.Get("SETTINGS_TRAINDISPLAY_OVERLAY_OPACITY"), Localization.Get("SETTINGS_TRAINDISPLAY_OVERLAY_OPACITY_TOOLTIP"),
                25f, 100f, 5f, setting.TrainDisplayOverlayOpacity * 100f,
                v => ModSetting.Instance.TrainDisplayOverlayOpacity = v / 100f, "%");
            section.AddSliderWithValue(Localization.Get("SETTINGS_TRAINDISPLAY_UPDATE_INTERVAL"), Localization.Get("SETTINGS_TRAINDISPLAY_UPDATE_INTERVAL_TOOLTIP"),
                50f, 1000f, 50f, setting.TrainDisplayUpdateInterval * 1000f,
                v => ModSetting.Instance.TrainDisplayUpdateInterval = v / 1000f, " ms");
            section.AddDropDown<ModSetting.TrainDisplayColorThemes>(Localization.Get("SETTINGS_TRAINDISPLAY_THEME"), Localization.Get("SETTINGS_TRAINDISPLAY_THEME_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.TrainDisplayColorThemes>(e => Localization.Get("SETTINGS_TRAINDISPLAY_THEME_" + e.ToString().ToUpperInvariant())),
                item => item.Value == setting.TrainDisplayColorTheme,
                item => ModSetting.Instance.TrainDisplayColorTheme = item.Value, null);
            section.AddCheckBox((setting.TrainDisplayVisibleFields & ModSetting.TrainDisplayFields.Line) != 0, Localization.Get("SETTINGS_TRAINDISPLAY_SHOW_LINE"), null, Localization.Get("SETTINGS_TRAINDISPLAY_SHOW_LINE_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.TrainDisplayVisibleFields = SetFlag(ModSetting.Instance.TrainDisplayVisibleFields, ModSetting.TrainDisplayFields.Line, isChecked));
            section.AddCheckBox((setting.TrainDisplayVisibleFields & ModSetting.TrainDisplayFields.Destination) != 0, Localization.Get("SETTINGS_TRAINDISPLAY_SHOW_DESTINATION"), null, Localization.Get("SETTINGS_TRAINDISPLAY_SHOW_DESTINATION_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.TrainDisplayVisibleFields = SetFlag(ModSetting.Instance.TrainDisplayVisibleFields, ModSetting.TrainDisplayFields.Destination, isChecked));
            section.AddCheckBox((setting.TrainDisplayVisibleFields & ModSetting.TrainDisplayFields.State) != 0, Localization.Get("SETTINGS_TRAINDISPLAY_SHOW_STATE"), null, Localization.Get("SETTINGS_TRAINDISPLAY_SHOW_STATE_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.TrainDisplayVisibleFields = SetFlag(ModSetting.Instance.TrainDisplayVisibleFields, ModSetting.TrainDisplayFields.State, isChecked));
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
