using ColossalFramework.UI;
using System;
using UnityEngine;

namespace CSLModsCommon.UI.Containers;

public class ScrollContainer : UIStateElement {
    public static readonly Vector2 MaxVerticalScroll = new(0f, 2.1474836E+09f);
    public static readonly Vector2 MaxHorizontalScroll = new(2.1474836E+09f, 0f);
    protected bool _autoReset = true;
    protected bool _autoLayout;
    protected Padding _scrollPadding;
    protected Padding _autoLayoutPadding;
    protected LayoutDirection _autoLayoutDirection = LayoutDirection.Vertical;
    protected LayoutStart _autoLayoutStart;
    protected bool _wrapLayout;
    protected bool _center;
    protected bool _freeScroll;
    protected bool _customScrollBounds;
    protected Vector2 _scrollPosition = Vector2.zero;
    protected int _scrollWheelAmount = 10;
    protected Scrollbar _horizontalScrollbar;
    protected Scrollbar _verticalScrollbar;
    protected UIOrientation _wheelDirection = UIOrientation.Vertical;
    protected bool _scrollWithArrowKeys;
    protected bool _useScrollMomentum;
    protected bool _useTouchMouseScroll;
    private bool _initialized;
    private bool _resetNeeded;
    private bool _scrolling;
    private bool _isMouseDown;
    private Vector2 _touchStartPosition = Vector2.zero;
    private Vector2 _scrollMomentum = Vector2.zero;
    protected float _rowGap;
    protected float _columnGap;

    public event PropertyChangedEventHandler<Vector2> EventScrollPositionChanged;

    public float RowGap {
        get => _rowGap;
        set {
            if (Mathf.Approximately(value, _rowGap)) return;
            _rowGap = value;
            Reset();
        }
    }

    public float ColumnGap {
        get => _columnGap;
        set {
            if (Mathf.Approximately(value, _columnGap)) return;
            _columnGap = value;
            Reset();
        }
    }

    public bool UseScrollMomentum {
        get => _useScrollMomentum;
        set {
            _useScrollMomentum = value;
            _scrollMomentum = Vector2.zero;
        }
    }

    public bool UseTouchMouseScroll {
        get => _useTouchMouseScroll;
        set => _useTouchMouseScroll = value;
    }

    public bool ScrollWithArrowKeys {
        get => _scrollWithArrowKeys;
        set => _scrollWithArrowKeys = value;
    }

    public bool FreeScroll {
        get => _freeScroll;
        set => _freeScroll = value;
    }

    public bool CustomScrollBounds {
        get => _customScrollBounds;
        set => _customScrollBounds = value;
    }

    public bool AutoLayout {
        get => _autoLayout;
        set {
            if (value == _autoLayout) return;
            _autoLayout = value;
            Reset();
        }
    }

    public bool AutoReset {
        get => _autoReset;
        set {
            if (value == _autoReset) return;
            _autoReset = value;
            if (value) Reset();
        }
    }

    public Padding ScrollPadding {
        get => _scrollPadding;
        set {
            if (_scrollPadding.Equals(value)) return;
            _scrollPadding.DetachParent();
            _scrollPadding = value;
            _scrollPadding.AttachParent(this);
            Reset();
        }
    }

    public bool WrapLayout {
        get => _wrapLayout;
        set {
            if (value == _wrapLayout) return;
            _wrapLayout = value;
            Reset();
        }
    }

    public LayoutDirection AutoLayoutDirection {
        get => !_center ? _autoLayoutDirection : LayoutDirection.Horizontal;
        set {
            if (value == _autoLayoutDirection) return;
            _autoLayoutDirection = value;
            Reset();
        }
    }

    public LayoutStart AutoLayoutStart {
        get => _autoLayoutStart;
        set {
            if (value is LayoutStart.TopRight or LayoutStart.BottomRight) throw new NotSupportedException("Right layout start is unsupported");

            if (value == _autoLayoutStart) return;
            _autoLayoutStart = value;
            Reset();
        }
    }

    public bool UseCenter {
        get => _center;
        set {
            if (value == _center) return;
            _center = value;
            for (var i = 0; i < childCount; i++) m_ChildComponents[i].PerformLayout();

            Reset();
        }
    }

    public Padding AutoLayoutPadding => _autoLayoutPadding;

