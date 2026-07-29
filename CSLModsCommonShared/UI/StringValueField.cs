namespace CSLModsCommon.UI;

public class StringValueField : ValueFieldBase<string, StringValueField> {
    public override void Awake() {
        base.Awake();
        builtinKeyNavigation = false;
    }

    protected override string GetStep(UIValueSteppingRate steppingRate) => string.Empty;

    protected override string ValueDecrease(UIValueSteppingRate steppingRate) => string.Empty;

    protected override string ValueIncrease(UIValueSteppingRate steppingRate) => string.Empty;
}