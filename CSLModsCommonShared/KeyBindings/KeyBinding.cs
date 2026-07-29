namespace CSLModsCommon.KeyBindings;

public class KeyBinding {
    protected bool _wasPressed;

    public KeyCombination Combination { get; set; }
    public KeyCombination DefaultCombination { get; }
    public KeyBindingTriggerMode TriggerMode { get; set; }

    public KeyBinding(KeyCombination combination, KeyBindingTriggerMode triggerMode = KeyBindingTriggerMode.PressOnce) {
        DefaultCombination = Combination = combination;
        TriggerMode = triggerMode;
    }

    public bool IsPressed() => TriggerMode switch {
        KeyBindingTriggerMode.PressOnce => Combination.IsPressedOnce(ref _wasPressed),
        KeyBindingTriggerMode.Hold => Combination.IsPressed(),
        KeyBindingTriggerMode.Press => Combination.IsPressed(),
        _ => false
    };

    public void Reset() => Combination = DefaultCombination;

    public string GetDisplayString() => Combination.ToString();

    public bool ConflictWith(KeyBinding other) => other != null && Combination == other.Combination;

    public override string ToString() => Combination.ToString();
}