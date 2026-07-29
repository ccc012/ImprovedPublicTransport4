using System;

namespace CSLModsCommon.UI.ValueFields;

public abstract class ValueBinderBase<T> : IValueBinder<T> where T : IComparable<T> {
    public T Value { get; set; }
    public T MinValue { get; set; }
    public T MaxValue { get; set; }
    public bool UseMin { get; set; }
    public bool UseMax { get; set; }

    protected T Clamp(T v) {
        if (UseMin && v.CompareTo(MinValue) < 0) v = MinValue;
        if (UseMax && v.CompareTo(MaxValue) > 0) v = MaxValue;
        return v;
    }

    public abstract T Increase(UIValueSteppingRate rate);
    public abstract T Decrease(UIValueSteppingRate rate);
    public abstract string ToText();
    public abstract T ParseFromText(string text);
}