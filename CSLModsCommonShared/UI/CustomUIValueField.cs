using System;

namespace CSLModsCommon.UI;

/// <summary>
/// A <see cref="ValueFieldBase{T,V}"/> for arbitrary value types where the caller supplies the
/// increment/decrement/step logic via delegates, instead of writing a new subclass per type
/// (compare IntValueField/FloatValueField/ByteValueField/LongValueField).
/// </summary>
public class CustomUIValueField<T> : ValueFieldBase<T, CustomUIValueField<T>> where T : IComparable<T> {
    public Func<T, UIValueSteppingRate, T> ValueDecreaseFunc { get; set; }
    public Func<T, UIValueSteppingRate, T> ValueIncreaseFunc { get; set; }
    public Func<UIValueSteppingRate, T> GetStepFunc { get; set; }

    protected override T ValueDecrease(UIValueSteppingRate steppingRate) =>
        ValueDecreaseFunc != null ? ValueDecreaseFunc(Value, steppingRate) : Value;

    protected override T ValueIncrease(UIValueSteppingRate steppingRate) =>
        ValueIncreaseFunc != null ? ValueIncreaseFunc(Value, steppingRate) : Value;

    protected override T GetStep(UIValueSteppingRate steppingRate) =>
        GetStepFunc != null ? GetStepFunc(steppingRate) : WheelStep;
}
