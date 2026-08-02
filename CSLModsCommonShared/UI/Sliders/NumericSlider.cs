using ColossalFramework.UI;
using CSLModsCommon.UI.Containers;
using UnityEngine;

namespace CSLModsCommon.UI.Sliders;

/// <summary>A <see cref="Slider"/> paired with an editable numeric field that stays in sync with it.</summary>
public class NumericSlider : LiteContainer {
    private bool _syncing;

    public event UIElementEventHandler<NumericSlider, float> ValueChanged;

    public Slider SliderElement { get; private set; }

    public FloatValueField FieldElement { get; private set; }

    public float MinValue {
        get => SliderElement.MinValue;
        set {
            SliderElement.MinValue = value;
            FieldElement.MinValue = value;
        }
    }

    public float MaxValue {
        get => SliderElement.MaxValue;
        set {
            SliderElement.MaxValue = value;
            FieldElement.MaxValue = value;
        }
    }

    public float StepSize {
        get => SliderElement.StepSize;
        set => SliderElement.StepSize = value;
    }

    public float Value {
        get => SliderElement.Value;
        set => SliderElement.Value = value;
    }

    public override void Awake() {
        base.Awake();
        size = new Vector2(300, 24);
        _direction = FlexDirection.Row;
        _autoLayout = true;
        _columnGap = 8;

        SliderElement = AddUIComponent<Slider>();
        SliderElement.size = new Vector2(200, 24);
        SliderElement.SetGreenStyle();
        SliderElement.ValueChanged += OnSliderValueChanged;

        FieldElement = AddUIComponent<FloatValueField>();
        FieldElement.size = new Vector2(70, 24);
        FieldElement.SetStyle(StyleType.OptionPanelStyle);
        FieldElement.CanWheel = true;
        FieldElement.UseValueLimit = true;
        FieldElement.EventValueChanged += OnFieldValueChanged;
    }

    private void OnSliderValueChanged(Slider slider, float value) {
        if (_syncing) return;
        _syncing = true;
        FieldElement.Value = value;
        _syncing = false;
        ValueChanged?.Invoke(this, value);
    }

    private void OnFieldValueChanged(FloatValueField field, float value) {
        if (_syncing) return;
        _syncing = true;
        SliderElement.Value = value;
        _syncing = false;
        ValueChanged?.Invoke(this, value);
    }
}
