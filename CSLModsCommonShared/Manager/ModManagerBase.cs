using ColossalFramework.UI;
using CSLModsCommon.Compatibility;
using CSLModsCommon.Extension;
using CSLModsCommon.Utilities;
using ICities;
using System;
using System.Collections.Generic;
using CSLModsCommon.Logging;
using UnityEngine;

namespace CSLModsCommon.Manager;

public abstract class ModManagerBase : ManagerBase {
    public static RuntimePlatform CurrentPlatform { get; } = Application.platform;
    public static event Action GameInitialized;

    protected UpdateManager _updateManager;
    protected SettingManager _modSettingManager;
    private List<ChangelogCollection> _changelogs;
    private bool _isInitialized;

    public abstract string ModName { get; }
    public abstract DateTime VersionDate { get; }

    public virtual string RowDescription => string.Empty;
    public virtual string ModTranslationURL => string.Empty;
    public virtual Version ModVersion => AssemblyHelper.CurrentAssemblyVersion;
    public virtual string ModSteamURL => string.Empty;
#if BETA
    public virtual BuildChannel CurrentBuildChannel => BuildChannel.Beta;
#elif STABLE
            public virtual BuildChannel CurrentBuildChannel => BuildChannel.Stable;
#else
    public virtual BuildChannel CurrentBuildChannel => BuildChannel.Alpha;
#endif

    public bool IsEnabled { get; private set; }
    public string SharedTranslationURL => "https://crowdin.com/project/csshared";
    public string DiscordURL => "https://discord.gg/ZxbyzmvGxG";
    public List<ChangelogCollection> Changelogs => _changelogs ??= GenerateChangelogs();

    protected abstract void OnCreateSettings(SettingManager settingManager);

    protected virtual void OnLoad() { }
    protected virtual void OnUnload() { }
    protected virtual void OnUpdateMangers(UpdateManager updateManager) { }

    protected virtual List<ChangelogCollection> GenerateChangelogs() => new List<ChangelogCollection>();

    protected virtual void AddDependencyModRule(IDependencyModRule rule) { }
    protected virtual void AddVersionModRule(IVersionModRule rule) { }
    protected virtual void AddIncompatibleModRule(IIncompatibleModRule rule) { }

    protected virtual void ChangeLogLevel() {
#if ALPHA
        Logger.SetLevel(LogLevel.Verbose).SetShowsErrorsInUI();
#endif
    }

    public virtual void OnSettingsUI(UIHelperBase helper) {
        Logger.Info("Setting UI");
        OptionsPanelManager.SettingsUI(helper);
    }

    internal override void OnInstanceCreated() {
        base.OnInstanceCreated();
        ChangeLogLevel();
        Domain.CacheModManager(this);
    }

    public string GetModEntryName() => CurrentBuildChannel switch {
        BuildChannel.Alpha or BuildChannel.Beta => $"{ModName} [BETA] {ModVersion.ToDisplayString()}",
        _ => $"{ModName} {ModVersion.ToDisplayString()}"
    };

    public string GetModEntryDescription() => LocalizationManager.GetModDescription();

    internal void OnEnabled() {
        Logger.Info("Enabled");
        IsEnabled = true;
        _updateManager = Domain.GetOrCreateManager<UpdateManager>();
        _modSettingManager = _updateManager.UpdateAt<SettingManager>(UpdatePhase.Default);
        OnCreateSettings(_modSettingManager);
        _updateManager.UpdateAt<LocalizationManager>(UpdatePhase.Default);
        _updateManager.UpdateAt<DialogManager>(UpdatePhase.Default);
        var compatibilityManager = _updateManager.UpdateAt<CompatibilityManager>(UpdatePhase.Default);
        _updateManager.UpdateAt<KeyBindingManager>(UpdatePhase.Default);
        _updateManager.UpdateAt<KeyBindingManager>(UpdatePhase.Default);
        OnUpdateMangers(_updateManager);

        if (!_isInitialized) {
            if (UIView.GetAView() != null)
                OnIntroLoaded();
            else
                LoadingManager.instance.m_introLoaded += OnIntroLoaded;
        }
        else {
            compatibilityManager.CheckAndShowDialogIfNeeded();
        }

        OnLoad();

        _isInitialized = true;
    }

    internal void OnDisabled() {
        Logger.Info("Disabled");
        IsEnabled = false;
        OnUnload();
    }

    internal void ResisterCompatibilityInfo(IDependencyModRule dependencyModRule, IVersionModRule versionModRule, IIncompatibleModRule incompatibleModRule) {
        AddDependencyModRule(dependencyModRule);
        AddVersionModRule(versionModRule);
        AddIncompatibleModRule(incompatibleModRule);
    }

    private void OnIntroLoaded() {
        Logger.Verbose("OnIntroLoaded");
        GameInitialized?.Invoke();
    }
}