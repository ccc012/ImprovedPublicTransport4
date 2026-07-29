using CSLModsCommon.Manager;
using System;
using System.Collections.Generic;

namespace CSLModsCommon.UI;

public class LocalizedString {
    private readonly Dictionary<string, string> _cachedPairs = new();
    public object[] Args { get; private set; }

    public IEnumerable<KeyValuePair<string, string>> CachedPairs => _cachedPairs;
    public string Id { get; private set; }
    public string Value => GetValue();

    public LocalizedString(string id) {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("Key cannot be null or empty.", nameof(id));
        Id = id;
        CacheAllLanguages();
    }

    public LocalizedString(string id, params object[] args) {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("Key cannot be null or empty.", nameof(id));
        Id = id;
        Args = args;
        CacheAllLanguages();
    }

    public static LocalizedString Create(string id) => new LocalizedString(id);

    public static LocalizedString Create(string id, params object[] args) => new LocalizedString(id, args);

    public static implicit operator LocalizedString(string key) => Create(key);

    public static implicit operator string(LocalizedString localizedString) => localizedString.GetValue();

    private void CacheAllLanguages() {
        foreach (var source in LocalizationManager.LocaleSources)
            if (source.Value.TryGetValue(Id, out var value))
                _cachedPairs.Add(source.Key, value);
    }

    public string GetValue() {
        if (!_cachedPairs.TryGetValue(LocalizationManager.ModActiveLocaleId, out var template)) return Id;

        return Args is { Length: > 0 } ? string.Format(template, Args) : template;
    }
}