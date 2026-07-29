using CSLModsCommon.Common;
using CSLModsCommon.Compatibility;
using CSLModsCommon.Extension;
using CSLModsCommon.Localization;
using CSLModsCommon.Logging;
using CSLModsCommon.Manager;
using CSLModsCommon.Setting;
using CSLModsCommon.UI.Atlas;
using CSLModsCommon.UI.Buttons;
using CSLModsCommon.UI.Containers;
using CSLModsCommon.UI.Dialogs;
using CSLModsCommon.UI.DropDown;
using CSLModsCommon.UI.Tab;
using CSLModsCommon.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace CSLModsCommon.UI.OptionsPanel;

public abstract partial class OptionsPanelBase : LiteContainer {
    protected const string General = nameof(General);
    protected const string KeyBinding = nameof(KeyBinding);
    protected const string Debug = nameof(Debug);
    protected const string Advanced = nameof(Advanced);

    private static readonly DropDownItem<LogLevel>[] LogLevelDropDownItem;

    protected ILog _logger;
    protected Domain _domain;
    protected ModManagerBase _modManagerBase;
    protected SettingManager _settingManager;
    protected ModSettingBase ModSettingBase;
    protected LocalizationManager _localizationManager;
    protected DialogManager _dialogManager;
    protected TabBar _tabBar;
    protected TabGroupLogic _tabGroupLogic;
    protected ScrollContainer _generalPage;
    protected ScrollContainer _keyBindingPage;
    protected ScrollContainer _debugPage;
    protected ScrollContainer _advancedPage;
    private ResetModSettingsWarningDialog _warningMessageBox;
    private CompatibilityManager _compatabilityManager;

    static OptionsPanelBase() => LogLevelDropDownItem = new[] {
        new DropDownItem<LogLevel>(LogLevel.Verbose, nameof(LogLevel.Verbose), true),
        new DropDownItem<LogLevel>(LogLevel.Debug, nameof(LogLevel.Debug), true),
        new DropDownItem<LogLevel>(LogLevel.Info, nameof(LogLevel.Info), true),
        new DropDownItem<LogLevel>(LogLevel.Warn, nameof(LogLevel.Warn), true),
        new DropDownItem<LogLevel>(LogLevel.Error, nameof(LogLevel.Error), true),
        new DropDownItem<LogLevel>(LogLevel.Fatal, nameof(LogLevel.Fatal), true),
        new DropDownItem<LogLevel>(LogLevel.Disabled, nameof(LogLevel.Disabled), true)
    };

    protected virtual void CacheManagers() { }
    protected virtual void FillGeneralPage(ScrollContainer page) { }
    protected virtual void FillKeyBindingPage(ScrollContainer page) { }
    protected virtual void FillDebugPage(ScrollContainer page) { }
    protected virtual void FillAdvancedPage(ScrollContainer page) { }
    protected virtual void OnResetSettings() { }

    protected virtual void OnModLocaleChanged() => OptionsPanelManager.OnLocaleChanged();

    protected virtual void AddExtraPage() {
        _keyBindingPage = AddPage(KeyBinding, SharedTranslations.KeyBinding);
        FillKeyBindingPage(_keyBindingPage);
    }

    public sealed override void Awake() {
        base.Awake();
        size = OptionsPanelLayout.Size;
        _bgAtlas = Atlases.Shared;
        _bgSprites.SetValues(SharedAtlasKeys.CustomBackground);
        _bgColors.SetValues(new Color32(130, 130, 130, 255));

        _logger = LogManager.GetLogger();
        _domain = Domain.DefaultDomain;
        _modManagerBase = _domain.GetManager<ModManagerBase>();
        _settingManager = _domain.GetOrCreateManager<SettingManager>();
        ModSettingBase = _settingManager.GetDefaultSetting();
        _localizationManager = _domain.GetOrCreateManager<LocalizationManager>();
        _dialogManager = _domain.GetOrCreateManager<DialogManager>();
        _compatabilityManager = _domain.GetOrCreateManager<CompatibilityManager>();
        CacheManagers();

        _tabBar = AddUIComponent<TabBar>();
        _tabBar.size = OptionsPanelLayout.TabSize;
        _tabBar.ColumnGap = 3;
        _tabBar.BgAtlas = Atlases.Shared;
        _tabBar.BgSprites.SetValues(SharedAtlasKeys.RoundRect6);
        _tabBar.BgColors.SetValues(UIColors.GroupBg1);
        _tabBar.relativePosition = new Vector3(OptionsPanelLayout.Padding, OptionsPanelLayout.Padding);
        _tabGroupLogic = TabGroupLogic.Create().BindingTabBar(_tabBar);

        AddGeneralPage();
        AddExtraPage();
#if ALPHA
        AddDebugPage();
#endif
        AddAdvancedPage();

        _tabGroupLogic.SelectPage(General);
    }

