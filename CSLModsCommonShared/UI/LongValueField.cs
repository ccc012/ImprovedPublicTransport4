namespace CSLModsCommon.UI; 
public class LongValueField : ValueFieldBase<long, LongValueField> {
    protected override long ValueDecrease(UIValueSteppingRate steppingRate) {
        var rate = GetStep(steppingRate);
        return Value - rate;
    }

    protected override long ValueIncrease(UIValueSteppingRate steppingRate) {
        var rate = GetStep(steppingRate);
        return Value + rate;
    }

    protected override long GetStep(UIValueSteppingRate steppingRate) => steppingRate switch {
        UIValueSteppingRate.Fast => WheelStep * 10,
        UIValueSteppingRate.Slow => WheelStep / 10,
        _ => WheelStep
    };
}