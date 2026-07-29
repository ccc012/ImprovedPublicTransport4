using CSLModsCommon.Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CSLModsCommon;

public class ChangelogCollection : IEnumerable<ChangelogEntry> {
    public static readonly ChangelogFlag[] ChangelogFlags = Enum.GetValues(typeof(ChangelogFlag)).Cast<ChangelogFlag>().ToArray();

    private readonly List<ChangelogEntry> _entries = new();

    public Version Version { get; }
    public DateTime Date { get; }
    public int ChangeCount => _entries.Count;
    public string PrefixKey { get; }
    public bool AutoGenerate { get; }
    public bool AutoGenerationCompleted { get; private set; }

    public ChangelogCollection(Version version, DateTime date, bool autoGenerate = true) {
        Version = version;
        Date = date;
        if (!autoGenerate) return;
        AutoGenerate = true;
        PrefixKey = Version.Revision != -1
            ? $"Changelog_v{Version.Major}_{Version.Minor}_{Version.Build}_{Version.Revision}"
            : $"Changelog_v{Version.Major}_{Version.Minor}_{Version.Build}";
    }

    public ChangelogEntry this[int index] {
        get {
            if (index < 0 || index >= _entries.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            return _entries[index];
        }
    }

    public ChangelogCollection AddEntries(ChangelogCollection other) {
        _entries.AddRange(other._entries);
        return this;
    }

    public ChangelogCollection AddEntry(ChangelogFlag type, FormattedString formattedString) {
        _entries.Add(new ChangelogEntry(type, formattedString));
        return this;
    }

    public ChangelogCollection AddEntry(ChangelogFlag type, string description, bool localizedDescription = false) {
        _entries.Add(new ChangelogEntry(type, description, localizedDescription));
        return this;
    }

    public void GenerateFromLocalization() {
        if (!AutoGenerate) return;
        if (AutoGenerationCompleted) return;

        var allKeys = LocalizationManager.EnLocaleSource
            .Where(v => v.Key.Contains(PrefixKey))
            .Select(v => v.Key);

        foreach (var key in allKeys) {
            var flag = GetChangelogFlag(key);
            AddEntry(flag, key, true);
        }

        AutoGenerationCompleted = true;
    }

    public IEnumerable<ChangelogEntry> GetEntries(ChangelogFlag? filterType = null) => filterType is null ? _entries : _entries.Where(e => e.Flag == filterType);

    public IEnumerator<ChangelogEntry> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => $"Version {Version} ({Date:yyyy-MM-dd}) - {_entries.Count} changes";

    private ChangelogFlag GetChangelogFlag(string key) {
        foreach (var flag in ChangelogFlags)
            if (key.Contains($"{PrefixKey}_{flag}"))
                return flag;

        return ChangelogFlag.None;
    }
}