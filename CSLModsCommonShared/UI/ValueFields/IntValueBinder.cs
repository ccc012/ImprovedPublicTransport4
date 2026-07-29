using UnityEngine;

namespace CSLModsCommon.UI.ValueFields;

public class IntValueBinder : ValueBinderBase<int> {
    public int Step { get; set; } = 1;

    public IntValueBinder(int initial = 0) => Value = initial;

    private int StepForRate(UIValueSteppingRate rate) => rate switch {
        UIValueSteppingRate.Fast => Step * 10,
        UIValueSteppingRate.Slow => Mathf.Max(1, Step / 10),
        _ => Step
    };

    public override int Increase(UIValueSteppingRate rate) {
        var s = StepForRate(rate);
        var v = Value + s;
        v = Clamp(v);
        Value = v;
        return Value;
    }

    public override int Decrease(UIValueSteppingRate rate) {
        var s = StepForRate(rate);
        var v = Value - s;
        v = Clamp(v);
        Value = v;
        return Value;
    }

    public override string ToText() => Value.ToString();

    public override int ParseFromText(string text) {
        if (int.TryParse(text, out var v)) {
            v = Clamp(v);
            Value = v;
            return v;
        }

        return Value;
    }
}