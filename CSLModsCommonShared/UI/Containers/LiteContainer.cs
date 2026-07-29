using ColossalFramework.UI;
using UnityEngine;

namespace CSLModsCommon.UI.Containers; 
public class LiteContainer : UIStateElement {
    protected Padding _layoutPadding;
    protected bool _autoLayout;
    protected float _columnGap;
    protected float _rowGap;
    protected FlexDirection _direction = FlexDirection.Column;
    protected AlignItems _itemsAlign;
    protected bool _autoFitChildrenVertically;
    protected bool _autoFitChildrenHorizontally;
    private int _suspendLayoutCounter;
    private bool _pendingArrange;

    public virtual Padding LayoutPadding => _layoutPadding;

    public float ColumnGap {
        get => _columnGap;
        set {
            if (Mathf.Approximately(value, _columnGap)) return;
            _columnGap = value;
            RequestArrange();
        }
    }

    public virtual FlexDirection Direction {
        get => _direction;
        set {
            if (value.Equals(_direction)) return;
            _direction = value;
            Invalidate();
            Arrange();
        }
    }

    public virtual AlignItems ItemsAlign {
        get => _itemsAlign;
        set {
            if (value.Equals(_itemsAlign)) return;
            _itemsAlign = value;
            RequestArrange();
        }
    }

    public float RowGap {
        get => _rowGap;
        set {
            if (Mathf.Approximately(value, _rowGap)) return;
            _rowGap = value;
            RequestArrange();
        }
    }

    public virtual bool AutoLayout {
        get => _autoLayout;
        set {
            if (Equals(value, _autoLayout)) return;
            _autoLayout = value;
            RequestArrange();
        }
    }

    public bool AutoFitChildrenVertically {
        get => _autoFitChildrenVertically;
        set {
            if (value == _autoFitChildrenVertically) return;
            _autoFitChildrenVertically = value;
            RequestArrange();
        }
    }

    public bool AutoFitChildrenHorizontally {
        get => _autoFitChildrenHorizontally;
        set {
            if (value == _autoFitChildrenHorizontally) return;
            _autoFitChildrenHorizontally = value;
            RequestArrange();
        }
    }

    protected virtual void OnPaddingChanged(Padding padding) => RequestArrange();

    public override void Awake() {
        base.Awake();
        _layoutPadding = Padding.GetZeroPadding(this, OnPaddingChanged);
    }

    public override void OnEnable() {
        base.OnEnable();

        if (_autoLayout) Arrange();
    }

    public override void Update() {
        base.Update();
        if (!_autoLayout || !_pendingArrange || _suspendLayoutCounter != 0) return;
        Arrange();
        _pendingArrange = false;
    }

    public override void OnDestroy() {
        base.OnDestroy();
        _layoutPadding.DetachParent(OnPaddingChanged);
    }

    protected override Plane[] GetClippingPlanes() {
        if (!clipChildren) return null;
        var corners = GetCorners();
        var vector = transform.TransformDirection(Vector3.right);
        var vector2 = transform.TransformDirection(Vector3.left);
        var vector3 = transform.TransformDirection(Vector3.up);
        var vector4 = transform.TransformDirection(Vector3.down);
        var d = PixelsToUnits();
        var padding = LayoutPadding;
        corners[0] += vector * padding.Left * d + vector4 * padding.Top * d;
        corners[1] += vector2 * padding.Right * d + vector4 * padding.Top * d;
        corners[2] += vector2 * padding.Right * d + vector3 * padding.Bottom * d;
        m_CachedClippingPlanes[0] = new Plane(vector, corners[0]);
        m_CachedClippingPlanes[1] = new Plane(vector2, corners[1]);
        m_CachedClippingPlanes[2] = new Plane(vector3, corners[2]);
        m_CachedClippingPlanes[3] = new Plane(vector4, corners[0]);
        return m_CachedClippingPlanes;
    }

    protected override void OnRebuildRenderData() {
        RenderBackground();
        RenderForeground();
    }

    protected override void OnComponentAdded(UIComponent child) {
        base.OnComponentAdded(child);
        OnComponentAddedInvoke(child);
        if (_autoLayout) Arrange();
    }

