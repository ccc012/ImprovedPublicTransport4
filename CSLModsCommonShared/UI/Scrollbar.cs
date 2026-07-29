using ColossalFramework;
using ColossalFramework.UI;
using CSLModsCommon.UI.Atlas;
using CSLModsCommon.UI.Containers;
using UnityEngine;

namespace CSLModsCommon.UI; 
public class Scrollbar : UIElement {
    protected UIOrientation _orientation = UIOrientation.Vertical;
    protected float _rawValue = 1f;
    protected float _minValue;
    protected float _maxValue = 100f;
    protected float _stepSize = 1f;
    protected float _scrollSize = 1f;
    protected float _increment = 1f;
    protected EasingType _scrollEasingType;
    protected float _scrollEasingTime = 1f;
    protected UIComponent _thumbObject;
    protected UIComponent _trackObject;
    protected UIComponent _incrementButton;
    protected UIComponent _decrementButton;
    protected RectOffset _thumbPadding;
    protected bool _autoHide = true;
    protected bool _autoDisableButtons;
    private Vector3 _thumbMouseOffset;
    private AnimatedFloat _easing;

    public event PropertyChangedEventHandler<float> EventValueChanged;

    public EasingType ScrollEasingType {
        get => _scrollEasingType;
        set => _scrollEasingType = value;
    }

    public float ScrollEasingTime {
        get => _scrollEasingTime;
        set => _scrollEasingTime = value;
    }

    public float MinValue {
        get => _minValue;
        set {
            if (Mathf.Approximately(value, _minValue)) return;
            _minValue = value;
            Value = Value;
            SetAutoHide();
            SetAutoDisableButtons();
        }
    }

    public float MaxValue {
        get => _maxValue;
        set {
            if (Mathf.Approximately(value, _maxValue)) return;
            _maxValue = value;
            Value = Value;
            Invalidate();
            SetAutoHide();
            SetAutoDisableButtons();
        }
    }

    public float StepSize {
        get => _stepSize;
        set {
            value = Mathf.Max(0f, value);
            if (Mathf.Approximately(value, _stepSize)) return;
            _stepSize = value;
            Value = Value;
            Invalidate();
        }
    }

    public float ScrollSize {
        get => _scrollSize;
        set {
            value = Mathf.Max(0f, value);
            if (Mathf.Approximately(value, _scrollSize)) return;
            _scrollSize = value;
            Value = Value;
            Invalidate();
            SetAutoHide();
            SetAutoDisableButtons();
        }
    }


    public float IncrementAmount {
        get => _increment;
        set {
            value = Mathf.Max(0f, value);
            if (!Mathf.Approximately(value, _increment)) _increment = value;
        }
    }

    public UIOrientation Orientation {
        get => _orientation;
        set {
            if (value == _orientation) return;
            _orientation = value;
            Invalidate();
        }
    }

    public float Value {
        get => _rawValue;
        set {
            value = AdjustValue(value);
            if (!Mathf.Approximately(value, _rawValue)) {
                _rawValue = value;
                OnValueChanged();
            }

            UpdateThumb(_rawValue);
            SetAutoHide();
            SetAutoDisableButtons();
        }
    }

    public bool AutoHide {
        get => _autoHide;
        set {
            if (value == _autoHide) return;
            _autoHide = value;
            Invalidate();
            SetAutoHide();
        }
    }


    public bool AutoDisableButtons {
        get => _autoDisableButtons;
        set {
            if (value == _autoDisableButtons) return;
            _autoDisableButtons = value;
            Invalidate();
            SetAutoDisableButtons();
        }
    }

    public UIComponent ThumbObject {
        get => _thumbObject;
        set {
            if (value == _thumbObject) return;
            _thumbObject = value;
            Invalidate();
        }
    }


    public UIComponent TrackObject {
        get => _trackObject;
        set {
            if (value == _trackObject) return;
            _trackObject = value;
            Invalidate();
        }
    }


    public UIComponent IncrementButton {
        get => _incrementButton;
        set {
            if (value == _incrementButton) return;
            _incrementButton = value;
            Invalidate();
        }
    }

    public UIComponent DecrementButton {
        get => _decrementButton;
        set {
            if (value == _decrementButton) return;
            _decrementButton = value;
            Invalidate();
        }
    }