    public Vector2 ScrollPosition {
        get => _scrollPosition;
        set {
            if (!_freeScroll) {
                var vector = CalculateViewSize();
                var vector2 = new Vector2(size.x - ScrollPadding.Horizontal, size.y - ScrollPadding.Vertical);
                value = Vector2.Min(vector - vector2, value);
                value = Vector2.Max(Vector2.zero, value);
                value = value.RoundToInt();
            }

            if ((value - _scrollPosition).sqrMagnitude > 1E-45f) {
                var vector3 = value - _scrollPosition;
                _scrollPosition = value;
                ScrollChildControls(vector3, _freeScroll);
                UpdateScrollbars();
            }

            OnScrollPositionChanged();
        }
    }


    public int ScrollWheelAmount {
        get => _scrollWheelAmount;
        set => _scrollWheelAmount = value;
    }


    public Scrollbar HorizontalScrollbar {
        get => _horizontalScrollbar;
        set {
            _horizontalScrollbar = value;
            UpdateScrollbars();
        }
    }

    public Scrollbar VerticalScrollbar {
        get => _verticalScrollbar;
        set {
            _verticalScrollbar = value;
            UpdateScrollbars();
        }
    }

    public UIOrientation ScrollWheelDirection {
        get => _wheelDirection;
        set => _wheelDirection = value;
    }


    protected override void OnVisibilityChanged() {
        base.OnVisibilityChanged();
        if (_horizontalScrollbar != null) _horizontalScrollbar.isVisible = isVisible;

        if (_verticalScrollbar != null) _verticalScrollbar.isVisible = isVisible;

        if (!isVisible || (!AutoReset && !AutoLayout)) return;
        Reset();
        UpdateScrollbars();
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();
        if (AutoReset || AutoLayout) {
            Reset();
            return;
        }

        var vector = CalculateMinChildPosition();
        if (vector.x > ScrollPadding.Left || vector.y > ScrollPadding.Top) {
            vector -= new Vector2(ScrollPadding.Left, ScrollPadding.Top);
            vector = Vector2.Max(vector, Vector2.zero);
            ScrollChildControls(vector);
        }

        UpdateScrollbars();
    }

    protected override void OnResolutionChanged(Vector2 previousResolution, Vector2 currentResolution) {
        base.OnResolutionChanged(previousResolution, currentResolution);
        _resetNeeded = true;
    }


    protected override void OnGotFocus(UIFocusEventParameter p) {
        base.OnGotFocus(p);
        var uicomponent = p.source;
        while (uicomponent != null) {
            if (m_ChildComponents.Contains(uicomponent)) {
                ScrollIntoView(uicomponent);
                return;
            }

            uicomponent = uicomponent.parent;
        }
    }


    protected override void OnKeyDown(UIKeyEventParameter p) {
        if (builtinKeyNavigation) {
            if (!ScrollWithArrowKeys || p.used) {
                base.OnKeyDown(p);
                return;
            }

            var num = HorizontalScrollbar != null ? HorizontalScrollbar.IncrementAmount : 1f;
            var num2 = VerticalScrollbar != null ? VerticalScrollbar.IncrementAmount : 1f;
            if (p.keycode == KeyCode.LeftArrow) {
                ScrollPosition += new Vector2(-num, 0f);
                p.Use();
            }
            else if (p.keycode == KeyCode.RightArrow) {
                ScrollPosition += new Vector2(num, 0f);
                p.Use();
            }
            else if (p.keycode == KeyCode.UpArrow) {
                ScrollPosition += new Vector2(0f, -num2);
                p.Use();
            }
            else if (p.keycode == KeyCode.DownArrow) {
                ScrollPosition += new Vector2(0f, num2);
                p.Use();
            }
        }

        base.OnKeyDown(p);
    }

    protected override void OnMouseEnter(UIMouseEventParameter p) {
        base.OnMouseEnter(p);
        _touchStartPosition = p.position;
    }

    protected override void OnMouseDown(UIMouseEventParameter p) {
        base.OnMouseDown(p);
        _touchStartPosition = p.position;
        _isMouseDown = true;
    }

    protected override void OnDragEnd(UIDragEventParameter p) {
        base.OnDragEnd(p);
        _isMouseDown = false;
    }

