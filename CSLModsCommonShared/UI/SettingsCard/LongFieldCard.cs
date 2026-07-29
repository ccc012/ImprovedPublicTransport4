namespace CSLModsCommon.UI.SettingsCard;

public class LongFieldCard : ValueFieldCardBase<LongValueField, long> {
    public override void Awake() {
        base.Awake();
        Control = AddUIComponent<LongValueField>();
    }
}