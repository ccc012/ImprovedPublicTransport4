namespace CSLModsCommon.UI.ValueFields;

public interface IValueBinder<T> {
    T Value { get; set; }
    T MinValue { get; set; }
    T MaxValue { get; set; }
    bool UseMin { get; set; }
    bool UseMax { get; set; }
    T Increase(UIValueSteppingRate rate);
    T Decrease(UIValueSteppingRate rate);
    string ToText();
    T ParseFromText(string text);
}