using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using CSLModsCommon.KeyBindings;
using CSLModsCommon.Localization;
using CSLModsCommon.UI.Atlas;
using ICities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CSLModsCommon.UI;

public abstract class ValueFieldBase<T, V> : UITextBase where T : IComparable<T> where V : ValueFieldBase<T, V> {
    protected bool _renderBg = true;
    private bool _hasImeInput;
    private int _selectionStart;
    private int _selectionEnd;
    protected string _selectionSprite = string.Empty;
    protected Color32 _selectionBgColor = new(34, 42, 58, 255);
    protected int _textMaxLength = 1024;
    protected bool _readOnly;
    protected bool _isPasswordField;
    protected string _passwordCharacter = "*";
    private bool _undoing;
    public const int UndoLimit = 20;
    private readonly List<UndoData> _undoData = new(UndoLimit);
    private int _undoCount;
    private int _cursorIndex;
    private bool _focusForced;
    private string _undoText = "";
    private int _scrollIndex;
    private bool _cursorShown;
    private int _mouseSelectionAnchor;
    private float[] _charWidths;
    private float _leftOffset;
    private int _lineScrollIndex;
    private List<int> _lines = new();
    private float _lineHeight;
    private bool _cursorAtEndOfLine;
    protected T _value;
    protected string _format = "{0}";
    private bool _showTooltip;
    private bool _autoHeight;

    public event UIElementEventHandler<V, T> EventValueChanged;
    public event UIElementEventHandler<V, string> EventTextChanged;
    public event UIElementEventHandler<V, string> EventPasswordCharacterChanged;
    public event UIElementEventHandler<V, bool> EventReadOnlyChanged;
    public event UIElementEventHandler<V, string> EventTextSubmitted;
    public event UIElementEventHandler<V, string> EventTextCancelled;

    public bool AutoHeight {
        get => _autoHeight;
        set {
            if (_autoHeight == value) return;
            _autoHeight = value;
            AutoHeightChanged();
            Invalidate();
        }
    }

    public bool RenderBg {
        get => _renderBg;
        set {
            if (_renderBg == value) return;
            _renderBg = value;
            AutoHeightChanged();
            Invalidate();
        }
    }

    private string CompositionString => _hasImeInput ? Input.compositionString : string.Empty;

    public override string Text {
        get => _text;
        set {
            if (value.Length > TextMaxLength)
                value = value.Substring(0, TextMaxLength);
            value = value.Replace("\t", " ");
            if (value == _text) return;
            _text = value;
            _scrollIndex = _cursorIndex = 0;
            OnTextChanged();
            Invalidate();
            AutoHeightChanged();
        }
    }

    public int SelectionStart {
        get => _selectionStart;
        set {
            if (value == _selectionStart) return;
            _selectionStart = Mathf.Max(0, Mathf.Min(value, _text.Length));
            _selectionEnd = Mathf.Max(_selectionEnd, _selectionStart);
            Invalidate();
        }
    }

    public int SelectionEnd {
        get => _selectionEnd;
        set {
            if (value == _selectionEnd) return;
            _selectionEnd = Mathf.Max(0, Mathf.Min(value, _text.Length));
            _selectionStart = Mathf.Max(_selectionStart, _selectionEnd);
            Invalidate();
        }
    }

    public int SelectionLength => _selectionEnd - _selectionStart;

    public string SelectedText => _selectionEnd == _selectionStart ? string.Empty : _text.Substring(SelectionStart, SelectionLength);

    public string SelectionSprite {
        get => _selectionSprite;
        set {
            if (value == _selectionSprite) return;
            _selectionSprite = value;
            Invalidate();
        }
    }

    public Color32 SelectionBgColor {
        get => _selectionBgColor;
        set {
            _selectionBgColor = value;
            Invalidate();
        }
    }

    public int TextMaxLength {
        get => _textMaxLength;
        set {
            if (value == _textMaxLength) return;
            _textMaxLength = Mathf.Max(0, value);
            if (TextMaxLength < _text.Length)
                Text = _text.Substring(0, TextMaxLength);
            Invalidate();
        }
    }

    public bool ReadOnly {
        get => _readOnly;
        set {
            if (value == _readOnly) return;
            _readOnly = value;
            OnReadOnlyChanged();
            Invalidate();
        }
    }

    public bool IsPasswordField {
        get => _isPasswordField;
        set {
            if (value == _isPasswordField) return;
            _isPasswordField = value;
            Invalidate();
        }
    }

    public string PasswordCharacter {
        get => _passwordCharacter;
        set {
            _passwordCharacter = !string.IsNullOrEmpty(value) ? value[0].ToString() : value;
            OnPasswordCharacterChanged();
            Invalidate();
        }
    }

    public bool Multiline { get; set; }
    public bool SubmitOnFocusLost { get; set; } = true;
    public bool SelectOnFocus { get; set; }
    public bool NumericalOnly { get; set; }
    public bool AllowFloats { get; set; }
    public bool AllowNegative { get; set; }
    public float CursorBlinkTime { get; set; } = 0.8f;
    public int CursorWidth { get; set; } = 1;
    public bool CallEventValueChanged { get; set; } = true;

    public T Value {
        get => _value;
        set => OnValueChanged(value);
    }

    public T MinValue { get; set; }
    public T MaxValue { get; set; }
    public bool UseMinValueLimit { get; set; }
    public bool UseMaxValueLimit { get; set; }

    public bool UseValueLimit {
        set => UseMinValueLimit = UseMaxValueLimit = value;
    }

    public bool CanWheel { get; set; }

    public T WheelStep { get; set; }

    public string Format {
        get => _format;
        set {
            _format = value;
            RefreshText();
        }
    }

    public bool ShowTooltip {
        get => _showTooltip;
        set {
            _showTooltip = value;
            if (value) tooltip = SharedTranslations.ScrollWheel;
        }
    }

    protected bool WheelAvailable { get; set; }
    public float CursorHeight { get; set; } = 1;
    public bool CustomCursorHeight { get; set; }

    private void AutoHeightChanged() {
        if (!_autoHeight || string.IsNullOrEmpty(Text)) return;
        var fr = _font.ObtainRenderer();
        Height = fr.MeasureString(Text).y + TextPadding.Vertical;
    }

    public override void Awake() {
        base.Awake();
        m_CanFocus = true;
        _textColors.SetValues(UIColors.MajorTextElementColors);
        _textAtlas = _bgAtlas = Atlases.Shared;
    }

