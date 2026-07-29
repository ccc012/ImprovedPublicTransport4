using ColossalFramework.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CSLModsCommon.UI.Containers;

public class FlexContainer : UIStateElement {
    protected FlexDirection _direction;
    protected JustifyContent _justify;
    protected AlignItems _itemsAlign;
    protected AlignContent _contentAlign;
    protected Padding _layoutPadding;
    protected float _columnGap;
    protected float _rowGap;
    protected bool _autoLayout;
    protected bool _autoFitChildrenVertically;
    protected bool _autoFitChildrenHorizontally;

    public virtual FlexDirection Direction {
        get => _direction;
        set {
            if (value.Equals(_direction)) return;
            _direction = value;
            Invalidate();
            Arrange();
        }
    }

    public virtual JustifyContent Justify {
        get => _justify;
        set {
            if (value.Equals(_justify)) return;
            _justify = value;
            Invalidate();
            Arrange();
        }
    }

    public virtual AlignItems ItemsAlign {
        get => _itemsAlign;
        set {
            if (value.Equals(_itemsAlign)) return;
            _itemsAlign = value;
            Invalidate();
            Arrange();
        }
    }

    public virtual AlignContent ContentAlign {
        get => _contentAlign;
        set {
            if (value.Equals(_contentAlign)) return;
            _contentAlign = value;
            Invalidate();
            Arrange();
        }
    }

    public virtual Padding LayoutPadding {
        get => _layoutPadding;
        set {
            if (value.Equals(_layoutPadding)) return;
            _layoutPadding.DetachParent();
            _layoutPadding = value;
            _layoutPadding.AttachParent(this);
            Invalidate();
            Arrange();
        }
    }

    public float ColumnGap {
        get => _columnGap;
        set {
            if (Equals(value, _columnGap)) return;
            _columnGap = value;
            Invalidate();
            Arrange();
        }
    }

    public float RowGap {
        get => _rowGap;
        set {
            if (Equals(value, _rowGap)) return;
            _rowGap = value;
            Invalidate();
            Arrange();
        }
    }

    public virtual bool AutoLayout {
        get => _autoLayout;
        set {
            if (Equals(value, _autoLayout)) return;
            _autoLayout = value;
            Invalidate();
            Arrange();
        }
    }

    public bool AutoFitChildrenVertically {
        get => _autoFitChildrenVertically;
        set {
            if (value == _autoFitChildrenVertically) return;
            _autoFitChildrenVertically = value;
            Invalidate();
            Arrange();
        }
    }

    public bool AutoFitChildrenHorizontally {
        get => _autoFitChildrenHorizontally;
        set {
            if (value == _autoFitChildrenHorizontally) return;
            _autoFitChildrenHorizontally = value;
            Invalidate();
            Arrange();
        }
    }

    public FlexContainer SetItemAlign(AlignItems align) {
        ItemsAlign = align;
        return this;
    }

    public FlexContainer SetContentAlign(AlignContent align) {
        ContentAlign = align;
        return this;
    }

    public FlexContainer SetLayoutPadding(int all) {
        LayoutPadding.SetAll(all);
        return this;
    }

    public FlexContainer SetLayoutPadding(int left, int top, int right, int bottom) {
        LayoutPadding.SetAll(left, top, right, bottom);
        return this;
    }

    public FlexContainer SetHorizontalSpacing(float spacing) {
        ColumnGap = spacing;
        return this;
    }

    public FlexContainer SetVerticalSpacing(float spacing) {
        RowGap = spacing;
        return this;
    }

    public FlexContainer SetAutoLayout(bool autoLayout) {
        AutoLayout = autoLayout;
        return this;
    }

    public FlexContainer SetAutoFitChildrenHorizontally(bool autoFit) {
        AutoFitChildrenHorizontally = autoFit;
        return this;
    }

    public FlexContainer SetAutoFitChildrenVertically(bool autoFit) {
        AutoFitChildrenVertically = autoFit;
        return this;
    }

    public FlexContainer SetJustify(JustifyContent justify) {
        Justify = justify;
        return this;
    }

