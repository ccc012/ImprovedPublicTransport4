using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Plugins;
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
        _modSetting = Domain.GetOrCreateManager<SettingManager>().GetDefaultSetting();

        // PluginManager may not list this assembly yet when Options UI spins up — resolve
        // the mod folder robustly and reload if the first pass found nothing.
        EnsureSourcesLoaded();
        ChangeLocale();
        RefreshLanguageOptions();
        LocaleManager.eventLocaleChanged += OnLocaleChanged;
        IsInitialized = true;
    }

    /// <summary>
    /// Resolve the on-disk mod directory. Prefer PluginManager.modPath (the real Addons/Mods
    /// folder); fall back to Assembly.Location. Cached AssemblyHelper path alone is wrong when
    /// LocalizationManager is created before the plugin is registered (empty LocaleSources →
    /// raw keys like "Version" / "ModCompatibility" in the Options header).
    /// </summary>
    private string ResolveModDirectory()
    {
        try
        {
            if (PluginManager.exists)
            {
                var asmName = Assembly.GetExecutingAssembly().GetName().Name;
                foreach (var plugin in PluginManager.instance.GetPluginsInfo())
                {
                    if (plugin == null) continue;
                    try
                    {
                        if (plugin.GetAssemblies().Any(a => a != null && a.GetName().Name == asmName)
                            && !string.IsNullOrEmpty(plugin.modPath)
                            && Directory.Exists(plugin.modPath))
                        {
                            return plugin.modPath;
                        }
                    }
                    catch
                    {
                        // broken plugin entry
                    }
                }
            }
        }
        catch
        {
            // PluginManager not ready
        }

        try
        {
            var loc = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(loc))
            {
                var dir = Path.GetDirectoryName(loc);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    return dir;
            }
        }
        catch
        {
            // ignored
        }

        return AssemblyHelper.CurrentAssemblyDirectory;
    }

    /// <summary>Load (or reload) common locale JSON. Safe to call more than once.</summary>
    public void EnsureSourcesLoaded()
    {
        _modDirectory = ResolveModDirectory();

        // If en-US is already loaded but the on-disk Common folder has more packs than we know about
        // (partial deploy fixed while the game still runs, or first load hit a half-copied folder),
        // force a full rescan so the language dropdown is complete.
        var diskLocaleCount = CountCommonLocaleFilesOnDisk(_modDirectory);
        var alreadyComplete = LocaleSources != null && LocaleSources.Count > 0
            && LocaleSources.ContainsKey(LocaleEntry.EnLocaleID)
            && EnLocaleSource != null && EnLocaleSource.Count > 0
            && (diskLocaleCount <= 0 || LocaleSources.Count >= diskLocaleCount);

        if (alreadyComplete)
        {
            return;
        }

        if (LocaleSources != null && LocaleSources.Count > 0 && diskLocaleCount > LocaleSources.Count)
        {
            Logger.Info($"LocalizationManager: reloading locales ({LocaleSources.Count} loaded, {diskLocaleCount} on disk).");
            LocaleSources.Clear();
            CurrentLocaleSource = null;
            _sourcesLoaded = false;
        }

        Logger.Info($"LocalizationManager: loading locales from '{_modDirectory}'");
        LoadAllSources();

        // Last resort: embedded JSON in the assembly (if any were linked as resources).
        if (LocaleSources.Count == 0 || !LocaleSources.ContainsKey(LocaleEntry.EnLocaleID))
        {
            try
            {
                var embedded = LoadEmbeddedCommonLocaleSource();
                embedded.Load();
                foreach (var kv in embedded.LocaleSource)
                {
                    if (!LocaleSources.TryGetValue(kv.Key, out var entry))
                    {
                        entry = new LocaleEntry(kv.Key)
                        {
                            IsDefault = kv.Key == LocaleEntry.EnLocaleID
                        };
                        LocaleSources[kv.Key] = entry;
                    }

                    entry.Add(kv.Value);
                }

                Logger.Info($"LocalizationManager: embedded fallback loaded {LocaleSources.Count} locales.");
            }
            catch (Exception ex)
            {
                Logger.Error($"LocalizationManager: embedded fallback failed: {ex.Message}");
            }
        }
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

            if (LocaleSources.TryGetValue(LocaleEntry.EnLocaleID, out var enSource) && enSource.TryGetValue(key, out var value2)) {
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

        // Lazy reload if first OnCreate ran before PluginManager knew our mod path.
        if (LocaleSources == null || LocaleSources.Count == 0
            || !LocaleSources.ContainsKey(LocaleEntry.EnLocaleID))
        {
            try
            {
                var mgr = Domain.DefaultDomain.GetManager<LocalizationManager>();
                mgr?.EnsureSourcesLoaded();
                if (CurrentLocaleSource == null)
                    mgr?.ChangeLocale();
            }
            catch
            {
                // best-effort
            }
        }

        if (LocaleSources is null || LocaleSources.Count == 0) return key;

        if (CurrentLocaleSource != null && CurrentLocaleSource.TryGetValue(key, out var value1))
            return value1;

        // Always fall back to en-US so framework labels never show raw keys (Version/ModInfo/...).
        if (LocaleSources.TryGetValue(LocaleEntry.EnLocaleID, out var enSource)
            && enSource != null
            && enSource.TryGetValue(key, out var value2))
        {
            return value2;
        }

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
        // Prefer resolved string; never show the raw "UseGameLanguage" key in the UI.
        var useGame = SharedTranslations.UseGameLanguage;
        if (string.IsNullOrEmpty(useGame) || useGame == "UseGameLanguage")
            useGame = "Use game language";
        list.Add(new LocaleOption(UseGameLanguage, useGame));

        // Stable alphabetical order so the list does not "shrink" or jump when locales reload.
        foreach (var localeId in LocaleSources.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase)) {
            if (localeId == LocaleEntry.EnLocaleID) {
                // Still include English.
            }

            var key = $"Language_{localeId}";
            // JSON packs use Language_pt-BR; SharedTranslations historically used Language_pt_BR.
            var currentLocaleName = TryGetLocalizedLanguageName(CurrentLocaleSource ?? EnLocaleSource, key, null)
                                    ?? TryGetLocalizedLanguageName(EnLocaleSource, key, localeId);
            var nativeLocaleName = TryGetLocalizedLanguageName(LocaleSources[localeId], key, null)
                                   ?? currentLocaleName
                                   ?? localeId;
            if (string.IsNullOrEmpty(currentLocaleName) || currentLocaleName == key)
                currentLocaleName = nativeLocaleName ?? localeId;
            var display = string.Equals(ModActiveLocaleId, localeId, StringComparison.OrdinalIgnoreCase)
                ? currentLocaleName
                : (currentLocaleName == nativeLocaleName
                    ? currentLocaleName
                    : $"{currentLocaleName} ({nativeLocaleName})");
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

    public string GetTranslationProgress() => CurrentLocaleSource?.TranslationProgress ?? string.Empty;

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

    private void SetDefaultLocale() {
        // Reached whenever the requested locale can't be found (e.g. game locale not shipped),
        // including from game event handlers (OnLocaleChanged) - a raw indexer here would throw
        // KeyNotFoundException and crash the caller if locale loading has failed entirely
        // (a race this class already guards against elsewhere via the embedded-resource fallback).
        LocaleSources.TryGetValue(LocaleEntry.EnLocaleID, out var enSource);
        CurrentLocaleSource = enSource;
    }

    private bool TryGetLocaleSource(string localeId, out LocaleEntry localeEntry) => LocaleSources.TryGetValue(localeId, out localeEntry);

    private static string TryGetLocalizedLanguageName(LocaleEntry source, string key, string fallback)
    {
        if (source != null && source.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) && value != key)
        {
            return value;
        }

        // Also try underscore form (Language_pt_BR) if JSON used hyphens (Language_pt-BR).
        if (source != null && key != null && key.IndexOf('-') >= 0)
        {
            var alt = key.Replace('-', '_');
            if (source.TryGetValue(alt, out var value2) && !string.IsNullOrEmpty(value2) && value2 != alt)
                return value2;
        }

        return fallback;
    }

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

    private static int CountCommonLocaleFilesOnDisk(string modDirectory)
    {
        try
        {
            if (string.IsNullOrEmpty(modDirectory)) return 0;
            var commonFolder = Path.Combine(Path.Combine(modDirectory, "Localization"), "Common");
            if (!Directory.Exists(commonFolder)) return 0;
            var n = 0;
            foreach (var file in Directory.GetFiles(commonFolder, "*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (!string.Equals(name, TranslationStatus, StringComparison.OrdinalIgnoreCase))
                    n++;
            }
            return n;
        }
        catch
        {
            return 0;
        }
    }

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

        // Optional per-mod JSON packs living directly under Localization/ (not Common/).
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
        }

        // Always load the full Common set so every shipped language appears in the dropdown
        // (even when optional mod-level locale files only cover a subset).
        {
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
