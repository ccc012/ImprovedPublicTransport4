using CSLModsCommon.Localization;

namespace CSLModsCommon.UI.Dialogs;

public class ConfirmDialog : SimpleDialog {
    public void AddContent(string title, string content, UIElementEventHandler yesCallback = null, UIElementEventHandler noCallBack = null, bool callCloseAfterAction = true) {
        TitleText = title;
        base.AddContent(title, content);
        AddButton(SharedTranslations.OK, () => {
            if (callCloseAfterAction)
                Close();
            yesCallback?.Invoke();
        });
        AddButton(SharedTranslations.Cancel, noCallBack ?? Close);
        ShowWithAnimation();
    }
}