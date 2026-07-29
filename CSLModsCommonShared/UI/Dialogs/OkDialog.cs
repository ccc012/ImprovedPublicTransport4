using CSLModsCommon.Localization;

namespace CSLModsCommon.UI.Dialogs; 
public class OkDialog : SimpleDialog {
    protected override void OnAwake() {
        base.OnAwake();
        AddButton(SharedTranslations.OK, Close);
    }
}