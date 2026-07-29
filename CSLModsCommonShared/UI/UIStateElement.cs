using ColossalFramework.UI;
using UnityEngine;

namespace CSLModsCommon.UI;

public abstract class UIStateElement : UIElement {
    protected bool _renderFg;
    protected UIState _state;
    protected ForegroundSpriteMode _fgSpriteMode;
    protected UIHorizontalAlignment _fgHorizontalAlignment = UIHorizontalAlignment.Center;
    protected UIVerticalAlignment _fgVerticalAlignment = UIVerticalAlignment.Middle;
    protected Padding _fgSpritePadding;
    protected float _fgScaleFactor = 1f;
    protected Vector2 _fgCustomSize;

    public event UIElementEventHandler<UIStateElement, UIState> EventStateChanged;

    public Padding FgSpritePadding {
        get => _fgSpritePadding;
        set {
            if (Equals(value, _fgSpritePadding)) return;
            _fgSpritePadding = value;
            Invalidate();
        }
    }

    public bool RenderFg {
        get => _renderFg;
        set {
            if (value.Equals(_renderFg)) return;
            _renderFg = value;
            Invalidate();
        }
    }

    public Vector2 FgCustomSize {
        get => _fgCustomSize;
        set {
            if (value == _fgCustomSize) return;
            _fgCustomSize = value;
            Invalidate();
        }
    }

    public float FgScaleFactor {
        get => _fgScaleFactor;
        set {
            if (Mathf.Approximately(value, _fgScaleFactor)) return;
            _fgScaleFactor = value;
            Invalidate();
        }
    }

    public virtual UIHorizontalAlignment FgHorizontalAlignment {
        get => _fgHorizontalAlignment;
        set {
            if (value == _fgHorizontalAlignment) return;
            _fgHorizontalAlignment = value;
            Invalidate();
        }
    }

    public virtual UIVerticalAlignment FgVerticalAlignment {
        get => _fgVerticalAlignment;
        set {
            if (value == _fgVerticalAlignment) return;
            _fgVerticalAlignment = value;
            Invalidate();
        }
    }

    public ForegroundSpriteMode FgSpriteMode {
        get => _fgSpriteMode;
        set {
            if (value == _fgSpriteMode) return;
            _fgSpriteMode = value;
            Invalidate();
        }
    }

    public virtual UIState State {
        get => _state;
        set {
            if (_state != value) OnStateChanged(value);
        }
    }

    public override void Awake() {
        base.Awake();
        _fgSpritePadding = Padding.GetZeroPadding(this);
    }

    protected override void RenderBackground() {
        if (_bgRenderData is not null) {
            _bgRenderData.Clear();
        }
        else {
            _bgRenderData = UIRenderData.Obtain();
            m_RenderData.Add(_bgRenderData);
        }

        if (_bgAtlas is null) return;
        _bgRenderData.material = _bgAtlas.material;
        var bgSprite = GetBgSprite();
        if (bgSprite is null) return;

        RenderOptions options = new() {
            Atlas = _bgAtlas,
            Color = GetBgRenderColor(),
            FillAmount = 1f,
            Flip = UISpriteFlip.None,
            Offset = pivot.TransformToUpperLeft(size, arbitraryPivotOffset),
            PixelsToUnits = PixelsToUnits(),
            Size = size,
            SpriteInfo = bgSprite
        };
        if (bgSprite.isSliced) {
            SlicedSprite.RenderSprite(_bgRenderData, options);
            return;
        }

        Sprite.RenderSprite(_bgRenderData, options);
    }

    protected override void RenderForeground() {
        if (_fgRenderData is not null) {
            _fgRenderData.Clear();
        }
        else {
            _fgRenderData = UIRenderData.Obtain();
            m_RenderData.Add(_fgRenderData);
        }

        if (!RenderFg || _fgAtlas is null) return;
        _fgRenderData.material = _fgAtlas.material;
        var fgSprite = GetFgSprite();
        if (fgSprite is null) return;

        var fgRenderSize = GetFgRenderSize(fgSprite);
        RenderOptions options = new() {
            Atlas = _fgAtlas,
            Color = GetFgRenderColor(),
            FillAmount = 1f,
            Flip = UISpriteFlip.None,
            Offset = GetFgRenderOffset(fgRenderSize),
            PixelsToUnits = PixelsToUnits(),
            Size = fgRenderSize,
            SpriteInfo = fgSprite
        };
        if (fgSprite.isSliced) {
            SlicedSprite.RenderSprite(_fgRenderData, options);
            return;
        }

        Sprite.RenderSprite(_fgRenderData, options);
    }

