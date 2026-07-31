using System;
using System.Collections.Generic;
using CSLModsCommon;
using CSLModsCommon.Compatibility;
using CSLModsCommon.Manager;

namespace ImprovedPublicTransport
{
    public class IptModManager : ModManagerBase
    {
        static IptModManager()
        {
            JsonNetBootstrap.EnsureLoaded();
        }

        public IptModManager()
        {
            JsonNetBootstrap.EnsureLoaded();
        }

        public override string ModName => "Improved Public Transport 4 (local fork)";
        public override string RowDescription => "Unified public transport management: fleet sizing, budgets, ticket prices, stops, unbunching and more.";
        public override DateTime VersionDate { get; } = new(2026, 7, 31);

        // Declared here rather than via a compile constant so the channel is explicit in code and
        // does not depend on how the project happens to be built. The framework's default is Alpha,
        // which is why the Options header showed "ALPHA" before this was set.
        //
        // Stable as of 4.3.8: the maintenance-overflow bug that motivated the fork is fixed at the
        // root (4.3.6) with a repair pass for saves it already damaged, and all 23 languages are
        // complete. Drop this back to Beta when landing something that needs field testing.
        public override BuildChannel CurrentBuildChannel => BuildChannel.Stable;

        protected override void OnCreateSettings(SettingManager settingManager)
        {
            JsonNetBootstrap.EnsureLoaded();
            settingManager.Load<ModSetting>();
        }

        public override void OnSettingsUI(ICities.UIHelperBase helper)
        {
            JsonNetBootstrap.EnsureLoaded();
            base.OnSettingsUI(helper);
        }

        protected override void AddVersionModRule(IVersionModRule rule)
        {
            base.AddVersionModRule(rule);
            // Built/tested against 1.21.1-f9. Generous upper bound so routine game patches don't
            // immediately flag this local fork as "not made for this version" until we've actually
            // checked against them.
            rule.Set(new GameVersionCompatibility(new GameVersion(1, 21, 1, 9), new GameVersion(1, 99, 9, 99)));
        }

