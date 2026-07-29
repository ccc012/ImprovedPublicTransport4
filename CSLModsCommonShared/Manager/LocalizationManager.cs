using ColossalFramework;
using ColossalFramework.Globalization;
using CSLModsCommon.Collections;
using CSLModsCommon.Extension;
using CSLModsCommon.Localization;
using CSLModsCommon.Logging;
using CSLModsCommon.Setting;
using CSLModsCommon.UI.DropDown;
using CSLModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CSLModsCommon.Manager; 
public class LocalizationManager : ManagerBase {
    public const string UseGameLanguage = "UGL";
    public const string ModDescriptionLocalizedId = "ModDescription";
    public const string TranslationStatus = nameof(TranslationStatus);

    public static event Action<string, LocalizationManager> ModActiveLocaleChanged;

    public static Dictionary<string, LocaleEntry> LocaleSources { get; private set; } = new();
    public static LocaleEntry CurrentLocaleSource { get; private set; }
    public static LocaleEntry EnLocaleSource => LocaleSources.TryGetValue(LocaleEntry.EnLocaleID, out var v) ? v : null;
    public static string ModActiveLocaleId => CurrentLocaleSource?.LocaleID;
    public static bool IsInitialized { get; private set; }

    private string _modDirectory;
    private ModSettingBase _modSetting;
    private bool _sourcesLoaded;
    private bool _processing;

    public string GameActiveLocaleId => LocaleManager.exists ? GetLocaleId(LocaleManager.instance.language) : GetLocaleId(new SavedString(Settings.localeID, Settings.gameSettingsFile, DefaultSettings.localeID).value);
    public DropDownItem<string>[] LanguageOptions { get; private set; }

    protected override void OnCreate() {
        base.OnCreate();
        _modDirectory = AssemblyHelper.CurrentAssemblyDirectory;
        _modSetting = Domain.GetOrCreateManager<SettingManager>().GetDefaultSetting();

        LoadAllSources();
        ChangeLocale();
        RefreshLanguageOptions();
        LocaleManager.eventLocaleChanged += OnLocaleChanged;
        IsInitialized = true;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        LocaleManager.eventLocaleChanged -= OnLocaleChanged;
        IsInitialized = false;
    }

    public static string Localize(string localeId, string key) {
        if (string.IsNullOrEmpty(localeId) || string.IsNullOrEmpty(key))
            throw new ArgumentNullException();
        if (LocaleSources.TryGetValue(localeId, out var source)) {
            if (source.TryGetValue(key, out var value)) return value;

            if (LocaleSources[LocaleEntry.EnLocaleID].TryGetValue(key, out var value2)) {
                LogManager.GetLogger().Info($"Cannot find {key} in {ModActiveLocaleId} source, fallback en-US value");
                return value2;
            }
        }

        LogManager.GetLogger().Info($"Cannot find {key} in  LocaleSources, fallback key");
        return key;
    }

    public static string GetModDescription() {
        var rowDescription = Domain.DefaultDomain.GetManager<ModManagerBase>().RowDescription;
        var rowDescriptionIsNullOrEmpty = string.IsNullOrEmpty(rowDescription);
        if (!IsInitialized) return rowDescriptionIsNullOrEmpty ? string.Empty : rowDescription;

        var description = Localize(ModDescriptionLocalizedId);
        return description == ModDescriptionLocalizedId ? rowDescription : description;
    }

    public static string LocalizeFormat(string format, params object[] args) => string.Format(Localize(format), args);

    public static string LocalizeFormat(string format, object arg0, object arg1, object arg2) => string.Format(Localize(format), arg0, arg1, arg2);

    public static string LocalizeFormat(string format, object arg0, object arg1) => string.Format(Localize(format), arg0, arg1);

    public static string LocalizeFormat(string format, object arg0) => string.Format(Localize(format), arg0);

    public static string Localize(string key) {
        if (key is null)
            throw new ArgumentNullException(nameof(key));
        if (CurrentLocaleSource is null || LocaleSources is null) return key;

        if (CurrentLocaleSource.TryGetValue(key, out var value1)) return value1;

        if (LocaleSources[LocaleEntry.EnLocaleID].TryGetValue(key, out var value2)) {
            LogManager.GetLogger().Debug($"Cannot find [{key}] in {ModActiveLocaleId} source, fallback en-US value");
            return value2;
        }

        LogManager.GetLogger().Debug($"Cannot find [{key}] in LocaleSources, fallback key");
        return key;
    }

    public void OnResetSettings() {
        ChangeLocale();
        RefreshLanguageOptions();
    }