    public RectOffset ThumbPadding {
        get => _thumbPadding ??= new RectOffset();
        set {
            if (Orientation == UIOrientation.Horizontal)
                value.top = value.bottom = 0;
            else
                value.left = value.right = 0;

            if (Equals(value, _thumbPadding)) return;
            _thumbPadding = value;
            UpdateThumb(_rawValue);
        }
    }

    public static Scrollbar AddScrollbar(UIComponent parent, ScrollContainer scrollablePanel, Vector2 size) {
        var scrollbar = parent.AddUIComponent<Scrollbar>();
        scrollbar.size = size;
        scrollbar.MinValue = 0;
        scrollbar.Value = 0;
        scrollbar.IncrementAmount = 50f;
        var trackSprite = scrollbar.AddUIComponent<SlicedSprite>();
        trackSprite.relativePosition = Vector2.zero;
        trackSprite.autoSize = true;
        trackSprite.anchor = UIAnchorStyle.All;
        trackSprite.size = trackSprite.parent.size;
        trackSprite.FillDirection = UIFillDirection.Vertical;
        scrollbar.TrackObject = trackSprite;
        var thumbSprite = scrollbar.AddUIComponent<SlicedSprite>();
        thumbSprite.relativePosition = Vector2.zero;
        thumbSprite.FillDirection = UIFillDirection.Vertical;
        thumbSprite.autoSize = true;
        thumbSprite.width = thumbSprite.parent.width;
        thumbSprite.Atlas = Atlases.Shared;
        thumbSprite.SpriteName = SharedAtlasKeys.RoundRect4;
        thumbSprite.color = UIColors.White;
        scrollbar.ThumbObject = thumbSprite;
        scrollablePanel.VerticalScrollbar = scrollbar;
        return scrollbar;
    }

    public override Vector2 CalculateMinimumSize() {
        var array = new Vector2[3];
        if (DecrementButton is not null) array[0] = DecrementButton.CalculateMinimumSize();

        if (IncrementButton is not null) array[1] = IncrementButton.CalculateMinimumSize();

        if (ThumbObject is not null) array[2] = ThumbObject.CalculateMinimumSize();

        var zero = Vector2.zero;
        if (Orientation == UIOrientation.Horizontal) {
            zero.x = array[0].x + array[1].x + array[2].x;
            zero.y = Mathf.Max(array[0].y, array[1].y, array[2].y);
        }
        else {
            zero.x = Mathf.Max(array[0].x, array[1].x, array[2].x);
            zero.y = array[0].y + array[1].y + array[2].y;
        }

        return Vector2.Max(zero, base.CalculateMinimumSize());
    }


    public override bool canFocus => (isEnabled && isVisible) || base.canFocus;

    protected virtual void OnValueChanged() {
        SetAutoHide();
        SetAutoDisableButtons();
        Invalidate();
        EventValueChanged?.Invoke(this, Value);
        InvokeUpward("OnValueChanged", Value);
    }


    public override void OnEnable() {
        base.OnEnable();
        AttachEvents();
    }

    public override void OnDisable() {
        base.OnDisable();
        DetachEvents();
    }

    public override void Update() {
        base.Update();
        if (_easing == null) return;
        if (!_easing.isDone) {
            Value = _easing;
            return;
        }

        Value = _easing.endValue;
        _easing = null;
    }

    private void SetAutoHide() {
        if (!_autoHide || !Application.isPlaying) return;
        if (Mathf.CeilToInt(ScrollSize) >= Mathf.CeilToInt(MaxValue - MinValue)) {
            Hide();
            return;
        }

        Show();
    }

    private void SetAutoDisableButtons() {
        if (!_autoDisableButtons || !Application.isPlaying) return;

        if (_decrementButton is not null) _decrementButton.isEnabled = Value.Quantize(StepSize) >= MinValue && !Value.NearlyEqual(MinValue, 1E-05f);

        if (_incrementButton is not null) {
            var num = Mathf.Max(MaxValue - MinValue, 0f);
            var num2 = Mathf.Max(num - ScrollSize, 0f) + MinValue;
            _incrementButton.isEnabled = Value.Quantize(StepSize) <= num2 && !Value.NearlyEqual(num2, 1E-05f);
        }
    }

