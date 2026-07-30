using System;
using CSLModsCommon.Common;
using CSLModsCommon.Manager;
using CSLModsCommon.Setting;

namespace ImprovedPublicTransport
{
    // Canonical settings store for IPT4, replacing the old OptionsFramework-attribute
    // based Settings/Settings.cs. Every property here mirrors one from the old class
    // (same name/type/default) so migrating call sites is a straight rename:
    //   OptionsWrapper<Settings.Settings>.Options.X  ->  ModSetting.Instance.X
    [FileLocation(nameof(ImprovedPublicTransport) + nameof(ModSetting))]
    public class ModSetting : ModSettingBase
    {
        public static ModSetting Instance => Domain.DefaultDomain.GetOrCreateManager<SettingManager>().GetSetting<ModSetting>();

        public enum VehicleSpeedUnits { KPH = 0, MPH = 1 }
        public enum BbspLogicModes { Disabled = 0, OriginalLogic = 1 }
        public enum WalkingSpeedModes { Vanilla = 0, Realistic = 1 }
        public enum BudgetControlModes { Disabled = 0, Enabled = 1 }
        public enum TicketPriceCustomizerModes { Disabled = 0, Enabled = 1 }
        public enum AutoLineBudgetModes { Disabled = 0, Enabled = 1 }
        public enum TrainDisplayModes { Disabled = 0, Enabled = 1 }
        public enum TrainDisplayOverlayPositions { TopLeft = 0, TopRight = 1, BottomLeft = 2, BottomRight = 3 }
        [Flags]
        public enum TrainDisplayFields { None = 0, Line = 1, Destination = 2, State = 4 }
        // Original replicates the layout of the upstream Train Display - Updated mod (Workshop
        // 3233229958): a header strip with a key/value grid, a big centred speed readout, and a
        // bottom-left route strip coloured to match the vehicle's actual line colour.
        public enum TrainDisplayColorThemes { Simple = 0, Dark = 1, Light = 2, Original = 3 }
        public enum ExpressBusServicesModes { None = 0, Prudential = 1, Aggressive = 2 }
        public enum ExpressTramServicesModes { Disabled = 0, LightRail = 1, TrueTram = 2 }
        public enum AutoLineColorStrategy { Disabled = 0, RandomHue = 1, RandomColor = 2, CategorisedColor = 3, NamedColors = 4 }
        public enum AutoLineColorNamingStrategy { Disabled = 0, Districts = 1, London = 2, Roads = 3, NamedColors = 4 }
        public enum VehicleEditorPositions { Bottom = 0, Right = 1 }
        // Controls the intercity bus terminal vehicle cap IntercityBusControl applies (see
        // Integration/IntercityBusControl/StationPatcher.cs) - Disabled matches the behaviour this
        // mod always had (an effectively uncapped terminal), Realistic leaves the terminal's own
        // prefab-defined capacity untouched, Intermediate applies a moderate fixed cap.
        public enum DepotCapacityModes { Disabled = 0, Intermediate = 1, Realistic = 2 }

        public VehicleSpeedUnits SpeedUnit { get; set; } = VehicleSpeedUnits.MPH;
        public string SpeedString => SpeedUnit == VehicleSpeedUnits.KPH ? Localization.Get("SETTINGS_SPEED_KPH") : Localization.Get("SETTINGS_SPEED_MPH");
        public BbspLogicModes BbspLogic { get; set; } = BbspLogicModes.OriginalLogic;
        public WalkingSpeedModes WalkingSpeedMode { get; set; } = WalkingSpeedModes.Realistic;
        public bool ShowLineInfo { get; set; } = true;

        public BudgetControlModes BudgetControl { get; set; } = BudgetControlModes.Enabled;
        public TicketPriceCustomizerModes TicketPriceCustomizerMode { get; set; } = TicketPriceCustomizerModes.Enabled;
        public AutoLineBudgetModes AutoLineBudgetMode { get; set; } = AutoLineBudgetModes.Disabled;
        public TrainDisplayModes TrainDisplayMode { get; set; } = TrainDisplayModes.Enabled;
        public TrainDisplayOverlayPositions TrainDisplayOverlayPosition { get; set; } = TrainDisplayOverlayPositions.TopLeft;
        public float TrainDisplayOverlayScale { get; set; } = 1.0f;
        public float TrainDisplayOverlayOpacity { get; set; } = 0.85f;
        public float TrainDisplayUpdateInterval { get; set; } = 0.2f;
        public TrainDisplayFields TrainDisplayVisibleFields { get; set; } = TrainDisplayFields.Line | TrainDisplayFields.Destination | TrainDisplayFields.State;
        public TrainDisplayColorThemes TrainDisplayColorTheme { get; set; } = TrainDisplayColorThemes.Simple;

        public VehicleEditorPositions VehicleEditorPosition { get; set; } = VehicleEditorPositions.Bottom;
        public bool HideVehicleEditor { get; set; }
        // Default is Disabled (today's behaviour, unchanged for existing players) rather than
        // Realistic, so upgrading to this version does not silently shrink an existing city's
        // intercity bus terminal capacity out from under them.
        public DepotCapacityModes IntercityTerminalCapacityMode { get; set; } = DepotCapacityModes.Disabled;

