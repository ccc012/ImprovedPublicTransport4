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
        public enum TrainDisplayFields
        {
            None = 0,
            Line = 1,
            Destination = 2,
            State = 4,
            Speed = 8,
            Passengers = 16,
            Elapsed = 32,
            // Extra strip (line-coloured) appears when any of these is on.
            ExtrasMask = State | Speed | Passengers | Elapsed,
        }
        // Original replicates the layout of the upstream Train Display - Updated mod (Workshop
        // 3233229958): a header strip with a key/value grid, a big centred speed readout, and a
        // bottom-left route strip coloured to match the vehicle's actual line colour.
        // Themes only recolour the same unified layout (no second panel / layout fork).
        // Simple kept for save compat (maps to Red face). Themes only recolour the single corner panel.
        // Simple=Red pastel, Dark=solid black, Light=semi white, Original=solid white,
        // Amber=Yellow pastel, BlackSemi=semi-transparent black.
        public enum TrainDisplayColorThemes
        {
            Simple = 0,
            Dark = 1,
            Light = 2,
            Original = 3,
            Blue = 4,
            Green = 5,
            Amber = 6,
            BlackSemi = 7,
        }
        public enum ExpressBusServicesModes { None = 0, Prudential = 1, Aggressive = 2 }
        public enum ExpressTramServicesModes { Disabled = 0, LightRail = 1, TrueTram = 2 }
        public enum AutoLineColorStrategy { Disabled = 0, RandomHue = 1, RandomColor = 2, CategorisedColor = 3, NamedColors = 4 }
        public enum AutoLineColorNamingStrategy { Disabled = 0, Districts = 1, London = 2, Roads = 3, NamedColors = 4 }
        public enum VehicleEditorPositions { Bottom = 0, Right = 1 }
        // One-click presets. Numeric values for Custom/Vanilla/Realistic stay stable so older
        // settings JSON files keep mapping correctly. Safe/Recommended are additive.
        // Safe = install default (everything off). Recommended = IPT core. Realistic = most on.
        public enum GameplayProfiles { Custom = 0, Vanilla = 1, Realistic = 2, Safe = 3, Recommended = 4 }
        // Controls the intercity bus terminal vehicle cap IntercityBusControl applies (see
        // Integration/IntercityBusControl/StationPatcher.cs) - Disabled matches the behaviour this
        // mod always had (an effectively uncapped terminal), Realistic leaves the terminal's own
        // prefab-defined capacity untouched, Intermediate applies a moderate fixed cap.
        public enum DepotCapacityModes { Disabled = 0, Intermediate = 1, Realistic = 2 }
        // Controls where the passenger patience widening in OptimisedOutsideConnections applies (see
        // Integration/OptimisedOutsideConnections/Patch_HumanAI_SimulationStep.cs). OutsideConnectionsOnly
        // is the corrected behaviour and the default; CityWide reproduces the source mod's actual
        // (likely unintended) effect of also slowing down ordinary domestic public-transport patience
        // for every citizen in the city, offered as an explicit opt-in rather than silently on.
        public enum PassengerWaitScopes { OutsideConnectionsOnly = 0, CityWide = 1, Disabled = 2 }

        // Record of last preset applied (dropdown selection). Fresh installs start on Safe.
        public GameplayProfiles GameplayProfile { get; set; } = GameplayProfiles.Safe;

        /// <summary>
        /// Global performance budget for heavy UI / scan / overlay work across integrations.
        /// One control instead of a slider on every feature.
        /// Light = minimize hitch risk; Normal = current balance; Maximum = fullest features.
        /// </summary>
        public enum PerformanceProfiles
        {
            Light = 0,
            Normal = 1,
            Maximum = 2,
        }

        public PerformanceProfiles PerformanceProfile { get; set; } = PerformanceProfiles.Normal;

        public VehicleSpeedUnits SpeedUnit { get; set; } = VehicleSpeedUnits.MPH;
        public string SpeedString => SpeedUnit == VehicleSpeedUnits.KPH ? Localization.Get("SETTINGS_SPEED_KPH") : Localization.Get("SETTINGS_SPEED_MPH");
        // Safety-first defaults: EVERY integration and optional behaviour starts OFF so a fresh
        // install cannot double-patch with an undetected Workshop conflict. Players opt in via
        // Gameplay profile (Recommended / Realistic) or individual toggles. Core Harmony patches
        // for the line panel still load, but fleet budget / unbunching aggression stay inactive
        // until a profile or checkbox turns them on.
        public BbspLogicModes BbspLogic { get; set; } = BbspLogicModes.Disabled;
        public WalkingSpeedModes WalkingSpeedMode { get; set; } = WalkingSpeedModes.Vanilla;
        public bool ShowLineInfo { get; set; } = false;
        // Formerly always-on absorbed mods - now opt-in for Safe-default installs.
        public bool EnableAdvancedStopSelection { get; set; } = false;
        public bool EnableBetterBoarding { get; set; } = false;
        public bool EnableMileageTaxi { get; set; } = false;
        public bool EnableElevatedStops { get; set; } = false;

        public BudgetControlModes BudgetControl { get; set; } = BudgetControlModes.Disabled;
        public TicketPriceCustomizerModes TicketPriceCustomizerMode { get; set; } = TicketPriceCustomizerModes.Disabled;
        /// <summary>
        /// When ticket prices are customized, also write NetLane.m_ticketCost on stop lanes so
        /// pathfinding slightly prefers cheaper routes (vanilla only does this for road tolls).
        /// Off by default — experimental / optional “fare as toll” behaviour.
        /// </summary>
        public bool EnableTicketPathfindingCost { get; set; } = false;
        public AutoLineBudgetModes AutoLineBudgetMode { get; set; } = AutoLineBudgetModes.Disabled;
        public TrainDisplayModes TrainDisplayMode { get; set; } = TrainDisplayModes.Disabled;
        // Original Train Display (3233229958) sits bottom-left; one corner panel only.
        public TrainDisplayOverlayPositions TrainDisplayOverlayPosition { get; set; } = TrainDisplayOverlayPositions.BottomLeft;
        // Default 200% so the unified overlay is readable at native UI scale; options slider is 100–200%.
        // User-facing 1.0 = former ~165% (readable at 100% without cranking the slider).
        // Options clamp 0.5–2.0 (50%–200%).
        public float TrainDisplayOverlayScale { get; set; } = 1.0f;
        public float TrainDisplayOverlayOpacity { get; set; } = 0.85f;
        // 250ms default — snappier sub-200ms defaults contributed to hitch reports in 4.8.
        public float TrainDisplayUpdateInterval { get; set; } = 0.25f;
        public TrainDisplayFields TrainDisplayVisibleFields { get; set; } = TrainDisplayFields.Line | TrainDisplayFields.Destination | TrainDisplayFields.State;
        // Original matches the source mod's own look (header strip + line-coloured route strip) -
        // that's what screenshots of "the real Train Display" are compared against, so it should be
        // what players see without having to discover and change a theme dropdown first.
        public TrainDisplayColorThemes TrainDisplayColorTheme { get; set; } = TrainDisplayColorThemes.Original;

        public VehicleEditorPositions VehicleEditorPosition { get; set; } = VehicleEditorPositions.Bottom;
        public bool HideVehicleEditor { get; set; }
        // Legacy / save-compat only. Capacity is intentionally not written on TransportStationAI
        // (stations are not depots; writing m_maxVehicleCount shows a fake "buses in use" line).
        // Hidden from Options; not applied by Realistic profile. Use Bus/Tram/Taxi/Trolley/Ferry
        // depot capacity modes for real fleet caps.
        public DepotCapacityModes IntercityTerminalCapacityMode { get; set; } = DepotCapacityModes.Disabled;
        // Vanilla's plain DepotAI, used by ordinary tram and taxi depots (there is no dedicated
        // TramDepotAI/TaxiDepotAI class; both share DepotAI and are told apart only by
        // m_transportInfo.m_transportType), defaults m_maxVehicleCount to the same
        // effectively-uncapped 100,000 as everything else built on that base class.
        public DepotCapacityModes TramDepotCapacityMode { get; set; } = DepotCapacityModes.Disabled;
        public DepotCapacityModes TaxiDepotCapacityMode { get; set; } = DepotCapacityModes.Disabled;
        // Covers plain bus depots (regular/biofuel garages) and tour bus garages - both use
        // TransportInfo.TransportType.Bus/.TouristBus on the same DepotAI base class.
        public DepotCapacityModes BusDepotCapacityMode { get; set; } = DepotCapacityModes.Disabled;
        public DepotCapacityModes TrolleybusDepotCapacityMode { get; set; } = DepotCapacityModes.Disabled;
        // Ferry depots ("Galpão das balsas") use TransportInfo.TransportType.Ship on DepotAI.
        public DepotCapacityModes FerryDepotCapacityMode { get; set; } = DepotCapacityModes.Disabled;
        public bool EnableOptimisedOutsideConnections { get; set; } = false;
        // Multiplies the 1-in-N chance a waiting cargo train/plane/ship rolls each simulation tick
        // to give up on a full load and depart anyway (vanilla N=2, i.e. 1-in-2 per tick) - higher
        // means it waits longer on average for a fuller load before leaving half-empty. Also used
        // (at a smaller effective scale, matching upstream's own tuning) for citizens waiting for
        // transport specifically at an outside connection. 1 leaves vanilla behaviour unchanged.
        public int OutsideConnectionWaitMultiplier { get; set; } = 4;
        public PassengerWaitScopes OutsideConnectionPassengerWaitScope { get; set; } = PassengerWaitScopes.OutsideConnectionsOnly;
        // Decorative pass-through traffic that never actually enters/exits the city, kept as four
        // separate toggles (matching the source mod) rather than one combined switch, since a player
        // may only want to quiet down (for example) road traffic while leaving rail/ship/plane alone.
        public bool DisableRoadDummyTraffic { get; set; } = false;
        public bool DisableTrainDummyTraffic { get; set; } = false;
        public bool DisablePlaneDummyTraffic { get; set; } = false;
        public bool DisableShipDummyTraffic { get; set; } = false;
        public bool EnableUnlimitedOutsideConnections { get; set; } = false;
        public bool EnableSingleTrainTrackAI { get; set; } = false;
        public bool EnableStopStacker { get; set; } = false;

        public byte IntervalAggressionFactor { get; set; } = 52;
        public int DefaultVehicleCount { get; set; } = 0;
        public int SpawnTimeInterval { get; set; } = 10;

        public ExpressBusServicesModes ExpressBusUnbunchingMode { get; set; } = ExpressBusServicesModes.None;
        public bool ExpressBusEnableSelfBalancing { get; set; } = true;
        public bool ExpressBusAllowMiddleStopBalancing { get; set; } = true;
        public bool ExpressBusEnableMinibusMode { get; set; } = true;
        public ExpressTramServicesModes ExpressTramUnbunchingMode { get; set; } = ExpressTramServicesModes.Disabled;

        public bool EnablePublicTransportUnstucker { get; set; } = false;
        public bool EnableIntercityBusControl { get; set; } = false;
        public bool EnableFlightTracker { get; set; } = false;
        public bool EnableSubBuildingsTabs { get; set; } = false;
        public bool EnableTaxiStandFix { get; set; } = false;
        // Off by default - rewrites shared global road-prefab data (higher risk class).
        public bool EnableSharedStopEnabler { get; set; } = false;
        public bool EnableCommuterDestination { get; set; } = false;

        /// <summary>
        /// Map / scan detail for Commuter Destination.
        /// Performance = few icons, faster refresh (default).
        /// FullMap = show as many destination icons as possible; refresh is slower (better for
        /// weaker PCs that still want full map info, or anyone who prefers completeness over snappiness).
        /// </summary>
        public enum CommuterDestinationMapDetails
        {
            Performance = 0,
            FullMap = 1,
        }

        public CommuterDestinationMapDetails CommuterDestinationMapDetail { get; set; } =
            CommuterDestinationMapDetails.Performance;

        /// <summary>
        /// When true, PT vehicles with passengers cannot leave their line for the depot until empty.
        /// Default off; Realistic profile turns it on.
        /// </summary>
        public bool EnableEmptyBeforeReturnToDepot { get; set; } = false;

        // Both below always ran unconditionally with no way to turn them off until this toggle was
        // added - default true so upgrading players see no behaviour change.
        public bool EnableAutoNameStops { get; set; } = true;
        public bool EnableRescueFullwidthDigits { get; set; } = true;

        // Which public-transport sub-services show the Train Display corner panel.
        [Flags]
        public enum TrainDisplayVehicleTypes
        {
            None = 0,
            Bus = 1,
            Trolleybus = 2,
            Tram = 4,
            Metro = 8,
            Train = 16,
            Monorail = 32,
            Ship = 64,
            Plane = 128,
            Taxi = 256,
            CableCar = 512,
            Tours = 1024,
            All = Bus | Trolleybus | Tram | Metro | Train | Monorail | Ship | Plane | Taxi | CableCar | Tours,
        }

        public TrainDisplayVehicleTypes TrainDisplayEnabledVehicleTypes { get; set; } = TrainDisplayVehicleTypes.All;

        public bool Unbunching { get; set; } = false; // hidden; toggled via profiles / advanced
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