    public override void SetStyle(StyleType style) {
        if (style == StyleType.ControlPanelStyle) {
            _bgSprites.SetValues(SharedAtlasKeys.RoundRect6);
            _bgColors.SetValues(UIColors.BgElementColors);
            _bgColors.FocusedColor = UIColors.GreenNormal;
            _selectionSprite = SharedAtlasKeys.Rectangle;
        } else if (style == StyleType.OptionPanelStyle) {
            _bgSprites.SetValues(SharedAtlasKeys.RoundRect8);
            _bgColors.SetValues(UIColors.Bg1ElementColors);
            _bgColors.FocusedColor = UIColors.BlueNormal;
            _selectionSprite = SharedAtlasKeys.Rectangle;
        }
    }

    protected virtual void RefreshText() {
        if (hasFocus)
            Text = _value != null ? string.Format(_format, _value.ToString()) : string.Empty;
        else
            Text = string.Format(_format, _value?.ToString() ?? string.Empty);
    }

    protected virtual void OnValueChanged(T value) {
        if (UseMinValueLimit && value.CompareTo(MinValue) < 0)
            _value = MinValue;
        else if (UseMaxValueLimit && value.CompareTo(MaxValue) > 0)
            _value = MaxValue;
        else
            _value = value;

        RefreshText();
        if (CallEventValueChanged)
            EventValueChanged?.Invoke(this as V, _value);
    }

    protected virtual void OnTextChanged() {
        if (!_undoing) {
            _undoData.RemoveRange(_undoData.Count - _undoCount, _undoCount);
            _undoData.Add(new UndoData { _text = Text, _position = _cursorIndex });
            _undoCount = 0;
            if (UndoLimit != 0 && UndoLimit <= _undoData.Count) _undoData.RemoveAt(0);
        }

        FilterText();
        EventTextChanged?.Invoke(this as V, Text);
        Invoke("OnTextChanged", new object[] { Text });
    }

    private void FilterText() {
        if (PlatformService.apiBackend != APIBackend.Rail || string.IsNullOrEmpty(_text) || NumericalOnly || IsPasswordField) return;
        var text = PlatformService.DirtyWordsFilter(_text, true);
        if (text != null) _text = text;
    }

    protected virtual void OnPasswordCharacterChanged() {
        EventPasswordCharacterChanged?.Invoke(this as V, PasswordCharacter);
        Invoke("OnPasswordCharacterChanged", new object[] { PasswordCharacter });
    }

    protected virtual void OnReadOnlyChanged() {
        EventReadOnlyChanged?.Invoke(this as V, ReadOnly);
        Invoke("OnReadOnlyChanged", new object[] { ReadOnly });
    }

    protected virtual void OnSubmit() {
        _focusForced = true;
        Unfocus();
        EventTextSubmitted?.Invoke(this as V, Text);
        if (!hasFocus && _text == Convert.ToString(Value)) {
            RefreshText();
            return;
        }

        var newValue = _value;
        try {
            if (typeof(T) == typeof(string))
                newValue = (T)(object)_text;
            else if (!string.IsNullOrEmpty(_text))
                newValue = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(_text);
        } catch { }

        OnValueChanged(newValue);
        InvokeUpward("OnTextSubmitted", Text);
    }

    protected virtual void OnCancel() {
        _focusForced = true;
        _text = _undoText;
        Unfocus();
        EventTextCancelled?.Invoke(this as V, Text);
        InvokeUpward("OnTextCancelled", new object[] { this, Text });
    }

    private bool IsDigit(char character) {
        var numberDecimalSeparator = LocaleManager.cultureInfo.NumberFormat.NumberDecimalSeparator;
        var negativeSign = LocaleManager.cultureInfo.NumberFormat.NegativeSign;
        return char.IsDigit(character) || (AllowFloats && character.ToString() == numberDecimalSeparator && !_text.Contains(numberDecimalSeparator)) || ((AllowFloats || AllowNegative) && character.ToString() == negativeSign && !_text.Contains(negativeSign));
    }

    protected override void OnKeyPress(UIKeyEventParameter p) {
        if (!builtinKeyNavigation) {
            base.OnKeyPress(p);
            return;
        }

        if (ReadOnly || char.IsControl(p.character)) {
            base.OnKeyPress(p);
            return;
        }

        if (NumericalOnly && !IsDigit(p.character)) {
            base.OnKeyPress(p);
            return;
        }

        base.OnKeyPress(p);
        if (p.used) return;

        DeleteSelection();
        SetIMEPosition();
        if (_text.Length < TextMaxLength) {
            if (_cursorIndex == _text.Length)
                _text += p.character;
            else
                _text = _text.Insert(_cursorIndex, p.character.ToString());

            _cursorIndex++;
            OnTextChanged();
            Invalidate();
        }

        p.Use();
    }

    public void Cancel() {
        ClearSelection();
        _cursorIndex = _scrollIndex = 0;
        Invalidate();
        OnCancel();
    }

