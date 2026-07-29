using CSLModsCommon.Collections;
using CSLModsCommon.Extension;
using CSLModsCommon.Logging;
using CSLModsCommon.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CSLModsCommon.Localization;

public class EmbeddedLocalizationLoader {
    public const string JsonExtension = ".json";
    private readonly Assembly _assembly;
    private readonly string _prefix;
    private readonly string[] _resourceNames;

    private readonly Dictionary<string, Dictionary<string, string>> _localeSources;
    private List<LanguageTranslationStatus> _translationStatuses;

    public CSLModsCommon.Collections.IReadOnlyDictionary<string, Dictionary<string, string>> LocaleSource { get; }
    public CSLModsCommon.Collections.IReadOnlyList<LanguageTranslationStatus> TranslationStatuses { get; }

    public EmbeddedLocalizationLoader(Assembly assembly, string prefix) {
        _localeSources = new Dictionary<string, Dictionary<string, string>>();
        LocaleSource = _localeSources.AsReadOnly();
        _translationStatuses = new List<LanguageTranslationStatus>();
        TranslationStatuses = ReadOnlyExtensions.AsReadOnly(_translationStatuses);
        _assembly = assembly;
        _prefix = prefix;
        _resourceNames = _assembly.GetManifestResourceNames();
    }

    public void Load() {
        foreach (var res in _resourceNames) {
            if (!res.StartsWith(_prefix)) continue;
            if (!res.EndsWith(JsonExtension)) continue;

            try {
                var stripped = res.Substring(_prefix.Length + 1);
                var parts = stripped.Split('.');
                if (parts.Length < 2) continue;

                var locale = parts[0];

                using var stream = _assembly.GetManifestResourceStream(res);
                using var reader = new StreamReader(stream);
                var jsonText = reader.ReadToEnd();

                if (locale == "TranslationStatus") {
                    _translationStatuses = JsonConvert.DeserializeObject<List<LanguageTranslationStatus>>(jsonText, JsonHelper.Settings);
                    continue;
                }

                if (_localeSources.ContainsKey(locale)) continue;

                var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonText, JsonHelper.Settings);
                _localeSources[locale] = dic;
            }
            catch (Exception e) {
                LogManager.GetLogger().Error(e, $"Failed to load embedded localization resource: {res}");
            }
        }
    }
}