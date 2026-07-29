using ColossalFramework.UI;
using System;

namespace CSLModsCommon.UI.Rendering;

public abstract class StateRendererBase<T> {
    public event Action<StateRendererBase<T>> StateChanged;

    public UIComponent Parent { get; set; }
    public bool CanRender { get; set; } = true;

    public StateRendererBase() { }

    public StateRendererBase(UIComponent parent) => Parent = parent;

    public abstract T GetValue(UIState state);
    public abstract void SetValues(T normal, T hovered, T pressed, T focused, T disabled);
    public abstract void SetValues(T all);

    public virtual void Render() {
        if (!CanRender || Parent is null) return;
        Parent.Invalidate();
        StateChanged?.Invoke(this);
    }
}