    protected override void OnMouseUp(UIMouseEventParameter p) {
        base.OnMouseUp(p);
        _isMouseDown = false;
    }

    protected override void OnMouseMove(UIMouseEventParameter p) {
        base.OnMouseMove(p);
        if (!UseTouchMouseScroll || !_isMouseDown || !((p.position - _touchStartPosition).magnitude > 5f)) return;
        var vector = p.moveDelta.Scale(-1f, 1f);
        ScrollPosition += vector;
        _scrollMomentum = (_scrollMomentum + vector) * 0.5f;
    }

    protected override void OnMouseWheel(UIMouseEventParameter p) {
        if (builtinKeyNavigation) {
            if (p.used) return;

            var num = ScrollWheelDirection == UIOrientation.Horizontal ? HorizontalScrollbar != null ? HorizontalScrollbar.IncrementAmount : ScrollWheelAmount : VerticalScrollbar != null ? VerticalScrollbar.IncrementAmount : ScrollWheelAmount;
            if (ScrollWheelDirection == UIOrientation.Horizontal) {
                ScrollPosition = new Vector2(ScrollPosition.x - num * p.wheelDelta, ScrollPosition.y);
                _scrollMomentum = new Vector2(-num * p.wheelDelta, 0f);
            }
            else {
                ScrollPosition = new Vector2(ScrollPosition.x, ScrollPosition.y - num * p.wheelDelta);
                _scrollMomentum = new Vector2(0f, -num * p.wheelDelta);
            }

            p.Use();
            Invoke("OnMouseWheel", p);
        }

        base.OnMouseWheel(p);
    }

    protected override void OnComponentAdded(UIComponent child) {
        base.OnComponentAdded(child);
        AttachEvents(child);
        if (AutoLayout) AutoArrange();
    }

    protected override void OnComponentRemoved(UIComponent child) {
        base.OnComponentRemoved(child);
        if (child is not null) DetachEvents(child);

        if (AutoLayout) AutoArrange();
    }

    protected void OnScrollPositionChanged() {
        Invalidate();
        if (EventScrollPositionChanged != null) EventScrollPositionChanged(this, ScrollPosition);

        InvokeUpward("OnScrollPositionChanged", new object[] { ScrollPosition });
    }

    protected override Plane[] GetClippingPlanes() {
        if (!clipChildren) return null;

        var corners = GetCorners();
        var vector = transform.TransformDirection(Vector3.right);
        var vector2 = transform.TransformDirection(Vector3.left);
        var vector3 = transform.TransformDirection(Vector3.up);
        var vector4 = transform.TransformDirection(Vector3.down);
        var num = PixelsToUnits();
        var scrollPadding = ScrollPadding;
        corners[0] += vector * scrollPadding.Left * num + vector4 * scrollPadding.Top * num;
        corners[1] += vector2 * scrollPadding.Right * num + vector4 * scrollPadding.Top * num;
        corners[2] += vector * scrollPadding.Left * num + vector3 * scrollPadding.Bottom * num;
        m_CachedClippingPlanes[0] = new Plane(vector, corners[0]);
        m_CachedClippingPlanes[1] = new Plane(vector2, corners[1]);
        m_CachedClippingPlanes[2] = new Plane(vector3, corners[2]);
        m_CachedClippingPlanes[3] = new Plane(vector4, corners[0]);

        return m_CachedClippingPlanes;
    }

    protected override void OnRebuildRenderData() => RenderBackground();

    public override bool canFocus => (isEnabled && isVisible) || base.canFocus;

    public void FitToContents() {
        if (childCount == 0) return;
        var vector = Vector2.zero;
        for (var i = 0; i < childCount; i++) {
            var uiComponent = m_ChildComponents[i];
            var vector2 = (Vector2)uiComponent.relativePosition + uiComponent.size;
            vector = Vector2.Max(vector, vector2);
        }

        size = vector + new Vector2(ScrollPadding.Right, ScrollPadding.Bottom);
    }

