namespace CSLModsCommon.Setting;

public class SettingSource {
    public string Path { get; set; }
    public ISetting Setting { get; set; }
    public bool IsDefault { get; set; }

    public SettingSource(string path, ISetting setting) {
        Path = path;
        Setting = setting;
    }

    public SettingSource(bool isDefault, string path, ISetting setting) : this(path, setting) => IsDefault = isDefault;

    public override string ToString() => Path;
}