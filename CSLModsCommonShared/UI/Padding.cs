using ColossalFramework.UI;
using System;
using UnityEngine;

namespace CSLModsCommon.UI;

public class Padding {
    private int _left;
    private int _right;
    private int _top;
    private int _bottom;

    public event Action<Padding> EventSizeChanged;

    public UIComponent Parent { get; private set; }

    public int Left {
        get => _left;
        set {
            if (_left == value) return;
            _left = value;
            Invalidate();
            InvokeSizeChanged();
        }
    }

    public int Right {
        get => _right;
        set {
            if (_right == value) return;
            _right = value;
            Invalidate();
            InvokeSizeChanged();
        }
    }

    public int Top {
        get => _top;
        set {
            if (_top == value) return;
            _top = value;
            Invalidate();
            InvokeSizeChanged();
        }
    }

    public int Bottom {
        get => _bottom;
        set {
            if (_bottom == value) return;
            _bottom = value;
            Invalidate();
            InvokeSizeChanged();
        }
    }

    public int Horizontal => _left + _right;

    public int Vertical => _top + _bottom;

    public Padding() { }

    public Padding(int all) => _left = _right = _top = _bottom = all;

    public Padding(int vertical, int horizontal) {
        _left = _right = horizontal / 2;
        _top = _bottom = vertical / 2;
    }

    public Padding(int left, int right, int top, int bottom) {
        _left = left;
        _right = right;
        _top = top;
        _bottom = bottom;
    }

    public static Padding GetAPadding() => GetZeroPadding();

    public static Padding GetAPadding(UIComponent parent) => GetZeroPadding(parent);

    public static Padding GetZeroPadding() => new Padding();

    public static Padding GetZeroPadding(UIComponent parent, Action<Padding> onSizeChanged = null) {
        if (parent is null) return GetZeroPadding();
        var padding = new Padding();
        padding.AttachParent(parent);
        if (onSizeChanged is not null)
            padding.EventSizeChanged += onSizeChanged;
        return padding;
    }

    private void InvokeSizeChanged() => EventSizeChanged?.Invoke(this);

    public Padding SetLeft(int left) {
        Left = left;
        return this;
    }

    public Padding SetRight(int right) {
        Right = right;
        return this;
    }

    public Padding SetTop(int top) {
        Top = top;
        return this;
    }

    public Padding SetBottom(int bottom) {
        Bottom = bottom;
        return this;
    }

    public void SetAll(int value) {
        if (IsUniform && _left == value) return;
        _left = _right = _top = _bottom = value;
        Invalidate();
        InvokeSizeChanged();
    }

    public void ResetAll() {
        _left = _right = _top = _bottom = 0;
        Invalidate();
        InvokeSizeChanged();
    }

    public void SetAll(int left, int right, int top, int bottom) {
        _left = left;
        _right = right;
        _top = top;
        _bottom = bottom;
        Invalidate();
        InvokeSizeChanged();
    }

    public bool IsUniform => _left == _right && _left == _top && _left == _bottom;

    public void ApplyTo(RectOffset rectOffset) {
        rectOffset.left = Left;
        rectOffset.right = Right;
        rectOffset.top = Top;
        rectOffset.bottom = Bottom;
    }

    public RectOffset ToRectOffset() => new RectOffset(Left, Right, Top, Bottom);

    public void SetFrom(Padding padding) {
        if (padding is null) return;
        _left = padding.Left;
        _right = padding.Right;
        _top = padding.Top;
        _bottom = padding.Bottom;
        Invalidate();
        InvokeSizeChanged();
    }

    public void Invalidate() => Parent?.Invalidate();

    public Padding DetachParent(Action<Padding> onSizeChanged = null) {
        if (onSizeChanged is not null)
            EventSizeChanged -= onSizeChanged;
        Parent = null;
        return this;
    }

    public Padding AttachParent(UIComponent parent) {
        Parent = parent;
        return this;
    }

    public Padding Clone() => new Padding(_left, _right, _top, _bottom);

    public static Padding Uniform(int value) => new Padding(value);

    public static Padding Symmetric(int vertical, int horizontal) => new Padding(vertical, horizontal);

    public static Padding operator +(Padding a, Padding b) {
        a ??= GetZeroPadding();
        b ??= GetZeroPadding();
        return new Padding(a.Left + b.Left, a.Right + b.Right, a.Top + b.Top, a.Bottom + b.Bottom);
    }

    public static Padding operator -(Padding a, Padding b) {
        a ??= GetZeroPadding();
        b ??= GetZeroPadding();
        return new Padding(a.Left - b.Left, a.Right - b.Right, a.Top - b.Top, a.Bottom - b.Bottom);
    }

    public bool Equals(Padding other) {
        if (other is null) return false;
        return _left == other._left && _right == other._right && _top == other._top && _bottom == other._bottom;
    }

    public override string ToString() => $"Padding ({Left}, {Right}, {Top}, {Bottom})";
}