    public void OnLanguageOptionsChanged(DropDownItem<string> downItem, Action<string> action = null) {
        if (_processing) return;
        _processing = true;

        string actionString;
        if (downItem.Value == UseGameLanguage) {
            if (TryGetLocaleSource(GameActiveLocaleId, out var localeSource))
                CurrentLocaleSource = localeSource;
            else
                SetDefaultLocale();
            actionString = UseGameLanguage;
            Logger.Info(
                $"Change locale on languages option changed, use game language, mod active locale: {ModActiveLocaleId}, game active locale: {GameActiveLocaleId}");
        }
        else {
            if (TryGetLocaleSource(downItem.Value, out var source))
                CurrentLocaleSource = source;
            else
                SetDefaultLocale();
            actionString = ModActiveLocaleId;
            Logger.Info(
                $"Change locale on languages option changed, customize locale, mod active locale: {ModActiveLocaleId}, game active locale: {GameActiveLocaleId}");
        }

        action?.Invoke(actionString);
        NotifyModActiveLocaleIdChanged();
        _processing = false;
    }

    public DropDownItem<string>[] GetLanguageOptions() {
        using var list = ReusableList<LocaleOption>.Rent();
        list.Add(new LocaleOption(UseGameLanguage, SharedTranslations.UseGameLanguage));

        foreach (var localeId in LocaleSources.Keys) {
            var prefix = Localize($"Language_{localeId}");
            var suffix = $"({Localize(localeId, $"Language_{localeId}")})";
            var total = prefix + suffix;
            var display = ModActiveLocaleId == localeId ? prefix : total;
            list.Add(new LocaleOption(localeId, display));
        }

        var languageItems = new DropDownItem<string>[list.Count];
        for (var i = 0; i < list.Count; i++) {
            var language = list[i];
            languageItems[i] = new DropDownItem<string>(language.LocaleId, language.DisplayText);
        }

        return languageItems;
    }

    public void RefreshLanguageOptions() => LanguageOptions = GetLanguageOptions();

    public void ChangeLocale(string localeId, Action action = null) {
        _processing = true;
        if (localeId == UseGameLanguage) {
            if (TryGetLocaleSource(GameActiveLocaleId, out var localeSource))
                CurrentLocaleSource = localeSource;
            else
                SetDefaultLocale();
            Logger.Info(
                $"Change locale, use game language, mod active locale: {ModActiveLocaleId}, game active locale: {GameActiveLocaleId}");
        }
        else {
            if (TryGetLocaleSource(localeId, out var source))
                CurrentLocaleSource = source;
            else
                SetDefaultLocale();
            Logger.Info(
                $"Change locale, customize locale, mod active locale: {ModActiveLocaleId}, game active locale: {GameActiveLocaleId}");
        }

        action?.Invoke();
        NotifyModActiveLocaleIdChanged();
        _processing = false;
    }

    public void ChangeLocale(Action action = null) {
        if (!_sourcesLoaded || _processing) return;
        _processing = true;
        var settingLocaleId = _modSetting.LocaleId;
        var tag = IsInitialized ? "Change" : "Init";
        if (settingLocaleId == UseGameLanguage) {
            if (TryGetLocaleSource(GameActiveLocaleId, out var localeEntry))
                CurrentLocaleSource = localeEntry;
            else
                SetDefaultLocale();
            Logger.Info(
                $"{tag} locale, use game language, mod active locale: {ModActiveLocaleId}, game active locale: {GameActiveLocaleId}");
        }
        else {
            if (TryGetLocaleSource(settingLocaleId, out var source))
                CurrentLocaleSource = source;
            else
                SetDefaultLocale();
            Logger.Info(
                $"{tag} locale, customize locale, mod active locale: {ModActiveLocaleId}, game active locale: {GameActiveLocaleId}");
        }

        action?.Invoke();
        _processing = false;
    }

    public bool IsLocaleSupported(string localeId) => LocaleManager.exists && LocaleManager.instance.supportedLocaleIDs.Any(v => GetLocaleId(v) == localeId);

    public string GetTranslationProgress() => CurrentLocaleSource.TranslationProgress;

    private void OnLocaleChanged() {
        if (_processing)
            return;
        ChangeLocale();
        _processing = true;
        NotifyModActiveLocaleIdChanged();
        _processing = false;
    }

    private void NotifyModActiveLocaleIdChanged() {
        if (!IsInitialized) return;
        RefreshLanguageOptions();
        ModActiveLocaleChanged?.Invoke(ModActiveLocaleId, this);
    }

    private void SetDefaultLocale() => CurrentLocaleSource = LocaleSources[LocaleEntry.EnLocaleID];

    private bool TryGetLocaleSource(string localeId, out LocaleEntry localeEntry) => LocaleSources.TryGetValue(localeId, out localeEntry);

    private string GetLocaleId(string localeId) => localeId switch {
        "de" => "de-DE",
        "en" => "en-US",
        "es" => "es-ES",
        "fr" => "fr-FR",
        "ko" => "ko-KR",
        "pl" => "pl-PL",
        "pt" => "pt-BR",
        "ru" => "ru-RU",
        "zh" => "zh-CN",
        _ => localeId
    };

