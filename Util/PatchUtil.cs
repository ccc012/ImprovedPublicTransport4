using System;
using System.Reflection;
using HarmonyLib;
using ImprovedPublicTransport.HarmonyPatches;
using UnityEngine;
using static ImprovedPublicTransport.ImprovedPublicTransportMod;

namespace ImprovedPublicTransport.Util
{
    internal static class PatchUtil
    {

        private static Harmony _harmonyInstance;

        private static Harmony HarmonyInstance =>
            _harmonyInstance ??= new Harmony(HarmonyId.Value);


        public static void Patch(
            MethodDefinition original,
            MethodDefinition prefix = null,
            MethodDefinition postfix = null,
            MethodDefinition transpiler = null)
        {
            if (prefix == null && postfix == null && transpiler == null)
            {
                throw new Exception(
                    $"{ShortModName}: prefix, postfix and transpiler are null for method {original.Type.FullName}.{original.MethodName}");
            }

            try
            {
                if (Diagnostics.VerboseTranspileLogs)
                {
                    Debug.Log($"{ShortModName}: Patching method {original.Type.FullName}.{original.MethodName}");
                }
                var methodInfo = GetOriginal(original);
                LogExistingPatches(methodInfo);
                HarmonyInstance.Patch(methodInfo,
                    prefix == null ? null : new HarmonyMethod(GetPatch(prefix), before: prefix.Before, after: prefix.After, priority: prefix.Priority),
                    postfix == null ? null : new HarmonyMethod(GetPatch(postfix), before: postfix.Before, after: postfix.After, priority: postfix.Priority),
                    transpiler == null ? null : new HarmonyMethod(GetPatch(transpiler), before: transpiler.Before, after: transpiler.After, priority: transpiler.Priority)
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"{ShortModName}: Failed to patch method {original.Type.FullName}.{original.MethodName}");
                Debug.LogException(e);
            }
        }

        internal static void LogExistingPatches(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                Debug.LogWarning($"{ShortModName}: LogExistingPatches called with null MethodInfo.");
                return;
            }

            try
            {
                var patchInfo = Harmony.GetPatchInfo(methodInfo);
                if (patchInfo == null) return;

                var otherPatchers = new System.Collections.Generic.List<string>();

                if (patchInfo.Prefixes != null)
                    foreach (var p in patchInfo.Prefixes)
                        if (p.owner != HarmonyId.Value) otherPatchers.Add($"prefix:{p.owner}");

                if (patchInfo.Postfixes != null)
                    foreach (var p in patchInfo.Postfixes)
                        if (p.owner != HarmonyId.Value) otherPatchers.Add($"postfix:{p.owner}");

                if (patchInfo.Transpilers != null)
                    foreach (var p in patchInfo.Transpilers)
                        if (p.owner != HarmonyId.Value) otherPatchers.Add($"transpiler:{p.owner}");

                if (otherPatchers.Count > 0)
                {
                    Debug.LogWarning($"{ShortModName}: Detected other patchers on {methodInfo.DeclaringType.FullName}.{methodInfo.Name} -> {string.Join(", ", otherPatchers.ToArray())}");
                }
            }
            catch (Exception e)
            {
                if (methodInfo != null)
                {
                    Debug.LogError($"{ShortModName}: Failed to inspect existing patches for {methodInfo.DeclaringType?.FullName}.{methodInfo.Name}");
                }
                else
                {
                    Debug.LogError($"{ShortModName}: Failed to inspect existing patches for unknown method.");
                }
                Debug.LogException(e);
            }
        }

        public static void Unpatch(MethodDefinition original)
        {
            if (Diagnostics.VerboseTranspileLogs)
            {
                Debug.Log($"{ShortModName}: Unpatching method {original.Type.FullName}.{original.MethodName}");
            }
            HarmonyInstance.Unpatch(GetOriginal(original), HarmonyPatchType.All, HarmonyId.Value);
        }

        private static MethodInfo GetOriginal(MethodDefinition original)
        {
            var bindingFlags = original.BindingFlags == BindingFlags.Default
                ? BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly
                : original.BindingFlags;
            var methodInfo = original.ArgumentTypes == null
                ? original.Type.GetMethod(original.MethodName, bindingFlags)
                : original.Type.GetMethod(original.MethodName, bindingFlags, null, original.ArgumentTypes, null);
            if (methodInfo == null)
            {
                throw new Exception(
                    $"{ShortModName}: Failed to find original method {original.Type.FullName}.{original.MethodName}");
            }

            return methodInfo;
        }

        private static MethodInfo GetPatch(MethodDefinition patch)
        {
            var bindingFlags = patch.BindingFlags == BindingFlags.Default
                ? BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly
                : patch.BindingFlags;
            var methodInfo = patch.ArgumentTypes == null
                ? patch.Type.GetMethod(patch.MethodName, bindingFlags)
                : patch.Type.GetMethod(patch.MethodName, bindingFlags, null, patch.ArgumentTypes, null);
            
            if (methodInfo == null)
            {
                throw new Exception($"{ShortModName}: Failed to find patch method {patch.Type.FullName}.{patch.MethodName}");
            }

            return methodInfo;
        }

        public class MethodDefinition
        {
            public MethodDefinition(Type type, string methodName,
                BindingFlags bindingFlags = BindingFlags.Default,
                Type[] argumentTypes = null,
                string[] before = null,
                string[] after = null,
                int priority = -1)
            {
                Type = type;
                MethodName = methodName;
                BindingFlags = bindingFlags;
                ArgumentTypes = argumentTypes;
                Before = before;
                After = after;
                Priority = priority;
            }

            public Type Type { get; }
            public string MethodName { get; }

            public BindingFlags BindingFlags { get; }
            
            public Type[] ArgumentTypes { get; }

            public string[] Before { get;  }
            
            public string[] After { get;  }
            
            public int Priority { get;  }
        }
    }
}