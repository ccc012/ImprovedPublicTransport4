using CSLModsCommon.UI.Atlas;

namespace CSLModsCommon.UI.Buttons; 
public class TabButton : SelectionButton {
    private bool _isInitialized;

    public override bool IsSelected {
        get => _isSelected;
        set {
            _isSelected = value;
            RefreshState();
        }
    }

    public override void Awake() {
        base.Awake();
        _bgAtlas = Atlases.Shared;
        _bgSprites.SetValues(SharedAtlasKeys.RoundRect6);
        _bgSprites.NormalSprite = string.Empty;
    }

    public override void Update() {
        base.Update();
        if (_isInitialized) return;
        _isInitialized = true;
        RefreshState();
    }

    public override void SetStyle(StyleType style) {
        switch (style) {
            case StyleType.ControlPanelStyle:
                BgColors.SetValues(UIColors.GroupBgNormal, UIColors.BgElementHovered, UIColors.BgElementPressed, UIColors.GreenNormal, UIColors.BgElementDisabled);
                break;
            case StyleType.Default:
            case StyleType.OptionPanelStyle:
            default:
                BgColors.SetValues(UIColors.GroupBg1, UIColors.Bg1ElementHovered, UIColors.Bg1ElementHovered, UIColors.BlueNormal, UIColors.Bg1ElementDisabled);
                break;
        }
    }

    private void RefreshState() {
        if (_isSelected)
            State = UIState.Focused;
        else
            State = m_IsMouseHovering ? UIState.Hover : UIState.Normal;
    }
}