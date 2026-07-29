using ColossalFramework.UI;
using CSLModsCommon.UI.Atlas;
using UnityEngine;

namespace CSLModsCommon.UI.Buttons;

public class CheckBoxIndicator : ToggleButton {
    public override void Awake() {
        base.Awake();
        _fgScaleFactor = 0.6f;
        size = new Vector2(20, 20);
        _renderFg = true;
        _bgAtlas = _fgAtlas = Atlases.Shared;
        _offVisuals.BgSprites.SetValues(SharedAtlasKeys.RoundRect4);
        _offVisuals.FgSprites.SetValues(SharedAtlasKeys.Checked);
        _offVisuals.FgColors.SetValues(new Color32(255, 255, 255, 0));
        _onVisuals.BgSprites.SetValues(SharedAtlasKeys.RoundRect4);
        _onVisuals.FgSprites.SetValues(SharedAtlasKeys.Checked);
        _onVisuals.FgColors.DisabledColor = UIColors.White60;
        _renderFg = true;
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
        if (!p.used)
            IsOn = !IsOn;

        base.OnClick(p);
    }
}