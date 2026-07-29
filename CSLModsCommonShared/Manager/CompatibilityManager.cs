using ColossalFramework.Plugins;
using CSLModsCommon.Common;
using CSLModsCommon.Compatibility;
using CSLModsCommon.Localization;
using CSLModsCommon.UI.Dialogs;
using System.Linq;
using CSLModsCommon.Utilities;

namespace CSLModsCommon.Manager;

public class CompatibilityManager : ManagerBase {
    private ModManagerBase _modManager;
    private DialogManager _dialogManager;
    private string _modName;
    private CompatibilityStatus _currentStatus;

    public bool ShouldRestartGame { get; private set; }
    internal DependencyModRule DependencyRule { get; private set; }
    internal VersionModRule VersionRule { get; private set; }
    internal IncompatibleModRule IncompatibleRule { get; private set; }
    public CompatibilityStatus CurrentStatus => _currentStatus;

    protected override void OnCreate() {
        base.OnCreate();
        _modManager = Domain.GetModManager();
        _dialogManager = Domain.GetOrCreateManager<DialogManager>();
        _modName = _modManager.ModName;
        DependencyRule = new DependencyModRule();
        VersionRule = new VersionModRule();
        IncompatibleRule = new IncompatibleModRule();
        _modManager.ResisterCompatibilityInfo(DependencyRule, VersionRule, IncompatibleRule);
    }

    protected override void OnGameInitialized() {
        base.OnGameInitialized();
        CheckAndShowDialogIfNeeded();
    }

    public void CheckAndShowDialog() {
        CheckCompatibility();
        ShowCompatibilityDialog();
    }

    internal void CheckAndShowDialogIfNeeded() {
        CheckCompatibility();
        if (_currentStatus.HasStatus(CompatibilityStatus.IncompatibleMods))
            ShowCompatibilityDialog();
    }

    internal void OnHandleIncompatibleMods() {
        if (IncompatibleRule.Count == 0) return;
        IncompatibleRule.Handle();
        ShouldRestartGame = true;
        ShowRestartGameDialog();
    }

    private void ShowCompatibilityDialog() => _dialogManager.Show<CompatibilityDialog>();

    private void ShowRestartGameDialog() => _dialogManager.Show<OkDialog>().AddContent(_modName, SharedTranslations.RestartGame);

    private void CheckCompatibility() {
        if (!PluginManager.exists) return;
#if ALPHA
        using var pc = PerformanceCounter.Start(v => Logger.Info($"Check compatibility took {v.ReportMilliseconds}"));
#endif
        _currentStatus = CompatibilityStatus.Normal;
        var pluginsInfo = PluginManager.instance.GetPluginsInfo();
        var pluginInfos = pluginsInfo as PluginManager.PluginInfo[] ?? pluginsInfo.ToArray();
        DependencyRule.Check(pluginInfos, ref _currentStatus);
        VersionRule.Check(pluginInfos, ref _currentStatus);
        IncompatibleRule.Check(pluginInfos, ref _currentStatus);
        Logger.Info($"Current mod Status: {_currentStatus.ToString()}");
    }
}