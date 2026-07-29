using ColossalFramework.Plugins;
using CSLModsCommon.Common;
using System.Collections.Generic;

namespace CSLModsCommon.Compatibility;

public class VersionModRule : IModRule, IVersionModRule {
    public static GameVersion Current => new(BuildConfig.applicationVersion);

    private bool _isChecked;

    public GameVersionCompatibility VersionCompatibility { get; private set; }
    public bool IsMatched { get; private set; }

    public void Set(GameVersionCompatibility item) => VersionCompatibility = item;

    public void Set(int gameVersionMajor, int gameVersionMinor, int gameVersionPatch, int gameVersionBuild = 0) => Set(new GameVersionCompatibility(gameVersionMajor, gameVersionMinor, gameVersionPatch, gameVersionBuild));

    public void Check(IEnumerable<PluginManager.PluginInfo> pluginsInfo, ref CompatibilityStatus status) {
        if (_isChecked) return;

        if (!VersionCompatibility.IsCompatible(Current)) {
            status = status.RemoveStatus(CompatibilityStatus.Normal).AddStatus(CompatibilityStatus.NotMakeForCurrentGameVersion);
            IsMatched = true;
        }
        else {
            IsMatched = false;
        }

        _isChecked = true;
    }
}