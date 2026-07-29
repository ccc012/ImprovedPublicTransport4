using CSLModsCommon.UI.DropDown;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CSLModsCommon.UI.Utilities;

public static class DropDownHelper {
    public static DropDownItem<TEnum>[] FromEnumWithDisplay<TEnum>(string[] displayNames) where TEnum : Enum {
        var values = (TEnum[])Enum.GetValues(typeof(TEnum));
        if (displayNames == null) throw new ArgumentNullException(nameof(displayNames));
        if (displayNames.Length != values.Length)
            throw new ArgumentException("Display name count must match enum value count.");

        var items = new DropDownItem<TEnum>[values.Length];
        for (var i = 0; i < values.Length; i++) items[i] = new DropDownItem<TEnum>(values[i], displayNames[i]);

        return items;
    }

    public static DropDownItem<TEnum>[] FromEnum<TEnum>(Func<TEnum, string> getText = null, bool localized = false) where TEnum : Enum {
        var values = (TEnum[])Enum.GetValues(typeof(TEnum));
        getText ??= e => e.ToString();

        var items = new DropDownItem<TEnum>[values.Length];
        for (var i = 0; i < values.Length; i++) items[i] = new DropDownItem<TEnum>(values[i], getText(values[i]), localized);

        return items;
    }

    public static DropDownItem<T>[] FromCollection<T>(IEnumerable<T> source, Func<T, string> getText) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return getText == null ? throw new ArgumentNullException(nameof(getText)) : source.Select(item => new DropDownItem<T>(item, getText(item))).ToArray();
    }

    [Obsolete("FromKeyDictionary or FromValueDictionary is preferred.")]
    public static DropDownItem<TKey>[] FromDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict) => dict == null ? throw new ArgumentNullException(nameof(dict)) : dict.Select(kvp => new DropDownItem<TKey>(kvp.Key, kvp.Value?.ToString() ?? string.Empty)).ToArray();

    public static DropDownItem<TKey>[] FromKeyDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict, Func<TKey, TValue, string> getDisplayText) => dict.Select(kvp => new DropDownItem<TKey>(kvp.Key, getDisplayText(kvp.Key, kvp.Value))).ToArray();

    public static DropDownItem<TValue>[] FromValueDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict, Func<TKey, TValue, string> getDisplayText) => dict.Select(kvp => new DropDownItem<TValue>(kvp.Value, getDisplayText(kvp.Key, kvp.Value))).ToArray();
}