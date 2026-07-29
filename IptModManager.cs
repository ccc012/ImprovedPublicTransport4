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
        public override DateTime VersionDate { get; } = new(2026, 7, 29);

        // Declared here rather than via a BETA compile constant so the channel is explicit in code
        // and does not depend on how the project happens to be built. The framework's default is
        // Alpha, which is why the Options header showed "ALPHA" until now.
        public override BuildChannel CurrentBuildChannel => BuildChannel.Beta;

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
                    "AutoLineBudget's fleet-sizing logic is now built into IPT4 (Options > Auto Line > Automatic Fleet Sizing) - running the standalone mod alongside it caused runaway maintenance costs by both writing the line budget at once.");
        }

        protected override List<ChangelogCollection> GenerateChangelogs() => new()
        {
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