        public byte IntervalAggressionFactor { get; set; } = 52;
        public int DefaultVehicleCount { get; set; } = 0;
        public int SpawnTimeInterval { get; set; } = 10;

        public ExpressBusServicesModes ExpressBusUnbunchingMode { get; set; } = ExpressBusServicesModes.None;
        public bool ExpressBusEnableSelfBalancing { get; set; } = true;
        public bool ExpressBusAllowMiddleStopBalancing { get; set; } = true;
        public bool ExpressBusEnableMinibusMode { get; set; } = true;
        public ExpressTramServicesModes ExpressTramUnbunchingMode { get; set; } = ExpressTramServicesModes.Disabled;

        public bool EnablePublicTransportUnstucker { get; set; } = true;
        public bool EnableIntercityBusControl { get; set; } = true;
        public bool EnableFlightTracker { get; set; } = true;
        public bool EnableSubBuildingsTabs { get; set; } = true;
        public bool EnableTaxiStandFix { get; set; } = true;
        // Off by default, unlike the other integrations above: this one relaxes stop-placement
        // flags on every loaded road prefab at level load (see SharedStopEnabler/LICENSE.txt for
        // why). Low risk in the reduced form actually shipped, but it is the one integration here
        // that changes shared, global prefab data rather than being purely additive/per-instance,
        // so it stays opt-in.
        public bool EnableSharedStopEnabler { get; set; } = false;
        public bool EnableCommuterDestination { get; set; } = true;

        public bool Unbunching { get; set; } = true; // hidden
        public int StatisticWeeks { get; set; } = 10; // hidden

        public string WhatsNewLastSeenVersion { get; set; } = "0.0.0";

        public TicketPriceCustomizerSettings TicketPriceCustomizer { get; set; } = new();

        public class TicketPriceCustomizerSettings
        {
            public float TaxiMultiplier { get; set; } = 1.0f;
            public float BusMultiplier { get; set; } = 1.0f;
            public float IntercityBusMultiplier { get; set; } = 1.0f;
            public float MetroMultiplier { get; set; } = 1.0f;
            public float TrainMultiplier { get; set; } = 1.0f;
            public float TramMultiplier { get; set; } = 1.0f;
            public float MonorailMultiplier { get; set; } = 1.0f;
            public float ShipMultiplier { get; set; } = 1.0f;
            public float FerryMultiplier { get; set; } = 1.0f;
            public float PlaneMultiplier { get; set; } = 1.0f;
            public float CableCarMultiplier { get; set; } = 1.0f;
            public float SightseeingBusMultiplier { get; set; } = 1.0f;
            public float TrolleybusMultiplier { get; set; } = 1.0f;
            public float BlimpMultiplier { get; set; } = 1.0f;
            public float HelicopterMultiplier { get; set; } = 1.0f;

            public float TaxiNightMultiplier { get; set; } = 1.0f;
            public float BusNightMultiplier { get; set; } = 1.0f;
            public float IntercityBusNightMultiplier { get; set; } = 1.0f;
            public float MetroNightMultiplier { get; set; } = 1.0f;
            public float TrainNightMultiplier { get; set; } = 1.0f;
            public float TramNightMultiplier { get; set; } = 1.0f;
            public float MonorailNightMultiplier { get; set; } = 1.0f;
            public float ShipNightMultiplier { get; set; } = 1.0f;
            public float FerryNightMultiplier { get; set; } = 1.0f;
            public float PlaneNightMultiplier { get; set; } = 1.0f;
            public float CableCarNightMultiplier { get; set; } = 1.0f;
            public float SightseeingBusNightMultiplier { get; set; } = 1.0f;
            public float TrolleybusNightMultiplier { get; set; } = 1.0f;
            public float BlimpNightMultiplier { get; set; } = 1.0f;
            public float HelicopterNightMultiplier { get; set; } = 1.0f;
        }

        public AutoLineColorStrategy AutoLineColorColorStrategy { get; set; } = AutoLineColorStrategy.Disabled;
        public AutoLineColorNamingStrategy AutoLineColorNamingStrategyMode { get; set; } = AutoLineColorNamingStrategy.Disabled;
        public int AutoLineColorMinColorDiffPercentage { get; set; } = 5;
        public int AutoLineColorMaxDiffColorPickAttempt { get; set; } = 10;

        public int MaxWaitingPassengersBus { get; set; } = 50;
        public int MaxWaitingPassengersTrolleybus { get; set; } = 50;
        public int MaxWaitingPassengersEvacuationBus { get; set; } = 100;
        public int MaxWaitingPassengersTouristBus { get; set; } = 50;
        public int MaxWaitingPassengersTram { get; set; } = 80;
        public int MaxWaitingPassengersMetro { get; set; } = 250;
        public int MaxWaitingPassengersTrain { get; set; } = 250;
        public int MaxWaitingPassengersMonorail { get; set; } = 250;
        public int MaxWaitingPassengersShip { get; set; } = 150;
        public int MaxWaitingPassengersAirplane { get; set; } = 250;
        public int MaxWaitingPassengersCableCar { get; set; } = 40;
        public int MaxWaitingPassengersHotAirBalloon { get; set; } = 40;
        public int MaxWaitingPassengersHelicopter { get; set; } = 40;
    }
}