    protected override void OnKeyDown(UIKeyEventParameter p) {
        if (ReadOnly) return;

        if (p.used) return;

        var keycode = p.keycode;
        if (keycode <= KeyCode.Escape) {
            if (keycode != KeyCode.Backspace) {
                if (keycode != KeyCode.Return) {
                    if (keycode == KeyCode.Escape) {
                        ClearSelection();
                        _cursorIndex = _scrollIndex = 0;
                        Invalidate();
                        OnCancel();
                        goto IL_45E;
                    }
                } else {
                    if (Multiline) {
                        AddLineBreak();
                        goto IL_45E;
                    }

                    OnSubmit();
                    goto IL_45E;
                }
            } else {
                if (p.control) {
                    if (builtinKeyNavigation)
                        DeletePreviousWord();
                    goto IL_45E;
                }

                if (builtinKeyNavigation)
                    DeletePreviousChar();
                goto IL_45E;
            }
        } else if (keycode <= KeyCode.Z) {
            switch (keycode) {
                case KeyCode.A:
                    if (p.control) SelectAll();

                    goto IL_45E;
                case KeyCode.B:
                    break;
                case KeyCode.C:
                    if (p.control) CopySelectionToClipboard();

                    goto IL_45E;
                default:
                    switch (keycode) {
                        case KeyCode.V: {
                                if (!p.control) goto IL_45E;

                                var text = Clipboard.text;
                                if (!string.IsNullOrEmpty(text)) PasteAtCursor(text);

                                goto IL_45E;
                            }
                        case KeyCode.X:
                            if (p.control) CutSelectionToClipboard();

                            goto IL_45E;
                        case KeyCode.Y:
                            if (p.control) {
                                _undoing = true;
                                try {
                                    _undoCount--;
                                    ClearSelection();
                                    Text = _undoData[_undoData.Count - _undoCount - 1]._text;
                                    _cursorIndex = _undoData[_undoData.Count - _undoCount - 1]._position;
                                } catch {
                                    _undoCount++;
                                }

                                _undoing = false;
                            }

                            goto IL_45E;
                        case KeyCode.Z:
                            if (p.control) {
                                _undoing = true;
                                try {
                                    _undoCount++;
                                    ClearSelection();
                                    Text = _undoData[_undoData.Count - _undoCount - 1]._text;
                                    _cursorIndex = _undoData[_undoData.Count - _undoCount - 1]._position;
                                } catch {
                                    _undoCount--;
                                }

                                _undoing = false;
                            }

                            goto IL_45E;
                    }

                    break;
            }
        } else if (keycode != KeyCode.Delete) {
            switch (keycode) {
                case KeyCode.UpArrow:
                    if (!Multiline) goto IL_45E;

                    if (p.shift) {
                        MoveSelectionPointUp();
                        goto IL_45E;
                    }

                    MoveToUpChar();
                    goto IL_45E;
                case KeyCode.DownArrow:
                    if (!Multiline) goto IL_45E;

                    if (p.shift) {
                        MoveSelectionPointDown();
                        goto IL_45E;
                    }

                    MoveToDownChar();
                    goto IL_45E;
                case KeyCode.RightArrow:
                    if (p.control) {
                        if (p.shift) {
                            MoveSelectionPointRightWord();
                            goto IL_45E;
                        }

                        MoveToNextWord();
                        goto IL_45E;
                    } else {
                        if (p.shift) {
                            MoveSelectionPointRight();
                            goto IL_45E;
                        }

                        if (SelectionLength > 0) {
                            MoveToSelectionEnd();
                            goto IL_45E;
                        }

                        MoveToNextChar();
                        goto IL_45E;
                    }
                case KeyCode.LeftArrow:
                    if (p.control) {
                        if (p.shift) {
                            MoveSelectionPointLeftWord();
                            goto IL_45E;
                        }

                        MoveToPreviousWord();
                        goto IL_45E;
                    } else {
                        if (p.shift) {
                            MoveSelectionPointLeft();
                            goto IL_45E;
                        }

                        if (SelectionLength > 0) {
                            MoveToSelectionStart();
                            goto IL_45E;
                        }

                        MoveToPreviousChar();
                        goto IL_45E;
                    }
                case KeyCode.Insert: {
                        if (!p.shift) goto IL_45E;

                        var text2 = Clipboard.text;
                        if (!string.IsNullOrEmpty(text2)) PasteAtCursor(text2);

                        goto IL_45E;
                    }
                case KeyCode.Home:
                    if (p.shift) {
                        SelectToStart();
                        goto IL_45E;
                    }

                    MoveToStart();
                    goto IL_45E;
                case KeyCode.End:
                    if (p.shift) {
                        SelectToEnd();
                        goto IL_45E;
                    }

                    MoveToEnd();
                    goto IL_45E;
            }
        } else {
            if (_selectionStart != _selectionEnd) {
                DeleteSelection();
                goto IL_45E;
            }

            if (p.control) {
                DeleteNextWord();
                goto IL_45E;
            }

            DeleteNextChar();
            goto IL_45E;
        }

        base.OnKeyDown(p);
        return;
    IL_45E:
        p.Use();
    }

    protected override void OnGotFocus(UIFocusEventParameter p) {
        base.OnGotFocus(p);
        _hasImeInput = true;
        Input.imeCompositionMode = IMECompositionMode.On;
        SetIMEPosition();
        _undoText = Text;
        if (!ReadOnly) {
            StartCoroutine(MakeCursorBlink());
            if (SelectOnFocus) {
                _selectionStart = 0;
                _selectionEnd = _text.Length;
            }
        }

        Invalidate();
    }

    protected override void OnLostFocus(UIFocusEventParameter p) {
        base.OnLostFocus(p);
        _hasImeInput = false;
        Input.imeCompositionMode = IMECompositionMode.Auto;
        if (!_focusForced) {
            if (SubmitOnFocusLost)
                OnSubmit();
            else
                OnCancel();
        }

        _focusForced = false;
        _cursorShown = false;
        ClearSelection();
        Invalidate();
    }

    protected override void OnDoubleClick(UIMouseEventParameter p) {
        if (!ReadOnly && p.buttons.IsFlagSet(UIMouseButton.Left)) {
            var charIndexAt = GetCharIndexAt(p);
            SelectWordAtIndex(charIndexAt);
        }

        base.OnDoubleClick(p);
    }

    protected override void OnClick(UIMouseEventParameter p) {
        p.Use();
        base.OnClick(p);
    }

    protected override void OnMouseDown(UIMouseEventParameter p) {
        base.OnMouseDown(p);
        if (!ReadOnly && p.buttons.IsFlagSet(UIMouseButton.Left)) {
            var charIndexAt = GetCharIndexAt(p);
            if (charIndexAt != _cursorIndex) {
                _cursorIndex = charIndexAt;
                _cursorShown = true;
                Invalidate();
                p.Use();
            }

            _mouseSelectionAnchor = _cursorIndex;
            _selectionStart = _selectionEnd = _cursorIndex;
        }
    }

    protected override void OnMouseLeave(UIMouseEventParameter p) {
        base.OnMouseLeave(p);
        WheelAvailable = false;
        Invalidate();
    }

    protected override void OnMouseEnter(UIMouseEventParameter p) {
        base.OnMouseEnter(p);
        WheelAvailable = true;
        Invalidate();
    }

    protected override void OnMouseWheel(UIMouseEventParameter p) {
        base.OnMouseWheel(p);
        tooltipBox.Hide();
        if (CanWheel) {
            p.Use();
            var typeRate = GetSteppingRate();
            if (p.wheelDelta < 0)
                OnValueChanged(ValueDecrease(typeRate));
            else
                OnValueChanged(ValueIncrease(typeRate));
        }
    }

    protected abstract T ValueDecrease(UIValueSteppingRate steppingRate);
    protected abstract T ValueIncrease(UIValueSteppingRate steppingRate);
    protected abstract T GetStep(UIValueSteppingRate steppingRate);

    private UIValueSteppingRate GetSteppingRate() {
        if (ModifierFlagsExtensions.IsShiftDown()) return UIValueSteppingRate.Fast;
        else if (ModifierFlagsExtensions.IsControlDown()) return UIValueSteppingRate.Slow;
        else return UIValueSteppingRate.Normal;
    }

