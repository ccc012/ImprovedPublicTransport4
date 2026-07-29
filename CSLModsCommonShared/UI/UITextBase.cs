using ColossalFramework.UI;
using CSLModsCommon.Manager;
using CSLModsCommon.UI.Extensions;
using UnityEngine;

namespace CSLModsCommon.UI;

public abstract class UITextBase : UIStateElement {
    protected UIFont _font;
    protected string _text = string.Empty;
    protected float _textScale = 1f;
    protected Padding _textPadding;
    protected UITextScaleMode _textScaleMode;
    protected int _characterSpacing;
    protected bool _processMarkup;
    protected bool _useOutline;
    protected int _outlineSize = 1;
    protected Color32 _outlineColor = Color.black;
    protected bool _useTextGradient;
    protected Color32 _gradientBottomNormalColor = Color.white;
    protected Color32 _gradientBottomDisabledColor = Color.white;
    protected bool _useDropShadow;
    protected Color32 _dropShadowColor = Color.black;
    protected Vector2 _dropShadowOffset;
    protected Vector2 _startSize;
    private bool _isFontCallbackAssigned;
    protected bool _colorizeSprites;
    protected UIHorizontalAlignment _textHorizontalAlignment = UIHorizontalAlignment.Center;
    protected UIVerticalAlignment _textVerticalAlignment = UIVerticalAlignment.Middle;
    protected bool _wordWrap;
    protected LocalizedString _localizedId;

    public LocalizedString LocalizedId {
        get => _localizedId;
        set {
            _localizedId = value;
            if (value is null) {
                ValidateLocalizedEvent(false);
                return;
            }

            Text = _localizedId.Value;
            ValidateLocalizedEvent(true);
        }
    }

    public UIFont Font {
        get => _font ??= GetUIView()?.defaultFont;
        set {
            if (value == _font) return;
            UnbindTextureRebuildCallback();
            _font = value;
            BindTextureRebuildCallback();
            Invalidate();
        }
    }

    public virtual string Text {
        get => _text;
        set {
            if (value == _text) return;
            UIFontManager.Invalidate(Font);
            _text = value;
            Invalidate();
        }
    }

    public float TextScale {
        get => _textScale;
        set {
            value = Mathf.Max(0.1f, value);
            if (Mathf.Approximately(_textScale, value)) return;
            UIFontManager.Invalidate(Font);
            _textScale = value;
            Invalidate();
        }
    }

    public Padding TextPadding {
        get => _textPadding;
        set {
            if (_textPadding.Equals(value)) return;
            _textPadding = value;
            Invalidate();
        }
    }

    public UITextScaleMode TextScaleMode {
        get => _textScaleMode;
        set {
            if (value == _textScaleMode) return;
            _textScaleMode = value;
            Invalidate();
        }
    }

    public int CharacterSpacing {
        get => _characterSpacing;
        set {
            value = Mathf.Max(0, value);
            if (value == _characterSpacing) return;
            _characterSpacing = value;
            Invalidate();
        }
    }

    public bool ProcessMarkup {
        get => _processMarkup;
        set {
            if (value == _processMarkup) return;
            _processMarkup = value;
            Invalidate();
        }
    }

    public bool UseOutline {
        get => _useOutline;
        set {
            if (value == _useOutline) return;
            _useOutline = value;
            Invalidate();
        }
    }

    public int OutlineSize {
        get => _outlineSize;
        set {
            value = Mathf.Max(0, value);
            if (value == _outlineSize) return;
            _outlineSize = value;
            Invalidate();
        }
    }

    public Color32 OutlineColor {
        get => _outlineColor;
        set {
            if (_outlineColor.EqualsColor(value)) return;
            _outlineColor = value;
            Invalidate();
        }
    }

    public bool UseTextGradient {
        get => _useTextGradient;
        set {
            if (value == _useTextGradient) return;
            _useTextGradient = value;
            Invalidate();
        }
    }

    public Color32 GradientBottomNormalColor {
        get => _gradientBottomNormalColor;
        set {
            if (_gradientBottomNormalColor.EqualsColor(value)) return;
            _gradientBottomNormalColor = value;
            OnColorChanged();
        }
    }

    public Color32 GradientBottomDisabledColor {
        get => _gradientBottomDisabledColor;
        set {
            if (_gradientBottomDisabledColor.EqualsColor(value)) return;
            _gradientBottomDisabledColor = value;
            OnColorChanged();
        }
    }