        protected override void AddIncompatibleModRule(IIncompatibleModRule rule)
        {
            base.AddIncompatibleModRule(rule);
            rule.Add("ImprovedPublicTransport3", IncompatibilityModLevel.EnableNotAllowed, "Improved Public Transport 3",
                    true, "Improved Public Transport 4 (local fork)",
                    "IPT4 is a local fork of IPT3 that replaces it entirely - running both patches the same game systems twice.")
                .Add("TransportLinesManager", IncompatibilityModLevel.EnableNotAllowed, "Transport Lines Manager",
                    true, "Improved Public Transport 4 (local fork)",
                    "TLM and IPT manage the same per-line vehicle count/budget state - running both caused the original budget glitch this project set out to fix.")
                .Add("AutoLineBudget", IncompatibilityModLevel.EnableNotAllowed, "Auto Line Budget 21",
                    true, "Improved Public Transport 4 (local fork)",
                    "AutoLineBudget's fleet-sizing logic is now built into IPT4 (Options > Auto Line > Automatic Fleet Sizing) - running the standalone mod alongside it caused runaway maintenance costs by both writing the line budget at once.")

                // --- Critical incompatibilities: other IPT-family / line-management mods ---
                .Add("ImprovedPublicTransport2", IncompatibilityModLevel.EnableNotAllowed, "Improved Public Transport 2 (IPT2)",
                    true, "Improved Public Transport 4 (local fork)",
                    "IPT2 is an earlier generation of the same mod family that IPT4 replaces - running both patches the same public transport systems twice.")
                .Add("ImprovedTransportManager", IncompatibilityModLevel.EnableNotAllowed, "Improved Transport Manager",
                    true, "Improved Public Transport 4 (local fork)",
                    "Improved Transport Manager is klyte45's successor to Transport Lines Manager and manages the same per-line vehicle count/budget state as IPT4.")
                .Add("TransportLineColorMod", IncompatibilityModLevel.EnableNotAllowed, "TransportLineColorMod",
                    true, "Improved Public Transport 4 (local fork)",
                    "TransportLineColorMod's line-coloring functionality overlaps with IPT4's own line color tools and can fight over the same line data.")
                .Add("VehicleUnbuncher", IncompatibilityModLevel.EnableNotAllowed, "Vehicle Unbuncher",
                    true, "Improved Public Transport 4 (local fork)",
                    "Vehicle Unbuncher's vehicle-spacing logic overlaps with IPT4's own unbunching/spawn-delay features and can fight over the same vehicles.")

                // --- Absorbed standalone mods (functionality already built into IPT4) ---
                .Add("AdvancedStopSelection", IncompatibilityModLevel.EnableNotAllowed, "Advanced Stop Selection Revisited",
                    true, "Improved Public Transport 4 (local fork)",
                    "Advanced Stop Selection's stop-selection logic is now built into IPT4 - running both patches the same stop-assignment behaviour twice.")
                .Add("AutoLineColor", IncompatibilityModLevel.EnableNotAllowed, "AutoLineColor / AutoLineColor Redux",
                    true, "Improved Public Transport 4 (local fork)",
                    "AutoLineColor's automatic line-coloring is now built into IPT4 - running the standalone mod alongside it causes both to write line colors at once.")
                .Add("BetterTrainBoarding", IncompatibilityModLevel.EnableNotAllowed, "Better Train Boarding",
                    true, "Improved Public Transport 4 (local fork)",
                    "Better Train Boarding's passenger-loading logic is now built into IPT4 - running both patches the same boarding behaviour twice.")
                .Add("BetterBusStopPosition", IncompatibilityModLevel.EnableNotAllowed, "Better Bus Stop Position",
                    true, "Improved Public Transport 4 (local fork)",
                    "Better Bus Stop Position's stop-placement adjustments are now built into IPT4 - running both patches the same stop geometry twice.")
                .Add("CommuterDestination.CS1", IncompatibilityModLevel.EnableNotAllowed, "Commuter Destination",
                    true, "Improved Public Transport 4 (local fork)",
                    "Commuter Destination's citizen-destination display is now built into IPT4 - running both reads/patches the same panel twice.")
                .Add("ElevatedStopsEnabler", IncompatibilityModLevel.EnableNotAllowed, "Elevated Stops Enabler (Original & Revisited)",
                    true, "Improved Public Transport 4 (local fork)",
                    "Elevated Stops Enabler's elevated-stop support is now built into IPT4 - running both patches the same road/bridge stop logic twice.")
                .Add("ExpressBusServices", IncompatibilityModLevel.EnableNotAllowed, "Express Bus Services",
                    true, "Improved Public Transport 4 (local fork)",
                    "Express Bus Services' express-line behaviour is now built into IPT4 - running both patches the same bus AI twice.")
                .Add("FlightTracker", IncompatibilityModLevel.EnableNotAllowed, "Flight Tracker",
                    true, "Improved Public Transport 4 (local fork)",
                    "Flight Tracker's flight-tracking panel is now built into IPT4 - running both patches the same building info panel twice.")
                .Add("RegionalBuses", IncompatibilityModLevel.EnableNotAllowed, "Intercity Bus Control",
                    true, "Improved Public Transport 4 (local fork)",
                    "Intercity Bus Control's regional/intercity bus management is now built into IPT4 - running both patches the same bus line behaviour twice.")
                .Add("MileageTaxiServices", IncompatibilityModLevel.EnableNotAllowed, "Mileage Taxi Services",
                    true, "Improved Public Transport 4 (local fork)",
                    "Mileage Taxi Services' taxi fare-by-distance logic is now built into IPT4 - running both patches the same taxi AI twice.")
                .Add("PublicTransportUnstucker", IncompatibilityModLevel.EnableNotAllowed, "Public Transport Unstucker",
                    true, "Improved Public Transport 4 (local fork)",
                    "Public Transport Unstucker's anti-rogue-vehicle patches are now built into IPT4 - running both patches the same vehicle AI twice.")
                .Add("RealisticWalkingSpeed", IncompatibilityModLevel.EnableNotAllowed, "Realistic Walking Speed",
                    true, "Improved Public Transport 4 (local fork)",
                    "Realistic Walking Speed's citizen walking-speed adjustments are now built into IPT4 - running both patches the same citizen AI twice.")
                .Add("SharedStopEnabler", IncompatibilityModLevel.EnableNotAllowed, "Shared Stop Enabler",
                    true, "Improved Public Transport 4 (local fork)",
                    "Shared Stop Enabler's shared-stop support is now built into IPT4 (as a reduced port) - running both patches the same stop-sharing logic twice.")
                .Add("StopsAndStations", IncompatibilityModLevel.EnableNotAllowed, "Stops & Stations",
                    true, "Improved Public Transport 4 (local fork)",
                    "Stops & Stations' stop/station management is now built into IPT4 - running both patches the same stop systems twice.")
                .Add("SubBuildingsTabBar", IncompatibilityModLevel.EnableNotAllowed, "Sub-Buildings Tabs",
                    true, "Improved Public Transport 4 (local fork)",
                    "Sub-Buildings Tabs' sub-building tab UI is now built into IPT4 - running both patches the same world info panel twice.")
                .Add("TicketPriceCustomizer", IncompatibilityModLevel.EnableNotAllowed, "Ticket Price Customizer",
                    true, "Improved Public Transport 4 (local fork)",
                    "Ticket Price Customizer's fare customization is now built into IPT4 - running both patches the same ticket-price logic twice.")
                .Add("TransitVehicleSpawnDelay", IncompatibilityModLevel.EnableNotAllowed, "Transit Vehicle Spawn Delay",
                    true, "Improved Public Transport 4 (local fork)",
                    "Transit Vehicle Spawn Delay's spawn-timing adjustments are now built into IPT4 - running both patches the same vehicle-spawning logic twice.")
                .Add("TrainDisplay", IncompatibilityModLevel.EnableNotAllowed, "Train Display (Original & Updated)",
                    true, "Improved Public Transport 4 (local fork)",
                    "Train Display's train-info readout is now built into IPT4 - running both patches the same vehicle display logic twice.")
                .Add("CargoHoldFix", IncompatibilityModLevel.EnableNotAllowed, "Optimised Outside Connections",
                    true, "Improved Public Transport 4 (local fork)",
                    "Optimised Outside Connections' outside-connection cargo/pathing fixes are now built into IPT4 - running both patches the same outside-connection logic twice.")
                .Add("UnlimitedOutsideConnectionsRevisited", IncompatibilityModLevel.EnableNotAllowed, "Unlimited Outside Connections Revisited",
                    true, "Improved Public Transport 4 (local fork)",
                    "Unlimited Outside Connections Revisited's outside-connection limit removal is now built into IPT4 - running both patches the same outside-connection logic twice.")

                // --- Obsolete & legacy originals (superseded, should also warn) ---
                .Add("MultiTrackStationEnabler", IncompatibilityModLevel.EnableNotAllowed, "Advanced Stop Selection (Original by BloodyPenguin)",
                    true, "Improved Public Transport 4 (local fork)",
                    "This is the original Multi-Track Station Enabler-based stop selection mod that Advanced Stop Selection Revisited superseded, and whose functionality is now built into IPT4.")
                .Add("ExtendedPublicTransportUI", IncompatibilityModLevel.EnableNotAllowed, "Extended Public Transport UI (+400)",
                    true, "Improved Public Transport 4 (local fork)",
                    "Extended Public Transport UI's line-count-limit removal and extended UI are now built into IPT4 - running both patches the same public transport UI twice.");
        }

