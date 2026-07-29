namespace CSLModsCommon.UI.ValueFields;

public class StringValueBinder : ValueBinderBase<string> {
    public StringValueBinder(string initial = "") => Value = initial;

    public override string Increase(UIValueSteppingRate rate) => Value;

    public override string Decrease(UIValueSteppingRate rate) => Value;

    public override string ToText() => Value ?? string.Empty;

    public override string ParseFromText(string text) {
        Value = text ?? string.Empty;
        return Value;
    }
}