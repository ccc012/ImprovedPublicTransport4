using ColossalFramework.Plugins;
using CSLModsCommon.Collections;
using CSLModsCommon.Common;
using CSLModsCommon.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSLModsCommon.Compatibility;

public class IncompatibleModRule : IModRule, IIncompatibleModRule {
    private readonly Dictionary<string, IncompatibleModItem> _lookup;

    public int Count => _lookup.Count;
    public CSLModsCommon.Collections.IReadOnlyDictionary<string, IncompatibleModItem> Lookup { get; }
    public bool IsMatched { get; private set; }

    public IncompatibleModRule() {
        _lookup = new Dictionary<string, IncompatibleModItem>();
        Lookup = _lookup.AsReadOnly();
    }

    public void Handle() {
        if (Count == 0) return;
        _lookup.ForEach(item => item.Value.Handle());
    }

    public IncompatibleModRule Add(string assemblyName, IncompatibilityModLevel incompatibilityLevel, string displayName, bool isDuplicateFunctionality = true, string alternativeModName = null, string customWarningMessage = null) {
        Add(new IncompatibleModItem(assemblyName, incompatibilityLevel, displayName, isDuplicateFunctionality, alternativeModName, customWarningMessage));
        return this;
    }

    public IncompatibleModRule Add(IncompatibleModItem incompatibleModItem) {
        if (incompatibleModItem is null)
            throw new ArgumentNullException(nameof(incompatibleModItem));
        _lookup[incompatibleModItem.AssemblyName] = incompatibleModItem;
        return this;
    }

    public string LogIncompatibleMods() {
        if (Count == 0 || !IsMatched) return string.Empty;
        var sb = new StringBuilder();
        foreach (var lookup in Lookup)
            if (lookup.Value.IsMatched)
                sb.AppendLine(lookup.Value.ToString());

        return sb.ToString();
    }

    public void Check(IEnumerable<PluginManager.PluginInfo> pluginsInfo, ref CompatibilityStatus status) {
        if (_lookup.Count == 0) return;
        _lookup.ForEach(item => item.Value.Unassign());
        foreach (var plugin in pluginsInfo)
            foreach (var asm in plugin.GetAssemblies()) {
                var asmName = asm.GetName().Name;
                if (_lookup.TryGetValue(asmName, out var item)) {
                    item.Assign(plugin);
                    break;
                }
            }

        if (_lookup.Any(i => i.Value.IsMatched)) {
            status = status.RemoveStatus(CompatibilityStatus.Normal)
                .AddStatus(CompatibilityStatus.IncompatibleMods);
            IsMatched = true;
            LogIncompatibleMods();
        }
        else {
            IsMatched = false;
        }
    }
}