using CSLModsCommon.Localization;
using CSLModsCommon.Manager;
using CSLModsCommon.UI.Atlas;

namespace CSLModsCommon.UI.Dialogs;

public class WarningExceptionDialog : AlertDialog {
    protected override void SetTitleText() => TitleText = $"{_domain.GetManager<ModManagerBase>().ModName} {SharedTranslations.Warning}";

    protected override void SetIconSpriteName() => _iconElement.SpriteName = SharedAtlasKeys.Warning;
}