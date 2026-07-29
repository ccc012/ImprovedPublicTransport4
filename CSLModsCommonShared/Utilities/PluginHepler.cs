using ColossalFramework.Plugins;
using ICities;
using System.Collections.Generic;
using System.Linq;

namespace CSLModsCommon.Utilities;

public static class PluginHelper {
    public static IEnumerable<PluginManager.PluginInfo> GetPluginsInfoSortedByName() => PluginManager.instance.GetPluginsInfo().Where(p => p?.userModInstance is IUserMod).OrderBy(p => ((IUserMod)p.userModInstance).Name);

    public static bool IsPluginEnabled(string assemblyName) => PluginManager.instance.GetPluginsInfo().Any(plugin => plugin is { userModInstance: IUserMod, isEnabled: true } && plugin.GetAssemblies().Any(a => a.GetName().Name == assemblyName));

    public static bool IsPluginEnabled(ulong id) => PluginManager.instance.GetPluginsInfo().Where(i => i.isEnabled).Any(i => i.publishedFileID.AsUInt64 == id);

    public static bool IsPluginSubscribed(string assemblyName) => PluginManager.instance.GetPluginsInfo().Any(plugin => plugin?.userModInstance is IUserMod && plugin.GetAssemblies().Any(a => a.GetName().Name == assemblyName));

    public static void GetPluginState(string assemblyName, out bool isSubscribed, out bool isEnabled) {
        isSubscribed = false;
        isEnabled = false;
        foreach (var item in GetPluginsInfoSortedByName()) {
            if (item?.userModInstance is not IUserMod) continue;
            if (!item.GetAssemblies().Any(a => a.GetName().Name == assemblyName)) continue;
            isSubscribed = true;
            isEnabled = item.isEnabled;
            return;
        }
    }
}