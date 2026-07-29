using ColossalFramework.UI;
using CSLModsCommon.UI.Labels;
using UnityEngine;

namespace CSLModsCommon.UI.Buttons.RadioButtons;

public class RadioButton : UIStateElement {
    public event UIElementEventHandler Clicked;

    protected Padding _layoutPadding;
    private uint _gap = 10;

    public Padding LayoutPadding => _layoutPadding;
    public Label TextElement { get; private set; }
    public RadioIndicator Radio { get; private set; }

    public uint Gap {
        get => _gap;
        set {
            _gap = value;
            Arrange();
        }
    }

    public Vector2 ButtonSize {
        get => Radio.size;
        set => Radio.size = value;
    }

    public string Content {
        get => TextElement.Text;
        set => TextElement.Text = value;
    }

    public override void Awake() {
        base.Awake();
        _layoutPadding = Padding.GetZeroPadding(this);
        size = new Vector2(100, 30);
        Radio = AddUIComponent<RadioIndicator>();
        Radio.SetStyle(StyleType.OptionPanelStyle);
        Radio.eventSizeChanged += (_, _) => Arrange();
        SetStyle(StyleType.OptionPanelStyle);
        TextElement = AddUIComponent<Label>();
        TextElement.SizeMode = TextSizeMode.AutoHeight;
        TextElement.WordWrap = true;
        TextElement.ProcessMarkup = true;
        TextElement.TextColors.SetValues(UIColors.MajorTextElementColors);
        TextElement.Text = "Radio Button Text";
        TextElement.eventSizeChanged += (_, _) => Arrange();
        Arrange();
    }

    public override void OnDestroy() {
        base.OnDestroy();
        _layoutPadding.DetachParent();
    }

    protected override void OnClick(UIMouseEventParameter p) {
        if (!p.used)
            Clicked?.Invoke();
        base.OnClick(p);
    }

    public void SetText(string text) => TextElement.Text = text;

    public void SetSelected(bool selected) => Radio.IsOn = selected;

    public void Arrange() {
        if (size.sqrMagnitude < 0.01f) return;
        Radio.relativePosition = new Vector3(_layoutPadding.Left, _layoutPadding.Top);
        TextElement.width = width - Radio.width - _gap;
        TextElement.relativePosition = new Vector3(_layoutPadding.Left + Radio.width + _gap, _layoutPadding.Top);
        height = Mathf.Max(Radio.height, TextElement.height);
    }
}