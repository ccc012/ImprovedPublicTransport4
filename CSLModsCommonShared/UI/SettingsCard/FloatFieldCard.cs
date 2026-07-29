namespace CSLModsCommon.UI.SettingsCard; 
public class FloatFieldCard : ValueFieldCardBase<FloatValueField, float> {
    public override void Awake() {
        base.Awake();
        Control = AddUIComponent<FloatValueField>();
    }
}