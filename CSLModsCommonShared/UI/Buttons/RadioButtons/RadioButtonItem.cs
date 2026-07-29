using System;

namespace CSLModsCommon.UI.Buttons.RadioButtons;

public class RadioButtonItem<T> {
    public T Value { get; }
    public RadioButton UI { get; }

    private bool _isChecked;
    private bool _settingChecked;

    public bool IsChecked {
        get => _isChecked;
        set => SetChecked(value);
    }

    public event Action<RadioButtonItem<T>> CheckedChanged;

    public RadioButtonItem(T value, RadioButton ui, bool isChecked = false) {
        Value = value;
        UI = ui;
        UI.Clicked += () => SetChecked(true);
        if (isChecked)
            SetChecked(true);
    }

    private void SetChecked(bool value) {
        if (_settingChecked || _isChecked == value) return;
        _settingChecked = true;

        _isChecked = value;
        UI.SetSelected(value);
        CheckedChanged?.Invoke(this);

        _settingChecked = false;
    }
}