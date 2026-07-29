namespace CSLModsCommon.Compatibility;

public interface IDependencyModRule {
    DependencyModRule Add(DependencyModItem dependencyModItem);
    DependencyModRule Add(string assemblyName, string displayName);
}