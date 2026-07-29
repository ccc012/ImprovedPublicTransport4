using System;

namespace CSLModsCommon.KeyBindings;

public class KeyBindingEntry {
    public string Name { get; }
    public KeyBinding Binding { get; }
    public Action Action { get; }
    public KeyBindingContext Context { get; }
    public bool HasConflict { get; set; }

    public KeyBindingEntry(string name, KeyBinding binding, Action action, KeyBindingContext context) {
        Name = name;
        Binding = binding;
        Action = action;
        Context = context;
    }
}