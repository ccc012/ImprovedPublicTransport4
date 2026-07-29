using System;
using System.Collections.Generic;
using UnityEngine;

namespace CSLModsCommon.KeyBindings; 
public struct KeyCombination : IEquatable<KeyCombination> {
    public static readonly KeyCombination Unbound = new(KeyCode.None);

    private static readonly Dictionary<KeyCode, string> SpecialNames = new() {
        { KeyCode.Return, "Enter" },
        { KeyCode.Escape, "Esc" },
        { KeyCode.UpArrow, "↑" },
        { KeyCode.DownArrow, "↓" },
        { KeyCode.LeftArrow, "←" },
        { KeyCode.RightArrow, "→" },
        { KeyCode.Keypad0, "Num 0" },
        { KeyCode.Keypad1, "Num 1" },
        { KeyCode.Keypad2, "Num 2" },
        { KeyCode.Keypad3, "Num 3" },
        { KeyCode.Keypad4, "Num 4" },
        { KeyCode.Keypad5, "Num 5" },
        { KeyCode.Keypad6, "Num 6" },
        { KeyCode.Keypad7, "Num 7" },
        { KeyCode.Keypad8, "Num 8" },
        { KeyCode.Keypad9, "Num 9" },
        { KeyCode.KeypadEnter, "Num Enter" },
        { KeyCode.Mouse0, "LMB" },
        { KeyCode.Mouse1, "RMB" },
        { KeyCode.Mouse2, "MMB" },
        { KeyCode.Mouse3, "Mouse 4" },
        { KeyCode.Mouse4, "Mouse 5" }
    };

    public KeyCode Key { get; set; }
    public ModifierFlags Modifiers { get; set; }

    public KeyCombination(KeyCode key, ModifierFlags modifiers = ModifierFlags.None) {
        Key = key;
        Modifiers = modifiers;
    }

    public KeyCombination(KeyCode key, bool control, bool shift, bool alt, bool meta = false) {
        Key = key;
        var modifiers = ModifierFlags.None;
        if (control) modifiers |= ModifierFlags.Control;
        if (shift) modifiers |= ModifierFlags.Shift;
        if (alt) modifiers |= ModifierFlags.Alt;
        if (meta) modifiers |= ModifierFlags.Meta;
        Modifiers = modifiers;
    }

    public static bool operator ==(KeyCombination left, KeyCombination right) {
        return left.Equals(right);
    }

    public static bool operator !=(KeyCombination left, KeyCombination right) {
        return !left.Equals(right);
    }

    public override bool Equals(object obj) => obj is KeyCombination other && Equals(other);

    public override int GetHashCode() => ((int)Key * 397) ^ (int)Modifiers;

    public override string ToString() {
        var mod = Modifiers.ToDisplayString();
        var keyStr = SpecialNames.TryGetValue(Key, out var name) ? name : Key.ToString();
        return string.IsNullOrEmpty(mod) ? keyStr : $"{mod} + {keyStr}";
    }

    public bool IsPressedOnce(ref bool wasPressed) {
        if (!IsPressed()) return wasPressed = false;
        if (wasPressed) return false;
        return wasPressed = true;
    }

    public bool Equals(KeyCombination other) => Key == other.Key && Modifiers == other.Modifiers;

    public bool IsModifierOnly => ModifierFlagsExtensions.IsModifierKey(Key);

    public bool IsPressed() => Input.GetKey(Key) && Modifiers.IsActive();
}