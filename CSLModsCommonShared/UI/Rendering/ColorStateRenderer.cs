using ColossalFramework.UI;
using CSLModsCommon.UI.Extensions;
using UnityEngine;

namespace CSLModsCommon.UI.Rendering; 
public class ColorStateRenderer : StateRendererBase<Color32> {
    private Color32 _normalColor = new(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
    private Color32 _hoveredColor = new(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
    private Color32 _pressedColor = new(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
    private Color32 _focusedColor = new(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
    private Color32 _disabledColor = new(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

    public virtual Color32 NormalColor {
        get => _normalColor;
        set {
            if (value.EqualsColor(_normalColor)) return;
            _normalColor = value;
            Render();
        }
    }

    public virtual Color32 HoveredColor {
        get => _hoveredColor;
        set {
            if (value.EqualsColor(_hoveredColor)) return;
            _hoveredColor = value;
            Render();
        }
    }

    public virtual Color32 PressedColor {
        get => _pressedColor;
        set {
            if (value.EqualsColor(_pressedColor)) return;
            _pressedColor = value;
            Render();
        }
    }

    public virtual Color32 FocusedColor {
        get => _focusedColor;
        set {
            if (value.EqualsColor(_focusedColor)) return;
            _focusedColor = value;
            Render();
        }
    }

    public virtual Color32 DisabledColor {
        get => _disabledColor;
        set {
            if (value.EqualsColor(_disabledColor)) return;
            _disabledColor = value;
            Render();
        }
    }

    public ColorStateRenderer() { }

    public ColorStateRenderer(UIComponent parent) => Parent = parent;

    public ColorStateRenderer(UIComponent parent, Color32 normalColor, Color32 hoveredColor, Color32 pressedColor, Color32 focusedColor, Color32 disabledColor) : this(parent) {
        _normalColor = normalColor;
        _hoveredColor = hoveredColor;
        _pressedColor = pressedColor;
        _focusedColor = focusedColor;
        _disabledColor = disabledColor;
    }

    public ColorStateRenderer(Color32 normalColor, Color32 hoveredColor, Color32 pressedColor, Color32 focusedColor, Color32 disabledColor) {
        _normalColor = normalColor;
        _hoveredColor = hoveredColor;
        _pressedColor = pressedColor;
        _focusedColor = focusedColor;
        _disabledColor = disabledColor;
    }

    public override Color32 GetValue(UIState state) => state switch {
        UIState.Hover => _hoveredColor,
        UIState.Pressed => _pressedColor,
        UIState.Focused => _focusedColor,
        UIState.Disabled => _disabledColor,
        _ => _normalColor
    };

    public void SetValues(IColorSetter colorSetter) {
        CanRender = false;
        colorSetter.ApplyColors(this);
        CanRender = true;
        Render();
    }

    public override void SetValues(Color32 normalColor, Color32 hoveredColor, Color32 pressedColor, Color32 focusedColor, Color32 disabledColor) {
        CanRender = false;
        NormalColor = normalColor;
        HoveredColor = hoveredColor;
        PressedColor = pressedColor;
        FocusedColor = focusedColor;
        DisabledColor = disabledColor;
        CanRender = true;
        Render();
    }

    public override void SetValues(Color32 color) {
        CanRender = false;
        NormalColor = color;
        HoveredColor = color;
        PressedColor = color;
        FocusedColor = color;
        DisabledColor = color;
        CanRender = true;
        Render();
    }

    public override string ToString() => $"Normal color: {_normalColor.ToString()}, hovered color: {_hoveredColor.ToString()}, pressed color: {_pressedColor.ToString()}, focused color: {_focusedColor.ToString()}, disabled color: {_disabledColor.ToString()}";
}