    protected SettingsSection AddSection(ScrollContainer container, string header = null, string description = null) {
        var panel = container.AddUIComponent<SettingsSection>();
        if (string.IsNullOrEmpty(header)) return panel;
        panel.Header = header;
        if (!string.IsNullOrEmpty(description))
            panel.Description = description;
        return panel;
    }

    protected ScrollContainer AddPage(string id, string text) {
        var page = AddUIComponent<ScrollContainer>();
        page.size = OptionsPanelLayout.ContainerSize;
        page.AutoLayout = true;
        page.AutoLayoutPadding.SetTop(4);
        page.RowGap = 20;
        page.AutoReset = false;
        var scrollbar0 = Scrollbar.AddScrollbar(this, page, new Vector2(8, OptionsPanelLayout.ContainerSize.y));
        scrollbar0.ThumbObject.color = UIColors.GroupBg1;
        scrollbar0.relativePosition = new Vector2(OptionsPanelLayout.Size.x - 8, OptionsPanelLayout.Padding + 40);
        page.relativePosition = new Vector2(OptionsPanelLayout.Padding, OptionsPanelLayout.Padding + 30 + 10);

        _tabGroupLogic.AddTab(id, text, page, b => {
            b.TextPadding.SetTop(2);
            b.SetStyle(StyleType.OptionPanelStyle);
        });

        return page;
    }

    private void AddAdvancedPage() {
        _advancedPage = AddPage(Advanced, SharedTranslations.Advanced);

        var section = AddSection(_advancedPage);

        section.AddButton(SharedTranslations.ChangelogHeader, null, SharedTranslations.Changelog, null, onButtonClicked: _ => _dialogManager.Show<ChangelogDialog>().Init());
        section.AddButton(SharedTranslations.CompatibilityCheckHeader, SharedTranslations.CompatibilityCheckDescription, SharedTranslations.Check, null, onButtonClicked: _ => _domain.GetOrCreateManager<CompatibilityManager>().CheckAndShowDialog());
        section.AddButton(SharedTranslations.ResetModMajor, SharedTranslations.ResetModMinor, SharedTranslations.Reset, null, onButtonClicked: ResetSettings);

        section.AddButton(SharedTranslations.CopyLogsToDesktop, null, SharedTranslations.Copy, null, onButtonClicked: CopyLogsToDesktop);

        LogLevelDropDownItem.ForEach(v => v.RefreshLocalization());
        section.AddDropDown(SharedTranslations.LogLevel, SharedTranslations.LogLevelDescription, LogLevelDropDownItem, i => i.Value == _logger.Level, item => _logger.SetLevel(item.Value), 100);

        var section1 = AddSection(_advancedPage);
        section1.AddButton(SharedTranslations.ImproveModSharedTranslation, null, SharedTranslations.Website, null, onButtonClicked: _ => URLHelper.OpenURL(_modManagerBase.SharedTranslationURL));
        if (!string.IsNullOrEmpty(_modManagerBase.ModTranslationURL)) section1.AddButton(SharedTranslations.ImproveModTranslation, null, SharedTranslations.Website, null, onButtonClicked: _ => URLHelper.OpenURL(_modManagerBase.ModTranslationURL));

        if (!string.IsNullOrEmpty(_modManagerBase.ModSteamURL)) section1.AddButton(SharedTranslations.SteamModPage, null, SharedTranslations.Website, null, onButtonClicked: _ => URLHelper.OpenSteamURL(_modManagerBase.ModSteamURL));

        if (!string.IsNullOrEmpty(_modManagerBase.DiscordURL)) section1.AddButton(SharedTranslations.DiscordGetHelp, null, SharedTranslations.Website, null, onButtonClicked: _ => URLHelper.OpenURL(_modManagerBase.DiscordURL));

        FillAdvancedPage(_advancedPage);
    }