    private void SetIMEPosition() {
        var uiView = GetUIView();
        var num = uiView.PixelsToUnits();
        var num2 = 0f;
        var num3 = _scrollIndex;
        while (num3 < _cursorIndex && num3 < _charWidths.Length) {
            num2 += _charWidths[num3] / num;
            num3++;
        }

        var vector = pivot.TransformToUpperLeft(size, arbitraryPivotOffset);
        Vector3 vector2 = new(vector.x + TextPadding.Left, vector.y - TextPadding.Top, 0f);
        var num4 = num2 + _leftOffset / num + TextPadding.Left;
        float num5 = uiView.uiCamera.pixelWidth / uiView.fixedWidth;
        float num6 = uiView.uiCamera.pixelHeight / uiView.fixedHeight;
        var vector3 = uiView.uiCamera.WorldToScreenPoint(transform.position);
        vector3.y = Screen.height - vector3.y;
        Vector2 compositionCursorPos = new(vector3.x + (vector2.x + num4) * num5, vector3.y + (vector2.y + size.y * 1.5f) * num6);
        Input.compositionCursorPos = compositionCursorPos;
    }

    protected override void OnMouseUp(UIMouseEventParameter p) {
        base.OnMouseUp(p);
        if (!ReadOnly && p.buttons.IsFlagSet(UIMouseButton.Left) && PlatformService.ShowGamepadTextInput(IsPasswordField ? GamepadTextInputMode.TextInputModePassword : GamepadTextInputMode.TextInputModeNormal, GamepadTextInputLineMode.TextInputLineModeSingleLine, "Input", TextMaxLength, Text)) {
            p.Use();
            PlatformService.eventSteamGamepadInputDismissed += OnSteamInputDismissed;
        }
    }

    private void OnSteamInputDismissed(string str) {
        PlatformService.eventSteamGamepadInputDismissed -= OnSteamInputDismissed;
        if (str != null) {
            Text = str;
            OnSubmit();
        }

        MoveToEnd();
        Unfocus();
    }


    private int GetCharIndexAt(UIMouseEventParameter p) {
        var hitPosition = GetHitPosition(p);
        var num = PixelsToUnits();
        int num2;
        if (Multiline) {
            var lineByVerticalPosition = GetLineByVerticalPosition(hitPosition.y);
            num2 = GetIndexByHorizontalPosition(hitPosition.x, lineByVerticalPosition);
        } else {
            num2 = _scrollIndex;
            var num3 = _leftOffset / num + TextPadding.Left;
            for (var i = _scrollIndex; i < _charWidths.Length; i++) {
                num3 += _charWidths[i] / num;
                if (num3 < hitPosition.x) num2++;
            }
        }

        return num2;
    }

    private IEnumerator MakeCursorBlink() {
        if (!Application.isPlaying) yield break;
        _cursorShown = true;
        while (hasFocus) {
            yield return new WaitForSeconds(CursorBlinkTime);
            _cursorShown = !_cursorShown;
            Invalidate();
        }

        _cursorShown = false;
    }

    public void ClearSelection() {
        _selectionStart = 0;
        _selectionEnd = 0;
        _mouseSelectionAnchor = 0;
    }

    public void MoveToStart() {
        ClearSelection();
        SetCursorPos(0);
    }

    public void MoveToEnd() {
        ClearSelection();
        SetCursorPos(_text.Length);
    }

    public void MoveToNextChar() {
        ClearSelection();
        SetCursorPos(_cursorIndex + 1);
    }

    public void MoveToPreviousChar() {
        ClearSelection();
        SetCursorPos(_cursorIndex - 1);
    }

    public void MoveToNextWord() {
        ClearSelection();
        if (_cursorIndex == _text.Length) return;

        var cursorPos = FindNextWord(_cursorIndex);
        SetCursorPos(cursorPos);
    }

    public void MoveToPreviousWord() {
        ClearSelection();
        if (_cursorIndex == 0) return;

        var cursorPos = FindPreviousWord(_cursorIndex);
        SetCursorPos(cursorPos);
    }

    public int FindPreviousWord(int startIndex) {
        int i;
        for (i = startIndex; i > 0; i--) {
            var c = _text[i - 1];
            if (!char.IsWhiteSpace(c) && !char.IsSeparator(c) && !char.IsPunctuation(c)) break;
        }

        for (var j = i; j >= 0; j--) {
            if (j == 0) {
                i = 0;
                break;
            }

            var c2 = _text[j - 1];
            if (char.IsWhiteSpace(c2) || char.IsSeparator(c2) || char.IsPunctuation(c2)) {
                i = j;
                break;
            }
        }

        return i;
    }

    public int FindNextWord(int startIndex) {
        var length = _text.Length;
        var i = startIndex;
        for (var j = i; j < length; j++) {
            var c = _text[j];
            if (char.IsWhiteSpace(c) || char.IsSeparator(c) || char.IsPunctuation(c)) {
                i = j;
                while (i < length) {
                    var c2 = _text[i];
                    if (!char.IsWhiteSpace(c2) && !char.IsSeparator(c2) && !char.IsPunctuation(c2)) break;

                    i++;
                }

                return i;
            }

            if (j == length - 1) i = length;
        }

        while (i < length) {
            var c2 = _text[i];
            if (!char.IsWhiteSpace(c2) && !char.IsSeparator(c2) && !char.IsPunctuation(c2)) break;

            i++;
        }

        return i;
    }

    public void MoveSelectionPointRightWord() {
        if (_cursorIndex == _text.Length) return;

        var num = FindNextWord(_cursorIndex);
        if (_selectionEnd == _selectionStart) {
            _selectionStart = _cursorIndex;
            _selectionEnd = num;
        } else if (_selectionEnd == _cursorIndex) {
            _selectionEnd = num;
        } else if (_selectionStart == _cursorIndex) {
            _selectionStart = num;
        }

        SetCursorPos(num);
    }

    public void MoveSelectionPointLeftWord() {
        if (_cursorIndex == 0) return;

        var num = FindPreviousWord(_cursorIndex);
        if (_selectionEnd == _selectionStart) {
            _selectionEnd = _cursorIndex;
            _selectionStart = num;
        } else if (_selectionEnd == _cursorIndex) {
            _selectionEnd = num;
        } else if (_selectionStart == _cursorIndex) {
            _selectionStart = num;
        }

        SetCursorPos(num);
    }

    public void MoveSelectionPointRight() {
        if (_cursorIndex == _text.Length) return;

        if (_selectionEnd == _selectionStart) {
            _selectionEnd = _cursorIndex + 1;
            _selectionStart = _cursorIndex;
        } else if (_selectionEnd == _cursorIndex) {
            _selectionEnd++;
        } else if (_selectionStart == _cursorIndex) {
            _selectionStart++;
        }

        SetCursorPos(_cursorIndex + 1);
    }

    public void MoveSelectionPointLeft() {
        if (_cursorIndex == 0) return;

        if (_selectionEnd == _selectionStart) {
            _selectionEnd = _cursorIndex;
            _selectionStart = _cursorIndex - 1;
        } else if (_selectionEnd == _cursorIndex) {
            _selectionEnd--;
        } else if (_selectionStart == _cursorIndex) {
            _selectionStart--;
        }

        SetCursorPos(_cursorIndex - 1);
    }

