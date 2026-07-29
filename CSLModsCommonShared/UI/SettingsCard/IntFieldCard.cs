namespace CSLModsCommon.UI.SettingsCard;

public class IntFieldCard : ValueFieldCardBase<IntValueField, int> {
    public override void Awake() {
        base.Awake();
        Control = AddUIComponent<IntValueField>();
    }
}