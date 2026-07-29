using ColossalFramework.UI;
using CSLModsCommon.UI.Rendering;
using UnityEngine;

namespace CSLModsCommon.UI;

public abstract class UIElement : UIElementBase, IStylable {
    protected UIRenderData _bgRenderData;
    protected UIRenderData _fgRenderData;
    protected UIRenderData _textRenderData;
    protected UITextureAtlas _bgAtlas;
    protected UITextureAtlas _fgAtlas;
    protected UITextureAtlas _textAtlas;
    protected ColorStateRenderer _bgColors;
    protected ColorStateRenderer _fgColors;
    protected ColorStateRenderer _textColors;
    protected SpriteStateRenderer _bgSprites;
    protected SpriteStateRenderer _fgSprites;
    protected bool _followParentOpacity;

    public float PixelSize => PixelsToUnits();

    public virtual UITextureAtlas BgAtlas {
        get => _bgAtlas ??= GetUIView()?.defaultAtlas;
        set {
            if (Equals(value, _bgAtlas)) return;
            _bgAtlas = value;
            Invalidate();
        }
    }

    public UITextureAtlas FgAtlas {
        get => _fgAtlas ??= GetUIView()?.defaultAtlas;
        set {
            if (Equals(value, _fgAtlas)) return;
            _fgAtlas = value;
            Invalidate();
        }
    }

    public UITextureAtlas TextAtlas {
        get => _textAtlas ??= GetUIView()?.defaultAtlas;
        set {
            if (Equals(value, _textAtlas)) return;
            _textAtlas = value;
            Invalidate();
        }
    }

    public virtual ColorStateRenderer BgColors {
        get => _bgColors;
        set {
            if (Equals(value, _bgColors)) return;
            _bgColors = value;
            Invalidate();
        }
    }

    public virtual ColorStateRenderer FgColors {
        get => _fgColors;
        set {
            if (Equals(value, _fgColors)) return;
            _fgColors = value;
            Invalidate();
        }
    }

    public virtual ColorStateRenderer TextColors {
        get => _textColors;
        set {
            if (Equals(value, _textColors)) return;
            _textColors = value;
            Invalidate();
        }
    }

    public virtual SpriteStateRenderer BgSprites {
        get => _bgSprites;
        set {
            if (Equals(value, _bgSprites)) return;
            _bgSprites = value;
            Invalidate();
        }
    }

    public virtual SpriteStateRenderer FgSprites {
        get => _fgSprites;
        set {
            if (Equals(value, _fgSprites)) return;
            _fgSprites = value;
            Invalidate();
        }
    }

    public virtual bool FollowParentOpacity {
        get => _followParentOpacity;
        set {
            if (Equals(value, _followParentOpacity)) return;
            _followParentOpacity = value;
            Invalidate();
        }
    }

    public float Width {
        get => m_Size.x;
        set => size = new Vector2(value, m_Size.y);
    }

    public float Height {
        get => m_Size.y;
        set => size = new Vector2(m_Size.x, value);
    }

    public override void Awake() {
        base.Awake();
        _bgColors = new ColorStateRenderer(this);
        _fgColors = new ColorStateRenderer(this);
        _textColors = new ColorStateRenderer(this);
        _bgSprites = new SpriteStateRenderer(this);
        _fgSprites = new SpriteStateRenderer(this);
    }

    protected virtual void RenderBackground() { }

    protected virtual void RenderForeground() { }

    protected virtual void RenderText() { }

    protected virtual Color32 GetBgRenderColor(UIState state) => _bgColors.GetValue(state);

    protected virtual Color32 GetFgRenderColor(UIState state) => _fgColors.GetValue(state);

    protected override void OnPositionChanged() {
        base.OnPositionChanged();
        if (_bgRenderData is not null || _fgRenderData is not null || _textRenderData is not null)
            GetUIView().Invalidate();
    }

    public virtual void SetStyle(StyleType style) { }
}