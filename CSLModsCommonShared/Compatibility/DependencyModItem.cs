namespace CSLModsCommon.Compatibility;

public class DependencyModItem {
    public string AssemblyName { get; set; }
    public bool IsIncluded { get; internal set; }
    public bool IsEnabled { get; internal set; }
    public string DisplayName { get; set; }

    public DependencyModItem(string assemblyName, string displayName) {
        AssemblyName = assemblyName;
        DisplayName = displayName;
    }
}