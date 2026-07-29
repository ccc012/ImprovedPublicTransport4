using System;

namespace CSLModsCommon.UI.ValueFields;

public class FloatValueBinder : ValueBinderBase<float> {
    public float Step { get; set; } = 1f;
    public int DecimalPlaces { get; set; } = 3;

    public FloatValueBinder(float initial = 0f) => Value = initial;

    private float StepForRate(UIValueSteppingRate rate) => rate switch {
        UIValueSteppingRate.Fast => Step * 10f,
        UIValueSteppingRate.Slow => Step * 0.1f,
        _ => Step
    };

    public override float Increase(UIValueSteppingRate rate) {
        var s = StepForRate(rate);
        var v = Value + s;
        v = Clamp(v);
        Value = v;
        return Value;
    }

    public override float Decrease(UIValueSteppingRate rate) {
        var s = StepForRate(rate);
        var v = Value - s;
        v = Clamp(v);
        Value = v;
        return Value;
    }

    public override string ToText() => Math.Round(Value, DecimalPlaces).ToString();

    public override float ParseFromText(string text) {
        if (float.TryParse(text, out var v)) {
            v = Clamp(v);
            Value = v;
            return v;
        }

        return Value;
    }
}