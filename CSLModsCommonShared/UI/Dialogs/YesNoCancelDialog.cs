using CSLModsCommon.Localization;

namespace CSLModsCommon.UI.Dialogs;

public class YesNoCancelDialog : SimpleDialog {
    public void AddContent(string title, string content, UIElementEventHandler yesCallback = null, UIElementEventHandler noCallBack = null, UIElementEventHandler cancelCallBack = null) {
        TitleText = title;
        base.AddContent(title, content);
        AddButton(SharedTranslations.Yes, yesCallback);
        AddButton(SharedTranslations.No, noCallBack);
        AddButton(SharedTranslations.Cancel, cancelCallBack);
    }
}