    private void ScrollEase(float delta) {
        if (_easing == null) {
            _easing = new AnimatedFloat(Value, Value + delta, _scrollEasingTime, _scrollEasingType);
        }
        else {
            var num = Mathf.Max(MaxValue - MinValue, 0f);
            var num2 = Mathf.Max(num - ScrollSize, 0f) + MinValue;
            var num3 = Mathf.Clamp(_easing.endValue + delta, MinValue, num2);
            if (!Mathf.Approximately(num3, _easing.endValue)) {
                _easing.startValue = _easing.value;
                _easing.endValue = num3;
            }
        }

        Value = _easing;
    }

    private void IncrementPressed(UIComponent sender, UIMouseEventParameter p) {
        if (!p.buttons.IsFlagSet(UIMouseButton.Left)) return;
        ScrollEase(IncrementAmount);
        p.Use();
    }

    private void DecrementPressed(UIComponent sender, UIMouseEventParameter p) {
        if (!p.buttons.IsFlagSet(UIMouseButton.Left)) return;
        ScrollEase(-IncrementAmount);
        p.Use();
    }

    private void AttachEvents() {
        if (!Application.isPlaying) return;

        if (IncrementButton is not null) {
            IncrementButton.eventMouseDown += IncrementPressed;
            IncrementButton.eventMouseHover += IncrementPressed;
        }

        if (DecrementButton is not null) {
            DecrementButton.eventMouseDown += DecrementPressed;
            DecrementButton.eventMouseHover += DecrementPressed;
        }
    }

    private void DetachEvents() {
        if (!Application.isPlaying) return;

        if (IncrementButton is not null) {
            IncrementButton.eventMouseDown -= IncrementPressed;
            IncrementButton.eventMouseHover -= IncrementPressed;
        }

        if (DecrementButton is not null) {
            DecrementButton.eventMouseDown -= DecrementPressed;
            DecrementButton.eventMouseHover -= DecrementPressed;
        }
    }

    private float AdjustValue(float valueToAdjust) {
        var num = Mathf.Max(MaxValue - MinValue, 0f);
        var num2 = Mathf.Max(num - ScrollSize, 0f) + MinValue;
        var num3 = Mathf.Max(Mathf.Min(num2, valueToAdjust), MinValue);
        return num3.Quantize(StepSize);
    }

    private void UpdateThumb(float rawValue) {
        if (components == null || components.Count == 0 || ThumbObject is null || TrackObject is null || !isVisible) return;
        var num = MaxValue - MinValue;
        if (num <= 0f || num <= ScrollSize) {
            ThumbObject.isVisible = false;
            return;
        }

        ThumbObject.isVisible = true;
        var num2 = Orientation == UIOrientation.Horizontal ? TrackObject.width - ThumbPadding.horizontal : TrackObject.height - ThumbPadding.vertical;
        var num3 = Orientation == UIOrientation.Horizontal ? Mathf.Max(ScrollSize / num * num2, ThumbObject.minimumSize.x) : Mathf.Max(ScrollSize / num * num2, ThumbObject.minimumSize.y);
        var vector = Orientation == UIOrientation.Horizontal ? new Vector2(num3, ThumbObject.height) : new Vector2(ThumbObject.width, num3);
        ThumbObject.size = vector;
        var num4 = (rawValue - MinValue) / (num - ScrollSize);
        var num5 = num4 * (num2 - num3);
        var vector2 = Orientation == UIOrientation.Horizontal ? Vector3.right : Vector3.up;
        var vector3 = Orientation == UIOrientation.Horizontal ? new Vector3(0f, (TrackObject.height - ThumbObject.height) * 0.5f) : new Vector3((TrackObject.width - ThumbObject.width) * 0.5f, 0f);
        if (Orientation == UIOrientation.Horizontal)
            vector3.x = ThumbPadding.left;
        else
            vector3.y = ThumbPadding.top;

        if (ThumbObject.parent == this) {
            ThumbObject.relativePosition = TrackObject.relativePosition + vector3 + vector2 * num5;
            return;
        }

        ThumbObject.relativePosition = vector2 * num5 + vector3;
    }