    public void MoveToSelectionEnd() {
        ClearSelection();
        SetCursorPos(_selectionEnd);
    }

    public void MoveToSelectionStart() {
        ClearSelection();
        SetCursorPos(_selectionStart);
    }

    public void SelectAll() {
        _selectionStart = 0;
        _selectionEnd = _text.Length;
        _scrollIndex = 0;
        SetCursorPos(0);
    }

    public void SelectToStart() {
        if (_cursorIndex == 0) return;

        if (_selectionEnd == _selectionStart)
            _selectionEnd = _cursorIndex;
        else if (_selectionEnd == _cursorIndex) _selectionEnd = _selectionStart;

        _selectionStart = 0;
        SetCursorPos(0);
    }

    public void SelectToEnd() {
        if (_cursorIndex == _text.Length) return;

        if (_selectionEnd == _selectionStart)
            _selectionStart = _cursorIndex;
        else if (_selectionStart == _cursorIndex) _selectionStart = _selectionEnd;

        _selectionEnd = _text.Length;
        SetCursorPos(_text.Length);
    }

    public void SelectWordAtIndex(int index) {
        if (_text.Length == 0) return;

        index = Mathf.Max(Mathf.Min(_text.Length - 1, index), 0);
        var c = _text[index];
        if (!char.IsLetterOrDigit(c)) {
            _selectionStart = index;
            _selectionEnd = index + 1;
            _mouseSelectionAnchor = 0;
        } else {
            _selectionStart = index;
            var num = index;
            while (num > 0 && char.IsLetterOrDigit(_text[num - 1])) {
                _selectionStart--;
                num--;
            }

            _selectionEnd = index;
            var num2 = index;
            while (num2 < _text.Length && char.IsLetterOrDigit(_text[num2])) {
                _selectionEnd = num2 + 1;
                num2++;
            }
        }

        _cursorIndex = _selectionStart;
        Invalidate();
    }

    private void CutSelectionToClipboard() {
        CopySelectionToClipboard();
        DeleteSelection();
    }

    private void CopySelectionToClipboard() {
        if (_selectionStart == _selectionEnd) return;

        Clipboard.text = _text.Substring(_selectionStart, SelectionLength);
    }

    private void PasteAtCursor(string clipData) {
        DeleteSelection();
        StringBuilder stringBuilder = new(_text.Length + clipData.Length);
        stringBuilder.Append(_text);
        foreach (var c in clipData.Where(c => c >= ' ')) stringBuilder.Insert(_cursorIndex++, c);

        stringBuilder.Length = Mathf.Min(stringBuilder.Length, TextMaxLength);
        _text = stringBuilder.ToString();
        OnTextChanged();
        SetCursorPos(_cursorIndex);
    }

    private void SetCursorPos(int index) {
        index = Mathf.Max(0, Mathf.Min(_text.Length, index));
        if (index == _cursorIndex) return;

        _cursorIndex = index;
        _cursorShown = hasFocus;
        _scrollIndex = Mathf.Min(_scrollIndex, _cursorIndex);
        Invalidate();
    }

    private void DeleteSelection() {
        if (_selectionStart == _selectionEnd) return;

        _text = _text.Remove(_selectionStart, SelectionLength);
        SetCursorPos(_selectionStart);
        ClearSelection();
        OnTextChanged();
        Invalidate();
    }

    private void DeleteNextChar() {
        ClearSelection();
        if (_cursorIndex >= _text.Length) return;

        _text = _text.Remove(_cursorIndex, 1);
        _cursorShown = true;
        OnTextChanged();
        Invalidate();
    }

    private void DeletePreviousChar() {
        if (_selectionStart != _selectionEnd) {
            DeleteSelection();
            SetCursorPos(_selectionStart);
            return;
        }

        ClearSelection();
        if (_cursorIndex == 0) return;

        _text = _text.Remove(_cursorIndex - 1, 1);
        _cursorIndex--;
        _cursorShown = true;
        OnTextChanged();
        Invalidate();
    }

    private void DeleteNextWord() {
        ClearSelection();
        if (_cursorIndex == _text.Length) return;

        var num = FindNextWord(_cursorIndex);
        if (num == _cursorIndex) num = _text.Length;

        _text = _text.Remove(_cursorIndex, num - _cursorIndex);
        OnTextChanged();
        Invalidate();
    }

    private void DeletePreviousWord() {
        ClearSelection();
        if (_cursorIndex == 0) return;

        var num = FindPreviousWord(_cursorIndex);
        if (num == _cursorIndex) num = 0;

        _text = _text.Remove(num, _cursorIndex - num);
        OnTextChanged();
        SetCursorPos(num);
    }

    public override void OnEnable() {
        base.OnEnable();
        if (size.magnitude == 0f) size = new Vector2(100f, 20f);

        _cursorShown = false;
        _cursorIndex = _scrollIndex = _lineScrollIndex = 0;
        var flag = _font != null && _font.isValid;
        if (Application.isPlaying && !flag) _font = GetUIView().defaultFont;
    }

    public override void Update() {
        base.Update();
        if (!string.IsNullOrEmpty(Input.compositionString))
            Invalidate();
        CheckForLostFocus();
    }

    protected override void OnRebuildRenderData() {
        if (_font == null || !_font.isValid) return;
        RenderBackground();
        WrapText();
        RenderText();
    }

    private string PasswordDisplayText(string text) => new string(PasswordCharacter[0], text.Length);

    protected override void RenderBackground() {
        if (!RenderBg) return;
        base.RenderBackground();
    }

