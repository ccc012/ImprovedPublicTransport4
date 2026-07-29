using CSLModsCommon.Compatibility;

namespace CSLModsCommon.Common;

public static class ModStatusExtensions {
    public static CompatibilityStatus AddStatus(this CompatibilityStatus current, CompatibilityStatus statusToAdd) => current | statusToAdd;

    public static CompatibilityStatus RemoveStatus(this CompatibilityStatus current, CompatibilityStatus statusToRemove) => current & ~statusToRemove;

    public static bool HasStatus(this CompatibilityStatus current, CompatibilityStatus statusToCheck) => (current & statusToCheck) == statusToCheck;

    public static bool IsOnlyFlag(this CompatibilityStatus status, CompatibilityStatus target) => (status & target) == target && (status & ~target) == 0;
}