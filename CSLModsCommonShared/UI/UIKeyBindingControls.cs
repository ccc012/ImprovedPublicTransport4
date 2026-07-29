using ColossalFramework.UI;
using CSLModsCommon.Collections;
using CSLModsCommon.KeyBindings;
using CSLModsCommon.Localization;
using CSLModsCommon.Manager;
using CSLModsCommon.UI.Atlas;
using CSLModsCommon.UI.Buttons;
using CSLModsCommon.UI.Containers;
using System;
using System.Linq;
using UnityEngine;

namespace CSLModsCommon.UI;

public class UIKeyBindingControls : LiteContainer {
    public event Action<KeyBinding> BindingChanged;

    private NormalButton _inputButton;
    private NormalButton _resetButton;
    private NormalButton _clearButton;
    private KeyBinding _binding;
    private BindingPhase _bindingPhase = BindingPhase.Idle;
    private string _originalText;
    private ReusableList<string> _reusableList;

    public KeyBinding Binding {
        get => _binding;
        set {
            if (value is null || value.Equals(_binding)) return;
            _binding = value;
            OnBindingChanged(value);
        }
    }

    public override void Awake() {
        base.Awake();
        _autoLayout = true;
        _columnGap = 6;
        size = new Vector2(280, 30);
        _direction = FlexDirection.Row;
        _autoFitChildrenHorizontally = true;
        AddControls();
        _reusableList = ReusableList<string>.Rent();
    }

    public override void OnDestroy() {
        base.OnDestroy();
        _reusableList.Return();
    }

    private void OnBindingChanged(KeyBinding binding) {
        if (_inputButton is null) return;
        _inputButton.Text = binding.ToString();
        BindingChanged?.Invoke(_binding);
    }

    private void AddControls() {
        _inputButton = AddUIComponent<NormalButton>();
        _inputButton.canFocus = true;
        _inputButton.SetStyle(StyleType.OptionPanelStyle);
        _inputButton.autoSize = false;
        _inputButton.size = new Vector2(280, 30);
        _inputButton.Text = "Key Binding";
        _inputButton.eventMouseDown += OnBindingMouseDown;
        _inputButton.eventKeyDown += OnBindingKeyDown;
        _resetButton = AddUIComponent<NormalButton>();
        _resetButton.SetStyle(StyleType.OptionPanelStyle);
        _resetButton.autoSize = false;
        _resetButton.size = new Vector2(30, 30);
        _resetButton.FgSprites.SetValues(SharedAtlasKeys.ResetOutline);
        _resetButton.FgScaleFactor = 0.6f;
        _resetButton.RenderFg = true;
        _resetButton.tooltip = LocalizationManager.Localize("Reset");
        _resetButton.eventClicked += OnResetButtonClicked;
        _clearButton = AddUIComponent<NormalButton>();
        _clearButton.SetStyle(StyleType.OptionPanelStyle);
        _clearButton.autoSize = false;
        _clearButton.size = new Vector2(30, 30);
        _clearButton.FgSprites.SetValues(SharedAtlasKeys.XClose);
        _clearButton.FgScaleFactor = 0.6f;
        _clearButton.RenderFg = true;
        _clearButton.tooltip = "Clear";
        _clearButton.eventClicked += OnClearButtonClicked;
    }

    private void OnResetButtonClicked(UIComponent component, UIMouseEventParameter eventParam) {
        if (_binding is null) return;
        _binding.Reset();
        OnBindingChanged(_binding);
    }

    private void OnClearButtonClicked(UIComponent component, UIMouseEventParameter eventParam) => ApplyKey(KeyCombination.Unbound);

    private void OnBindingMouseDown(UIComponent component, UIMouseEventParameter p) {
        if (_bindingPhase != BindingPhase.Idle) return;
        p.Use();
        _originalText = _inputButton.Text;
        _bindingPhase = BindingPhase.WaitingForKey;
        _inputButton.Text = SharedTranslations.PressAnyKey;
        _inputButton.Focus();
        UIView.PushModal(_inputButton);

        if (p.buttons != UIMouseButton.Left && p.buttons != UIMouseButton.Right) HandleMouseBinding(p.buttons);
    }

    private void HandleMouseBinding(UIMouseButton button) {
        if (_bindingPhase != BindingPhase.WaitingForKey) return;

        var keyCode = button switch {
            UIMouseButton.Middle => KeyCode.Mouse2,
            UIMouseButton.Special0 => KeyCode.Mouse3,
            UIMouseButton.Special1 => KeyCode.Mouse4,
            UIMouseButton.Special2 => KeyCode.Mouse5,
            UIMouseButton.Special3 => KeyCode.Mouse6,
            _ => KeyCode.None
        };

        if (keyCode != KeyCode.None)
            ApplyKey(new KeyCombination(
                keyCode,
                ModifierFlagsExtensions.IsControlDown(),
                ModifierFlagsExtensions.IsShiftDown(),
                ModifierFlagsExtensions.IsAltDown()));
    }

    private void OnBindingKeyDown(UIComponent component, UIKeyEventParameter e) {
        if (_bindingPhase == BindingPhase.Idle) return;

        if (e.keycode == KeyCode.Escape) {
            CancelBinding();
            e.Use();
            return;
        }

        if (e.keycode == KeyCode.Backspace) {
            ApplyKey(KeyCombination.Unbound);
            EndBinding();
            e.Use();
            return;
        }

        if (ModifierFlagsExtensions.IsModifierKey(e.keycode)) {
            _bindingPhase = BindingPhase.PreviewingModifiers;
            e.Use();
            return;
        }

        var combination = new KeyCombination(e.keycode, ModifierFlagsExtensions.IsControlDown(), ModifierFlagsExtensions.IsShiftDown(), ModifierFlagsExtensions.IsAltDown());
        ApplyKey(combination);
        EndBinding();
        e.Use();
    }

    private string GetActiveModifierDisplay() {
        _reusableList.Clear();
        if (ModifierFlagsExtensions.IsShiftDown()) _reusableList.Add("Shift");
        if (ModifierFlagsExtensions.IsControlDown()) _reusableList.Add("Ctrl");
        if (ModifierFlagsExtensions.IsAltDown()) _reusableList.Add("Alt");
        var result = string.Join("+", _reusableList.ToArray());
        return string.IsNullOrEmpty(result) ? string.Empty : result + "+";
    }

    private void ApplyKey(KeyCombination keyCombination) {
        if (_binding is null) return;
        _binding.Combination = keyCombination;
        OnBindingChanged(_binding);
    }

    public override void Update() {
        base.Update();
        if (_bindingPhase != BindingPhase.PreviewingModifiers) return;
        var preview = GetActiveModifierDisplay();
        _inputButton.Text = string.IsNullOrEmpty(preview) ? SharedTranslations.PressAnyKey : preview;
    }

    private void CancelBinding() {
        _inputButton.Text = _originalText;
        _bindingPhase = BindingPhase.Idle;
        UIView.PopModal();
    }

    private void EndBinding() {
        _bindingPhase = BindingPhase.Idle;
        UIView.PopModal();
    }

    private enum BindingPhase {
        Idle,
        WaitingForKey,
        PreviewingModifiers
    }
}