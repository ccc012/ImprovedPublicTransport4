using CSLModsCommon.UI.Atlas;
using CSLModsCommon.UI.Buttons;

namespace CSLModsCommon.UI.DropDown;

public class DropDownPopupButton : SelectionButton {
    public override void Awake() {
        base.Awake();
        _bgAtlas = Atlases.Shared;
        _bgSprites.SetValues(SharedAtlasKeys.RoundRect6);
        _bgSprites.NormalSprite = string.Empty;
    }

    public override void SetStyle(StyleType style) {
        switch (style) {
            case StyleType.ControlPanelStyle:
                BgColors.SetValues(UIColors.GroupBgNormal, UIColors.BgElementHovered, UIColors.BgElementPressed, UIColors.GreenNormal, UIColors.BgElementDisabled);
                break;
            case StyleType.Default:
            case StyleType.OptionPanelStyle:
            default:
                BgColors.SetValues(UIColors.GroupBg1, UIColors.Bg1ElementHovered, UIColors.GroupBg1, UIColors.BlueNormal, UIColors.Bg1ElementDisabled);
                break;
        }
    }
}