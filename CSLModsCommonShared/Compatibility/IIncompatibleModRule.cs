using CSLModsCommon.Collections;

namespace CSLModsCommon.Compatibility; 
public interface IIncompatibleModRule {
    CSLModsCommon.Collections.IReadOnlyDictionary<string, IncompatibleModItem> Lookup { get; }

    IncompatibleModRule Add(IncompatibleModItem incompatibleModItem);
    IncompatibleModRule Add(string assemblyName, IncompatibilityModLevel incompatibilityLevel, string displayName, bool isDuplicateFunctionality = true, string alternativeModName = null, string customWarningMessage = null);
}