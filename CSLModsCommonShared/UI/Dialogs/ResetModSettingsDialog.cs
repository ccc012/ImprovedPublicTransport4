using CSLModsCommon.Localization;
using CSLModsCommon.Manager;

namespace CSLModsCommon.UI.Dialogs;

public class ResetModSettingsDialog : OkDialog {
    public void Init(bool isSucceeded = true) {
        var modManager = _domain.GetManager<ModManagerBase>();
        AddContent($"{modManager.ModName} {SharedTranslations.Reset}", isSucceeded ? SharedTranslations.ResetModSucceeded : SharedTranslations.ResetModFailed);
        ShowWithAnimation();
    }
}