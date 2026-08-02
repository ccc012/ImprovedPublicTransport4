using ColossalFramework.UI;
using UnityEngine;

namespace CSLModsCommon.UI.Containers;

public class GridContainer : UIStateElement {
    protected int _columns = 1;
    protected float _columnGap;
    protected float _rowGap;
    protected Padding _layoutPadding;
    protected bool _autoLayout;
    protected bool _autoFitChildrenVertically;
    protected bool _uniformCellSize;

    public virtual Padding LayoutPadding => _layoutPadding;

    public int Columns {
        get => _columns;
        set {
            value = Mathf.Max(1, value);
            if (value == _columns) return;
            _columns = value;
            RequestArrange();
        }
    }

    public float ColumnGap {
        get => _columnGap;
        set {
            if (Mathf.Approximately(value, _columnGap)) return;
            _columnGap = value;
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
            if (value == _autoLayout) return;
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

    /// <summary>When true, every cell is sized to the widest/tallest child in the grid instead of its own child's size.</summary>
    public bool UniformCellSize {
        get => _uniformCellSize;
        set {
            if (value == _uniformCellSize) return;
            _uniformCellSize = value;
            RequestArrange();
        }
    }

    public GridContainer SetColumns(int columns) {
        Columns = columns;
        return this;
    }

    public GridContainer SetColumnGap(float gap) {
        ColumnGap = gap;
        return this;
    }

    public GridContainer SetRowGap(float gap) {
        RowGap = gap;
        return this;
    }

    public GridContainer SetLayoutPadding(int all) {
        LayoutPadding.SetAll(all);
        return this;
    }

    public GridContainer SetLayoutPadding(int left, int top, int right, int bottom) {
        LayoutPadding.SetAll(left, right, top, bottom);
        return this;
    }

    public override void Awake() {
        base.Awake();
        _layoutPadding = Padding.GetZeroPadding(this, OnPaddingChanged);
    }

    public override void OnDestroy() {
        base.OnDestroy();
        _layoutPadding.DetachParent(OnPaddingChanged);
    }

    public virtual void Arrange() {
        if (!_autoLayout) return;
        var children = GetActiveChildren();
        if (children.Count == 0) return;

        var innerWidth = width - _layoutPadding.Horizontal;
        var columnWidth = (innerWidth - _columnGap * (_columns - 1)) / _columns;
        var rowCount = Mathf.CeilToInt(children.Count / (float)_columns);

        var rowHeights = new float[rowCount];
        for (var row = 0; row < rowCount; row++) {
            var maxHeight = 0f;
            for (var col = 0; col < _columns; col++) {
                var index = row * _columns + col;
                if (index >= children.Count) break;
                maxHeight = Mathf.Max(maxHeight, children[index].height);
            }

            rowHeights[row] = maxHeight;
        }

        float y = _layoutPadding.Top;
        for (var row = 0; row < rowCount; row++) {
            float x = _layoutPadding.Left;
            for (var col = 0; col < _columns; col++) {
                var index = row * _columns + col;
                if (index >= children.Count) break;

                var child = children[index];
                if (_uniformCellSize) {
                    child.width = columnWidth;
                    child.height = rowHeights[row];
                }

                child.relativePosition = new Vector3(x, y);
                x += columnWidth + _columnGap;
            }

            y += rowHeights[row] + _rowGap;
        }

        if (_autoFitChildrenVertically) {
            var contentHeight = (rowCount > 0 ? y - _rowGap : _layoutPadding.Top) + _layoutPadding.Bottom;
            if (!Mathf.Approximately(contentHeight, height)) height = contentHeight;
        }
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
        if (!_autoLayout) return;
        AttachEvents(child);
        Arrange();
    }

    protected override void OnComponentRemoved(UIComponent child) {
        base.OnComponentRemoved(child);
        if (child != null) DetachEvents(child);
        if (_autoLayout) Arrange();
    }

    private void OnPaddingChanged(Padding padding) => Arrange();

    private void AttachEvents(UIComponent child) {
        child.eventVisibilityChanged += ChildIsVisibleChanged;
        child.eventSizeChanged += ChildInvalidated;
        child.eventZOrderChanged += ChildZOrderChanged;
    }

    private void DetachEvents(UIComponent child) {
        child.eventVisibilityChanged -= ChildIsVisibleChanged;
        child.eventSizeChanged -= ChildInvalidated;
        child.eventZOrderChanged -= ChildZOrderChanged;
    }

    private void ChildIsVisibleChanged(UIComponent child, bool value) => ChildInvalidatedLayout();

    private void ChildZOrderChanged(UIComponent child, int value) => ChildInvalidatedLayout();

    private void ChildInvalidated(UIComponent child, Vector2 value) => ChildInvalidatedLayout();

    private void ChildInvalidatedLayout() {
        Arrange();
        Invalidate();
    }

    private void RequestArrange() {
        Invalidate();
        Arrange();
    }
}
