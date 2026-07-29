using ColossalFramework.UI;
using UnityEngine;

namespace CSLModsCommon.UI.Buttons; 
public abstract class TextButton : UITextBase {
    protected bool _autoWidth;
    protected float _maxWidth = float.MaxValue;
    protected float _minWidth;

    public bool IsHovering => m_IsMouseHovering;

    public virtual bool AutoWidth {
        get => _autoWidth;
        set {
            if (value == _autoWidth) return;
            _autoWidth = value;
            if (AutoWidth) UpdateWidthForText();

            Invalidate();
        }
    }

    public virtual float MinWidth {
        get => _minWidth;
        set {
            if (Mathf.Approximately(value, _minWidth)) return;
            _minWidth = value;
            if (AutoWidth) UpdateWidthForText();

            Invalidate();
        }
    }

    public virtual float MaxWidth {
        get => _maxWidth;
        set {
            if (Mathf.Approximately(value, _maxWidth)) return;
            _maxWidth = value;
            if (AutoWidth) UpdateWidthForText();

            Invalidate();
        }
    }

    public override void Invalidate() {
        base.Invalidate();
        if (_autoWidth)
            UpdateWidthForText();
    }

    protected override void OnRebuildRenderData() {
        RenderBackground();
        RenderForeground();
        RenderText();
    }

    public override Vector2 CalculateMinimumSize() {
        var backgroundSprite = GetBgSprite();
        if (backgroundSprite == null) return base.CalculateMinimumSize();

        var border = backgroundSprite.border;
        if (border.horizontal > 0 || border.vertical > 0) return Vector2.Max(base.CalculateMinimumSize(), new Vector2(border.horizontal, border.vertical));

        return base.CalculateMinimumSize();
    }

    protected override Color32 GetTextColor() => _textColors.GetValue(State);

    protected override void RenderText() {
        if (Font is null || !Font.isValid) return;
        if (_textRenderData != null) {
            _textRenderData.Clear();
        }
        else {
            _textRenderData = UIRenderData.Obtain();
            m_RenderData.Add(_textRenderData);
        }

        _textRenderData.material = TextAtlas.material;
        using var uiFontRenderer = ObtainTextRenderer();
        if (uiFontRenderer is UIDynamicFont.DynamicFontRenderer dynamicFontRenderer) {
            dynamicFontRenderer.spriteAtlas = TextAtlas;
            dynamicFontRenderer.spriteBuffer = _textRenderData;
        }

        uiFontRenderer.Render(Text, _textRenderData);
    }

    public Vector2 MeasureString() {
        using var uiFontRenderer = ObtainTextRenderer();
        var num = uiFontRenderer.MeasureString(Text);
        return num;
    }

    private UIFontRenderer ObtainTextRenderer() {
        var vector = size - new Vector2(TextPadding.Horizontal, TextPadding.Vertical);
        var maxSize = autoSize ? Vector2.one * 2.1474836E+09f : vector;
        var num = PixelsToUnits();
        var vectorOffset = (pivot.TransformToUpperLeft(size, arbitraryPivotOffset) + new Vector3(TextPadding.Left, -(float)TextPadding.Top)) * num;
        GetTextScaleMultiplier();
        var uiFontRenderer = Font.ObtainRenderer();
        uiFontRenderer.wordWrap = WordWrap;
        uiFontRenderer.multiLine = true;
        uiFontRenderer.maxSize = maxSize;
        uiFontRenderer.pixelRatio = num;
        uiFontRenderer.textScale = TextScale;
        uiFontRenderer.vectorOffset = vectorOffset;
        uiFontRenderer.textAlign = TextHorizontalAlignment;
        uiFontRenderer.processMarkup = _processMarkup;
        uiFontRenderer.defaultColor = GetTextColor();
        uiFontRenderer.bottomColor = _useTextGradient ? new Color32?(GetGradientBottomColorForState()) : null;
        uiFontRenderer.overrideMarkupColors = false;
        uiFontRenderer.opacity = CalculateOpacity();
        uiFontRenderer.shadow = _useDropShadow;
        uiFontRenderer.shadowColor = _dropShadowColor;
        uiFontRenderer.shadowOffset = _dropShadowOffset;
        uiFontRenderer.outline = _useOutline;
        uiFontRenderer.outlineSize = _outlineSize;
        uiFontRenderer.outlineColor = _outlineColor;
        if (!autoSize && TextVerticalAlignment != UIVerticalAlignment.Top) uiFontRenderer.vectorOffset = GetVertAlignOffset(uiFontRenderer);

        return uiFontRenderer;
    }

    private Vector3 GetVertAlignOffset(UIFontRenderer fontRenderer) {
        var unitScale = PixelsToUnits();
        var textSize = fontRenderer.MeasureString(Text) * unitScale;
        var vectorOffset = fontRenderer.vectorOffset;
        var availableHeight = (height - TextPadding.Vertical) * unitScale;

        if (textSize.y >= availableHeight) return vectorOffset;

        var verticalGap = availableHeight - textSize.y;
        switch (TextVerticalAlignment) {
            case UIVerticalAlignment.Middle:
                vectorOffset.y -= verticalGap * 0.5f;
                break;
            case UIVerticalAlignment.Bottom:
                vectorOffset.y -= verticalGap;
                break;
        }

        return vectorOffset;
    }

    protected virtual void UpdateWidthForText() {
        if (Font is null) return;
        var textSize = MeasureString();
        var newWidth = textSize.x + TextPadding.Horizontal;
        if (RenderFg) {
            var fgSprite = GetFgSprite();
            if (fgSprite is not null) newWidth = newWidth + GetFgRenderSize(fgSprite).x + 6;
        }

        if (MinWidth > 0) newWidth = Mathf.Max(newWidth, MinWidth);
        if (MaxWidth > 0) newWidth = Mathf.Min(newWidth, MaxWidth);
        width = newWidth;
    }
}