namespace CSLModsCommon.UI.SettingsCard; 
public class StringFieldCard : ValueFieldCardBase<StringValueField, string> {
    public override void Awake() {
        base.Awake();
        Control = AddUIComponent<StringValueField>();
    }
}