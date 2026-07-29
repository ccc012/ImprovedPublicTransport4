using ColossalFramework.UI;

namespace CSLModsCommon.UI.Rendering;

public class SpriteStateRenderer : StateRendererBase<string> {
    private string _normalSprite = string.Empty;
    private string _hoveredSprite = string.Empty;
    private string _pressedSprite = string.Empty;
    private string _focusedSprite = string.Empty;
    private string _disabledSprite = string.Empty;

    public virtual string NormalSprite {
        get => _normalSprite;
        set {
            if (value == _normalSprite) return;
            _normalSprite = value;
            Render();
        }
    }

    public virtual string HoveredSprite {
        get => _hoveredSprite;
        set {
            if (value == _hoveredSprite) return;
            _hoveredSprite = value;
            Render();
        }
    }

    public virtual string PressedSprite {
        get => _pressedSprite;
        set {
            if (value == _pressedSprite) return;
            _pressedSprite = value;
            Render();
        }
    }

    public virtual string FocusedSprite {
        get => _focusedSprite;
        set {
            if (value == _focusedSprite) return;
            _focusedSprite = value;
            Render();
        }
    }

    public virtual string DisabledSprite {
        get => _disabledSprite;
        set {
            if (value == _disabledSprite) return;
            _disabledSprite = value;
            Render();
        }
    }

    public SpriteStateRenderer() { }

    public SpriteStateRenderer(UIComponent parent) => Parent = parent;

    public SpriteStateRenderer(UIComponent parent, string normalSprite, string hoveredSprite, string pressedSprite, string focusedSprite, string disabledSprite) : this(parent) {
        if (normalSprite is not null)
            _normalSprite = normalSprite;
        if (hoveredSprite is not null)
            _hoveredSprite = hoveredSprite;
        if (pressedSprite is not null)
            _pressedSprite = pressedSprite;
        if (focusedSprite is not null)
            _focusedSprite = focusedSprite;
        if (disabledSprite is not null)
            _disabledSprite = disabledSprite;
    }

    public SpriteStateRenderer(string normalSprite, string hoveredSprite, string pressedSprite, string focusedSprite, string disabledSprite) {
        if (normalSprite is not null)
            _normalSprite = normalSprite;
        if (hoveredSprite is not null)
            _hoveredSprite = hoveredSprite;
        if (pressedSprite is not null)
            _pressedSprite = pressedSprite;
        if (focusedSprite is not null)
            _focusedSprite = focusedSprite;
        if (disabledSprite is not null)
            _disabledSprite = disabledSprite;
    }

    public override string GetValue(UIState state) => state switch {
        UIState.Hover => HoveredSprite,
        UIState.Pressed => PressedSprite,
        UIState.Focused => FocusedSprite,
        UIState.Disabled => DisabledSprite,
        _ => NormalSprite
    };

    public override void SetValues(string normalSprite, string hoveredSprite, string pressedSprite, string focusedSprite, string disabledSprite) {
        CanRender = false;
        if (normalSprite is not null)
            NormalSprite = normalSprite;
        if (hoveredSprite is not null)
            HoveredSprite = hoveredSprite;
        if (pressedSprite is not null)
            PressedSprite = pressedSprite;
        if (focusedSprite is not null)
            FocusedSprite = focusedSprite;
        if (disabledSprite is not null)
            DisabledSprite = disabledSprite;
        CanRender = true;
        Render();
    }

    public override void SetValues(string sprite) {
        if (sprite is null) return;
        CanRender = false;
        NormalSprite = sprite;
        HoveredSprite = sprite;
        PressedSprite = sprite;
        FocusedSprite = sprite;
        DisabledSprite = sprite;
        CanRender = true;
        Render();
    }
}