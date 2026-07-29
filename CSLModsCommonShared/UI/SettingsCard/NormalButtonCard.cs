using CSLModsCommon.UI.Buttons;

namespace CSLModsCommon.UI.SettingsCard;

public class NormalButtonCard : SettingsCardBase<NormalButton> {
    public override void Awake() {
        base.Awake();
        Control = AddUIComponent<NormalButton>();
    }
}