    protected override void OnComponentRemoved(UIComponent child) {
        base.OnComponentRemoved(child);
        OnComponentRemovedInvoke(child);
        if (_autoLayout) Arrange();
    }

    public virtual void Arrange() {
        var children = m_ChildComponents;
        var visibleCount = 0;

        foreach (var child in children)
            if (child.isVisible && child.enabled && child.gameObject.activeSelf && child.name != IgnoreUIElement)
                visibleCount++;

        if (visibleCount == 0) return;

        var totalMain = 0f;
        var maxCross = 0f;
        foreach (var child in children) {
            if (!child.isVisible || !child.enabled || !child.gameObject.activeSelf || child.name == IgnoreUIElement) continue;

            if (_direction is FlexDirection.Column or FlexDirection.ColumnReverse) {
                totalMain += child.height + _rowGap;
                maxCross = Mathf.Max(maxCross, child.width);
            }
            else {
                totalMain += child.width + _columnGap;
                maxCross = Mathf.Max(maxCross, child.height);
            }
        }

        if (_direction is FlexDirection.Column or FlexDirection.ColumnReverse) totalMain -= _rowGap;
        else totalMain -= _columnGap;

        var contentWidth = _direction is FlexDirection.Column or FlexDirection.ColumnReverse ? maxCross : totalMain;
        var contentHeight = _direction is FlexDirection.Column or FlexDirection.ColumnReverse ? totalMain : maxCross;

        var mainPos = _direction == FlexDirection.ColumnReverse ? totalMain : _direction == FlexDirection.RowReverse ? totalMain : 0f;

        var currentIndex = 0;
        foreach (var child in children) {
            if (!child.isVisible || !child.enabled || !child.gameObject.activeSelf || child.name == IgnoreUIElement) continue;

            float x = _layoutPadding.Left;
            float y = _layoutPadding.Top;

            // 交叉轴对齐
            switch (_itemsAlign) {
                case AlignItems.FlexStart: break;
                case AlignItems.Center:
                    if (_direction is FlexDirection.Column or FlexDirection.ColumnReverse)
                        x += (contentWidth - child.width) / 2f;
                    else
                        y += (contentHeight - child.height) / 2f;
                    break;
                case AlignItems.FlexEnd:
                    if (_direction is FlexDirection.Column or FlexDirection.ColumnReverse)
                        x += contentWidth - child.width;
                    else
                        y += contentHeight - child.height;
                    break;
            }

            var isLast = currentIndex == visibleCount - 1;

            // 主轴位置
            switch (_direction) {
                case FlexDirection.Column:
                    child.relativePosition = new Vector3(x, _layoutPadding.Top + mainPos);
                    mainPos += child.height + (isLast ? 0f : _rowGap);
                    break;

                case FlexDirection.ColumnReverse:
                    child.relativePosition = new Vector3(x, _layoutPadding.Top + mainPos - child.height);
                    mainPos -= child.height + (isLast ? 0f : _rowGap);
                    break;

                case FlexDirection.Row:
                    child.relativePosition = new Vector3(_layoutPadding.Left + mainPos, y);
                    mainPos += child.width + (isLast ? 0f : _columnGap);
                    break;

                case FlexDirection.RowReverse:
                    child.relativePosition = new Vector3(_layoutPadding.Left + mainPos - child.width, y);
                    mainPos -= child.width + (isLast ? 0f : _columnGap);
                    break;
            }

            currentIndex++;
        }

        // Auto-fit
        if (_autoFitChildrenHorizontally) {
            var newWidth = _direction is FlexDirection.Column or FlexDirection.ColumnReverse ? contentWidth + _layoutPadding.Horizontal : totalMain + _layoutPadding.Horizontal;
            if (!Mathf.Approximately(newWidth, width)) width = newWidth;
        }

        if (_autoFitChildrenVertically) {
            var newHeight = _direction is FlexDirection.Column or FlexDirection.ColumnReverse ? totalMain + _layoutPadding.Vertical : contentHeight + _layoutPadding.Vertical;
            if (!Mathf.Approximately(newHeight, height)) height = newHeight;
        }
    }

