using ColossalFramework.Plugins;
using CSLModsCommon.Collections;
using CSLModsCommon.Common;
using CSLModsCommon.Extension;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CSLModsCommon.Compatibility;

public class DependencyModRule : IModRule, IDependencyModRule {
    private readonly List<DependencyModItem> _lookup;

    public bool IsMatched { get; private set; }
    public CSLModsCommon.Collections.IReadOnlyList<DependencyModItem> Lookup { get; }

    public DependencyModRule() {
        _lookup = new List<DependencyModItem>();
        Lookup = ReadOnlyExtensions.AsReadOnly(_lookup);
    }

    public DependencyModRule Add(string assemblyName, string displayName) => Add(new DependencyModItem(assemblyName, displayName));

    public DependencyModRule Add(DependencyModItem dependencyModItem) {
        if (dependencyModItem is null)
            throw new ArgumentNullException(nameof(dependencyModItem));
        _lookup.Add(dependencyModItem);
        return this;
    }

    public void Check(IEnumerable<PluginManager.PluginInfo> pluginsInfo, ref CompatibilityStatus status) {
        if (_lookup.Count == 0) return;

        foreach (var plugin in pluginsInfo)
            foreach (var asm in plugin.GetAssemblies()) {
                var firstOrDefault = _lookup.FirstOrDefault(i => i.AssemblyName == asm.GetName().Name);
                if (firstOrDefault is null) continue;
                firstOrDefault.IsEnabled = plugin.isEnabled;
                firstOrDefault.IsIncluded = true;
            }

        if (_lookup.Any(i => !i.IsIncluded)) {
            status = status.RemoveStatus(CompatibilityStatus.Normal).AddStatus(CompatibilityStatus.MissingDependencies);
            IsMatched = true;
        }
        else {
            IsMatched = false;
        }
    }
}