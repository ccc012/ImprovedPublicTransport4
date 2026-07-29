using ColossalFramework;
using ColossalFramework.UI;
using CSLModsCommon.KeyBindings;
using CSLModsCommon.UI.Atlas;
using System;
using UnityEngine;

namespace CSLModsCommon.UI.Sliders;

public class Slider : UIElement {
    public event UIElementEventHandler<Slider, float> ValueChanged;

    protected float _minValue;
    protected float _maxValue = 100f;
    protected float _value = 10f;
    protected float _stepSize = 1f;
    protected bool _canWheel;
    protected UIComponent _thumb;
    protected Vector2 _thumbOffset;
    protected Padding _thumbPadding;
    protected UIComponent _fill;
    protected Padding _fillPadding;
    protected UIFillMode _fillMode = UIFillMode.Fill;
    protected UIOrientation _orientation;

    public float MinValue {
        get => _minValue;
        set {
            if (Mathf.Approximately(value, _minValue)) return;
            _minValue = value;
            if (_value < value)
                Value = value;
            Invalidate();
        }
    }

    public float MaxValue {
        get => _maxValue;
        set {
            if (Mathf.Approximately(value, _maxValue)) return;
            _maxValue = value;
            if (_value > value)
                Value = value;
            Invalidate();
        }
    }

    public float Value {
        get => _value;
        set {
            value = Mathf.Max(MinValue, Mathf.Min(MaxValue, value)).Quantize(StepSize);
            if (Mathf.Approximately(value, _value)) return;
            _value = value;
            OnRawValueChanged();
        }
    }

    public float StepSize {
        get => _stepSize;
        set {
            value = Mathf.Max(0f, value);
            if (Mathf.Approximately(value, _stepSize)) return;
            _stepSize = value;
            Value = _value.Quantize(value);
            Invalidate();
        }
    }

    public bool CanWheel {
        get => _canWheel;
        set {
            if (_canWheel == value) return;
            _canWheel = value;
            Invalidate();
        }
    }

    public UIOrientation Orientation {
        get => _orientation;
        set {
            if (value == _orientation) return;
            _orientation = value;
            Invalidate();
            UpdateValueIndicators(_value);
        }
    }

    public UIComponent Thumb {
        get => _thumb;
        set {
            if (value == _thumb) return;
            _thumb = value;
            Invalidate();
            UpdateValueIndicators(_value);
        }
    }

    public Vector2 ThumbOffset {
        get => _thumbOffset;
        set {
            if (!(Vector2.Distance(value, _thumbOffset) > 1E-45f)) return;
            _thumbOffset = value;
            UpdateValueIndicators(_value);
        }
    }

    public Padding ThumbPadding {
        get => _thumbPadding;
        set {
            if (_thumbPadding.Equals(value)) return;
            _thumbPadding = value;
            UpdateValueIndicators(_value);
            Invalidate();
        }
    }

    public UIComponent Fill {
        get => _fill;
        set {
            if (value == _fill) return;
            _fill = value;
            Invalidate();
            UpdateValueIndicators(_value);
        }
    }

    public Padding FillPadding {
        get => _fillPadding;
        set {
            if (_fillPadding.Equals(value)) return;
            _fillPadding = value;
            UpdateValueIndicators(_value);
            Invalidate();
        }
    }

    public UIFillMode FillMode {
        get => _fillMode;
        set {
            if (value == _fillMode) return;
            _fillMode = value;
            Invalidate();
        }
    }

    public override void Awake() {
        base.Awake();
        _thumbPadding = Padding.GetAPadding(this);
        _fillPadding = Padding.GetAPadding(this);
    }

    public void SetGradientStyle() {
        _bgAtlas = Atlases.Shared;
        _bgSprites.NormalSprite = SharedAtlasKeys.GradientSlider;
        _bgColors.DisabledColor = new Color32(130, 130, 130, 255);
        var thumb = AddUIComponent<UISprite>();
        thumb.atlas = Atlases.Shared;
        thumb.spriteName = SharedAtlasKeys.Circle;
        thumb.color = new Color32(220, 220, 220, 255);
        thumb.disabledColor = new Color32(80, 80, 80, 255);
        thumb.size = new Vector2(size.y + 4, size.y + 4);
        Thumb = thumb;
        ThumbPadding.SetAll((int)thumb.size.y / 2, (int)thumb.size.y / 2, 0, 0);
    }

