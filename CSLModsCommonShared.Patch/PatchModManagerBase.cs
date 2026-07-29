using CitiesHarmony.API;
using CSLModsCommon.Compatibility;
using CSLModsCommon.Localization;
using CSLModsCommon.Manager;
using CSLModsCommon.UI.Dialogs;
using CSLModsCommon.Utilities;

namespace CSLModsCommon.Patch;

public abstract class PatchModManagerBase : ModManagerBase {
    public virtual string HarmonyID { get; set; } = AssemblyHelper.CurrentAssemblyName;
    public bool IsPatched { get; private set; }
    public HarmonyPatcher Patcher { get; private set; }

    protected override void OnLoad() => ApplyPatches();

    protected override void OnUnload() => RemovePatches();

    protected override void AddDependencyModRule(IDependencyModRule rule) {
        base.AddDependencyModRule(rule);
        rule.Add("CitiesHarmony.Harmony", "Harmony");
    }

    private void ApplyPatches() {
        if (IsPatched) return;
        if (HarmonyHelper.IsHarmonyInstalled) {
            Logger.Info("Starting Harmony patches");
            Patcher = new HarmonyPatcher(HarmonyID);
            Patcher.Enable(RegisterPatches);
            IsPatched = true;
            Logger.Info("Harmony patches completed");
        }
        else {
            Domain.GetOrCreateManager<DialogManager>().Show<OkDialog>().AddContent(ModName, SharedTranslations.MissingHarmonyDependency);
            Logger.Fatal("Harmony is not installed correctly");
        }
    }

    private void RemovePatches() {
        if (!IsPatched || !HarmonyHelper.IsHarmonyInstalled)
            return;
        Logger.Info("Reverting Harmony patches");
        Patcher.Disable();
        Patcher = null;
        IsPatched = false;
    }

    protected virtual void RegisterPatches(HarmonyPatcher harmonyPatcher) { }
}