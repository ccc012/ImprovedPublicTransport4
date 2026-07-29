using System;

namespace CSLModsCommon.Extension;

public static class VersionExtension {
    public static bool IsBetween(this Version v, Version min, Version max) => v.CompareTo(min) >= 0 && v.CompareTo(max) <= 0;

    public static bool HasBuild(this Version v) => v.Build > 0;

    public static bool HasRevision(this Version v) => v.Revision > 0;

    public static bool IsNewerThan(this Version a, Version b) => a.CompareTo(b) > 0;

    public static bool IsOlderThan(this Version a, Version b) => a.CompareTo(b) < 0;

    public static bool IsSameOrNewerThan(this Version a, Version b) => a.CompareTo(b) >= 0;

    public static string ToDisplayString(this Version version, bool includeSuffix = false, string suffix = null) {
        if (version == null) return string.Empty;
        var core = version.Revision > 0 ? version.ToString(4) : version.ToString(version.Build > 0 ? 3 : 2);
        return includeSuffix && !string.IsNullOrEmpty(suffix) ? $"{core}-{suffix}" : core;
    }
}