        protected override List<ChangelogCollection> GenerateChangelogs() => new()
        {
            new ChangelogCollection(new Version(4, 8, 0), new DateTime(2026, 7, 31), autoGenerate: false)
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_8_0_1"))
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_8_0_2"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_0_3"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_0_4"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_0_5"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_0_6"))
                .AddEntry(ChangelogFlag.Optimized, L("CHANGELOG_4_8_0_7"))
                .AddEntry(ChangelogFlag.Updated, L("CHANGELOG_4_8_0_8"))
            ,
            new ChangelogCollection(new Version(4, 7, 0), new DateTime(2026, 7, 30), autoGenerate: false)
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_7_0_1"))
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_7_0_2"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_7_0_3"))
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_7_0_4"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_7_0_5"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_7_0_6"))
                .AddEntry(ChangelogFlag.Optimized, L("CHANGELOG_4_7_0_7"))
                .AddEntry(ChangelogFlag.Updated, L("CHANGELOG_4_7_0_8"))
            ,
            new ChangelogCollection(new Version(4, 3, 8), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Updated, L("CHANGELOG_4_3_8_1"))
            ,
            new ChangelogCollection(new Version(4, 3, 7), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Translation, L("CHANGELOG_4_3_7_1"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_3_7_2"))
            ,
            new ChangelogCollection(new Version(4, 3, 6), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_3_6_1"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_3_6_2"))
            ,
            new ChangelogCollection(new Version(4, 3, 5), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_3_5_1"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_3_5_2"))
            ,
            new ChangelogCollection(new Version(4, 3, 4), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_3_4_1"))
            ,
            new ChangelogCollection(new Version(4, 3, 3), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_3_3_1"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_3_3_2"))
                .AddEntry(ChangelogFlag.Translation, L("CHANGELOG_4_3_3_3"))
                .AddEntry(ChangelogFlag.Optimized, L("CHANGELOG_4_3_3_4"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_3_3_5"))
            ,
            new ChangelogCollection(new Version(4, 3, 2), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_3_2_1"))
            ,
            new ChangelogCollection(new Version(4, 3, 1), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_3_1_1"))
                .AddEntry(ChangelogFlag.Translation, L("CHANGELOG_4_3_1_2"))
                .AddEntry(ChangelogFlag.Translation, L("CHANGELOG_4_3_1_3"))
            ,
            new ChangelogCollection(new Version(4, 3, 0), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_3_0_1"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_3_0_2"))
            ,
            new ChangelogCollection(new Version(4, 2, 4), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_2_4_1"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_2_4_2"))
                .AddEntry(ChangelogFlag.Updated, L("CHANGELOG_4_2_4_3"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_2_4_4"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_2_4_5"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_2_4_6"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_2_4_7"))
            ,
            new ChangelogCollection(new Version(4, 2, 3), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_2_3_1"))
                .AddEntry(ChangelogFlag.Removed, L("CHANGELOG_4_2_3_2"))
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_2_3_3"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_2_3_4"))
            ,
            new ChangelogCollection(new Version(4, 2, 0), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_2_0_1"))
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_2_0_2"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_2_0_3"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_2_0_4"))
            ,
            new ChangelogCollection(new Version(4, 1, 5), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_1_5_1"))
            ,
            new ChangelogCollection(new Version(4, 1, 4), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_1_4_1"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_1_4_2"))
            ,
            new ChangelogCollection(new Version(4, 1, 3), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_1_3_1"))
                .AddEntry(ChangelogFlag.Removed, L("CHANGELOG_4_1_3_2"))
            ,
            new ChangelogCollection(new Version(4, 0, 0), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_0_0_1"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_0_0_2"))
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_0_0_3"))
        };

        private static string L(string key) => Localization.Get(key);
    }
}
