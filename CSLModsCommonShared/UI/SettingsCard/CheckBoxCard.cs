using CSLModsCommon.UI.Buttons;

namespace CSLModsCommon.UI.SettingsCard; 
public class CheckBoxCard : SettingsCardBase<CheckBox> {
    public override void Awake() {
        base.Awake();
        Control = AddUIComponent<CheckBox>();
    }
}