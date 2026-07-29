using CSLModsCommon.UI.Sliders;

namespace CSLModsCommon.UI.SettingsCard;

public class SliderCard : SettingsCardBase<Slider> {
    public override void Awake() {
        base.Awake();
        Control = AddUIComponent<Slider>();
    }
}