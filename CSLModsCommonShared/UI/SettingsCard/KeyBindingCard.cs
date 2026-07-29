namespace CSLModsCommon.UI.SettingsCard;

public class KeyBindingCard : SettingsCardBase<UIKeyBindingControls> {
    public override void Awake() {
        base.Awake();
        Control = AddUIComponent<UIKeyBindingControls>();
    }
}