    public void SetBlueStyle() {
        _bgAtlas = Atlases.Shared;
        _bgSprites.NormalSprite = SharedAtlasKeys.RoundRect8;
        _bgColors.NormalColor = UIColors.Bg1ElementNormal;
        _bgColors.DisabledColor = UIColors.Bg1ElementDisabled;
        var fill = AddUIComponent<UISlicedSprite>();
        fill.atlas = Atlases.Shared;
        fill.spriteName = SharedAtlasKeys.RoundRect8;
        fill.color = UIColors.BlueNormal;
        fill.disabledColor = UIColors.BlueDisabled;
        fill.size = size;
        Fill = fill;
        var thumb = AddUIComponent<UISprite>();
        thumb.atlas = Atlases.Shared;
        thumb.spriteName = SharedAtlasKeys.Circle;
        thumb.color = new Color32(220, 220, 220, 255);
        thumb.disabledColor = new Color32(110, 110, 110, 255);
        thumb.size = new Vector2(size.y + 4, size.y + 4);
        Thumb = thumb;
        ThumbPadding.SetAll((int)thumb.size.y / 2, (int)thumb.size.y / 2, 0, 0);
    }

    public void SetGreenStyle() {
        _bgAtlas = Atlases.Shared;
        _bgSprites.NormalSprite = SharedAtlasKeys.RoundRect8;
        _bgColors.NormalColor = UIColors.BgElementNormal;
        _bgColors.DisabledColor = UIColors.BgElementDisabled;
        var fillIndicator = AddUIComponent<UISlicedSprite>();
        fillIndicator.atlas = Atlases.Shared;
        fillIndicator.spriteName = SharedAtlasKeys.RoundRect8;
        fillIndicator.color = UIColors.GreenNormal;
        fillIndicator.disabledColor = UIColors.GreenDisabled;
        fillIndicator.size = size;
        var thumbObject = AddUIComponent<UISprite>();
        thumbObject.atlas = Atlases.Shared;
        thumbObject.spriteName = SharedAtlasKeys.Circle;
        thumbObject.color = new Color32(220, 220, 220, 255);
        thumbObject.disabledColor = new Color32(110, 110, 110, 255);
        thumbObject.size = new Vector2(size.y + 4, size.y + 4);
        Thumb = thumbObject;
        ThumbPadding.SetAll((int)thumbObject.size.y / 2, (int)thumbObject.size.y / 2, 0, 0);
        Fill = fillIndicator;
    }

    public override bool canFocus => (isEnabled && isVisible) || base.canFocus;

    protected virtual void OnRawValueChanged() {
        Invalidate();
        UpdateValueIndicators(_value);
        ValueChanged?.Invoke(this, Value);
        InvokeUpward("OnValueChanged", Value);
    }

    protected virtual float ValueDecrease(UIValueSteppingRate steppingRate) {
        var rate = GetStep(steppingRate);
        return (float)Math.Round(Value - rate, 1);
    }

    protected virtual float ValueIncrease(UIValueSteppingRate steppingRate) {
        var rate = GetStep(steppingRate);
        return (float)Math.Round(Value + rate, 1);
    }

    protected virtual float GetStep(UIValueSteppingRate steppingRate) => steppingRate switch {
        UIValueSteppingRate.Fast => StepSize * 10,
        UIValueSteppingRate.Slow => StepSize / 10,
        _ => StepSize
    };

    public override void Start() {
        base.Start();
        UpdateValueIndicators(_value);
    }

    public override void OnEnable() {
        if (size.magnitude < 1E-45f) size = new Vector2(100f, 25f);

        base.OnEnable();
        UpdateValueIndicators(_value);
    }

    protected virtual void UpdateValueIndicators(float rawValue) {
        if (Thumb != null) {
            GetBoundingPoints(true, out var vector, out var a);
            var vector2 = a - vector;
            var d = (rawValue - MinValue) / (MaxValue - MinValue) * vector2.magnitude;
            Vector3 b = ThumbOffset * PixelsToUnits();
            var thumbPosition = vector + vector2.normalized * d + b;
            Thumb.pivot = UIPivotPoint.MiddleCenter;
            Thumb.transform.position = thumbPosition;
            Thumb.ResetLayout();
        }

        if (Fill == null) return;

        var num = (rawValue - MinValue) / (MaxValue - MinValue);
        Vector3 indicatorRelativePosition = new(FillPadding.Left, FillPadding.Top);
        var indicatorSize = size - new Vector2(FillPadding.Horizontal, FillPadding.Vertical);
        var uiSprite = Fill as UISprite;
        if (uiSprite != null && FillMode == UIFillMode.Fill) {
            uiSprite.fillDirection = Orientation == UIOrientation.Horizontal ? UIFillDirection.Horizontal : UIFillDirection.Vertical;
            uiSprite.fillAmount = num;
        }
        else if (Orientation == UIOrientation.Horizontal) {
            indicatorSize.x = width * num - FillPadding.Horizontal;
        }
        else {
            indicatorSize.y = height * num - FillPadding.Vertical;
        }

        Fill.size = indicatorSize;
        Fill.relativePosition = indicatorRelativePosition;
    }