    private void LoadAllSources() {
        using var pc = PerformanceCounter.Start(c => Logger.Verbose($"LocalizationManager.LoadAllSources cost: {c.ReportMilliseconds}"));

        var modFolder = Path.Combine(_modDirectory, "Localization");
        var commonFolder = PathExtensions.Combine(_modDirectory, "Localization", "Common");

        if (!Directory.Exists(commonFolder)) {
            Logger.Error($"{commonFolder} not found. Skipping locale loading.");
            return;
        }

        var modStatusList = new List<LanguageTranslationStatus>();
        var modTranslationStatusFile = Path.Combine(modFolder, TranslationStatus + ".json");
        if (File.Exists(modTranslationStatusFile))
            modStatusList = JsonHelper.DeserializeFromJsonFile<List<LanguageTranslationStatus>>(modTranslationStatusFile);

        var commonStatusList = new List<LanguageTranslationStatus>();
        var commonTranslationStatusFile = Path.Combine(commonFolder, TranslationStatus + ".json");
        if (File.Exists(commonTranslationStatusFile))
            commonStatusList = JsonHelper.DeserializeFromJsonFile<List<LanguageTranslationStatus>>(commonTranslationStatusFile);

        var modLocales = new List<string>();
        if (Directory.Exists(modFolder))
            foreach (var file in new DirectoryInfo(modFolder).GetFiles("*.json")) {
                var localeID = Path.GetFileNameWithoutExtension(file.Name);
                if (localeID != TranslationStatus)
                    modLocales.Add(localeID);
            }

        if (modLocales.Count > 0) {
            var sbMod = new StringBuilder("Added locale source: ");
            foreach (var localeID in modLocales) {
                var filePath = Path.Combine(modFolder, localeID + ".json");
                var source = JsonHelper.DeserializeFromJsonFile<Dictionary<string, string>>(filePath);
                var status = modStatusList.FirstOrDefault(v => v.Locale == localeID);

                if (!LocaleSources.TryGetValue(localeID, out var entry)) {
                    entry = new LocaleEntry(localeID) {
                        IsDefault = localeID == LocaleEntry.EnLocaleID
                    };
                    LocaleSources[localeID] = entry;
                }

                entry.Add(source);
                if (status != null) entry.ModTranslationStatus = status;

                sbMod.Append(localeID).Append(' ');
            }

            Logger.Info(sbMod.ToString());

            foreach (var localeID in modLocales) {
                var commonFile = Path.Combine(commonFolder, localeID + ".json");
                if (!File.Exists(commonFile)) continue;

                var source = JsonHelper.DeserializeFromJsonFile<Dictionary<string, string>>(commonFile);
                var status = commonStatusList.FirstOrDefault(v => v.Locale == localeID);

                var entry = LocaleSources[localeID];
                entry.Add(source);
                if (status != null) entry.CommonTranslationStatus = status;
            }
        }
        else {
            var sbCommon = new StringBuilder("Added common locale source: ");
            foreach (var file in new DirectoryInfo(commonFolder).GetFiles("*.json")) {
                var localeID = Path.GetFileNameWithoutExtension(file.Name);
                if (localeID == TranslationStatus) continue;

                var source = JsonHelper.DeserializeFromJsonFile<Dictionary<string, string>>(file.FullName);
                var status = commonStatusList.FirstOrDefault(v => v.Locale == localeID);

                if (!LocaleSources.TryGetValue(localeID, out var entry)) {
                    entry = new LocaleEntry(localeID) {
                        IsDefault = localeID == LocaleEntry.EnLocaleID
                    };
                    LocaleSources[localeID] = entry;
                }

                entry.Add(source);
                if (status != null) entry.CommonTranslationStatus = status;

                sbCommon.Append(localeID).Append(' ');
            }

            Logger.Info(sbCommon.ToString());
        }

        if (!LocaleSources.TryGetValue(LocaleEntry.EnLocaleID, out var enEntry)) return;

        foreach (var kv in LocaleSources) {
            var entry = kv.Value;
            if (entry.LocaleID == LocaleEntry.EnLocaleID) continue;

            foreach (var key in enEntry.Keys) {
                if (entry.ContainsKey(key)) continue;
                Logger.Warn($"Missing key '{key}' in locale '{entry.LocaleID}', using fallback value.");
                entry.Add(key, enEntry[key]);
            }
        }

        Logger.Info("All locale sources loaded.");
        _sourcesLoaded = true;
    }

    public static EmbeddedLocalizationLoader LoadEmbeddedCommonLocaleSource() {
        using var pc = PerformanceCounter.Start(v => Logger.Info($"LocalizationManager.LoadEmbeddedCommonLocaleSource cost: {v.ReportMilliseconds}"));
        var loader = new EmbeddedLocalizationLoader(Assembly.GetExecutingAssembly(), "SkylinesShared.Localization.Common");
        loader.Load();

        var locales = string.Join(" ", loader.LocaleSource.Keys.ToArray());
        Logger.Info($"Load common embedded locale source: {locales}");
        return loader;
    }
}