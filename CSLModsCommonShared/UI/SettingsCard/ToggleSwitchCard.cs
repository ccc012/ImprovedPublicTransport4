using CSLModsCommon.UI.Buttons;

namespace CSLModsCommon.UI.SettingsCard; 
public class ToggleSwitchCard : SettingsCardBase<ToggleSwitchIndicator> {
    public override void Awake() {
        base.Awake();
        Control = AddUIComponent<ToggleSwitchIndicator>();
    }
}