    protected virtual float GetValueFromMouseEvent(UIMouseEventParameter p) {
        GetBoundingPoints(true, out var vector, out var vector2);
        Plane plane = new(transform.TransformDirection(Vector3.back), vector);
        var ray = p.ray;
        if (!plane.Raycast(ray, out var d)) return _value;

        var test = ray.origin + ray.direction * d;
        var a = IntersectionTest.ClosestPointOnLine(vector, vector2, test);
        var num = (a - vector).magnitude / (vector2 - vector).magnitude;
        return MinValue + (MaxValue - MinValue) * num;
    }

    protected override void OnKeyDown(UIKeyEventParameter p) {
        if (builtinKeyNavigation && m_IsMouseHovering) {
            p.Use();
            if (Orientation == UIOrientation.Horizontal) {
                if (p.keycode == KeyCode.LeftArrow) {
                    Value -= _stepSize;
                    p.Use();
                    return;
                }

                if (p.keycode == KeyCode.RightArrow) {
                    Value += _stepSize;
                    p.Use();
                    return;
                }
            }
            else {
                if (p.keycode == KeyCode.UpArrow) {
                    Value -= _stepSize;
                    p.Use();
                    return;
                }

                if (p.keycode == KeyCode.DownArrow) {
                    Value += _stepSize;
                    p.Use();
                    return;
                }
            }
        }

        base.OnKeyDown(p);
    }

    protected override void OnMouseWheel(UIMouseEventParameter p) {
        if (!_canWheel) return;
        p.Use();
        var typeRate = GetSteppingRate();
        Value = p.wheelDelta < 0 ? ValueDecrease(typeRate) : ValueIncrease(typeRate);
        Invoke("OnMouseWheel", p);
    }

    protected override void OnMouseMove(UIMouseEventParameter p) {
        if (!p.buttons.IsFlagSet(UIMouseButton.Left)) {
            base.OnMouseMove(p);
            return;
        }

        Value = GetValueFromMouseEvent(p);
        p.Use();
        Invoke("OnMouseMove", p);
    }

    protected override void OnMouseDown(UIMouseEventParameter p) {
        if (!p.buttons.IsFlagSet(UIMouseButton.Left)) {
            base.OnMouseMove(p);
            return;
        }

        Focus();
        Value = GetValueFromMouseEvent(p);
        p.Use();
        Invoke("OnMouseDown", p);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();
        UpdateValueIndicators(_value);
    }

    protected override void OnVisibilityChanged() => UpdateValueIndicators(_value);

    protected override void OnRebuildRenderData() => RenderBackground();

    protected override void RenderBackground() {
        if (_bgRenderData is null) {
            _bgRenderData = UIRenderData.Obtain();
            m_RenderData.Add(_bgRenderData);
        }
        else {
            _bgRenderData.Clear();
        }

        if (_bgAtlas == null) return;
        _bgRenderData.material = _bgAtlas.material;
        var spriteInfo = _bgAtlas[_bgSprites.NormalSprite];
        if (spriteInfo is null) return;
        var renderColor = GetBgRenderColor(isEnabled ? UIState.Normal : UIState.Disabled);
        RenderOptions options = new() {
            Atlas = _bgAtlas,
            Color = renderColor,
            FillAmount = 1f,
            Flip = UISpriteFlip.None,
            Offset = pivot.TransformToUpperLeft(size, arbitraryPivotOffset),
            PixelsToUnits = PixelsToUnits(),
            Size = size,
            SpriteInfo = spriteInfo
        };
        if (spriteInfo.isSliced) {
            SlicedSprite.RenderSprite(_bgRenderData, options);
            return;
        }

        Sprite.RenderSprite(_bgRenderData, options);
    }

    private void GetBoundingPoints(bool convertToWorld, out Vector3 start, out Vector3 end) {
        var vector = pivot.TransformToUpperLeft(size, arbitraryPivotOffset);
        if (Orientation == UIOrientation.Vertical) {
            end = new Vector3(vector.x + size.x * 0.5f, vector.y - ThumbPadding.Top);
            start = end - new Vector3(0f, size.y - ThumbPadding.Vertical);
        }
        else {
            start = new Vector3(vector.x + ThumbPadding.Left, vector.y - size.y * 0.5f);
            end = start + new Vector3(size.x - ThumbPadding.Horizontal, 0f);
        }

        if (convertToWorld) {
            var d = PixelsToUnits();
            var localToWorldMatrix = transform.localToWorldMatrix;
            start = localToWorldMatrix.MultiplyPoint(start * d);
            end = localToWorldMatrix.MultiplyPoint(end * d);
        }
    }

    private UIValueSteppingRate GetSteppingRate() {
        if (ModifierFlagsExtensions.IsShiftDown())
            return UIValueSteppingRate.Fast;
        return ModifierFlagsExtensions.IsControlDown() ? UIValueSteppingRate.Slow : UIValueSteppingRate.Normal;
    }
}