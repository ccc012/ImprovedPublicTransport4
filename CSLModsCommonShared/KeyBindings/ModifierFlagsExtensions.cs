using CSLModsCommon.Collections;
using System.Linq;
using UnityEngine;

namespace CSLModsCommon.KeyBindings; 
public static class ModifierFlagsExtensions {
    public static bool HasFlag(this ModifierFlags flags, ModifierFlags flag) => (flags & flag) == flag;

    public static ModifierFlags SetFlag(this ModifierFlags flags, ModifierFlags flag) => flags | flag;

    public static ModifierFlags ClearFlag(this ModifierFlags flags, ModifierFlags flag) => flags & ~flag;

    public static ModifierFlags ToggleFlag(this ModifierFlags flags, ModifierFlags flag) => flags ^ flag;

    public static bool HasAny(this ModifierFlags flags) => flags != ModifierFlags.None;

    public static bool IsActive(this ModifierFlags flags) {
        if (flags.HasFlag(ModifierFlags.Control) && !IsControlDown()) return false;
        if (flags.HasFlag(ModifierFlags.Shift) && !IsShiftDown()) return false;
        if (flags.HasFlag(ModifierFlags.Alt) && !IsAltDown()) return false;
        if (flags.HasFlag(ModifierFlags.Meta) && !IsMetaDown()) return false;
        return true;
    }

    public static bool IsModifierKey(KeyCode code) => code is KeyCode.LeftControl or KeyCode.RightControl or
            KeyCode.LeftShift or KeyCode.RightShift or
            KeyCode.LeftAlt or KeyCode.RightAlt or KeyCode.AltGr;

    public static bool AnyModifierDown() => IsControlDown() || IsShiftDown() || IsAltDown();

    public static bool IsControlDown() => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

    public static bool IsShiftDown() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

    public static bool IsAltDown() => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr);

    public static bool IsMetaDown() => Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand) || Input.GetKey(KeyCode.LeftWindows) || Input.GetKey(KeyCode.RightWindows);

    public static string ToDisplayString(this ModifierFlags flags) {
        using var parts = ReusableList<string>.Rent();
        if (flags.HasFlag(ModifierFlags.Control)) parts.Add("Ctrl");
        if (flags.HasFlag(ModifierFlags.Shift)) parts.Add("Shift");
        if (flags.HasFlag(ModifierFlags.Alt)) parts.Add("Alt");
        if (flags.HasFlag(ModifierFlags.Meta)) parts.Add("Meta");
        return string.Join(" + ", parts.ToArray());
    }
}