    private void AddDebugPage() {
        _debugPage = AddPage(Debug, Debug);

        var section = AddSection(_debugPage, Debug);

        section.AddButton("Open settings directory", null, "Open settings directory", null, 30, OpenSettingsDirectory);
        section.AddButton("Open mod setting file", null, "Open mod setting file", null, 30, OpenModSettingFile);
        section.AddButton("Open logs directory", null, "Open logs directory", null, 30, OpenLogsDirectory);
        section.AddButton("Open log", null, "Open log", null, 30, OpenLogFile);
        section.AddButton("Open game log", null, "Open game log", null, 30, OpenGameLogFile);
        FillDebugPage(_debugPage);
    }

    private void AddGeneralPage() {
        _generalPage = AddPage(General, SharedTranslations.General);
        var modInfoSection = AddSection(_generalPage, SharedTranslations.ModInfo);
        var flag = _modManagerBase.CurrentBuildChannel switch {
            BuildChannel.Beta => SharedTranslations.BETA,
            BuildChannel.Stable => SharedTranslations.STABLE,
            _ => "ALPHA"
        };
        modInfoSection.AddLabel(SharedTranslations.Version, $"{AssemblyHelper.CurrentAssemblyVersion.ToDisplayString()} {flag}", null, c => {
            var label = c.Control;
            label.BgColors.SetValues(_modManagerBase.CurrentBuildChannel switch {
                BuildChannel.Stable => UIColors.GreenNormal,
                BuildChannel.Beta => UIColors.YellowNormal,
                _ => new Color32(6, 132, 138, 255)
            });
            label.TextPadding.SetAll(4, 4, 4, 2);
            label.TextAtlas = label.BgAtlas = Atlases.Shared;
            label.BgSprites.SetValues(SharedAtlasKeys.RoundRect6);
        });

        var status = _compatabilityManager.CurrentStatus.IsOnlyFlag(CompatibilityStatus.Normal) && !_compatabilityManager.ShouldRestartGame;
        modInfoSection.AddLabel(SharedTranslations.ModCompatibility, status ? SharedTranslations.Normal : SharedTranslations.Warning, null, c => {
            var label = c.Control;
            label.TextAtlas = label.BgAtlas = Atlases.Shared;
            label.BgSprites.SetValues(SharedAtlasKeys.RoundRect6);
            if (status)
                label.BgColors.SetValues(UIColors.GreenColors);
            else
                label.BgColors.SetValues(UIColors.YellowColors);

            label.TextPadding.SetAll(4, 4, 4, 2);
            label.width += 8;
            label.eventClicked += (_, _) => _compatabilityManager.CheckAndShowDialog();
        });

        var isDebugBuild = _modManagerBase.CurrentBuildChannel == BuildChannel.Alpha;
        var debugBuildDate = string.Empty;
        if (isDebugBuild) {
            var directory = AssemblyHelper.CurrentAssemblyDirectory;
            debugBuildDate = Directory.Exists(directory) ? new DirectoryInfo(directory).CreationTime.ToString("yyyy-MM-dd HH:mm:ss") : DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        }

        modInfoSection.AddLabel($"{SharedTranslations.ModReleaseDate}", isDebugBuild ? debugBuildDate : $"{_modManagerBase.VersionDate:yyyy-MM-dd}");

        modInfoSection.AddDropDown(SharedTranslations.Language, $"{SharedTranslations.TranslationProgress}: {_localizationManager.GetTranslationProgress()}", _localizationManager.LanguageOptions, i => ModSettingBase.LocaleId == LocalizationManager.UseGameLanguage ? i.Value == LocalizationManager.UseGameLanguage : i.Value == ModSettingBase.LocaleId, OnLanguageSelectedChanged, 310);

        FillGeneralPage(_generalPage);
    }

