using CSLModsCommon.UI.Labels;

namespace CSLModsCommon.UI.SettingsCard;

public class LabelCard : SettingsCardBase<Label> {
    public override void Awake() {
        base.Awake();
        Control = AddUIComponent<Label>();
    }
}