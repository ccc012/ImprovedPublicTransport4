namespace CSLModsCommon.UI.SettingsCard;

public class ByteFieldCard : ValueFieldCardBase<ByteValueField, byte> {
    public override void Awake() {
        base.Awake();
        Control = AddUIComponent<ByteValueField>();
    }
}