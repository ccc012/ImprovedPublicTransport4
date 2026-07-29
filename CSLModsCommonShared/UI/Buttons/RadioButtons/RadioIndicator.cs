using ColossalFramework.UI;
using CSLModsCommon.UI.Atlas;
using UnityEngine;

namespace CSLModsCommon.UI.Buttons.RadioButtons;

public class RadioIndicator : ToggleButton {
    public override void Awake() {
        base.Awake();
        size = new Vector2(20, 20);
        _bgAtlas = Atlases.Shared;
        _renderFg = true;
        _onVisuals.BgSprites.SetValues(SharedAtlasKeys.RadioButtonOn);
        _offVisuals.BgSprites.SetValues(SharedAtlasKeys.Circle);
    }

    public override void SetStyle(StyleType style) {
        switch (style) {
            case StyleType.ControlPanelStyle:
                _offVisuals.BgColors.SetValues(UIColors.BgElementColors);
                _onVisuals.BgColors.SetValues(UIColors.GreenColors);
                break;
            case StyleType.Default:
            case StyleType.OptionPanelStyle:
            default:
                _offVisuals.BgColors.SetValues(UIColors.Bg1ElementColors);
                _onVisuals.BgColors.SetValues(UIColors.BlueColors);
                break;
        }

        Invalidate();
    }

    protected override void OnClick(UIMouseEventParameter p) {
        if (!p.used && !IsOn) IsOn = true;
        // RadioGroup?.NotifySelected(this);
        base.OnClick(p);
    }
}