    // public virtual void Arrange() {
    //     var children = m_ChildComponents;
    //     var visibleCount = children.Count;
    //     float contentWidth;
    //     float contentHeight;
    //     if (_direction == LayoutDirection.Vertical) {
    //         var y = 0f;
    //         var maxWidth = 0f;
    //         for (var i = 0; i < visibleCount; i++) {
    //             var child = children[i];
    //             if (!child.isVisible || !child.enabled || !child.gameObject.activeSelf || child.name == IgnoreUIElement) continue;
    //             child.relativePosition = new Vector3(_layoutPadding.Left, _layoutPadding.Top + y);
    //             y += child.height;
    //             if (i < visibleCount - 1) y += _rowGap;
    //             maxWidth = Mathf.Max(maxWidth, child.width);
    //         }
    //
    //         contentHeight = y;
    //         contentWidth = maxWidth;
    //     }
    //     else {
    //         var x = 0f;
    //         var maxHeight = 0f;
    //         for (var i = 0; i < visibleCount; i++) {
    //             var child = children[i];
    //             if (!child.isVisible || !child.enabled || !child.gameObject.activeSelf || child.name == IgnoreUIElement) continue;
    //             child.relativePosition = new Vector3(_layoutPadding.Left + x, _layoutPadding.Top);
    //             x += child.width;
    //             if (i < visibleCount - 1) x += _columnGap;
    //             maxHeight = Mathf.Max(maxHeight, child.height);
    //         }
    //
    //         contentWidth = x;
    //         contentHeight = maxHeight;
    //     }
    //
    //     if (_autoFitChildrenHorizontally) {
    //         var newWidth = contentWidth + _layoutPadding.Horizontal;
    //         if (!Mathf.Approximately(newWidth, width)) {
    //             width = newWidth;
    //         }
    //     }
    //
    //     if (_autoFitChildrenVertically) {
    //         var newHeight = contentHeight + _layoutPadding.Vertical;
    //         if (!Mathf.Approximately(newHeight, height)) {
    //             height = newHeight;
    //         }
    //     }
    // }

    protected virtual void OnComponentAddedInvoke(UIComponent child) {
        if (!_autoLayout) return;
        AttachEvents(child);
        Arrange();
    }

    protected virtual void OnComponentRemovedInvoke(UIComponent child) {
        if (!_autoLayout) return;
        if (child != null) DetachEvents(child);
    }

    public void SuspendLayout() => _suspendLayoutCounter++;

    public void ResumeLayout() {
        if (_suspendLayoutCounter > 0) _suspendLayoutCounter--;

        if (_suspendLayoutCounter != 0 || !_pendingArrange) return;
        _pendingArrange = false;
        Arrange();
    }

    private void RequestArrange() {
        if (_suspendLayoutCounter > 0) {
            _pendingArrange = true;
            return;
        }

        if (_autoLayout) {
            _pendingArrange = true;
            Invalidate();
        }
    }

    private void ChildIsVisibleChanged(UIComponent child, bool value) => ChildInvalidatedLayout();

    private void ChildZOrderChanged(UIComponent child, int value) => ChildInvalidatedLayout();

    private void ChildInvalidated(UIComponent child, Vector2 value) => ChildInvalidatedLayout();

    private void ChildInvalidatedLayout() {
        if (isLayoutSuspended) return;
        Arrange();
        Invalidate();
        // RequestArrange();
    }

    private void DetachEvents(UIComponent child) {
        child.eventVisibilityChanged -= ChildIsVisibleChanged;
        child.eventPositionChanged -= ChildInvalidated;
        child.eventSizeChanged -= ChildInvalidated;
        child.eventZOrderChanged -= ChildZOrderChanged;
    }

    private void AttachEvents(UIComponent child) {
        child.eventVisibilityChanged += ChildIsVisibleChanged;
        child.eventPositionChanged += ChildInvalidated;
        child.eventSizeChanged += ChildInvalidated;
        child.eventZOrderChanged += ChildZOrderChanged;
    }
}