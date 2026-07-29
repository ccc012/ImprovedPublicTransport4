using System;
using CSLModsCommon.UI.OptionsPanel;
using CSLModsCommon.UI.Containers;
using CSLModsCommon.UI.Utilities;
using ColossalFramework.PlatformServices;
using ImprovedPublicTransport.Settings;

namespace ImprovedPublicTransport.UI
{
    // CSLModsCommon-based replacement for the old attribute-driven ModOptions panel
    // (see UI/ModOptions.cs). Reads/writes ModSetting directly - one explicit card per
    // setting instead of a [DropDown]/[Checkbox]/[Slider] attribute, since CSLModsCommon's
    // options UI is built imperatively rather than via reflection.
    public class CSLModsCommonOptionsPanel : OptionsPanelBase
    {
        private const string AutoLineTab = "AutoLine";
        private const string StopsTab = "Stops";
        private const string UnbunchingTab = "UnbunchingTab";
        private const string DeleteLinesTab = "DeleteLines";

        protected override void AddExtraPage()
        {
            base.AddExtraPage();
            var autoLinePage = AddPage(AutoLineTab, Localization.Get("SETTINGS_TAB_AUTOLINE"));
            FillAutoLinePage(autoLinePage);
            var stopsPage = AddPage(StopsTab, Localization.Get("SETTINGS_TAB_STOPS"));
            FillStopsPage(stopsPage);
            var unbunchingPage = AddPage(UnbunchingTab, Localization.Get("SETTINGS_TAB_UNBUNCHING"));
            FillUnbunchingPage(unbunchingPage);
            var deletePage = AddPage(DeleteLinesTab, Localization.Get("SETTINGS_TAB_DELETE"));
            FillDeleteLinesPage(deletePage);
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

        private void FillAutoLinePage(ScrollContainer page)
        {
            var setting = ModSetting.Instance;

            var autoLineSection = AddSection(page, Localization.Get("SETTINGS_AUTO_LINE"));
            autoLineSection.AddCheckBox(setting.ShowLineInfo, Localization.Get("SETTINGS_AUTOSHOW_LINE_INFO"), null, Localization.Get("SETTINGS_AUTOSHOW_LINE_INFO_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.ShowLineInfo = isChecked);

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

            var autoColorSection = AddSection(page, "AutoLineColor");
            autoColorSection.AddDropDown<ModSetting.AutoLineColorStrategy>(Localization.Get("AUTOLINECOLOR_COLOR_STRATEGY"), Localization.Get("AUTOLINECOLOR_COLOR_STRATEGY_TOOLTIP"),
                DropDownHelper.FromEnum<ModSetting.AutoLineColorStrategy>(e => Localization.Get("AUTOLINECOLOR_STRATEGY_" + e.ToString().ToUpperInvariant())),
                item => item.Value == setting.AutoLineColorColorStrategy,
                item => ModSetting.Instance.AutoLineColorColorStrategy = item.Value, null);
            autoColorSection.AddSlider(Localization.Get("AUTOLINECOLOR_MIN_COLOR_DIFF"), Localization.Get("AUTOLINECOLOR_MIN_COLOR_DIFF_TOOLTIP"),
                1f, 50f, 1f, setting.AutoLineColorMinColorDiffPercentage,
                v => ModSetting.Instance.AutoLineColorMinColorDiffPercentage = (int)v);
            autoColorSection.AddSlider(Localization.Get("AUTOLINECOLOR_MAX_COLOR_PICK"), Localization.Get("AUTOLINECOLOR_MAX_COLOR_PICK_TOOLTIP"),
                1f, 50f, 1f, setting.AutoLineColorMaxDiffColorPickAttempt,
                v => ModSetting.Instance.AutoLineColorMaxDiffColorPickAttempt = (int)v);

            var ebsBusSection = AddSection(page, Localization.Get("SETTINGS_EBS_GROUP_BUS"));
            ebsBusSection.AddDropDown<ModSetting.ExpressBusServicesModes>(Localization.Get("SETTINGS_EBS_DROPDOWN_UNBUNCHING_MODE"), Localization.Get("SETTINGS_EBS_TOOLTIP_UNBUNCHING_MODE"),
                DropDownHelper.FromEnum<ModSetting.ExpressBusServicesModes>(e => Localization.Get("SETTINGS_EBS_MODE_" + (e == ModSetting.ExpressBusServicesModes.None ? "NONE" : e.ToString().ToUpperInvariant()))),
                item => item.Value == setting.ExpressBusUnbunchingMode,
                item => ModSetting.Instance.ExpressBusUnbunchingMode = item.Value, null);
            ebsBusSection.AddCheckBox(setting.ExpressBusEnableSelfBalancing, Localization.Get("SETTINGS_EBS_ENABLE_SELFBAL"), null, Localization.Get("SETTINGS_EBS_TOOLTIP_SELFBAL"),
                (_, isChecked) => ModSetting.Instance.ExpressBusEnableSelfBalancing = isChecked);
            ebsBusSection.AddCheckBox(setting.ExpressBusAllowMiddleStopBalancing, Localization.Get("SETTINGS_EBS_ENABLE_SELFBAL_TARGETMID"), null, Localization.Get("SETTINGS_EBS_TOOLTIP_SELFBAL_TARGETMID"),
                (_, isChecked) => ModSetting.Instance.ExpressBusAllowMiddleStopBalancing = isChecked);
            ebsBusSection.AddCheckBox(setting.ExpressBusEnableMinibusMode, Localization.Get("SETTINGS_EBS_ENABLE_MINIBUS"), null, Localization.Get("SETTINGS_EBS_TOOLTIP_MINIBUS"),
                (_, isChecked) => ModSetting.Instance.ExpressBusEnableMinibusMode = isChecked);

            var ebsTramSection = AddSection(page, Localization.Get("SETTINGS_EBS_GROUP_TRAM"));
            ebsTramSection.AddDropDown<ModSetting.ExpressTramServicesModes>(Localization.Get("SETTINGS_EBS_DROPDOWN_TRAM_UNBUNCHING_MODE"), Localization.Get("SETTINGS_EBS_TOOLTIP_TRAM_UNBUNCHING"),
                DropDownHelper.FromEnum<ModSetting.ExpressTramServicesModes>(e => Localization.Get("SETTINGS_EBS_TRAM_MODE_" + (e == ModSetting.ExpressTramServicesModes.Disabled ? "NONE" : e == ModSetting.ExpressTramServicesModes.LightRail ? "LIGHT_RAIL" : "TRAM"))),
                item => item.Value == setting.ExpressTramUnbunchingMode,
                item => ModSetting.Instance.ExpressTramUnbunchingMode = item.Value, null);

            var ptuSection = AddSection(page, Localization.Get("SETTINGS_PTU_GROUP"));
            ptuSection.AddCheckBox(setting.EnablePublicTransportUnstucker, Localization.Get("SETTINGS_PTU_ENABLE"), null, Localization.Get("SETTINGS_PTU_TOOLTIP"),
                (_, isChecked) =>
                {
                    ModSetting.Instance.EnablePublicTransportUnstucker = isChecked;
                    SettingsActions.OnPublicTransportUnstuckerChanged(isChecked ? 1 : 0);
                });
        }

        private void FillStopsPage(ScrollContainer page)
        {
            var setting = ModSetting.Instance;
            var section = AddSection(page, Localization.Get("SETTINGS_STOPS"));

            void PassengerSlider(string headerKey, string tooltipKey, float min, float max, float step, int current, Action<int> setter)
                => section.AddSlider(Localization.Get(headerKey), Localization.Get(tooltipKey), min, max, step, current, v => setter((int)v));

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

        private void FillUnbunchingPage(ScrollContainer page)
        {
            var setting = ModSetting.Instance;
            var section = AddSection(page, Localization.Get("SETTINGS_UNBUNCHING"));

            section.AddSlider(Localization.Get("SETTINGS_UNBUNCHING_AGGRESSION"),
                Localization.Get("SETTINGS_UNBUNCHING_AGGRESSION_TOOLTIP") + "\n" + Localization.Get("EXPLANATION_UNBUNCHING"),
                0f, 52f, 1f, setting.IntervalAggressionFactor,
                v => ModSetting.Instance.IntervalAggressionFactor = (byte)v);

            section.AddSlider(Localization.Get("SETTINGS_VEHICLE_COUNT"), Localization.Get("SETTINGS_VEHICLE_COUNT_TOOLTIP"),
                0f, 100f, 1f, setting.DefaultVehicleCount,
                v =>
                {
                    ModSetting.Instance.DefaultVehicleCount = (int)v;
                    SettingsActions.OnDefaultVehicleCountSubmitted((int)v);
                });

            section.AddSlider(Localization.Get("SETTINGS_SPAWN_TIME_INTERVAL"), Localization.Get("SETTINGS_SPAWN_TIME_INTERVAL_TOOLTIP"),
                0f, 100f, 1f, setting.SpawnTimeInterval,
                v => ModSetting.Instance.SpawnTimeInterval = (int)v);

            section.AddButton(null, Localization.Get("SETTINGS_UNBUNCHING_RESET_BUTTON_TOOLTIP"), Localization.Get("SETTINGS_RESET"),
                () => SettingsActions.OnResetButtonClick());
        }

        private void FillDeleteLinesPage(ScrollContainer page)
        {
            var setting = ModSetting.Instance;
            var section = AddSection(page, Localization.Get("SETTINGS_LINE_DELETION_TOOL"));

            section.AddCheckBox(setting.DeleteBusLines, Localization.Get("INFO_PUBLICTRANSPORT_BUS"), null, Localization.Get("SETTINGS_DELETE_BUS_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.DeleteBusLines = isChecked);
            section.AddCheckBox(setting.DeleteSightseeingBusLines, Localization.Get("SETTINGS_DELETE_SIGHTSEEING_BUS_LABEL"), null, Localization.Get("SETTINGS_DELETE_SIGHTSEEING_BUS_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.DeleteSightseeingBusLines = isChecked);
            if (PlatformService.IsDlcInstalled(SteamHelper.kWinterDLCAppID))
            {
                section.AddCheckBox(setting.DeleteTramLines, Localization.Get("INFO_PUBLICTRANSPORT_TRAM"), null, Localization.Get("SETTINGS_DELETE_TRAM_TOOLTIP"),
                    (_, isChecked) => ModSetting.Instance.DeleteTramLines = isChecked);
            }
            section.AddCheckBox(setting.DeleteTrolleybusLines, Localization.Get("INFO_PUBLICTRANSPORT_TROLLEYBUS"), null, Localization.Get("SETTINGS_DELETE_TROLLEYBUS_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.DeleteTrolleybusLines = isChecked);
            section.AddCheckBox(setting.DeleteTrainLines, Localization.Get("INFO_PUBLICTRANSPORT_TRAIN"), null, Localization.Get("SETTINGS_DELETE_TRAIN_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.DeleteTrainLines = isChecked);
            section.AddCheckBox(setting.DeleteMetroLines, Localization.Get("INFO_PUBLICTRANSPORT_METRO"), null, Localization.Get("SETTINGS_DELETE_METRO_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.DeleteMetroLines = isChecked);
            if (PlatformService.IsDlcInstalled(SteamHelper.kMotionDLCAppID))
            {
                section.AddCheckBox(setting.DeleteMonorailLines, Localization.Get("INFO_PUBLICTRANSPORT_MONORAIL"), null, Localization.Get("SETTINGS_DELETE_MONORAIL_TOOLTIP"),
                    (_, isChecked) => ModSetting.Instance.DeleteMonorailLines = isChecked);
            }
            section.AddCheckBox(setting.DeleteShipLines, Localization.Get("SETTINGS_DELETE_FERRY_LABEL"), null, Localization.Get("SETTINGS_DELETE_SHIP_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.DeleteShipLines = isChecked);
            section.AddCheckBox(setting.DeleteHelicopterLines, Localization.Get("SETTINGS_DELETE_HELICOPTER_LABEL"), null, Localization.Get("SETTINGS_DELETE_HELICOPTER_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.DeleteHelicopterLines = isChecked);
            section.AddCheckBox(setting.DeleteBlimpLines, Localization.Get("SETTINGS_DELETE_BLIMP_LABEL"), null, Localization.Get("SETTINGS_DELETE_BLIMP_TOOLTIP"),
                (_, isChecked) => ModSetting.Instance.DeleteBlimpLines = isChecked);

            section.AddButton(null, Localization.Get("SETTINGS_LINE_DELETION_TOOL_BUTTON_TOOLTIP"), Localization.Get("SETTINGS_DELETE"),
                () => SettingsActions.OnDeleteLinesClick());
        }
    }
}
