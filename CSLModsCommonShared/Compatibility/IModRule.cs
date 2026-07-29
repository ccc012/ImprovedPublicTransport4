using ColossalFramework.Plugins;
using System.Collections.Generic;

namespace CSLModsCommon.Compatibility; 
public interface IModRule {
    bool IsMatched { get; }
    void Check(IEnumerable<PluginManager.PluginInfo> pluginsInfo, ref CompatibilityStatus status);
}