    protected override void RenderText() {
        if (_textAtlas is null) return;
        if (_textRenderData != null) {
            _textRenderData.Clear();
        } else {
            _textRenderData = UIRenderData.Obtain();
            m_RenderData.Add(_textRenderData);
        }

        _textRenderData.material = _textAtlas.material;
        var num = PixelsToUnits();
        Vector2 vector = new(size.x - TextPadding.Horizontal, size.y - TextPadding.Vertical);
        var vector2 = pivot.TransformToUpperLeft(size, arbitraryPivotOffset);
        var vector3 = new Vector3(vector2.x + TextPadding.Left, vector2.y - TextPadding.Top, 0f) * num;
        var text = _text + CompositionString;
        var text2 = IsPasswordField && !string.IsNullOrEmpty(PasswordCharacter) ? PasswordDisplayText(text) : text;
        var defaultColor = isEnabled ? _textColors.NormalColor : _textColors.DisabledColor;
        var textScaleMultiplier = GetTextScaleMultiplier();
        var vector4 = vector * num;
        var num2 = 0;
        if (Multiline) {
            _cursorIndex = Mathf.Min(_cursorIndex, text2.Length);
            _scrollIndex = 0;
            var num3 = Mathf.Max(1, Mathf.CeilToInt((size.y - TextPadding.Vertical) / _lineHeight) - 1);
            var lineByIndex = GetLineByIndex(_cursorIndex, true);
            _lineScrollIndex = Mathf.Min(_lineScrollIndex, lineByIndex);
            _lineScrollIndex = Mathf.Max(_lineScrollIndex, lineByIndex - (num3 - 1));
            num2 = Mathf.Min(_lineScrollIndex + num3 - 1, _lines.Count - 1);
        } else {
            _cursorIndex = Mathf.Min(_cursorIndex, text2.Length);
            _scrollIndex = Mathf.Min(Mathf.Min(_scrollIndex, _cursorIndex), text2.Length);
            _lineScrollIndex = 0;
            _leftOffset = 0f;
            if (_textHorizontalAlignment == UIHorizontalAlignment.Left) {
                var num4 = 0f;
                for (var i = _scrollIndex; i < _cursorIndex; i++) num4 += _charWidths[i];

                while (num4 >= vector4.x) {
                    if (_scrollIndex >= _cursorIndex) break;

                    num4 -= _charWidths[_scrollIndex++];
                }
            } else {
                _scrollIndex = Mathf.Max(0, Mathf.Min(_cursorIndex, text2.Length - 1));
                var num5 = 0f;
                var num6 = _font.size * 1.25f * num;
                while (_scrollIndex > 0 && num5 < vector4.x - num6) num5 += _charWidths[_scrollIndex--];

                var num7 = text2.Length > 0 ? TextWidth(_scrollIndex, text.Length) : 0f;
                switch (_textHorizontalAlignment) {
                    case UIHorizontalAlignment.Center:
                        _leftOffset = Mathf.Max(0f, (vector4.x - num7) * 0.5f);
                        break;
                    case UIHorizontalAlignment.Right:
                        _leftOffset = Mathf.Max(0f, vector4.x - num7);
                        break;
                }
            }
        }

        if (_selectionEnd != _selectionStart) RenderSelection();

        for (var j = _lineScrollIndex; j <= num2; j++) {
            using var uiFontRenderer = _font.ObtainRenderer();
            uiFontRenderer.wordWrap = WordWrap;
            uiFontRenderer.maxSize = vector;
            uiFontRenderer.pixelRatio = num;
            uiFontRenderer.textScale = _textScale * textScaleMultiplier;
            uiFontRenderer.characterSpacing = _characterSpacing;
            uiFontRenderer.vectorOffset = vector3;
            uiFontRenderer.multiLine = false;
            uiFontRenderer.textAlign = UIHorizontalAlignment.Left;
            uiFontRenderer.processMarkup = _processMarkup;
            uiFontRenderer.colorizeSprites = _colorizeSprites;
            uiFontRenderer.defaultColor = defaultColor;
            uiFontRenderer.bottomColor = _useTextGradient ? new Color32?(GetGradientBottomColorForState()) : null;
            uiFontRenderer.overrideMarkupColors = false;
            uiFontRenderer.opacity = CalculateOpacity();
            uiFontRenderer.outline = _useOutline;
            uiFontRenderer.outlineSize = _outlineSize;
            uiFontRenderer.outlineColor = _outlineColor;
            uiFontRenderer.shadow = _useDropShadow;
            uiFontRenderer.shadowColor = _dropShadowColor;
            uiFontRenderer.shadowOffset = _dropShadowOffset;
            if (Multiline) {
                var text3 = text2.Substring(_lines[j], LineLenght(j));
                Vector3 b = new(CalculateLineLeftOffset(j), -(float)(j - _lineScrollIndex) * _lineHeight * num);
                var vectorOffset = vector3 + b;
                uiFontRenderer.vectorOffset = vectorOffset;
                uiFontRenderer.Render(text3, _textRenderData);
            } else {
                vector3.x += _leftOffset;
                uiFontRenderer.vectorOffset = vector3;
                uiFontRenderer.Render(text2.Substring(_scrollIndex), _textRenderData);
            }
        }

        if (_cursorShown && _selectionEnd == _selectionStart) RenderCursor();
    }

    private void RenderSelection() {
        if (string.IsNullOrEmpty(SelectionSprite) || _bgAtlas is null || _bgAtlas[SelectionSprite] is null) return;

        var num = PixelsToUnits();
        var vector = (pivot.TransformToUpperLeft(size, arbitraryPivotOffset) + new Vector3(TextPadding.Left, -(float)TextPadding.Top)) * num;
        var vector2 = new Vector3(size.x - TextPadding.Horizontal, size.y - TextPadding.Vertical) * num;
        var num2 = Mathf.Max(GetLineByIndex(_selectionStart), _lineScrollIndex);
        var b = Mathf.Min(Mathf.FloorToInt((size.y - TextPadding.Vertical) / _lineHeight) + _lineScrollIndex, _lines.Count - 1);
        var num3 = Mathf.Min(GetLineByIndex(_selectionEnd), b);
        using var uIFontRenderer = _font.ObtainRenderer();
        var textHeight = uIFontRenderer.MeasureString(Text).y;
        for (var i = num2; i <= num3; i++) {
            var num4 = CalculateLineLeftOffset(i);
            var begin = _lines[i] + _scrollIndex;
            float left;
            if (_selectionStart < _lines[i])
                left = vector.x + num4;
            else
                left = vector.x + num4 + TextWidth(begin, _selectionStart);

            var num5 = vector.y - (i - _lineScrollIndex) * _lineHeight * num;
            var bottom = CustomCursorHeight ? vector.y - CursorHeight * num : Multiline ? Mathf.Max(vector.y - vector2.y, num5 - _lineHeight * num) : vector.y - textHeight * num;
            float right;
            if (_selectionEnd > _lines[i] + LineLenght(i))
                right = vector.x + num4 + TextWidth(begin, _lines[i] + LineLenght(i));
            else
                right = Mathf.Min(vector.x + num4 + TextWidth(begin, _selectionEnd), vector.x + vector2.x);

            RenderSelectionBox(left, num5, right, bottom);
        }
    }

