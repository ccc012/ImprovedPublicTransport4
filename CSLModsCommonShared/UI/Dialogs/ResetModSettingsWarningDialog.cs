using CSLModsCommon.Localization;
using CSLModsCommon.Manager;

namespace CSLModsCommon.UI.Dialogs; 
public class ResetModSettingsWarningDialog : SimpleDialog {
    public void Init(UIElementEventHandler callback) {
        var modManager = _domain.GetManager<ModManagerBase>();
        AddContent($"{modManager.ModName} {SharedTranslations.Reset}", SharedTranslations.ResetModWarning);
        AddButton(SharedTranslations.OK, () => callback?.Invoke()).TextColors.SetValues(UIColors.RedNormal);
        AddButton(SharedTranslations.Cancel, Close);
        ShowWithAnimation();
    }
}