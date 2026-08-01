using ColossalFramework.PlatformServices;
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

    public IncompatibleModRule AddWithWorkshop(string assemblyName, IncompatibilityModLevel level, string displayName, string warning, params ulong[] workshopIds) {
        var item = new IncompatibleModItem(assemblyName, level, displayName, true, "Improved Public Transport 4", warning)
            .WithWorkshopIds(workshopIds);
        return Add(item);
    }

    public IncompatibleModRule Add(IncompatibleModItem incompatibleModItem) {
        if (incompatibleModItem is null)
            throw new ArgumentNullException(nameof(incompatibleModItem));
        // Unique dictionary key — prefer assembly name; workshop-only entries use a synthetic key.
        var key = string.IsNullOrEmpty(incompatibleModItem.AssemblyName)
            ? "ws:" + (incompatibleModItem.WorkshopIds != null && incompatibleModItem.WorkshopIds.Length > 0
                ? incompatibleModItem.WorkshopIds[0].ToString()
                : Guid.NewGuid().ToString("N"))
            : incompatibleModItem.AssemblyName;
        // Avoid overwriting when the same assembly is registered twice under different logical names.
        if (_lookup.ContainsKey(key) && key == incompatibleModItem.AssemblyName
            && incompatibleModItem.WorkshopIds != null && incompatibleModItem.WorkshopIds.Length > 0) {
            key = key + "#" + incompatibleModItem.WorkshopIds[0];
        }
        _lookup[key] = incompatibleModItem;
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

        // Build a flat list so alternate assembly names / workshop IDs still resolve.
        var items = _lookup.Values.ToList();

        foreach (var plugin in pluginsInfo) {
            if (plugin == null) continue;

            // 1) Assembly name (primary key + alternates).
            var assigned = false;
            try {
                foreach (var asm in plugin.GetAssemblies()) {
                    if (asm == null) continue;
                    var asmName = asm.GetName().Name;
                    // Exact dictionary hit first (fast path for primary names).
                    if (_lookup.TryGetValue(asmName, out var byKey) && byKey.Plugin == null) {
                        byKey.Assign(plugin);
                        assigned = true;
                        break;
                    }

                    foreach (var item in items) {
                        if (item.Plugin != null) continue;
                        if (!item.MatchesAssemblyName(asmName)) continue;
                        item.Assign(plugin);
                        assigned = true;
                        break;
                    }

                    if (assigned) break;
                }
            }
            catch {
                // GetAssemblies can throw for broken plugins — still try workshop id.
            }

            if (assigned) continue;

            // 2) Workshop published file ID (survives renames / recompiles).
            try {
                var pubId = plugin.publishedFileID;
                if (pubId == PublishedFileId.invalid) continue;
                var id = pubId.AsUInt64;
                if (id == 0) continue;
                foreach (var item in items) {
                    if (item.Plugin != null) continue;
                    if (!item.MatchesWorkshopId(id)) continue;
                    item.Assign(plugin);
                    break;
                }
            }
            catch {
                // ignore
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