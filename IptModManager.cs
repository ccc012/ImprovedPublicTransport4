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
        public override DateTime VersionDate { get; } = new(2026, 8, 2);

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

        /// <summary>CitiesHarmony is required for every Harmony patch in IPT4.</summary>
        protected override void AddDependencyModRule(IDependencyModRule rule)
        {
            base.AddDependencyModRule(rule);
            rule.Add("CitiesHarmony.Harmony", "Harmony (CitiesHarmony) — Workshop 2040656402");
        }

        protected override void AddIncompatibleModRule(IIncompatibleModRule rule)
        {
            base.AddIncompatibleModRule(rule);

            const string Alt = "Improved Public Transport 4";
            const IncompatibilityModLevel Ban = IncompatibilityModLevel.EnableNotAllowed;

            // Critical: same fleet/budget domain as IPT4 (or earlier IPT generations).
            rule.AddWithWorkshop("ImprovedPublicTransport", Ban, "Improved Public Transport (original)",
                    "IPT1 is replaced entirely by IPT4 — both patch the same transit systems.", 424106600u)
                .AddWithWorkshop("ImprovedPublicTransport2", Ban, "Improved Public Transport 2 (IPT2)",
                    "IPT2 is an earlier generation IPT4 replaces — double-patching breaks fleet/budget control.", 928128676u)
                .AddWithWorkshop("ImprovedPublicTransport3", Ban, "Improved Public Transport 3 (IPT3)",
                    "IPT4 is a fork of IPT3 that replaces it entirely — never run both.", 3690061052u)
                .AddWithWorkshop("TransportLinesManager", Ban, "Transport Lines Manager",
                    "TLM and IPT both own per-line vehicle count/budget — together they fight over fleets.", 1312767991u, 3007903394u)
                .AddWithWorkshop("ImprovedTransportManager", Ban, "Improved Transport Manager (ITM)",
                    "Klyte45 unfinished TLM/IPT2 successor — same per-line budget/fleet domain as IPT4.", 2888964436u)
                .AddWithWorkshop("AutoLineBudget", Ban, "Auto Line Budget 21",
                    "Fleet sizing is built into IPT4 — both writing budgets caused runaway maintenance costs.", 2349240408u);

            // Absorbed / clean-room standalones (name + Workshop ID). Commuter Destination alt
            // assemblies registered separately below. Other mods stay unlisted to avoid false positives.
            rule.AddWithWorkshop("SharedStopEnabler", Ban, "SharedStopEnabler", "Conflicts with IPT4 shared-stop implementation.", 2096382380u)
                .AddWithWorkshop("SingleTrainTrackAI", Ban, "SingleTrainTrackAI", "Clean-room implementation already in IPT4 — both can double-brake trains.", 949504539u)
                .AddWithWorkshop("RealisticWalkingSpeed", Ban, "Realistic Walking Speed", "Walking/cycling speed modes are built into IPT4.", 1412844620u)
                .AddWithWorkshop("UnlimitedOutsideConnections", Ban, "Unlimited Outside Connections", "Unlimited outside connections is built into IPT4.", 2367735356u)
                .AddWithWorkshop("ExpressBusServices", Ban, "Express Bus Services", "Express bus/tram AI is built into IPT4.", 2262054175u)
                .AddWithWorkshop("AdvancedStopSelection", Ban, "Advanced Stop Selection Revisited", "Stop platform selection is built into IPT4.", 2862973068u)
                .AddWithWorkshop("BetterBusStopPosition", Ban, "Better Bus Stop Position", "Stop positioning is built into IPT4.", 3491515535u)
                .AddWithWorkshop("BetterTrainBoarding", Ban, "Better Train Boarding", "Boarding logic is built into IPT4.", 2773460744u)
                .AddWithWorkshop("ElevatedStopsEnabler", Ban, "Elevated Stops Enabler (Revisited)", "Elevated stop support is built into IPT4.", 2862992091u, 634913093u)
                .AddWithWorkshop("MileageTaxiServices", Ban, "Mileage Taxi Services", "Taxi fare-by-distance is built into IPT4.", 3492156582u)
                .AddWithWorkshop("OptimisedOutsideConnections", Ban, "Optimised Outside Connections", "Outside-connection cargo wait tuning is built into IPT4.", 1721492498u)
                .AddWithWorkshop("PublicTransportUnstucker", Ban, "Public Transport Unstucker", "Unstucker patches are built into IPT4.", 2774427140u)
                .AddWithWorkshop("TransitVehicleSpawnDelay", Ban, "Transit Vehicle Spawn Delay", "Redundant: IPT4 DepotAI.StartTransfer enforces a per-line spawn interval (Unbunching spawn time).", 2654110611u)
                .AddWithWorkshop("CommuterDestination.CS1", Ban, "Commuter Destination", "Commuter Destination UI/icons are built into IPT4.", 2475986859u);

            // Known alternate assembly names for absorbed mods (forks / renames).
            rule.Add(new IncompatibleModItem("CommuterDestination", Ban, "Commuter Destination (alt assembly)",
                    true, Alt, "Commuter Destination is built into IPT4.")
                .WithAlternateAssemblies("CSL-ShowCommuterDestination", "ShowCommuterDestination")
                .WithWorkshopIds(2475986859u));
        }

        protected override List<ChangelogCollection> GenerateChangelogs() => new()
        {
            new ChangelogCollection(new Version(4, 8, 9), new DateTime(2026, 8, 7), autoGenerate: false)
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_8_9_1"))
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_8_9_2"))
                .AddEntry(ChangelogFlag.Updated, L("CHANGELOG_4_8_9_3"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_9_4"))
                .AddEntry(ChangelogFlag.Updated, L("CHANGELOG_4_8_9_5"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_9_6"))
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_8_9_7"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_9_8"))
            ,
            new ChangelogCollection(new Version(4, 8, 8), new DateTime(2026, 8, 3), autoGenerate: false)
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_8_1"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_8_2"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_8_3"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_8_4"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_8_5"))
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_8_8_6"))
            ,
            new ChangelogCollection(new Version(4, 8, 7), new DateTime(2026, 8, 2), autoGenerate: false)
                .AddEntry(ChangelogFlag.Updated, L("CHANGELOG_4_8_7_1"))
                .AddEntry(ChangelogFlag.Removed, L("CHANGELOG_4_8_7_2"))
            ,
            new ChangelogCollection(new Version(4, 8, 6), new DateTime(2026, 8, 2), autoGenerate: false)
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_6_1"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_6_2"))
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_8_6_3"))
            ,
            new ChangelogCollection(new Version(4, 8, 5), new DateTime(2026, 8, 1), autoGenerate: false)
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_5_1"))
                .AddEntry(ChangelogFlag.Optimized, L("CHANGELOG_4_8_5_2"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_5_3"))
                .AddEntry(ChangelogFlag.Updated, L("CHANGELOG_4_8_5_4"))
                .AddEntry(ChangelogFlag.Optimized, L("CHANGELOG_4_8_5_5"))
            ,
            new ChangelogCollection(new Version(4, 8, 0), new DateTime(2026, 7, 31), autoGenerate: false)
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_8_0_1"))
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_8_0_2"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_0_3"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_0_4"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_0_5"))
                .AddEntry(ChangelogFlag.Fixed, L("CHANGELOG_4_8_0_6"))
                .AddEntry(ChangelogFlag.Optimized, L("CHANGELOG_4_8_0_7"))
                .AddEntry(ChangelogFlag.Updated, L("CHANGELOG_4_8_0_8"))
                .AddEntry(ChangelogFlag.Updated, L("CHANGELOG_4_8_0_9"))
                .AddEntry(ChangelogFlag.Added, L("CHANGELOG_4_8_0_10"))
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