    public FlexContainer SetDirection(FlexDirection direction) {
        Direction = direction;
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

        var isRow = _direction is FlexDirection.Row or FlexDirection.RowReverse;
        var spacingMain = isRow ? _columnGap : _rowGap;
        var spacingCross = isRow ? _rowGap : _columnGap;

        var innerWidth = width - _layoutPadding.Horizontal;
        var innerHeight = height - _layoutPadding.Vertical;

        // Step1: Line/column processing
        var lines = new List<LineInfo>();
        var lineStart = 0;
        var lineMain = 0f;
        var lineCross = 0f;

        for (var i = 0; i < children.Count; i++) {
            var child = children[i];
            var childSize = child.size;
            var childMain = isRow ? childSize.x : childSize.y;
            var childCross = isRow ? childSize.y : childSize.x;

            var needWrap = lineMain > 0f && lineMain + spacingMain + childMain > (isRow ? innerWidth : innerHeight);
            if (needWrap) {
                lines.Add(new LineInfo {
                    StartIndex = lineStart,
                    Count = i - lineStart,
                    MainSize = lineMain,
                    CrossSize = lineCross
                });
                lineStart = i;
                lineMain = 0f;
                lineCross = 0f;
            }

            lineMain += (lineMain > 0f ? spacingMain : 0f) + childMain;
            lineCross = Mathf.Max(lineCross, childCross);
        }

        if (lineStart < children.Count)
            lines.Add(new LineInfo {
                StartIndex = lineStart,
                Count = children.Count - lineStart,
                MainSize = lineMain,
                CrossSize = lineCross
            });

        // Step2: Calculate the total size of the cross axis
        var totalCrossSize = lines.Sum(line => line.CrossSize);
        totalCrossSize += spacingCross * (lines.Count - 1);

        var remainingCross = (isRow ? innerHeight : innerWidth) - totalCrossSize;
        var crossExtraSpacing = spacingCross;
        var crossStart = 0f;

        switch (_contentAlign) {
            case AlignContent.FlexStart: crossStart = 0f; break;
            case AlignContent.FlexEnd: crossStart = remainingCross; break;
            case AlignContent.Center: crossStart = remainingCross / 2f; break;
            case AlignContent.SpaceBetween:
                crossExtraSpacing = lines.Count > 1 ? remainingCross / (lines.Count - 1) : 0f;
                crossStart = 0f;
                break;
            case AlignContent.SpaceAround:
                crossExtraSpacing = lines.Count > 0 ? remainingCross / lines.Count : 0f;
                crossStart = crossExtraSpacing / 2f;
                break;
        }

        // Step3: Layout each row
        var crossPos = crossStart;
        foreach (var line in lines) {
            var lineMainSize = line.MainSize;
            var lineCrossSize = line.CrossSize;

            var mainStart = 0f;
            var extraMainSpacing = spacingMain;
            switch (_justify) {
                case JustifyContent.FlexStart: mainStart = 0f; break;
                case JustifyContent.FlexEnd: mainStart = (isRow ? innerWidth : innerHeight) - lineMainSize; break;
                case JustifyContent.Center: mainStart = ((isRow ? innerWidth : innerHeight) - lineMainSize) / 2f; break;
                case JustifyContent.SpaceBetween:
                    extraMainSpacing = line.Count > 1 ? ((isRow ? innerWidth : innerHeight) - (lineMainSize - spacingMain * (line.Count - 1))) / (line.Count - 1) : 0f;
                    mainStart = 0f;
                    break;
                case JustifyContent.SpaceAround:
                    extraMainSpacing = line.Count > 0 ? ((isRow ? innerWidth : innerHeight) - (lineMainSize - spacingMain * (line.Count - 1))) / line.Count : 0f;
                    mainStart = extraMainSpacing / 2f;
                    break;
                case JustifyContent.SpaceEvenly:
                    extraMainSpacing = line.Count > 0 ? ((isRow ? innerWidth : innerHeight) - (lineMainSize - spacingMain * (line.Count - 1))) / (line.Count + 1) : 0f;
                    mainStart = extraMainSpacing;
                    break;
            }

            var posMain = mainStart;

            for (var i = 0; i < line.Count; i++) {
                var child = children[line.StartIndex + i];
                var childSize = child.size;
                var childMain = isRow ? childSize.x : childSize.y;
                var childCross = isRow ? childSize.y : childSize.x;

                var childCrossPos = 0f;
                switch (_itemsAlign) {
                    case AlignItems.FlexStart: childCrossPos = 0f; break;
                    case AlignItems.FlexEnd: childCrossPos = lineCrossSize - childCross; break;
                    case AlignItems.Center: childCrossPos = (lineCrossSize - childCross) / 2f; break;
                    case AlignItems.Stretch:
                        childCrossPos = 0f;
                        if (isRow) child.height = lineCrossSize;
                        else child.width = lineCrossSize;
                        break;
                }

                float posX, posY;
                if (isRow) {
                    posX = _layoutPadding.Left + (Direction == FlexDirection.Row ? posMain : innerWidth - posMain - childMain);
                    posY = _layoutPadding.Top + crossPos + childCrossPos;
                }
                else {
                    posY = _layoutPadding.Top + (Direction == FlexDirection.Column ? posMain : innerHeight - posMain - childMain);
                    posX = _layoutPadding.Left + crossPos + childCrossPos;
                }

                posMain += childMain + extraMainSpacing;

                child.relativePosition = new Vector3(posX, posY);
            }

            crossPos += lineCrossSize + crossExtraSpacing;
        }

        // Step4: AutoFit
        if (_autoFitChildrenHorizontally)
            width = _layoutPadding.Horizontal + (isRow ? GetMaxLineMainSize(lines) : totalCrossSize);

        if (_autoFitChildrenVertically)
            height = _layoutPadding.Vertical + (isRow ? totalCrossSize : GetMaxLineMainSize(lines));
    }

    protected float GetMaxLineMainSize(List<LineInfo> lines) => lines.Aggregate(0f, (current, line) => Mathf.Max(current, line.MainSize));

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

    private void ChildIsVisibleChanged(UIComponent child, bool value) => ChildInvalidatedLayout();

    private void ChildZOrderChanged(UIComponent child, int value) => ChildInvalidatedLayout();

    private void ChildInvalidated(UIComponent child, Vector2 value) => ChildInvalidatedLayout();

    private void ChildInvalidatedLayout() {
        Arrange();
        Invalidate();
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

    protected virtual void OnComponentAddedInvoke(UIComponent child) {
        if (!_autoLayout) return;
        AttachEvents(child);
        Arrange();
    }

    protected virtual void OnComponentRemovedInvoke(UIComponent child) {
        if (!_autoLayout) return;
        if (child != null) DetachEvents(child);
    }

    protected override void OnComponentAdded(UIComponent child) {
        base.OnComponentAdded(child);
        OnComponentAddedInvoke(child);
    }

    protected override void OnComponentRemoved(UIComponent child) {
        base.OnComponentRemoved(child);
        OnComponentRemovedInvoke(child);
    }

    private void OnPaddingChanged(Padding padding) => Arrange();

    public struct LineInfo {
        public int StartIndex;
        public int Count;
        public float MainSize;
        public float CrossSize;
    }
}