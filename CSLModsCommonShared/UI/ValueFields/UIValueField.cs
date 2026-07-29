using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using CSLModsCommon.KeyBindings;
using CSLModsCommon.UI.Atlas;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CSLModsCommon.UI.ValueFields; 
public class UIValueField<T, TBinder> : UITextBase
    where TBinder : IValueBinder<T>
    where T : IComparable<T> {
    protected readonly TextDocument Document = new();
    protected TBinder Binder;


    public bool Multiline { get; set; } = false;
    public bool ReadOnly { get; set; } = false;
    public bool SelectOnFocus { get; set; } = false;
    public bool SubmitOnFocusLost { get; set; } = true;
    public bool IsPasswordField { get; set; } = false;
    public string PasswordCharacter { get; set; } = "*";
    public bool CanWheel { get; set; } = false;
    public bool NumericalOnly { get; set; } = false; // if true, only numeric chars allowed
    public int TextMaxLength { get; set; } = 1024;
    public int CursorWidth { get; set; } = 1;


    // render
    private float[] _charWidths = new float[0];
    private List<int> _lines = new() { 0 };
    private float _lineHeight = 1f;
    private bool _cursorShown = false;
    private float _cursorBlinkTime = 0.8f;
    private bool _hasImeInput = false;
    private string _compositionString => _hasImeInput ? Input.compositionString : string.Empty;

    // Events
    public event Action<UIValueField<T, TBinder>, T> EventValueChanged;
    public event Action<UIValueField<T, TBinder>, string> EventTextChanged;
    public event Action<UIValueField<T, TBinder>, string> EventTextSubmitted;
    public event Action<UIValueField<T, TBinder>, string> EventTextCancelled;

    // ctor style init
    public void InitializeBinder(TBinder binder) {
        Binder = binder;
        // init text from binder
        Document.Text = Binder?.ToText() ?? string.Empty;
    }

    public T Value {
        get => Binder != null ? Binder.Value : default;
        set {
            if (Binder == null) return;
            Binder.Value = value;
            Document.Text = Binder.ToText();
            EventValueChanged?.Invoke(this, Binder.Value);
            Invalidate();
        }
    }

    public override void Awake() {
        base.Awake();
        m_CanFocus = true;
        Document.OnChanged += OnDocumentChanged;
        Document.OnCursorChanged += () => Invalidate();
        Document.OnSelectionChanged += () => Invalidate();
    }

    public override void SetStyle(StyleType style) {
        if (style == StyleType.ControlPanelStyle) {
            _bgSprites.SetValues(SharedAtlasKeys.RoundRect6);
            _bgColors.SetValues(UIColors.BgElementColors);
            _bgColors.FocusedColor = UIColors.GreenNormal;
            SelectionSprite = SharedAtlasKeys.Rectangle;
        }
        else if (style == StyleType.OptionPanelStyle) {
            _bgSprites.SetValues(SharedAtlasKeys.RoundRect8);
            _bgColors.SetValues(UIColors.Bg1ElementColors);
            _bgColors.FocusedColor = UIColors.BlueNormal;
            SelectionSprite = SharedAtlasKeys.Rectangle;
        }
    }

    protected virtual void OnDocumentChanged() {
        // update binder only on submit, not on every change (but user may want live update)
        EventTextChanged?.Invoke(this, Document.Text);
        Invalidate();
    }

    public override void OnEnable() {
        base.OnEnable();
        if (size.magnitude == 0f) size = new Vector2(100f, 20f);
        _cursorShown = false;
    }

    // ----------------- input handlers -------------------

    protected override void OnGotFocus(UIFocusEventParameter p) {
        base.OnGotFocus(p);
        _hasImeInput = true;
        Input.imeCompositionMode = IMECompositionMode.On;
        if (SelectOnFocus && !ReadOnly) Document.SelectAll();
        _cursorShown = true;
        StartCoroutine(MakeCursorBlinkCoroutine());
        Invalidate();
    }

    protected override void OnLostFocus(UIFocusEventParameter p) {
        base.OnLostFocus(p);
        _hasImeInput = false;
        Input.imeCompositionMode = IMECompositionMode.Auto;
        if (!p.used) {
            if (SubmitOnFocusLost) Submit();
            else Cancel();
        }

        _cursorShown = false;
        Document.ClearSelection();
        Invalidate();
    }

    protected override void OnKeyPress(UIKeyEventParameter p) {
        if (!builtinKeyNavigation) {
            base.OnKeyPress(p);
            return;
        }

        if (ReadOnly) {
            base.OnKeyPress(p);
            return;
        }

        if (char.IsControl(p.character)) {
            base.OnKeyPress(p);
            return;
        }

        if (NumericalOnly && !IsDigitOrAllowed(p.character)) {
            base.OnKeyPress(p);
            return;
        }

        // accept char
        Document.SaveUndoPoint();
        if (Document.SelectionLength > 0) Document.DeleteSelection();
        if (Document.Text.Length < TextMaxLength) {
            Document.InsertChar(p.character);
        }

        p.Use();
    }

    protected virtual bool IsDigitOrAllowed(char c) {
        if (char.IsDigit(c)) return true;
        var decimalSep = LocaleManager.cultureInfo.NumberFormat.NumberDecimalSeparator;
        var neg = LocaleManager.cultureInfo.NumberFormat.NegativeSign;
        var s = c.ToString();
        return (AllowFloats && s == decimalSep && !Document.Text.Contains(decimalSep))
               || ((AllowFloats || AllowNegative) && s == neg && !Document.Text.Contains(neg));
    }

    public bool AllowFloats { get; set; } = true;
    public bool AllowNegative { get; set; } = true;

    protected override void OnKeyDown(UIKeyEventParameter p) {
        if (ReadOnly) return;
        if (p.used) return;

        var k = p.keycode;
        if (k == KeyCode.Backspace) {
            if (p.control) Document.DeletePreviousWord();
            else Document.DeletePreviousChar();
            p.Use();
            return;
        }

        if (k == KeyCode.Delete) {
            if (p.control) Document.DeleteNextWord();
            else Document.DeleteNextChar();
            p.Use();
            return;
        }

        if (k == KeyCode.LeftArrow) {
            if (p.control) Document.MoveCursor(Document.FindPreviousWord(Document.CursorIndex));
            else Document.MoveCursor(Document.CursorIndex - 1);
            if (p.shift) AdjustSelectionAfterMove();
            else Document.ClearSelection();
            p.Use();
            return;
        }

        if (k == KeyCode.RightArrow) {
            if (p.control) Document.MoveCursor(Document.FindNextWord(Document.CursorIndex));
            else Document.MoveCursor(Document.CursorIndex + 1);
            if (p.shift) AdjustSelectionAfterMove();
            else Document.ClearSelection();
            p.Use();
            return;
        }

        if (k == KeyCode.UpArrow && Multiline) {
            MoveToUpperLine();
            p.Use();
            return;
        }

        if (k == KeyCode.DownArrow && Multiline) {
            MoveToLowerLine();
            p.Use();
            return;
        }

        if (k == KeyCode.Home) {
            Document.MoveCursor(0);
            p.Use();
            return;
        }

        if (k == KeyCode.End) {
            Document.MoveCursor(Document.Text.Length);
            p.Use();
            return;
        }

        if (k == KeyCode.A && p.control) {
            Document.SelectAll();
            p.Use();
            return;
        }

        if (k == KeyCode.C && p.control) {
            if (Document.SelectionLength > 0) Clipboard.text = Document.SelectedText;
            p.Use();
            return;
        }

        if (k == KeyCode.X && p.control) {
            if (Document.SelectionLength > 0) {
                Clipboard.text = Document.SelectedText;
                Document.DeleteSelection();
            }

            p.Use();
            return;
        }

        if (k == KeyCode.V && p.control) {
            var t = Clipboard.text;
            if (!string.IsNullOrEmpty(t)) {
                // filter control chars except newline when multiline
                var filtered = new string(t.Where(ch => ch >= ' ' || (Multiline && ch == '\n')).ToArray());
                Document.SaveUndoPoint();
                Document.Insert(filtered);
            }

            p.Use();
            return;
        }

        if (p.control && k == KeyCode.Z) {
            Document.Undo();
            p.Use();
            return;
        }

        if (p.control && k == KeyCode.Y) {
            Document.Redo();
            p.Use();
            return;
        }

        if (k == KeyCode.Return) {
            if (Multiline && !p.control) {
                Document.Insert("\n");
            }
            else {
                Submit();
            }

            p.Use();
            return;
        }

        if (k == KeyCode.Escape) {
            Cancel();
            p.Use();
            return;
        }
    }

    protected void AdjustSelectionAfterMove() {
        // simple behaviour: when shift pressed, extend selection from previous cursor to current cursor
        // In a richer impl we track anchor; here we use a simple scheme:
        // if selection empty => selection start = previous cursor pos (can't know here), so we approximate:
        // we'll set selection start to 0 if none — but better: track anchor on mouse down or shift start. We'll keep simple:
        // For now: if no selection, set selection start to 0 (not ideal), but most navigation will be fine.
        // NOTE: You can extend this by storing a _mouse/keyboard anchor in UIValueField.
        if (Document.SelectionLength == 0) {
            // try reasonable anchor = 0 or Document.CursorIndex before move - we lack previous; skip for now
        }

        Invalidate();
    }

    // mouse
    protected override void OnMouseDown(UIMouseEventParameter p) {
        base.OnMouseDown(p);
        if (ReadOnly) return;
        if (!p.buttons.IsFlagSet(UIMouseButton.Left)) return;
        var idx = GetCharIndexAt(p);
        Document.MoveCursor(idx);
        Document.SetSelection(idx, idx);
        _mouseSelectionAnchor = idx;
        p.Use();
    }

    private int _mouseSelectionAnchor = 0;

    protected override void OnMouseMove(UIMouseEventParameter p) {
        if (!ReadOnly && hasFocus && p.buttons.IsFlagSet(UIMouseButton.Left)) {
            var idx = GetCharIndexAt(p);
            if (idx != Document.CursorIndex) {
                Document.MoveCursor(idx);
                Document.SetSelection(Math.Min(_mouseSelectionAnchor, idx), Math.Max(_mouseSelectionAnchor, idx));
                p.Use();
            }
        }
        else {
            base.OnMouseMove(p);
        }
    }

    protected override void OnMouseWheel(UIMouseEventParameter p) {
        base.OnMouseWheel(p);
        if (!CanWheel || Binder == null || ReadOnly) return;
        p.Use();
        var rate = GetSteppingRateFromModifiers();
        if (p.wheelDelta < 0) Binder.Decrease(rate);
        else Binder.Increase(rate);
        // reflect binder back to document
        Document.Text = Binder.ToText();
        EventValueChanged?.Invoke(this, Binder.Value);
        EventTextChanged?.Invoke(this, Document.Text);
        Invalidate();
    }

    private UIValueSteppingRate GetSteppingRateFromModifiers() {
        if (ModifierFlagsExtensions.IsShiftDown()) return UIValueSteppingRate.Fast;
        if (ModifierFlagsExtensions.IsControlDown()) return UIValueSteppingRate.Slow;
        return UIValueSteppingRate.Normal;
    }

    // keyboard -> submit/cancel
    public virtual void Submit() {
        // parse to binder
        if (Binder != null) {
            try {
                Binder.ParseFromText(Document.Text);
                EventValueChanged?.Invoke(this, Binder.Value);
            }
            catch {
                /* ignore parse errors */
            }
        }

        EventTextSubmitted?.Invoke(this, Document.Text);
        Invalidate();
        Unfocus();
    }

    public virtual void Cancel() {
        // revert to binder's text or previous undo text
        Document.Text = Binder != null ? Binder.ToText() : string.Empty;
        EventTextCancelled?.Invoke(this, Document.Text);
        Invalidate();
        Unfocus();
    }

    // ----------------- render --------------------

    // simplified rendering path: obtain char widths, wrap lines, render selection and cursor
    protected override void OnRebuildRenderData() {
        base.OnRebuildRenderData();
        if (_font == null || !_font.isValid) return;
        RenderBackground();
        WrapTextAndMeasure();
        // RenderTextInternal();
    }

    private void WrapTextAndMeasure() {
        using var r = _font.ObtainRenderer();
        var text = Document.Text + _compositionString;
        var pw = PixelsToUnits();
        _charWidths = r.GetCharacterWidths(text);
        _lineHeight = _font.lineHeight;
        if (Multiline) {
            _lines = CalculateLineBreaksForMultiline(text);
        }
        else {
            _lines = new List<int> { 0 };
        }
    }

    private List<int> CalculateLineBreaksForMultiline(string text) {
        var res = new List<int> { 0 };
        if (string.IsNullOrEmpty(text)) return res;
        var maxWidth = (size.x - TextPadding.Horizontal) * PixelsToUnits();
        var i = 0;
        while (i < text.Length) {
            // find the longest substring starting at i that fits
            var j = i;
            var width = 0f;
            while (j < text.Length && width + (j < _charWidths.Length ? _charWidths[j] : 0f) < maxWidth && text[j] != '\n') {
                width += j < _charWidths.Length ? _charWidths[j] : 0f;
                j++;
            }

            if (j == i && text[i] != '\n') {
                // forced single char
                j = i + 1;
            }

            // if newline occurs earlier, split there
            var newline = text.IndexOf('\n', i, Math.Max(0, j - i));
            if (newline >= 0) j = newline + 1;
            res.Add(j);
            i = j;
        }

        return res;
    }

    // protected virtual void RenderTextInternal() {
    //     if (_textAtlas == null) return;
    //
    //     if (_textRenderData != null) {
    //         _textRenderData.Clear();
    //     }
    //     else {
    //         _textRenderData = UIRenderData.Obtain();
    //         m_RenderData.Add(_textRenderData);
    //     }
    //
    //     _textRenderData.material = _textAtlas.material;
    //
    //     var num = PixelsToUnits();
    //     Vector2 contentSize = new Vector2(size.x - TextPadding.Horizontal, size.y - TextPadding.Vertical);
    //     var origin = pivot.TransformToUpperLeft(size, arbitraryPivotOffset);
    //     var baseOffset = new Vector3(origin.x + TextPadding.Left, origin.y - TextPadding.Top, 0f) * num;
    //
    //     string rawText = Document.Text + (_hasImeInput ? Input.compositionString : string.Empty);
    //     string displayText = IsPasswordField ? PasswordDisplayText(rawText) : rawText;
    //     var defaultColor = isEnabled ? _textColors.NormalColor : _textColors.DisabledColor;
    //     var textScaleMultiplier = GetTextScaleMultiplier();
    //
    //     using (var uiFontRenderer = _font.ObtainRenderer()) {
    //         uiFontRenderer.wordWrap = false;
    //         uiFontRenderer.pixelRatio = num;
    //         uiFontRenderer.textScale = _textScale * textScaleMultiplier;
    //         uiFontRenderer.characterSpacing = _characterSpacing;
    //         uiFontRenderer.processMarkup = _processMarkup;
    //         uiFontRenderer.colorizeSprites = _colorizeSprites;
    //         uiFontRenderer.defaultColor = defaultColor;
    //         uiFontRenderer.bottomColor = _useTextGradient ? new Color32?(GetGradientBottomColorForState()) : null;
    //         uiFontRenderer.overrideMarkupColors = false;
    //         uiFontRenderer.opacity = CalculateOpacity();
    //         uiFontRenderer.outline = _useOutline;
    //         uiFontRenderer.outlineSize = _outlineSize;
    //         uiFontRenderer.outlineColor = _outlineColor;
    //         uiFontRenderer.shadow = _useDropShadow;
    //         uiFontRenderer.shadowColor = _dropShadowColor;
    //         uiFontRenderer.shadowOffset = _dropShadowOffset;
    //         uiFontRenderer.multiLine = false;
    //         uiFontRenderer.textAlign = UIHorizontalAlignment.Left;
    //
    //         if (Multiline) {
    //             int visibleLines = Mathf.Max(1, Mathf.CeilToInt((size.y - TextPadding.Vertical) / _lineHeight));
    //             // iterate lines by start index stored in _lines (NOTE: _lines stores end indices beyond start)
    //             for (int li = 0; li < _lines.Count - 1; li++) {
    //                 int lineStart = _lines[li];
    //                 int lineLen = _lines[li + 1] - lineStart;
    //                 if (lineStart >= displayText.Length) continue;
    //                 int safeLen = Math.Min(lineLen, displayText.Length - lineStart);
    //                 string segment = displayText.Substring(lineStart, Math.Max(0, safeLen));
    //                 Vector3 lineOffset = baseOffset + new Vector3(0f, -(li - _lineScrollIndex) * _lineHeight * num, 0f);
    //                 uiFontRenderer.vectorOffset = lineOffset;
    //                 uiFontRenderer.Render(segment, _textRenderData);
    //             }
    //         }
    //         else {
    //             // single-line: render from scroll index
    //             int start = _scrollIndex;
    //             if (start < 0) start = 0;
    //             if (start >= displayText.Length) start = displayText.Length;
    //             string seg = displayText.Substring(start);
    //             uiFontRenderer.vectorOffset = baseOffset + new Vector3(_leftOffset * num, 0f, 0f);
    //             uiFontRenderer.Render(seg, _textRenderData);
    //         }
    //     }
    //
    //     if (Document.SelectionLength > 0) {
    //         RenderSelectionSimple(num, baseOffset, displayText);
    //     }
    //
    //     if (_cursorShown && Document.SelectionLength == 0 && hasFocus) {
    //         RenderCursorSimple(num, baseOffset, displayText);
    //     }
    // }

    private string PasswordDisplayText(string text) {
        if (string.IsNullOrEmpty(PasswordCharacter)) return text;
        return new string(PasswordCharacter[0], text.Length);
    }

    // private void EnsureCursorVisible() {
    //     if (Multiline) {
    //         int cursorLine = GetLineByIndex(Document.CursorIndex, true);
    //         int visibleLines = Mathf.Max(1, Mathf.FloorToInt((size.y - TextPadding.Vertical) / _lineHeight));
    //         if (cursorLine < _lineScrollIndex) _lineScrollIndex = cursorLine;
    //         else if (cursorLine >= _lineScrollIndex + visibleLines) _lineScrollIndex = cursorLine - visibleLines + 1;
    //         _lineScrollIndex = Mathf.Max(0, _lineScrollIndex);
    //     }
    //     else {
    //         float cursorWidth = TextWidth(0, Document.CursorIndex); // 字体内部单位
    //         float viewWidthUnits = (size.x - TextPadding.Horizontal) * PixelsToUnits();
    //         while (_scrollIndex < Document.Text.Length && cursorWidth - TextWidth(0, _scrollIndex) > viewWidthUnits) {
    //             _scrollIndex++;
    //         }
    //         while (_scrollIndex > 0 && cursorWidth - TextWidth(0, _scrollIndex - 1) <= 0f) {
    //             _scrollIndex--;
    //         }
    //         _leftOffset = 0f;
    //         _scrollIndex = Mathf.Clamp(_scrollIndex, 0, Document.Text.Length);
    //     }
    // }

    private void RenderSelectionSimple(float pixelsPerUnit, Vector3 origin, string displayText) {
        // naive selection render: compute left/right by summing char widths
        if (_bgAtlas is null || string.IsNullOrEmpty(SelectionSprite)) return;
        var selLeft = TextWidth(0, Document.SelectionStart) / pixelsPerUnit + origin.x;
        var selRight = TextWidth(0, Document.SelectionEnd) / pixelsPerUnit + origin.x;
        var top = origin.y;
        var bottom = origin.y - _lineHeight * pixelsPerUnit;
        var color = ApplyOpacity(SelectionBgColor);
        AddTriangles(_bgRenderData.triangles, _bgRenderData.vertices.Count);
        _bgRenderData.vertices.Add(new Vector3(selLeft, top));
        _bgRenderData.vertices.Add(new Vector3(selRight, top));
        _bgRenderData.vertices.Add(new Vector3(selRight, bottom));
        _bgRenderData.vertices.Add(new Vector3(selLeft, bottom));
        for (var i = 0; i < 4; i++) _bgRenderData.colors.Add(color);
        // uvs - use whole sprite
        var spriteInfo = _bgAtlas[SelectionSprite];
        var region = spriteInfo.region;
        _bgRenderData.uvs.Add(new Vector2(region.x, region.yMax));
        _bgRenderData.uvs.Add(new Vector2(region.xMax, region.yMax));
        _bgRenderData.uvs.Add(new Vector2(region.xMax, region.y));
        _bgRenderData.uvs.Add(new Vector2(region.x, region.y));
    }

    private void RenderCursorSimple(float pixelsPerUnit, Vector3 origin, string displayText) {
        if (_bgAtlas is null || string.IsNullOrEmpty(SelectionSprite)) return;
        var pos = TextWidth(0, Document.CursorIndex) / pixelsPerUnit + origin.x;
        var cw = GetUIView().ratio * CursorWidth * pixelsPerUnit;
        var top = origin.y;
        var bottom = origin.y - _lineHeight * pixelsPerUnit;
        AddTriangles(_bgRenderData.triangles, _bgRenderData.vertices.Count);
        _bgRenderData.vertices.Add(new Vector3(pos, top));
        _bgRenderData.vertices.Add(new Vector3(pos + cw, top));
        _bgRenderData.vertices.Add(new Vector3(pos + cw, bottom));
        _bgRenderData.vertices.Add(new Vector3(pos, bottom));
        var col = _textColors.NormalColor;
        for (var i = 0; i < 4; i++) _bgRenderData.colors.Add(col);
        var spriteInfo = _bgAtlas[SelectionSprite];
        var region = spriteInfo.region;
        _bgRenderData.uvs.Add(new Vector2(region.x, region.yMax));
        _bgRenderData.uvs.Add(new Vector2(region.xMax, region.yMax));
        _bgRenderData.uvs.Add(new Vector2(region.xMax, region.y));
        _bgRenderData.uvs.Add(new Vector2(region.x, region.y));
    }

    private float TextWidth(int begin, int end) {
        if (begin < 0 || end > Document.Text.Length || end <= begin) return 0f;
        float sum = 0f;
        for (int i = begin; i < end && i < _charWidths.Length; i++) sum += _charWidths[i];
        return sum;
    }

    private int GetCharIndexAt(UIMouseEventParameter p) {
        var hitPos = GetHitPosition(p);
        var pixelsToUnits = PixelsToUnits();
        var localX = hitPos.x - TextPadding.Left;
        var accum = 0f;
        var idx = 0;
        for (var i = 0; i < _charWidths.Length; i++) {
            accum += _charWidths[i] / pixelsToUnits;
            if (accum >= localX) {
                idx = i;
                break;
            }

            idx = i + 1;
        }

        return Mathf.Clamp(idx, 0, Document.Text.Length);
    }

    // cursor blink coroutine (simple)
    private System.Collections.IEnumerator MakeCursorBlinkCoroutine() {
        if (!Application.isPlaying) yield break;
        _cursorShown = true;
        while (hasFocus) {
            yield return new WaitForSeconds(_cursorBlinkTime);
            _cursorShown = !_cursorShown;
            Invalidate();
        }

        _cursorShown = false;
    }

    // helpers to move vertically - best-effort simple implementations
    private void MoveToUpperLine() =>
        // find line start and move to previous line same x
        // simplified: just move to start of text
        Document.MoveCursor(0);

    private void MoveToLowerLine() => Document.MoveCursor(Document.Text.Length);

    // helper to get composition cursor pos for IME (rough)
    private void SetIMEPosition() {
        var uiView = GetUIView();
        var num = uiView.PixelsToUnits();
        var vector = pivot.TransformToUpperLeft(size, arbitraryPivotOffset);
        var pos = new Vector3(vector.x + TextPadding.Left + TextWidth(0, Document.CursorIndex) / num, vector.y - TextPadding.Top, 0f);
        var screen = uiView.uiCamera.WorldToScreenPoint(transform.position);
        screen.y = Screen.height - screen.y;
        Input.compositionCursorPos = new Vector2(screen.x + pos.x * (uiView.uiCamera.pixelWidth / uiView.fixedWidth), screen.y + pos.y * (uiView.uiCamera.pixelHeight / uiView.fixedHeight));
    }

    // utility for selection sprite and background triangles (copied from earlier)
    private void AddTriangles(PoolList<int> triangles, int baseIndex) {
        foreach (var t in Sprite.TriangleIndices) triangles.Add(t + baseIndex);
    }

    // for selection sprite and color - expose some UI properties to match your prior class
    public string SelectionSprite { get; set; } = string.Empty;
    public Color32 SelectionBgColor { get; set; } = new Color32(34, 42, 58, 255);

    // mouse enter/leave toggles wheel availability (same as your previous)
    protected override void OnMouseEnter(UIMouseEventParameter p) {
        base.OnMouseEnter(p);
        WheelAvailable = true;
        Invalidate();
    }

    protected override void OnMouseLeave(UIMouseEventParameter p) {
        base.OnMouseLeave(p);
        WheelAvailable = false;
        Invalidate();
    }

    protected bool WheelAvailable { get; private set; } = false;

    // additional utilities: get char widths, measure string etc can be expanded as needed
    // expose Document for advanced operations:
    public TextDocument GetDocument() => Document;
}