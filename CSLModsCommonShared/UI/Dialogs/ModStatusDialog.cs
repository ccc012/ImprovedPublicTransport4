using CSLModsCommon.Common;
using CSLModsCommon.Compatibility;
using CSLModsCommon.Extension;
using CSLModsCommon.Localization;
using CSLModsCommon.Manager;

namespace CSLModsCommon.UI.Dialogs;

/// <summary>Simple summary dialog: mod name/version/release date and current compatibility status.</summary>
public class ModStatusDialog : OkDialog {
    public void Init() {
        var modManager = _domain.GetManager<ModManagerBase>();
        var compatibilityManager = _domain.GetOrCreateManager<CompatibilityManager>();

        TitleText = $"{modManager.ModName} {SharedTranslations.ModStatus}";

        var isNormal = compatibilityManager.CurrentStatus.IsOnlyFlag(CompatibilityStatus.Normal) && !compatibilityManager.ShouldRestartGame;

        AddContent($"{SharedTranslations.Version}: {modManager.ModVersion.ToDisplayString()}");
        AddContent($"{SharedTranslations.ModReleaseDate}: {modManager.VersionDate:yyyy-MM-dd}");
        AddContent($"{SharedTranslations.ModCompatibility}: {(isNormal ? SharedTranslations.Normal : SharedTranslations.Warning)}");

        ShowWithAnimation();
    }
}
