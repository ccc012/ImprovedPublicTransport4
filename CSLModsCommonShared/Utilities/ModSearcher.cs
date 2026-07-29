using ColossalFramework.Plugins;
using System.Linq;

namespace CSLModsCommon.Utilities;

public class ModSearcher {
    public string AssemblyName { get; }
    public bool IsIncluded { get; private set; }
    public bool IsEnabled { get; private set; }
    public string Name { get; private set; }

    public ModSearcher(string assemblyName, string name) {
        AssemblyName = assemblyName;
        Name = name;
        Search();
    }

    public static ModSearcher Search(string assemblyName, string name) => new ModSearcher(assemblyName, name);

    public ModSearcher Search() {
        var plugin = PluginManager.instance.GetPluginsInfo().FirstOrDefault(pluginInfo => pluginInfo.GetAssemblies().Any(assembly => assembly.GetName().Name == AssemblyName));
        IsIncluded = plugin != null;
        if (plugin != null)
            IsEnabled = plugin.isEnabled;
        return this;
    }
}