    private void RenderSelectionBox(float left, float top, float right, float bottom) {
        AddTriangles(_bgRenderData.triangles, _bgRenderData.vertices.Count);
        Vector3 item = new(left, top);
        Vector3 item2 = new(right, top);
        Vector3 item3 = new(left, bottom);
        Vector3 item4 = new(right, bottom);
        _bgRenderData.vertices.Add(item);
        _bgRenderData.vertices.Add(item2);
        _bgRenderData.vertices.Add(item4);
        _bgRenderData.vertices.Add(item3);
        var item5 = ApplyOpacity(SelectionBgColor);
        _bgRenderData.colors.Add(item5);
        _bgRenderData.colors.Add(item5);
        _bgRenderData.colors.Add(item5);
        _bgRenderData.colors.Add(item5);
        var spriteInfo = _bgAtlas[SelectionSprite];
        var region = spriteInfo.region;
        var num = region.width / spriteInfo.pixelSize.x;
        var num2 = region.height / spriteInfo.pixelSize.y;
        _bgRenderData.uvs.Add(new Vector2(region.x + num, region.yMax - num2));
        _bgRenderData.uvs.Add(new Vector2(region.xMax - num, region.yMax - num2));
        _bgRenderData.uvs.Add(new Vector2(region.xMax - num, region.y + num2));
        _bgRenderData.uvs.Add(new Vector2(region.x + num, region.y + num2));
    }

    private void RenderCursor() {
        if (string.IsNullOrEmpty(SelectionSprite) || _bgAtlas is null) return;

        var num = PixelsToUnits();
        using var uIFontRenderer = _font.ObtainRenderer();
        var b = m_Pivot.TransformToUpperLeft(size, arbitraryPivotOffset) * num;
        var lineByIndex = GetLineByIndex(_cursorIndex, true);
        var num2 = -(float)TextPadding.Top * num - (lineByIndex - _lineScrollIndex) * _lineHeight * num;
        var num3 = (CalculateLineLeftOffset(lineByIndex) + TextWidth(_lines[lineByIndex] + _scrollIndex, _cursorIndex) + TextPadding.Left * num).Quantize(num);
        var num4 = num * GetUIView().ratio * CursorWidth;
        var num5 = CustomCursorHeight ? CursorHeight * num : Multiline ? Mathf.Min(_lineHeight * num, (size.y - TextPadding.Vertical) * num) : uIFontRenderer.MeasureString(Text).y * num;
        var tl = new Vector3(num3, num2) + b;
        var tr = new Vector3(num3 + num4, num2) + b;
        var bl = new Vector3(num3, num2 - num5) + b;
        var br = new Vector3(num3 + num4, num2 - num5) + b;
        RenderCursorAt(tl, tr, bl, br);
    }

    private void RenderCursorAt(Vector3 tl, Vector3 tr, Vector3 bl, Vector3 br) {
        var vertices = _bgRenderData.vertices;
        var triangles = _bgRenderData.triangles;
        var uvs = _bgRenderData.uvs;
        var colors = _bgRenderData.colors;
        AddTriangles(triangles, vertices.Count);
        vertices.Add(tl);
        vertices.Add(tr);
        vertices.Add(br);
        vertices.Add(bl);
        var item = _textColors.NormalColor;
        colors.Add(item);
        colors.Add(item);
        colors.Add(item);
        colors.Add(item);
        var spriteInfo = _bgAtlas[SelectionSprite];
        var region = spriteInfo.region;
        uvs.Add(new Vector2(region.x, region.yMax));
        uvs.Add(new Vector2(region.xMax, region.yMax));
        uvs.Add(new Vector2(region.xMax, region.y));
        uvs.Add(new Vector2(region.x, region.y));
    }

    private void AddTriangles(PoolList<int> triangles, int baseIndex) {
        foreach (var t in Sprite.TriangleIndices) triangles.Add(t + baseIndex);
    }

    private void WrapText() {
        _lineHeight = _font.lineHeight;
        var d = PixelsToUnits();
        Vector2 maxSize = new(size.x - TextPadding.Horizontal, size.y - TextPadding.Vertical);
        var vector = pivot.TransformToUpperLeft(size, arbitraryPivotOffset);
        var vectorOffset = new Vector3(vector.x + TextPadding.Left, vector.y - TextPadding.Top, 0f) * d;
        var text = _text + CompositionString;
        var text2 = IsPasswordField && !string.IsNullOrEmpty(PasswordCharacter) ? PasswordDisplayText(text) : text;
        var defaultColor = isEnabled ? _textColors.NormalColor : _textColors.DisabledColor;
        var textScaleMultiplier = GetTextScaleMultiplier();
        using (var uiFontRenderer = _font.ObtainRenderer()) {
            uiFontRenderer.wordWrap = WordWrap;
            uiFontRenderer.maxSize = maxSize;
            uiFontRenderer.pixelRatio = PixelsToUnits();
            uiFontRenderer.textScale = _textScale * textScaleMultiplier;
            uiFontRenderer.characterSpacing = _characterSpacing;
            uiFontRenderer.vectorOffset = vectorOffset;
            uiFontRenderer.multiLine = false;
            uiFontRenderer.textAlign = UIHorizontalAlignment.Left;
            uiFontRenderer.processMarkup = _processMarkup;
            uiFontRenderer.colorizeSprites = _colorizeSprites;
            uiFontRenderer.defaultColor = defaultColor;
            uiFontRenderer.bottomColor = _useTextGradient ? _gradientBottomNormalColor : _gradientBottomDisabledColor;
            uiFontRenderer.overrideMarkupColors = false;
            uiFontRenderer.opacity = CalculateOpacity();
            uiFontRenderer.outline = _useOutline;
            uiFontRenderer.outlineSize = _outlineSize;
            uiFontRenderer.outlineColor = _outlineColor;
            uiFontRenderer.shadow = _useDropShadow;
            uiFontRenderer.shadowColor = _dropShadowColor;
            uiFontRenderer.shadowOffset = _dropShadowOffset;
            _charWidths = uiFontRenderer.GetCharacterWidths(text2);
            _lineHeight = _font.lineHeight;
        }

        if (Multiline) {
            _lines = CalculateLineBreaks(GetWords());
            return;
        }

        _lines = new List<int> { 0 };
    }

    private List<int> CalculateLineBreaks(List<int> words) {
        List<int> list = new() { 0 };
        var num = 0;
        var num2 = (size.x - TextPadding.Horizontal) * PixelsToUnits();
        var count = words.Count;
        var num3 = 0;
        while (num3 < count && words[num3] != _text.Length) {
            var num4 = num3 == count - 1 ? _text.Length : words[num3 + 1];
            if (_text[words[num3]] == '\n') {
                list.Add(words[num3] + 1);
                num++;
            } else if (TextWidth(list[num], num4) >= num2) {
                if (words[num3] != list[num]) {
                    list.Add(words[num3]);
                    num++;
                }

                var num5 = list[num];
                for (var i = num5; i < num4; i++)
                    if (TextWidth(list[num], i + 1) >= num2) {
                        list.Add(i);
                        num++;
                    }
            }

            num3++;
        }

        return list;
    }