    public void CenterChildControls() {
        if (childCount == 0) return;

        var vector = Vector2.one * float.MaxValue;
        var vector2 = Vector2.one * float.MinValue;
        for (var i = 0; i < childCount; i++) {
            var uicomponent = m_ChildComponents[i];
            Vector2 vector3 = uicomponent.relativePosition;
            var vector4 = vector3 + uicomponent.size;
            vector = Vector2.Min(vector, vector3);
            vector2 = Vector2.Max(vector2, vector4);
        }

        var vector5 = vector2 - vector;
        var vector6 = (size - vector5) * 0.5f;
        for (var j = 0; j < childCount; j++) {
            var uicomponent2 = m_ChildComponents[j];
            uicomponent2.relativePosition = (Vector2)uicomponent2.relativePosition - vector + vector6;
        }
    }

    public void ScrollToTop() => ScrollPosition = new Vector2(ScrollPosition.x, 0f);

    public void ScrollToBottom() => ScrollPosition = new Vector2(ScrollPosition.x, 2.1474836E+09f);

    public void ScrollToLeft() => ScrollPosition = new Vector2(0f, ScrollPosition.y);

    public void ScrollToRight() => ScrollPosition = new Vector2(2.1474836E+09f, ScrollPosition.y);

    public void ScrollIntoView(UIComponent component) {
        if (!m_ChildComponents.Contains(component)) return;

        var rect = new Rect(ScrollPosition.x + ScrollPadding.Left, ScrollPosition.y + ScrollPadding.Top, size.x - ScrollPadding.Horizontal, size.y - ScrollPadding.Vertical).RoundToInt();
        var componentRelativePosition = component.relativePosition;
        var componentSize = component.size;
        var rect2 = new Rect(ScrollPosition.x + componentRelativePosition.x, ScrollPosition.y + componentRelativePosition.y, componentSize.x, componentSize.y).RoundToInt();
        if (rect.Intersects(rect2)) return;

        var scrollPosition = ScrollPosition;
        if (rect2.xMin < rect.xMin)
            scrollPosition.x = rect2.xMin - ScrollPadding.Left;
        else if (rect2.xMax > rect.xMax) scrollPosition.x = rect2.xMax - Mathf.Max(size.x, componentSize.x) + ScrollPadding.Horizontal;

        if (rect2.y < rect.y)
            scrollPosition.y = rect2.yMin - ScrollPadding.Top;
        else if (rect2.yMax > rect.yMax) scrollPosition.y = rect2.yMax - Mathf.Max(size.y, componentSize.y) + ScrollPadding.Vertical;

        ScrollPosition = scrollPosition;
    }

    public void Reset() {
        if (AutoLayout) {
            if (AutoReset) ScrollPosition = Vector2.zero;

            AutoArrange();
        }
        else {
            Vector3 vector = CalculateMinChildPosition();
            vector -= new Vector3(ScrollPadding.Left, ScrollPadding.Top);
            foreach (var t in m_ChildComponents) t.relativePosition -= vector;
        }

        if (AutoReset) {
            if (AutoLayoutStart == LayoutStart.TopLeft)
                ScrollPosition = Vector2.zero;
            else if (AutoLayoutStart == LayoutStart.BottomLeft) ScrollPosition = MaxVerticalScroll;
        }

        Invalidate();
        UpdateScrollbars();
    }

