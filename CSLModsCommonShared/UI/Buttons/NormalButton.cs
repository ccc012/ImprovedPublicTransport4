using ColossalFramework.UI;
using CSLModsCommon.UI.Atlas;

namespace CSLModsCommon.UI.Buttons;

public class NormalButton : TextButton {
    public event UIElementEventHandler<NormalButton, string> TextChanged;

    public override string Text {
        get => _text;
        set {
            if (value == _text) return;
            UIFontManager.Invalidate(Font);
            _text = value;
            TextChanged?.Invoke(this, value);
            if (AutoWidth) UpdateWidthForText();

            Invalidate();
        }
    }

    public override void SetStyle(StyleType style) {
        BgAtlas = FgAtlas = Atlases.Shared;
        BgSprites.SetValues(SharedAtlasKeys.RoundRect8);
        switch (style) {
            case StyleType.ControlPanelStyle:
                BgColors.SetValues(UIColors.BgElementColors);
                break;
            case StyleType.OptionPanelStyle:
            case StyleType.Default:
            default:
                BgColors.SetValues(UIColors.Bg1ElementColors);
                break;
        }

        Invalidate();
    }
}