using System;

namespace CSLModsCommon;

public struct ModVersion : IComparable<ModVersion>, IEquatable<ModVersion> {
    public int Major;
    public int Minor;
    public int Build;
    public int Revision;

    public ModVersion(int major, int minor, int build = 0, int revision = 0) {
        Major = major;
        Minor = minor;
        Build = build;
        Revision = revision;
    }

    public override readonly bool Equals(object obj) => obj is ModVersion v && Equals(v);

    public override readonly int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = hash * 31 + Major;
            hash = hash * 31 + Minor;
            hash = hash * 31 + Build;
            hash = hash * 31 + Revision;
            return hash;
        }
    }

    public override readonly string ToString() {
        if (Revision != 0) return $"{Major}.{Minor}.{Build}.{Revision}";
        if (Build != 0) return $"{Major}.{Minor}.{Build}";
        return $"{Major}.{Minor}";
    }

    public static int CompareWithoutRevision(ModVersion a, ModVersion b) {
        int r = a.Major.CompareTo(b.Major);
        if (r != 0) return r;

        r = a.Minor.CompareTo(b.Minor);
        if (r != 0) return r;

        return a.Build.CompareTo(b.Build);
    }

    public static bool EqualsWithoutRevision(ModVersion a, ModVersion b) {
        return a.Major == b.Major &&
               a.Minor == b.Minor &&
               a.Build == b.Build;
    }

    public static bool TryParse(string s, out ModVersion version) {
        version = default;
        if (string.IsNullOrEmpty(s))
            return false;

        var parts = s.Split('.');
        int[] nums = new int[4];

        for (int i = 0; i < nums.Length; i++)
            nums[i] = 0;

        for (int i = 0; i < parts.Length && i < 4; i++) {
            if (!int.TryParse(parts[i], out nums[i]))
                return false;
        }

        version = new ModVersion(nums[0], nums[1], nums[2], nums[3]);
        return true;
    }

    public static ModVersion Parse(string s) {
        if (!TryParse(s, out var v))
            throw new FormatException($"Invalid version string: {s}");
        return v;
    }

    public static ModVersion FromVersion(Version v) {
        return new ModVersion(
            v.Major,
            v.Minor,
            v.Build < 0 ? 0 : v.Build,
            v.Revision < 0 ? 0 : v.Revision
        );
    }

    public static bool operator ==(ModVersion a, ModVersion b) => a.Equals(b);
    public static bool operator !=(ModVersion a, ModVersion b) => !a.Equals(b);
    public static bool operator <(ModVersion a, ModVersion b) => a.CompareTo(b) < 0;
    public static bool operator >(ModVersion a, ModVersion b) => a.CompareTo(b) > 0;
    public static bool operator <=(ModVersion a, ModVersion b) => a.CompareTo(b) <= 0;
    public static bool operator >=(ModVersion a, ModVersion b) => a.CompareTo(b) >= 0;

    public readonly Version ToVersion() => new(Major, Minor, Build < 0 ? 0 : Build, Revision < 0 ? 0 : Revision);
    public readonly int CompareWithoutRevision(ModVersion other) => CompareWithoutRevision(this, other);
    public readonly bool EqualsWithoutRevision(ModVersion other) => EqualsWithoutRevision(this, other);

    public int CompareTo(ModVersion other) {
        int r = Major.CompareTo(other.Major); if (r != 0) return r;
        r = Minor.CompareTo(other.Minor); if (r != 0) return r;
        r = Build.CompareTo(other.Build); if (r != 0) return r;
        return Revision.CompareTo(other.Revision);
    }

    public readonly bool Equals(ModVersion other) => Major == other.Major &&
               Minor == other.Minor &&
               Build == other.Build &&
               Revision == other.Revision;
}
