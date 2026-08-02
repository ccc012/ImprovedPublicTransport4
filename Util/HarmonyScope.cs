using System;
using System.Reflection;
using HarmonyLib;

namespace ImprovedPublicTransport.Util
{
    /// <summary>
    /// Scopes Harmony attribute discovery to a single integration namespace.
    /// Every absorbed mod lives in the same assembly, so
    /// <c>harmony.PatchAll(Assembly.GetExecutingAssembly())</c> would re-apply
    /// every integration's <c>[HarmonyPatch]</c> types under whichever Harmony ID
    /// called it - duplicate postfixes, broken toggles, and wasted CPU on hot paths.
    /// </summary>
    internal static class HarmonyScope
    {
        /// <summary>
        /// Patches only types whose namespace is <paramref name="namespacePrefix"/>
        /// or a child of it (e.g. <c>ExpressBusServices.PerformanceBoost</c> for prefix
        /// <c>ExpressBusServices</c>).
        /// </summary>
        public static void PatchNamespace(Harmony harmony, string namespacePrefix)
        {
            if (harmony == null)
            {
                throw new ArgumentNullException(nameof(harmony));
            }

            if (string.IsNullOrEmpty(namespacePrefix))
            {
                throw new ArgumentException("Namespace prefix is required.", nameof(namespacePrefix));
            }

            var childPrefix = namespacePrefix + ".";
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type == null || type.IsInterface)
                {
                    continue;
                }

                var ns = type.Namespace;
                if (ns == null)
                {
                    continue;
                }

                if (ns != namespacePrefix && !ns.StartsWith(childPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                try
                {
                    // CreateClassProcessor is a no-op for types with no Harmony annotations.
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception ex)
                {
                    // Assembly.GetTypes() enumeration continues after this - but without this
                    // try/catch, one type whose Harmony target can't be resolved (e.g. a bad
                    // [HarmonyPatch] attribute) throws out of the whole foreach, silently skipping
                    // every remaining type in this namespace's patch scan with no log entry at all.
                    Utils.LogError($"HarmonyScope: failed to patch {type.FullName}: {ex}");
                }
            }
        }
    }
}
