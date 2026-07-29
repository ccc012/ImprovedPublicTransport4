using ColossalFramework.Plugins;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CSLModsCommon.Utilities;

public static class AssemblyHelper {
    public static string CurrentAssemblyName { get; }
    public static Version CurrentAssemblyVersion { get; }
    public static string CurrentAssemblyDirectory { get; }

    static AssemblyHelper() {
        CurrentAssemblyName = GetCurrentAssemblyName();
        CurrentAssemblyVersion = GetCurrentAssemblyVersion();
        CurrentAssemblyDirectory = GetCurrentAssemblyDirectory();
    }

    private static string GetCurrentAssemblyName() => Assembly.GetExecutingAssembly().GetName().Name;

    private static Version GetCurrentAssemblyVersion() => Assembly.GetExecutingAssembly().GetName().Version;

    private static string GetCurrentAssemblyDirectory() {
        try {
            var modPath = PluginManager.instance.GetPluginsInfo()
                .FirstOrDefault(item => item.GetAssemblies().Any(a => a.GetName().Name == CurrentAssemblyName))
                ?.modPath;

            if (!string.IsNullOrEmpty(modPath)) {
                return modPath;
            }
        }
        catch {
        }

        try {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(assemblyLocation)) {
                return Path.GetDirectoryName(assemblyLocation);
            }
        }
        catch {
        }

        return AppDomain.CurrentDomain.BaseDirectory;
    }
}
