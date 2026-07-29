using ColossalFramework.UI;
using CSLModsCommon.UI.Atlas;

namespace CSLModsCommon.UI.Buttons;

public class ToggleSwitchIndicator : ToggleButton {
    public override void Awake() {
        base.Awake();
        _bgAtlas = _fgAtlas = Atlases.Shared;
        _renderFg = true;
        _onVisuals.FgColors.SetValues(UIColors.ToggleFgNormal);
        _onVisuals.FgColors.DisabledColor = UIColors.ToggleFgDisabled;
        _offVisuals.FgColors.SetValues(UIColors.ToggleFgNormal);
        _offVisuals.FgColors.DisabledColor = UIColors.ToggleFgDisabled;
    }

    public override void SetStyle(StyleType style) {
        switch (style) {
            case StyleType.ControlPanelStyle:
                _offVisuals.BgSprites.SetValues(SharedAtlasKeys.ToggleBg);
                _offVisuals.BgColors.SetValues(UIColors.BgElementColors);
                _offVisuals.FgSprites.SetValues(SharedAtlasKeys.ToggleOffFg);
                _onVisuals.BgSprites.SetValues(SharedAtlasKeys.ToggleBg);
                break;
            case StyleType.OptionPanelStyle:
            case StyleType.Default:
            default:
                _offVisuals.BgSprites.SetValues(SharedAtlasKeys.RoundRect12);
                _offVisuals.BgColors.SetValues(UIColors.Bg1ElementColors);
                _offVisuals.FgSprites.SetValues(SharedAtlasKeys.ToggleOffFg);
                _onVisuals.BgSprites.SetValues(SharedAtlasKeys.RoundRect12);
                break;
        }

        _onVisuals.BgColors.SetValues(UIColors.GreenColors);
        _onVisuals.FgSprites.SetValues(SharedAtlasKeys.ToggleOnFg);
    }

    protected override void OnClick(UIMouseEventParameter p) {
        if (!p.used)
            IsOn = !IsOn;

        base.OnClick(p);
    }
}