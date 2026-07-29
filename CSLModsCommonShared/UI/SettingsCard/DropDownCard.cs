using CSLModsCommon.UI.DropDown;

namespace CSLModsCommon.UI.SettingsCard;

public class DropDownCard : SettingsCardBase<DropDownButton> {
    public override void Awake() {
        base.Awake();
        Control = AddUIComponent<DropDownButton>();
    }
}