using UnityEngine;

namespace CSLModsCommon.UI;

public class ByteValueField : ValueFieldBase<byte, ByteValueField> {
    protected override byte ValueDecrease(UIValueSteppingRate steppingRate) => Value - GetStep(steppingRate) < MinValue ? MinValue : (byte)(Value - GetStep(steppingRate));

    protected override byte ValueIncrease(UIValueSteppingRate steppingRate) => (byte)Mathf.Min(Value + GetStep(steppingRate), MaxValue);

    protected override byte GetStep(UIValueSteppingRate steppingRate) => steppingRate switch {
        UIValueSteppingRate.Fast => (byte)(WheelStep * 10),
        UIValueSteppingRate.Slow => (byte)(WheelStep / 10),
        _ => WheelStep
    };
}