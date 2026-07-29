using CSLModsCommon.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CSLModsCommon.Localization; 
public class LocaleEntry : CSLModsCommon.Collections.IReadOnlyDictionary<string, string> {
    public const string EnLocaleID = "en-US";

    private readonly Dictionary<string, string> _source;
    private LanguageTranslationStatus _commonTranslationStatus;
    private LanguageTranslationStatus _modTranslationStatus;
    private bool _isDefault;

    public string LocaleID { get; }
    public int Count => _source.Count;

    public bool IsDefault {
        get => _isDefault;
        set {
            _isDefault = value;
            RecalculateTranslationProgress();
        }
    }

    public int TranslationPercent { get; private set; }
    public string TranslationProgress => $"{TranslationPercent}%";

    public LanguageTranslationStatus CommonTranslationStatus {
        get => _commonTranslationStatus;
        set {
            if (value == _commonTranslationStatus) return;
            _commonTranslationStatus = value;
            RecalculateTranslationProgress();
        }
    }

    public LanguageTranslationStatus ModTranslationStatus {
        get => _modTranslationStatus;
        set {
            if (value == _modTranslationStatus) return;
            _modTranslationStatus = value;
            RecalculateTranslationProgress();
        }
    }

    public LocaleEntry(string localeID) {
        if (string.IsNullOrEmpty(localeID))
            throw new ArgumentException("Locale ID cannot be null or empty.");
        LocaleID = localeID;
        _source = new Dictionary<string, string>();
    }

    public LocaleEntry Add(Dictionary<string, string> source) {
        foreach (var kv in source) _source[kv.Key] = kv.Value;

        return this;
    }

    public void Add(string key, string value) {
        if (string.IsNullOrEmpty(key))
            return;
        _source[key] = value;
    }

    public string this[string key] => _source.TryGetValue(key, out var val) ? val : key;

    public IEnumerable<string> Keys => _source.Keys;
    public IEnumerable<string> Values => _source.Values;

    public bool ContainsKey(string key) => _source.ContainsKey(key);

    public bool TryGetValue(string key, out string value) => _source.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _source.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void RecalculateTranslationProgress() {
        if (IsDefault) {
            TranslationPercent = 100;
            return;
        }

        var translated = 0;
        var total = 0;

        if (ModTranslationStatus != null) {
            translated += ModTranslationStatus.TranslatedStrings;
            total += ModTranslationStatus.TotalStrings;
        }

        if (CommonTranslationStatus != null) {
            translated += CommonTranslationStatus.TranslatedStrings;
            total += CommonTranslationStatus.TotalStrings;
        }

        TranslationPercent = total == 0 ? 0 : (int)((decimal)translated / total * 100);
    }
}