    private List<int> GetWords() {
        List<int> list = new() { 0 };
        var num = 0;
        var flag = false;
        while (!flag) {
            var num2 = FindNextWord(num);
            for (var i = FindNextLineBreak(num); i < num2; i = FindNextLineBreak(i + 1))
                if (i != 0)
                    list.Add(i);

            if (num2 == _text.Length) {
                flag = true;
            } else {
                list.Add(num2);
                num = num2;
            }
        }

        return list;
    }

    private int FindNextLineBreak(int start) {
        var num = start;
        while (num < _text.Length && _text[num] != '\n') num++;

        return num;
    }

    private int LineLenght(int line) {
        var count = _lines.Count;
        if (line < 0 || line >= count) return 0;

        if (line == count - 1) return _text.Length - _lines[line];

        return _lines[line + 1] - _lines[line];
    }

    private float TextWidth(int begin, int end) {
        if (begin < 0 || end > _text.Length || end <= begin) return 0f;

        var num = 0f;
        var num2 = begin;
        while (num2 < end && num2 != _text.Length) {
            num += _charWidths[num2];
            num2++;
        }

        return num;
    }

    private int GetLineByIndex(int index, bool cursor = false) {
        var count = _lines.Count;
        if (index == _text.Length) return count - 1;

        var num = 0;
        while (num < count && index >= _lines[num] + LineLenght(num)) num++;

        if (cursor && _cursorAtEndOfLine && index == _lines[num] && LineLenght(num) > 0) return Mathf.Max(0, num - 1);

        _cursorAtEndOfLine = false;
        return num;
    }

    private float CalculateLineLeftOffset(int line) {
        if (!Multiline) return _leftOffset;

        var num = TextWidth(_lines[line], _lines[line] + LineLenght(line));
        var num2 = (size.x - TextPadding.Horizontal) * PixelsToUnits();
        var result = 0f;
        switch (_textHorizontalAlignment) {
            case UIHorizontalAlignment.Left:
                result = 0f;
                break;
            case UIHorizontalAlignment.Center:
                result = Mathf.Max(0f, (num2 - num) * 0.5f);
                break;
            case UIHorizontalAlignment.Right:
                result = Mathf.Max(0f, num2 - num);
                break;
        }

        return result;
    }

    public void MoveToUpChar() {
        ClearSelection();
        SetCursorPos(FindUpperIndex(_cursorIndex));
    }

    public void MoveToDownChar() {
        ClearSelection();
        SetCursorPos(FindLowerIndex(_cursorIndex));
    }

    public void MoveSelectionPointDown() {
        var num = FindLowerIndex(_cursorIndex);
        if (_selectionEnd == _selectionStart) {
            _selectionEnd = num;
            _selectionStart = _cursorIndex;
        } else if (_selectionEnd == _cursorIndex) {
            _selectionEnd = num;
        } else if (_selectionStart == _cursorIndex) {
            if (num <= _selectionEnd) {
                _selectionStart = num;
            } else {
                _selectionStart = _selectionEnd;
                _selectionEnd = num;
            }
        }

        SetCursorPos(num);
    }

    public void MoveSelectionPointUp() {
        var num = FindUpperIndex(_cursorIndex);
        if (_selectionEnd == _selectionStart) {
            _selectionStart = num;
            _selectionEnd = _cursorIndex;
        } else if (_selectionStart == _cursorIndex) {
            _selectionStart = num;
        } else if (_selectionEnd == _cursorIndex) {
            if (num >= _selectionStart) {
                _selectionEnd = num;
            } else {
                _selectionEnd = _selectionStart;
                _selectionStart = num;
            }
        }

        SetCursorPos(num);
    }

    private int FindLowerIndex(int index) {
        var lineByIndex = GetLineByIndex(index, true);
        if (lineByIndex >= _lines.Count - 1) return _text.Length;

        return GetIndexByHorizontalPosition(GetHorizontalPositionByIndex(index), lineByIndex + 1);
    }

    private int FindUpperIndex(int index) {
        var lineByIndex = GetLineByIndex(index, true);
        if (lineByIndex <= 0) return 0;

        return GetIndexByHorizontalPosition(GetHorizontalPositionByIndex(index), lineByIndex - 1);
    }

    private int GetIndexByHorizontalPosition(float positionX, int line) {
        if (line < 0) return 0;

        if (line >= _lines.Count) return _text.Length;

        var num = PixelsToUnits();
        var num2 = TextPadding.Left + CalculateLineLeftOffset(line) / num;
        var i = _lines[line];
        var num3 = _lines[line] + LineLenght(line);
        while (i < num3) {
            num2 += _charWidths[i] / num;
            if (num2 > positionX || _text[i] == '\n') break;

            i++;
        }

        if (i == num3) _cursorAtEndOfLine = true;

        return i;
    }

    private float GetHorizontalPositionByIndex(int index) {
        var lineByIndex = GetLineByIndex(index);
        var num = PixelsToUnits();
        var num2 = CalculateLineLeftOffset(lineByIndex) / num + TextPadding.Left;
        for (var i = _lines[lineByIndex]; i < index; i++) num2 += _charWidths[i] / num;

        return num2;
    }

    private void AddLineBreak() {
        if (_text.Length >= TextMaxLength) return;
        DeleteSelection();
        if (_cursorIndex == _text.Length)
            _text += '\n';
        else
            _text = _text.Insert(_cursorIndex, '\n'.ToString());

        _cursorIndex++;
        OnTextChanged();
        Invalidate();
    }

    private int GetLineByVerticalPosition(float positionY) {
        var a = _lineScrollIndex + Mathf.FloorToInt((positionY - TextPadding.Top) / _lineHeight) + _scrollIndex;
        a = Mathf.Min(a, _lines.Count - 1);
        return Mathf.Max(a, 0);
    }

    protected override void OnMouseMove(UIMouseEventParameter p) {
        if (!ReadOnly && hasFocus && p.buttons.IsFlagSet(UIMouseButton.Left)) {
            var charIndexAt = GetCharIndexAt(p);
            if (charIndexAt != _cursorIndex) {
                _cursorIndex = charIndexAt;
                _cursorShown = true;
                Invalidate();
                p.Use();
                _selectionStart = Mathf.Min(_mouseSelectionAnchor, charIndexAt);
                _selectionEnd = Mathf.Max(_mouseSelectionAnchor, charIndexAt);
                return;
            }
        }

        base.OnMouseMove(p);
    }

    private void CheckForLostFocus() {
        if (!isVisible) return;
        if (!Input.GetMouseButtonDown(0)) return;

        var camera = GetCamera();
        var ray = camera.ScreenPointToRay(Input.mousePosition);
        if (Raycast(ray))
            return;

        Unfocus();
    }

    public class UndoData {
        public string _text;
        public int _position;
    }
}