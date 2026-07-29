using ColossalFramework.UI;
using CSLModsCommon.UI.Containers;
using CSLModsCommon.UI.Labels;
using System;
using UnityEngine;

namespace CSLModsCommon.UI.Buttons;

public class CheckBox : LiteContainer {
    public event UIElementEventHandler<CheckBox, bool> CheckChanged;

    private bool _isSetCheck;

    public CheckBoxIndicator CheckBoxIndicatorElement { get; private set; }
    public Label LabelElement { get; private set; }
    public bool TextCanToggle { get; set; }

    public string Text {
        get => LabelElement?.Text;
        set => OnTextChanged(value);
    }

    public bool IsChecked {
        get => CheckBoxIndicatorElement.IsOn;
        set {
            if (!_isSetCheck) _isSetCheck = true;

            CheckBoxIndicatorElement.IsOn = value;
        }
    }

    protected virtual void OnCheckChanged(ToggleButton checkboxBase, bool value) {
        if (_isSetCheck)
            CheckChanged?.Invoke(this, value);
    }

    public override void Awake() {
        base.Awake();

        size = new Vector2(100, 30);
        _columnGap = 10;
        _autoLayout = true;
        _direction = FlexDirection.Row;
        _autoFitChildrenVertically = true;

        CheckBoxIndicatorElement = AddUIComponent<CheckBoxIndicator>();
        CheckBoxIndicatorElement.ToggleChanged += OnCheckChanged;
        CheckBoxIndicatorElement.eventSizeChanged += OnCheckBoxIndicatorElementSizeChanged;
        LabelElement = AddUIComponent<Label>();
        LabelElement.SizeMode = TextSizeMode.AutoHeight;
        LabelElement.WordWrap = true;
        LabelElement.TextColors.DisabledColor = UIColors.White60;
        LabelElement.TextChanged += OnLabelElementTextChanged;
    }

    protected override void OnClick(UIMouseEventParameter p) {
        base.OnClick(p);
        if (!p.used && TextCanToggle) CheckBoxIndicatorElement.IsOn = !CheckBoxIndicatorElement.IsOn;
    }

    protected override void OnPaddingChanged(Padding padding) {
        base.OnPaddingChanged(padding);
        if (LabelElement != null)
            UpdateTextWidth();
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();
        if (LabelElement != null)
            UpdateTextWidth();
    }

    private void OnLabelElementTextChanged(Label element, string arg) => UpdateTextWidth();

    private void OnCheckBoxIndicatorElementSizeChanged(UIComponent component, Vector2 value) => UpdateTextWidth();

    private void OnTextChanged(string text) {
        if (string.IsNullOrEmpty(text)) return;
        LabelElement.Text = text;
        UpdateTextWidth();
    }

    private void UpdateTextWidth() {
        var textWidth = width - LayoutPadding.Horizontal - CheckBoxIndicatorElement.width - _columnGap;
        if (Math.Abs(textWidth - LabelElement.width) <= 0.01f) return;
        LabelElement.width = textWidth;
    }
}