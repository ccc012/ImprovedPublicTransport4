using System;

namespace CSLModsCommon.UI;

public class FloatValueField : ValueFieldBase<float, FloatValueField> {
    protected override float ValueDecrease(UIValueSteppingRate steppingRate) {
        var rate = GetStep(steppingRate);
        return (float)Math.Round(Value - rate, 1);
    }

    protected override float ValueIncrease(UIValueSteppingRate steppingRate) {
        var rate = GetStep(steppingRate);
        return (float)Math.Round(Value + rate, 1);
    }

    protected override float GetStep(UIValueSteppingRate steppingRate) => steppingRate switch {
        UIValueSteppingRate.Fast => WheelStep * 10,
        UIValueSteppingRate.Slow => WheelStep / 10,
        _ => WheelStep
    };
}