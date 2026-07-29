using System;

namespace CSLModsCommon.Localization;

public readonly struct LocaleOption {
    public string LocaleId { get; }
    public string DisplayText { get; }

    public LocaleOption(string localeId, string displayText) {
        LocaleId = localeId ?? throw new ArgumentNullException(nameof(localeId));
        DisplayText = displayText ?? throw new ArgumentNullException(nameof(displayText));
    }

    public override string ToString() => DisplayText;
}