    private void AutoArrange() {
        var xFrom = AutoLayoutPadding.Left - ScrollPosition.x;
        var yFrom = 0f;
        if (!UseCenter) {
            if (AutoLayoutStart == LayoutStart.TopLeft)
                yFrom = AutoLayoutPadding.Top - ScrollPosition.y;
            else if (AutoLayoutStart == LayoutStart.BottomLeft) yFrom = height - AutoLayoutPadding.Bottom - ScrollPosition.y;
        }

        var num3 = 0f;
        var num4 = 0f;
        for (var i = 0; i < childCount; i++) {
            var uiComponent = m_ChildComponents[i];
            if (uiComponent.isVisibleSelf && uiComponent.enabled && uiComponent.gameObject.activeSelf && !(uiComponent == HorizontalScrollbar) && !(uiComponent == VerticalScrollbar)) {
                if (!UseCenter && WrapLayout) {
                    if (AutoLayoutDirection == LayoutDirection.Horizontal) {
                        if (xFrom + uiComponent.width >= size.x) {
                            xFrom = AutoLayoutPadding.Left;
                            if (AutoLayoutStart == LayoutStart.TopLeft)
                                yFrom += num4;
                            else if (AutoLayoutStart == LayoutStart.BottomLeft) yFrom -= num4;

                            num4 = 0f;
                        }
                    }
                    else if (yFrom + uiComponent.height + AutoLayoutPadding.Vertical >= size.y) {
                        yFrom = AutoLayoutPadding.Top;
                        xFrom += num3;
                        num3 = 0f;
                    }
                }

                var zero = Vector2.zero;
                if (UseCenter)
                    zero = new Vector2(xFrom, uiComponent.relativePosition.y);
                else if (AutoLayoutStart == LayoutStart.TopLeft)
                    zero = new Vector2(xFrom, yFrom);
                else if (AutoLayoutStart == LayoutStart.BottomLeft) zero = new Vector2(xFrom, yFrom - uiComponent.height);

                uiComponent.relativePosition = zero;
                var num5 = uiComponent.width + _columnGap;
                var num6 = uiComponent.height + _rowGap;
                num3 = Mathf.Max(num5, num3);
                num4 = Mathf.Max(num6, num4);
                if (AutoLayoutDirection == LayoutDirection.Horizontal)
                    xFrom += num5;
                else if (AutoLayoutStart == LayoutStart.TopLeft)
                    yFrom += num6;
                else if (AutoLayoutStart == LayoutStart.BottomLeft) yFrom -= num6;
            }
        }

        UpdateScrollbars();
    }

    private void Initialize() {
        if (_initialized) return;
        _initialized = true;
        if (Application.isPlaying) {
            if (HorizontalScrollbar is not null) HorizontalScrollbar.EventValueChanged += HorizontalScrollbarValueChanged;

            if (VerticalScrollbar != null) VerticalScrollbar.EventValueChanged += VerticalScrollbarValueChanged;
        }

        if (_resetNeeded || AutoLayout || AutoReset) Reset();

        Invalidate();
        if (AutoReset) {
            if (AutoLayoutStart == LayoutStart.TopLeft)
                ScrollPosition = Vector2.zero;
            else if (AutoLayoutStart == LayoutStart.BottomLeft) ScrollPosition = MaxVerticalScroll;
        }

        UpdateScrollbars();
    }

    private void ScrollChildControls(Vector3 delta, bool free = false) {
        try {
            _scrolling = true;
            delta = delta.Scale(1f, -1f, 1f);
            for (var i = 0; i < childCount; i++) {
                var uiComponent = m_ChildComponents[i];
                var vector = uiComponent.position - delta;
                if (!free) vector = vector.RoundToInt();

                uiComponent.position = vector;
            }
        }
        finally {
            _scrolling = false;
        }
    }

    private Vector2 CalculateMinChildPosition() {
        var num = float.MaxValue;
        var num2 = float.MaxValue;
        for (var i = 0; i < childCount; i++) {
            var childComponent = m_ChildComponents[i];
            if (childComponent.enabled && childComponent.gameObject.activeSelf) {
                var vector = childComponent.relativePosition.FloorToInt();
                num = Mathf.Min(num, vector.x);
                num2 = Mathf.Min(num2, vector.y);
            }
        }

        return new Vector2(num, num2);
    }

    public Vector2 CalculateViewSize() {
        if (_customScrollBounds) {
            var num = 0f;
            var num2 = 0f;
            if (_horizontalScrollbar != null) num = _horizontalScrollbar.MaxValue - _horizontalScrollbar.MinValue;

            if (_verticalScrollbar != null) num2 = _verticalScrollbar.MaxValue - _verticalScrollbar.MinValue;

            return new Vector2(num, num2);
        }

        var vector = new Vector2(ScrollPadding.Horizontal, ScrollPadding.Vertical).RoundToInt();
        var vector2 = size.RoundToInt() - vector;
        if (childCount == 0) return vector2;

        var vector3 = Vector2.one * float.MaxValue;
        var vector4 = Vector2.one * float.MinValue;
        for (var i = 0; i < childCount; i++) {
            var childComponent = m_ChildComponents[i];
            if (childComponent.isVisibleSelf) {
                Vector2 vector5 = childComponent.relativePosition.RoundToInt();
                var vector6 = vector5 + childComponent.size.RoundToInt();
                vector6.x += AutoLayoutPadding.Horizontal;
                vector6.y += AutoLayoutPadding.Vertical;
                vector3 = Vector2.Min(vector5, vector3);
                vector4 = Vector2.Max(vector6, vector4);
            }
        }

        return vector4 - vector3;
    }

