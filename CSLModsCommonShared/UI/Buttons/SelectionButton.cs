using ColossalFramework.UI;

namespace CSLModsCommon.UI.Buttons;

public class SelectionButton : TextButton {
    public event UIElementEventHandler<SelectionButton, string> TextChanged;

    protected bool _isSelected;

    public virtual bool IsSelected {
        get => _isSelected;
        set {
            if (value == _isSelected) return;
            _isSelected = value;
            Invalidate();
        }
    }

    public override string Text {
        get => _text;
        set {
            if (value == _text) return;
            UIFontManager.Invalidate(Font);
            _text = value;
            TextChanged?.Invoke(this, value);
            Invalidate();
        }
    }

    protected override void OnMouseEnter(UIMouseEventParameter p) {
        base.OnMouseEnter(p);
        if (isEnabled && !IsSelected)
            State = UIState.Hover;
    }

    protected override void OnMouseLeave(UIMouseEventParameter p) {
        base.OnMouseLeave(p);
        if (isEnabled)
            State = IsSelected ? UIState.Focused : UIState.Normal;
    }
}