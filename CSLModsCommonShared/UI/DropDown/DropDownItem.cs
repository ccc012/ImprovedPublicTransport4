using CSLModsCommon.Manager;
using System;

namespace CSLModsCommon.UI.DropDown; 
public class DropDownItem<T> {
    public event Action<DropDownItem<T>> EnabledChanged;

    private bool _isEnabled;
    private readonly string _localizationKey;
    private readonly bool _isLocalized;

    public T Value { get; }
    public string DisplayText { get; private set; }

    public bool IsEnabled {
        get => _isEnabled;
        set {
            if (_isEnabled == value) return;
            _isEnabled = value;
            EnabledChanged?.Invoke(this);
        }
    }

    public override string ToString() => DisplayText ?? base.ToString();

    public DropDownItem(T value, string text, bool localized = false, bool isEnabled = true) {
        Value = value;
        _isEnabled = isEnabled;
        _isLocalized = localized;

        if (localized) {
            _localizationKey = text;
            DisplayText = LocalizationManager.Localize(text) ?? text ?? string.Empty;
        }
        else {
            DisplayText = text ?? value?.ToString() ?? string.Empty;
        }
    }

    public void RefreshLocalization() {
        if (!_isLocalized || string.IsNullOrEmpty(_localizationKey))
            return;

        DisplayText = LocalizationManager.Localize(_localizationKey) ?? _localizationKey ?? string.Empty;
    }
}