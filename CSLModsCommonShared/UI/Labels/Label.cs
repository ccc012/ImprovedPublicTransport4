using ColossalFramework.UI;
using System.Collections.Generic;
using UnityEngine;

namespace CSLModsCommon.UI.Labels; 
public class Label : UITextBase {
    public event UIElementEventHandler<Label, string> TextChanged;
    public event UIElementEventHandler<Label, string> RawTextChanged;
    public event UIElementEventHandler<Label, string> PrefixTextChanged;
    public event UIElementEventHandler<Label, string> SuffixTextChanged;

    protected string _prefixText = string.Empty;
    protected string _suffixText = string.Empty;
    protected int _tabSize = 48;
    protected readonly List<int> _tabStops = new();
    protected TextSizeMode _sizeMode = TextSizeMode.AutoSize;
    protected bool _isInvalidating;
    protected bool _sizeInitialized;

    public override string Text {
        get => ComposeFullText();
        set {
            if (string.Equals(value, _text)) return;
            _text = value;
            OnRawTextChanged();
            OnTextChanged();
        }
    }

    public TextSizeMode SizeMode {
        get => _sizeMode;
        set {
            if (_sizeMode == value) return;
            _sizeMode = value;
            Invalidate();
        }
    }

    public string PrefixText {
        get => _prefixText;
        set {
            if (string.Equals(value, _prefixText)) return;
            _prefixText = value;
            OnPrefixTextChanged();
            OnTextChanged();
        }
    }

    public string SuffixText {
        get => _suffixText;
        set {
            if (string.Equals(value, _suffixText)) return;
            _suffixText = value;
            OnSuffixTextChanged();
            OnTextChanged();
        }
    }

    public int TabSize {
        get => _tabSize;
        set {
            value = Mathf.Max(0, value);
            if (value == _tabSize) return;
            _tabSize = value;
            Invalidate();
        }
    }

    public IList<int> TabStops => _tabStops;

    protected virtual void OnRawTextChanged() {
        Invalidate();
        RawTextChanged?.Invoke(this, _text);
    }

    protected virtual void OnPrefixTextChanged() {
        Invalidate();
        PrefixTextChanged?.Invoke(this, _prefixText);
    }

    protected virtual void OnSuffixTextChanged() {
        Invalidate();
        SuffixTextChanged?.Invoke(this, _suffixText);
    }

    protected virtual void OnTextChanged() {
        Invalidate();
        TextChanged?.Invoke(this, Text);
    }

    protected virtual void TextInvalidate() {
        if (_isInvalidating) return;
        _isInvalidating = true;

        try {
            if (Font == null || !Font.isValid || string.IsNullOrEmpty(Text) || !isVisible) return;

            using var renderer = ObtainRenderer();
            var measured = renderer.MeasureString(Text).RoundToInt();
            var paddingVec = new Vector2(TextPadding.Horizontal, TextPadding.Vertical);

            var useInitial = !_sizeInitialized && !(size.sqrMagnitude > 0.01f);

            var newSize = _sizeMode switch {
                TextSizeMode.AutoSize => measured + paddingVec,
                TextSizeMode.AutoHeight => new Vector2(useInitial ? measured.x + TextPadding.Horizontal : size.x, measured.y + TextPadding.Vertical),
                TextSizeMode.Fixed => useInitial ? measured + paddingVec : size,
                _ => size
            };

            if ((newSize - size).sqrMagnitude > 0.01f) size = newSize;

            _sizeInitialized = true;

            if (_sizeMode != TextSizeMode.Fixed &&
                anchor.IsAnyFlagSet(UIAnchorStyle.CenterHorizontal | UIAnchorStyle.CenterVertical))
                PerformLayout();
        }
        finally {
            _isInvalidating = false;
        }
    }

    public override void Awake() {
        base.Awake();
        _textHorizontalAlignment = UIHorizontalAlignment.Left;
    }

    public override Vector2 CalculateMinimumSize() {
        if (Font == null) return base.CalculateMinimumSize();
        var num = Font.size * _textScale * 0.75f;
        return Vector2.Max(base.CalculateMinimumSize(), new Vector2(num, num));
    }

    public override void Invalidate() {
        base.Invalidate();
        TextInvalidate();
    }

    protected override void OnTextPaddingChanged(Padding padding) {
        base.OnTextPaddingChanged(padding);
        Invalidate();
    }

    protected override void OnRebuildRenderData() {
        RenderBackground();
        RenderForeground();
        RenderText();
    }

