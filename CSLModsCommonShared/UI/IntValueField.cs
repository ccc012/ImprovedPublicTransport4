namespace CSLModsCommon.UI;

public class IntValueField : ValueFieldBase<int, IntValueField> {
    protected override int ValueDecrease(UIValueSteppingRate steppingRate) {
        var rate = GetStep(steppingRate);
        return Value - rate;
    }

    protected override int ValueIncrease(UIValueSteppingRate steppingRate) {
        var rate = GetStep(steppingRate);
        return Value + rate;
    }

    protected override int GetStep(UIValueSteppingRate steppingRate) => steppingRate switch {
        UIValueSteppingRate.Fast => WheelStep * 10,
        UIValueSteppingRate.Slow => WheelStep / 10,
        _ => WheelStep
    };
}