namespace CSLModsCommon.UI.SettingsCard; 
public class SpriteCard : SettingsCardBase<Sprite> {
    public override void Awake() {
        base.Awake();
        Control = AddUIComponent<Sprite>();
    }
}