    private void OnLanguageSelectedChanged(DropDownItem<string> arg2) {
        _localizationManager.OnLanguageOptionsChanged(arg2, a => ModSettingBase.LocaleId = a);
        OnLocaleChanged();
        OnModLocaleChanged();
    }

    private void OpenGameLogFile(NormalButton _) {
        var path = Path.Combine(Application.dataPath, "output_log.txt");
        if (!File.Exists(path)) {
            _logger.Error("Couldn't find game log file");
            return;
        }

        Process.Start(new ProcessStartInfo {
            FileName = path,
            UseShellExecute = true
        });
    }

    private void OpenLogFile(NormalButton _) {
        if (!File.Exists(LogManager.DefaultLogPath)) {
            _logger.Error("Couldn't find log file");
            return;
        }

        Process.Start(new ProcessStartInfo {
            FileName = LogManager.DefaultLogPath,
            UseShellExecute = true
        });
    }

    private void OpenLogsDirectory(NormalButton _) {
        if (!Directory.Exists(Application.dataPath)) {
            _logger.Error("Couldn't find logs directory");
            return;
        }

        var filePath = LogManager.DefaultLogPath.Replace('/', '\\');
        _logger.Debug($"/select,\"{filePath}\"");
        Process.Start("explorer.exe", $"/select,\"{filePath}\"");
    }

    private void OpenModSettingFile(NormalButton _) {
        var attr = ModSettingBase.GetType().GetCustomAttribute<FileLocationAttribute>();
        if (attr == null) return;

        var path = attr.Path;

        if (!File.Exists(path)) {
            _logger.Error("Couldn't find mod setting file");
            return;
        }

        Process.Start(new ProcessStartInfo {
            FileName = path,
            UseShellExecute = true
        });
    }

    private void OpenSettingsDirectory(NormalButton _) {
        if (!Directory.Exists(FileLocationAttribute.DefaultDirectory)) {
            _logger.Error("Couldn't find settings directory");
            return;
        }

        Process.Start(new ProcessStartInfo {
            FileName = FileLocationAttribute.DefaultDirectory,
            UseShellExecute = true
        });
    }

    private void ResetSettings(NormalButton _) {
        try {
            _warningMessageBox = _dialogManager.Show<ResetModSettingsWarningDialog>();
            _warningMessageBox.Init(First);
        }
        catch (Exception e) {
            _logger.Error(e, "Reset settings failed:");
            _dialogManager.Show<ResetModSettingsDialog>().Init(false);
        }

        return;

        void First() {
            ModSettingBase.SetDefaults();
            _localizationManager.OnResetSettings();
            OnResetSettings();
            OnModLocaleChanged();
            _dialogManager.Hide(_warningMessageBox);
            _warningMessageBox = null;
            _dialogManager.Show<ResetModSettingsDialog>().Init();
            _logger.Info("Reset mod settings succeeded");
        }
    }

    private void CopyLogsToDesktop(NormalButton _) {
        try {
            var folderName = AssemblyHelper.CurrentAssemblyName + "Logs";
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var destFolder = Path.Combine(desktopPath, folderName);

            if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

            var gameLogFilePath = Path.Combine(Application.dataPath, "output_log.txt");
            if (!File.Exists(gameLogFilePath)) {
                _logger.Error("Couldn't find game log file");
                return;
            }

            var modLogFilePath = LogManager.DefaultLogPath;
            if (!string.IsNullOrEmpty(modLogFilePath) && File.Exists(modLogFilePath)) {
                var destModLogFilePath = Path.Combine(destFolder, Path.GetFileName(modLogFilePath));
                File.Copy(modLogFilePath, destModLogFilePath, true);
            }
            else {
                _logger.Error("Couldn't find mod log file");
            }

            var destGameLogFilePath = Path.Combine(destFolder, "output_log.txt");
            File.Copy(gameLogFilePath, destGameLogFilePath, true);
            _logger.Info($"Copied logs to desktop: {destFolder}");
            _dialogManager.Show<OkDialog>().AddContent(AssemblyHelper.CurrentAssemblyName, SharedTranslations.CopiedLogsSucceeded);
        }
        catch (Exception ex) {
            _logger.Error(ex, "Couldn't copy logs to desktop");
        }
    }
}