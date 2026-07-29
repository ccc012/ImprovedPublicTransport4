using CSLModsCommon.UI.Buttons.RadioButtons;

namespace CSLModsCommon.UI.SettingsCard;

public class RadioGroupCard : SettingsCardBase<RadioGroup> {
    public override void Awake() {
        base.Awake();
        Control = AddUIComponent<RadioGroup>();
    }
}