    protected override void OnKeyDown(UIKeyEventParameter p) {
        if (builtinKeyNavigation) {
            if (Orientation == UIOrientation.Horizontal) {
                if (p.keycode == KeyCode.LeftArrow) {
                    ScrollEase(-IncrementAmount);
                    p.Use();
                    return;
                }

                if (p.keycode == KeyCode.RightArrow) {
                    ScrollEase(IncrementAmount);
                    p.Use();
                    return;
                }
            }
            else {
                if (p.keycode == KeyCode.UpArrow) {
                    ScrollEase(-IncrementAmount);
                    p.Use();
                    return;
                }

                if (p.keycode == KeyCode.DownArrow) {
                    ScrollEase(IncrementAmount);
                    p.Use();
                    return;
                }
            }
        }

        base.OnKeyDown(p);
    }

    protected override void OnMouseWheel(UIMouseEventParameter p) {
        Value += IncrementAmount * -p.wheelDelta;
        p.Use();
        Invoke("OnMouseWheel", p);
    }

    protected override void OnMouseHover(UIMouseEventParameter p) {
        if (p.source == IncrementButton || p.source == DecrementButton || p.source == ThumbObject) return;

        if (p.source != TrackObject || !p.buttons.IsFlagSet(UIMouseButton.Left)) {
            base.OnMouseHover(p);
            return;
        }

        UpdateFromTrackClick(p);
        p.Use();
        Invoke("OnMouseHover", p);
    }

    protected override void OnMouseMove(UIMouseEventParameter p) {
        if (p.source == IncrementButton || p.source == DecrementButton) return;

        if ((p.source != TrackObject && p.source != ThumbObject) || !p.buttons.IsFlagSet(UIMouseButton.Left)) {
            base.OnMouseMove(p);
            return;
        }

        Value = Mathf.Max(MinValue, GetValueFromMouseEvent(p) - ScrollSize * 0.5f);
        p.Use();
        Invoke("OnMouseMove", p);
    }

    protected override void OnMouseDown(UIMouseEventParameter p) {
        if (p.buttons.IsFlagSet(UIMouseButton.Left)) Focus();

        if (p.source == IncrementButton || p.source == DecrementButton) return;

        if ((p.source != TrackObject && p.source != ThumbObject) || !p.buttons.IsFlagSet(UIMouseButton.Left)) {
            base.OnMouseDown(p);
            return;
        }

        if (p.source == ThumbObject) {
            ThumbObject.Raycast(p.ray, out var vector);
            var vector2 = ThumbObject.transform.position + ThumbObject.pivot.TransformToCenter(ThumbObject.size * PixelsToUnits(), ThumbObject.arbitraryPivotOffset);
            _thumbMouseOffset = vector2 - vector;
        }
        else {
            UpdateFromTrackClick(p);
        }

        p.Use();
        Invoke("OnMouseDown", p);
    }

    private float GetValueFromMouseEvent(UIMouseEventParameter p) {
        var corners = TrackObject.GetCorners();
        var vector = corners[0];
        var vector2 = corners[Orientation == UIOrientation.Horizontal ? 1 : 2];
        var plane = new Plane(transform.TransformDirection(Vector3.back), vector);
        var ray = p.ray;
        if (!plane.Raycast(ray, out var num)) return _rawValue;

        var vector3 = ray.origin + ray.direction * num;
        if (p.source == ThumbObject) vector3 += _thumbMouseOffset;

        var vector4 = IntersectionTest.ClosestPointOnLine(vector, vector2, vector3);
        var num2 = (vector4 - vector).magnitude / (vector2 - vector).magnitude;
        return MinValue + (MaxValue - MinValue) * num2;
    }

    private void UpdateFromTrackClick(UIMouseEventParameter p) {
        var valueFromMouseEvent = GetValueFromMouseEvent(p);
        if (valueFromMouseEvent > _rawValue + ScrollSize) {
            Value += ScrollSize;
            return;
        }

        if (valueFromMouseEvent < _rawValue) Value -= ScrollSize;
    }

    protected override void OnRebuildRenderData() {
        UpdateThumb(_rawValue);
        base.OnRebuildRenderData();
    }

    protected override void OnVisibilityChanged() {
        base.OnVisibilityChanged();
        foreach (var component in components) component.isVisible = isVisible;
    }
}