    public bool UseDropShadow {
        get => _useDropShadow;
        set {
            if (value == _useDropShadow) return;
            _useDropShadow = value;
            Invalidate();
        }
    }

    public Color32 DropShadowColor {
        get => _dropShadowColor;
        set {
            if (_dropShadowColor.EqualsColor(value)) return;
            _dropShadowColor = value;
            Invalidate();
        }
    }

    public Vector2 DropShadowOffset {
        get => _dropShadowOffset;
        set {
            if (value == _dropShadowOffset) return;
            _dropShadowOffset = value;
            Invalidate();
        }
    }

    public bool ColorizeSprites {
        get => _colorizeSprites;
        set {
            if (value == _colorizeSprites) return;
            _colorizeSprites = value;
            Invalidate();
        }
    }

    public UIHorizontalAlignment TextHorizontalAlignment {
        get => autoSize ? UIHorizontalAlignment.Left : _textHorizontalAlignment;
        set {
            if (value == _textHorizontalAlignment) return;
            _textHorizontalAlignment = value;
            Invalidate();
        }
    }

    public virtual UIVerticalAlignment TextVerticalAlignment {
        get => _textVerticalAlignment;
        set {
            if (value == _textVerticalAlignment) return;
            _textVerticalAlignment = value;
            Invalidate();
        }
    }

    public bool WordWrap {
        get => _wordWrap;
        set {
            if (value == _wordWrap) return;
            _wordWrap = value;
            Invalidate();
        }
    }

    protected virtual void OnTextPaddingChanged(Padding padding) { }

    public override void Awake() {
        base.Awake();
        _startSize = size;
        _textPadding = Padding.GetZeroPadding(this, OnTextPaddingChanged);
    }

    public override void OnEnable() {
        base.OnEnable();
        BindTextureRebuildCallback();
    }

    public override void OnDisable() {
        base.OnDisable();
        UnbindTextureRebuildCallback();
    }

    public override void OnDestroy() {
        _textPadding.DetachParent(OnTextPaddingChanged);
        base.OnDestroy();
    }

    private void ValidateLocalizedEvent(bool isLocalizing) {
        if (isLocalizing)
            LocalizationManager.ModActiveLocaleChanged += OnModLocaleChanged;
        else
            LocalizationManager.ModActiveLocaleChanged -= OnModLocaleChanged;
    }

    private void OnModLocaleChanged(string locale, LocalizationManager manager) {
        if (!string.IsNullOrEmpty(_localizedId)) Text = LocalizedId.Value;
    }

    protected virtual Color32 GetTextColor() => isEnabled ? TextColors.NormalColor : TextColors.DisabledColor;

    protected virtual Color32 GetGradientBottomColorForState() => isEnabled ? GradientBottomNormalColor : GradientBottomDisabledColor;

    protected virtual float GetTextScaleMultiplier() {
        if (TextScaleMode == UITextScaleMode.None || !Application.isPlaying) return 1f;

        if (TextScaleMode == UITextScaleMode.ScreenResolution) return (float)Screen.height / GetUIView().fixedHeight;

        return size.y / _startSize.y;
    }

    private void BindTextureRebuildCallback() {
        if (_isFontCallbackAssigned || Font is null) return;
        if (Font is not UIDynamicFont) return;
        UnityEngine.Font.textureRebuilt += OnFontTextureRebuilt;
        _isFontCallbackAssigned = true;
    }

    private void UnbindTextureRebuildCallback() {
        if (!_isFontCallbackAssigned || Font is null) return;
        if (Font is UIDynamicFont)
            UnityEngine.Font.textureRebuilt -= OnFontTextureRebuilt;
        _isFontCallbackAssigned = false;
    }

    private void OnFontTextureRebuilt(Font font) {
        RequestCharacterInfo();
        Invalidate();
    }

    public virtual void UpdateFontInfo() => RequestCharacterInfo();

    protected virtual void RequestCharacterInfo() {
        if (Font is not UIDynamicFont uiDynamicFont || !UIFontManager.IsDirty(Font) || string.IsNullOrEmpty(Text)) return;
        var num = TextScale * GetTextScaleMultiplier();
        var fontSize = Mathf.CeilToInt(Font.size * num);
        uiDynamicFont.AddCharacterRequest(Text, fontSize, FontStyle.Normal);
    }
}