    protected virtual void UpdateScrollbars() {
        var vector = CalculateViewSize();
        var vector2 = size - new Vector2(ScrollPadding.Horizontal, ScrollPadding.Vertical);
        if (HorizontalScrollbar != null) {
            HorizontalScrollbar.MinValue = 0f;
            HorizontalScrollbar.MaxValue = vector.x;
            HorizontalScrollbar.ScrollSize = vector2.x;
            HorizontalScrollbar.Value = Mathf.Max(0f, ScrollPosition.x);
        }

        if (VerticalScrollbar != null) {
            VerticalScrollbar.MinValue = 0f;
            VerticalScrollbar.MaxValue = vector.y;
            VerticalScrollbar.ScrollSize = vector2.y;
            VerticalScrollbar.Value = Mathf.Max(0f, ScrollPosition.y);
        }
    }

    private void AttachEvents(UIComponent child) {
        child.eventVisibilityChanged += ChildIsVisibleChanged;
        child.eventPositionChanged += ChildInvalidated;
        child.eventSizeChanged += ChildInvalidated;
        child.eventZOrderChanged += ChildZOrderChanged;
    }

    private void DetachEvents(UIComponent child) {
        child.eventVisibilityChanged -= ChildIsVisibleChanged;
        child.eventPositionChanged -= ChildInvalidated;
        child.eventSizeChanged -= ChildInvalidated;
        child.eventZOrderChanged -= ChildZOrderChanged;
    }

    private void ChildZOrderChanged(UIComponent child, int value) => ChildInvalidatedLayout();

    private void ChildIsVisibleChanged(UIComponent child, bool value) => ChildInvalidatedLayout();

    private void ChildInvalidated(UIComponent child, Vector2 value) => ChildInvalidatedLayout();

    private void ChildInvalidatedLayout() {
        if (_scrolling || isLayoutSuspended) return;

        if (AutoLayout) AutoArrange();

        UpdateScrollbars();
        Invalidate();
    }


    public override void Awake() {
        base.Awake();
        _autoLayoutPadding = Padding.GetZeroPadding(this);
        _autoLayoutPadding.EventSizeChanged += OnPaddingSizeChanged;
        _scrollPadding = Padding.GetZeroPadding(this);
        _scrollPadding.EventSizeChanged += OnPaddingSizeChanged;
        m_ClipChildren = true;
        m_BuiltinKeyNavigation = true;
    }

    private void OnPaddingSizeChanged(Padding obj) => Reset();

    public override void OnEnable() {
        base.OnEnable();
        if (size == Vector2.zero) {
            var camera = GetCamera();
            size = new Vector3((float)camera.pixelWidth / 2, (float)camera.pixelHeight / 2);
        }

        if (AutoLayout) AutoArrange();

        UpdateScrollbars();
    }

    public override void Update() {
        base.Update();
        if (UseScrollMomentum && !_isMouseDown && _scrollMomentum != Vector2.zero) ScrollPosition += _scrollMomentum;

        if (m_IsComponentInvalidated && AutoLayout && isVisible) {
            AutoArrange();
            UpdateScrollbars();
        }

        _scrollMomentum *= 0.95f - Time.deltaTime;
        if (_scrollMomentum.sqrMagnitude < 0.01f) _scrollMomentum = Vector2.zero;
    }

    public override void LateUpdate() {
        Initialize();
        if (_resetNeeded) {
            _resetNeeded = false;
            if (AutoReset || AutoLayout) Reset();
        }
    }

    public override void OnDestroy() {
        if (_horizontalScrollbar is not null) _horizontalScrollbar.EventValueChanged -= HorizontalScrollbarValueChanged;

        if (_verticalScrollbar is not null) _verticalScrollbar.EventValueChanged -= VerticalScrollbarValueChanged;

        _horizontalScrollbar = null;
        _verticalScrollbar = null;
    }

    private void VerticalScrollbarValueChanged(UIComponent component, float value) => ScrollPosition = new Vector2(ScrollPosition.x, value);

    private void HorizontalScrollbarValueChanged(UIComponent component, float value) => ScrollPosition = new Vector2(value, ScrollPosition.y);
}