    protected virtual Vector2 GetFgRenderOffset(Vector2 renderSize) {
        Vector2 result = pivot.TransformToUpperLeft(size, arbitraryPivotOffset);
        if (FgHorizontalAlignment == UIHorizontalAlignment.Left) {
            result.x += FgSpritePadding.Left;
        }
        else if (FgHorizontalAlignment == UIHorizontalAlignment.Center) {
            result.x += (width - renderSize.x) * 0.5f;
            result.x += FgSpritePadding.Left - FgSpritePadding.Right;
        }
        else if (FgHorizontalAlignment == UIHorizontalAlignment.Right) {
            result.x += width - renderSize.x;
            result.x -= FgSpritePadding.Right;
        }

        if (FgVerticalAlignment == UIVerticalAlignment.Bottom) {
            result.y -= height - renderSize.y;
            result.y += FgSpritePadding.Bottom;
        }
        else if (FgVerticalAlignment == UIVerticalAlignment.Middle) {
            result.y -= (height - renderSize.y) * 0.5f;
            result.y -= FgSpritePadding.Top - FgSpritePadding.Bottom;
        }
        else if (FgVerticalAlignment == UIVerticalAlignment.Top) {
            result.y -= FgSpritePadding.Top;
        }

        return result;
    }

    protected virtual Vector2 GetFgRenderSize(UITextureAtlas.SpriteInfo spriteInfo) {
        var vector = Vector2.zero;
        if (spriteInfo == null) return vector;

        if (_fgSpriteMode == ForegroundSpriteMode.Custom) {
            vector = _fgCustomSize;
        }
        else if (_fgSpriteMode == ForegroundSpriteMode.Fill) {
            vector = spriteInfo.pixelSize;
        }
        else if (_fgSpriteMode == ForegroundSpriteMode.Scale) {
            var num = Mathf.Min(width / spriteInfo.width, height / spriteInfo.height);
            vector = new Vector2(num * spriteInfo.width, num * spriteInfo.height);
            vector *= _fgScaleFactor;
        }
        else {
            vector = size * _fgScaleFactor;
        }

        return vector;
    }

    protected virtual Color32 GetFgRenderColor() => FgColors.GetValue(State);

    protected virtual Color32 GetBgRenderColor() => BgColors.GetValue(State);

    protected virtual UITextureAtlas.SpriteInfo GetBgSprite() => BgAtlas?[BgSprites.GetValue(State)];

    protected virtual UITextureAtlas.SpriteInfo GetFgSprite() => FgAtlas?[FgSprites.GetValue(State)];

    protected virtual void OnStateChanged(UIState value) {
        if (!isEnabled && value != UIState.Disabled) return;
        _state = value;
        EventStateChanged?.Invoke(this, value);
        Invalidate();
    }

    protected override void OnIsEnabledChanged() {
        EnabledChanged();
        base.OnIsEnabledChanged();
    }

    protected virtual void EnabledChanged() => State = isEnabled ? UIState.Normal : UIState.Disabled;

    protected override void OnEnterFocus(UIFocusEventParameter p) {
        EnterFocus();
        base.OnEnterFocus(p);
    }

    protected virtual void EnterFocus() {
        if (State != UIState.Pressed)
            State = UIState.Focused;
    }

    protected override void OnLeaveFocus(UIFocusEventParameter p) {
        LeaveFocus();
        base.OnLeaveFocus(p);
    }

    protected virtual void LeaveFocus() => State = containsMouse ? UIState.Hover : UIState.Normal;

    protected override void OnMouseUp(UIMouseEventParameter p) {
        if (m_IsMouseHovering)
            State = containsFocus ? UIState.Focused : UIState.Hover;
        else if (hasFocus)
            State = UIState.Focused;
        else
            State = UIState.Normal;
        base.OnMouseUp(p);
    }

    protected override void OnMouseDown(UIMouseEventParameter p) {
        if (isEnabled && State != UIState.Focused)
            State = UIState.Pressed;
        base.OnMouseDown(p);
    }

    protected override void OnMouseEnter(UIMouseEventParameter p) {
        if (isEnabled)
            State = UIState.Hover;
        base.OnMouseEnter(p);
    }

    protected override void OnMouseLeave(UIMouseEventParameter p) {
        if (isEnabled)
            State = containsFocus ? UIState.Focused : UIState.Normal;
        base.OnMouseLeave(p);
    }
}