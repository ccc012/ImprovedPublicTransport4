using CSLModsCommon.Collections;

namespace CSLModsCommon.Compatibility; 
public interface IIncompatibleModRule {
    CSLModsCommon.Collections.IReadOnlyDictionary<string, IncompatibleModItem> Lookup { get; }

    IncompatibleModRule Add(IncompatibleModItem incompatibleModItem);
    IncompatibleModRule Add(string assemblyName, IncompatibilityModLevel incompatibilityLevel, string displayName, bool isDuplicateFunctionality = true, string alternativeModName = null, string customWarningMessage = null);
    /// <summary>Register an incompatibility with one or more Steam Workshop IDs (fallback when DLL names differ).</summary>
    IncompatibleModRule AddWithWorkshop(string assemblyName, IncompatibilityModLevel level, string displayName, string warning, params ulong[] workshopIds);
}