    protected override void RenderText() {
        if (Font is null || !Font.isValid || string.IsNullOrEmpty(Text))
            return;
        if (_textRenderData is null) {
            _textRenderData = UIRenderData.Obtain();
            m_RenderData.Add(_textRenderData);
        }
        else {
            _textRenderData.Clear();
        }

        _textRenderData.material = TextAtlas.material;
        var flag = size.sqrMagnitude <= float.Epsilon;
        using var uiFontRenderer = ObtainRenderer();
        if (uiFontRenderer is UIDynamicFont.DynamicFontRenderer dynamicFontRenderer) {
            dynamicFontRenderer.spriteAtlas = TextAtlas;
            dynamicFontRenderer.spriteBuffer = _textRenderData;
        }

        uiFontRenderer.Render(Text, _textRenderData);
        if (_sizeMode == TextSizeMode.AutoSize || flag)
            size = uiFontRenderer.renderedSize.RoundToInt() + new Vector2(TextPadding.Horizontal, TextPadding.Vertical);
        else if (_sizeMode == TextSizeMode.AutoHeight) size = new Vector2(size.x, uiFontRenderer.renderedSize.y + TextPadding.Vertical).RoundToInt();
    }

    protected override float GetTextScaleMultiplier() {
        if (_textScaleMode == UITextScaleMode.None || !Application.isPlaying)
            return 1f;
        if (_textScaleMode == UITextScaleMode.ScreenResolution)
            return Screen.height / (float)GetUIView().fixedHeight;
        if (_sizeMode == TextSizeMode.AutoSize)
            return 1f;
        return size.y / _startSize.y;
    }

    protected override Color32 GetTextColor() => _textColors.GetValue(_state);

    public UIFontRenderer ObtainRenderer() {
        var vector = size - new Vector2(TextPadding.Horizontal, TextPadding.Vertical);
        var maxSize = vector;
        if (_sizeMode == TextSizeMode.AutoHeight)
            maxSize = new Vector2(vector.x, 4096f);
        var num = PixelsToUnits();
        var vector2 = (pivot.TransformToUpperLeft(size, arbitraryPivotOffset) + new Vector3(TextPadding.Left, -(float)TextPadding.Top)) * num;
        var textScale = _textScale * GetTextScaleMultiplier();
        var uiFontRenderer = Font.ObtainRenderer();
        uiFontRenderer.wordWrap = WordWrap;
        uiFontRenderer.maxSize = maxSize;
        uiFontRenderer.pixelRatio = num;
        uiFontRenderer.textScale = textScale;
        uiFontRenderer.characterSpacing = _characterSpacing;
        uiFontRenderer.vectorOffset = vector2.Quantize(num);
        uiFontRenderer.multiLine = true;
        uiFontRenderer.tabSize = _tabSize;
        uiFontRenderer.tabStops = _tabStops;
        uiFontRenderer.textAlign = _textHorizontalAlignment;
        uiFontRenderer.processMarkup = _processMarkup;
        uiFontRenderer.defaultColor = GetTextColor();
        uiFontRenderer.colorizeSprites = _colorizeSprites;
        uiFontRenderer.bottomColor = _useTextGradient ? new Color32?(GetGradientBottomColorForState()) : null;
        uiFontRenderer.overrideMarkupColors = !isEnabled;
        uiFontRenderer.opacity = CalculateOpacity();
        uiFontRenderer.outline = _useOutline;
        uiFontRenderer.outlineSize = _outlineSize;
        uiFontRenderer.outlineColor = _outlineColor;
        uiFontRenderer.shadow = _useDropShadow;
        uiFontRenderer.shadowColor = _dropShadowColor;
        uiFontRenderer.shadowOffset = _dropShadowOffset;
        if (TextVerticalAlignment != UIVerticalAlignment.Top)
            uiFontRenderer.vectorOffset = GetVertAlignOffset(uiFontRenderer);
        return uiFontRenderer;
    }

    public void SetWrappingWidth() => width = MeasureTextWidth();

    public Vector2 MeasureText() {
        using var fontRenderer = ObtainRenderer();
        return fontRenderer.MeasureString(Text);
    }

    public float MeasureTextWidth() => MeasureText().x;

    public float MeasureTextHeight() => MeasureText().y;

    private Vector3 GetVertAlignOffset(UIFontRenderer fontRenderer) {
        var num = PixelsToUnits();
        var vector = fontRenderer.MeasureString(Text) * num;
        var vectorOffset = fontRenderer.vectorOffset;
        var num2 = (height - TextPadding.Vertical) * num;
        if (vector.y >= num2) return vectorOffset;

        switch (TextVerticalAlignment) {
            case UIVerticalAlignment.Middle:
                vectorOffset.y -= (num2 - vector.y) * 0.5f;
                break;
            case UIVerticalAlignment.Bottom:
                vectorOffset.y -= num2 - vector.y;
                break;
        }

        return vectorOffset;
    }

    private string ComposeFullText() => string.Concat(_prefixText, _text, _suffixText);
}