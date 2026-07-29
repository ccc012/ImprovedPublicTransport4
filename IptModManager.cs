using System;
using System.Collections.Generic;
using CSLModsCommon;
using CSLModsCommon.Compatibility;
using CSLModsCommon.Manager;

namespace ImprovedPublicTransport
{
    public class IptModManager : ModManagerBase
    {
        public override string ModName => "Improved Public Transport 4 (local fork)";
        public override string RowDescription => "Unified public transport management: fleet sizing, budgets, ticket prices, stops, unbunching and more.";
        public override DateTime VersionDate { get; } = new(2026, 7, 29);

        protected override void OnCreateSettings(SettingManager settingManager) => settingManager.Load<ModSetting>();

        public override void OnSettingsUI(ICities.UIHelperBase helper)
        {
            base.OnSettingsUI(helper);
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
            new ChangelogCollection(new Version(4, 1, 3), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Fixed, "Fixed three settings-save bugs left over from the CSLModsCommon migration (What's New dismissal, Options Reset button, per-hour ticket price editor): each wrote the new value to ModSetting correctly but then saved the old, now-dead settings object, so the change never actually persisted to disk.")
                .AddEntry(ChangelogFlag.Removed, "Removed dead code from the pre-CSLModsCommon era: the attribute-based OptionsFramework, the monolithic Settings.cs, and the standalone VehicleEditorPositions enum - all fully superseded by ModSetting and CSLModsCommonOptionsPanel.")
            ,
            new ChangelogCollection(new Version(4, 0, 0), new DateTime(2026, 7, 29), autoGenerate: false)
                .AddEntry(ChangelogFlag.Added, "Forked from Improved Public Transport 3.")
                .AddEntry(ChangelogFlag.Fixed, "Ported AutoLineBudget's demand-based fleet sizing to coordinate with IPT's own vehicle-count API instead of writing the line budget directly, fixing a runaway maintenance-cost bug caused by the two mods fighting over the same state.")
                .AddEntry(ChangelogFlag.Added, "Adopted CSLModsCommon for the Options UI (version badge, changelog, compatibility warnings, translation progress).")
        };
    }
}
