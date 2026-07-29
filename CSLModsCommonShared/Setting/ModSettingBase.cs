using CSLModsCommon.Manager;
using System.IO;

namespace CSLModsCommon.Setting;

public abstract partial class ModSettingBase : IModSetting {
    public static string DefaultFilePath { get; } = Path.Combine(FileLocationAttribute.DefaultDirectory, "GlobalSetting.json");
    public ModVersion CurrentModVersion { get; set; }
    public string LocaleId { get; set; } = LocalizationManager.UseGameLanguage;

    public virtual void SetDefaults() {
        CurrentModVersion = new ModVersion();
        LocaleId = LocalizationManager.UseGameLanguage;
    }
}