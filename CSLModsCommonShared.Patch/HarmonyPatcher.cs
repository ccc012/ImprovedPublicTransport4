using ColossalFramework.UI;
using CSLModsCommon.Logging;
using CSLModsCommon.Utilities;
using HarmonyLib;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace CSLModsCommon.Patch;

public sealed class HarmonyPatcher {
    private readonly Stopwatch _patchTimer = new();

    public Harmony HarmonyInstance { get; private set; }
    private ILog Logger { get; } = LogManager.GetLogger();
    public bool IsEnabled { get; private set; }

    public HarmonyPatcher(string harmonyID = null) {
        if (string.IsNullOrEmpty(harmonyID))
            harmonyID = AssemblyHelper.CurrentAssemblyName;
        HarmonyInstance = new Harmony(harmonyID);
    }

    public void Enable(Action<HarmonyPatcher> patchAction = null, bool patchAll = true) {
        if (IsEnabled) return;
        _patchTimer.Reset();
        _patchTimer.Start();
        try {
            if (patchAll) HarmonyInstance.PatchAll();
            patchAction?.Invoke(this);
            _patchTimer.Stop();
            IsEnabled = true;
            Logger.Debug($"Enabled patch in {_patchTimer.ElapsedMilliseconds}ms");
        }
        catch (Exception ex) {
            _patchTimer.Stop();
            Logger.Error(ex, "Failed to enable patcher");
            Disable();
        }
    }

    public void Disable() {
        if (HarmonyInstance is null || !IsEnabled) return;
        _patchTimer.Reset();
        _patchTimer.Start();
        try {
            HarmonyInstance.UnpatchAll();
            IsEnabled = false;
            _patchTimer.Stop();
            Logger.Info($"Disabled patch in {_patchTimer.ElapsedMilliseconds}ms");
        }
        catch (Exception ex) {
            _patchTimer.Stop();
            Logger.Error(ex, "Failed to disable patcher");
        }
    }

    public void ApplyPrefix<TypeTarget, TypePatch>(string targetMethod, string patchMethod, Type[] parameters = null) => Patch<TypeTarget, TypePatch>(HarmonyPatchType.Prefix, targetMethod, patchMethod, parameters);

    public void ApplyPrefix(Type targetType, string targetMethod, Type patchType, string patchMethod, Type[] parameters = null) => Patch(HarmonyPatchType.Prefix, targetType, targetMethod, patchType, patchMethod, parameters);

    public void ApplyPrefix(MethodInfo targetMethodInfo, MethodInfo patchMethodInfo) => Patch(HarmonyPatchType.Prefix, targetMethodInfo, patchMethodInfo);

    public void ApplyPostfix<TypeTarget, TypePatch>(string targetMethod, string patchMethod, Type[] parameters = null) => Patch<TypeTarget, TypePatch>(HarmonyPatchType.Postfix, targetMethod, patchMethod, parameters);

    public void ApplyPostfix(Type targetType, string targetMethod, Type patchType, string patchMethod, Type[] parameters = null) => Patch(HarmonyPatchType.Postfix, targetType, targetMethod, patchType, patchMethod, parameters);

    public void ApplyPostfix(MethodInfo targetMethodInfo, MethodInfo patchMethodInfo) => Patch(HarmonyPatchType.Postfix, targetMethodInfo, patchMethodInfo);

    public void ApplyTranspiler<TypeTarget, TypePatch>(string targetMethod, string patchMethod, Type[] parameters = null) => Patch<TypeTarget, TypePatch>(HarmonyPatchType.Transpiler, targetMethod, patchMethod, parameters);

    public void ApplyTranspiler(Type targetType, string targetMethod, Type patchType, string patchMethod, Type[] parameters = null) => Patch(HarmonyPatchType.Transpiler, targetType, targetMethod, patchType, patchMethod, parameters);

    public void ApplyTranspiler(MethodInfo targetMethodInfo, MethodInfo patchMethodInfo) => Patch(HarmonyPatchType.Transpiler, targetMethodInfo, patchMethodInfo);

    public void ApplyFinalizer<TypeTarget, TypePatch>(string targetMethod, string patchMethod, Type[] parameters = null) => Patch<TypeTarget, TypePatch>(HarmonyPatchType.Finalizer, targetMethod, patchMethod, parameters);

    public void ApplyFinalizer(Type targetType, string targetMethod, Type patchType, string patchMethod, Type[] parameters = null) => Patch(HarmonyPatchType.Finalizer, targetType, targetMethod, patchType, patchMethod, parameters);

    public void ApplyFinalizer(MethodInfo targetMethodInfo, MethodInfo patchMethodInfo) => Patch(HarmonyPatchType.Finalizer, targetMethodInfo, patchMethodInfo);

    private void Patch<TypeTarget, TypePatch>(HarmonyPatchType patchType, string targetMethod, string patchMethod, Type[] parameters = null) => Patch(patchType, typeof(TypeTarget), targetMethod, typeof(TypePatch), patchMethod, parameters);

    private void Patch(HarmonyPatchType harmonyPatchType, Type targetType, string targetMethod, Type patchType, string patchMethod, Type[] parameters = null) {
        try {
            var target = AccessTools.Method(targetType, targetMethod, parameters);
            var patch = AccessTools.Method(patchType, patchMethod);
            if (target is null) {
                Logger.Error($"Target method [{targetType.FullName}.{targetMethod}] was not found during patch process");
                return;
            }

            if (patch is null) {
                Logger.Error($"Patch method [{patchType.FullName}.{patchMethod}] was not found during patch process");
                return;
            }

            ApplyPatch(harmonyPatchType, target, patch);
            Logger.Info($"[Harmony {harmonyPatchType}] target method: {target.DeclaringType?.FullName}.{targetMethod}, patch method: {patch.DeclaringType?.FullName}.{patchMethod}");
        }
        catch (Exception ex) {
            Logger.Error(ex, $"Patch failed target:{targetType?.FullName}.{targetMethod} patch:{patchType?.FullName}.{patchMethod}");
        }
    }

    private void Patch(HarmonyPatchType harmonyPatchType, MethodInfo targetMethodInfo, MethodInfo patchMethodInfo) {
        try {
            if (targetMethodInfo is null) {
                var stackTrace = new StackTrace();
                var stackTraceString = stackTrace.ToString();
                Logger.Error($"Target method was not found during patch process:\n{stackTraceString}");
                return;
            }

            if (patchMethodInfo is null) {
                var stackTrace = new StackTrace();
                var stackTraceString = stackTrace.ToString();
                Logger.Error($"Patch method was not found during patch process:\n{stackTraceString}");
                return;
            }

            ApplyPatch(harmonyPatchType, targetMethodInfo, patchMethodInfo);
            Logger.Info($"[Harmony {harmonyPatchType}] target method: {targetMethodInfo.DeclaringType?.FullName}, patch method: {patchMethodInfo.DeclaringType?.FullName}");
        }
        catch (Exception ex) {
            Logger.Error(ex, $"Patch failed target:{targetMethodInfo?.DeclaringType?.FullName},  patch:{patchMethodInfo?.DeclaringType?.FullName}");
        }
    }

    private void ApplyPatch(HarmonyPatchType type, MethodInfo target, MethodInfo patch) {
        switch (type) {
            case HarmonyPatchType.Prefix:
                HarmonyInstance.Patch(target, new HarmonyMethod(patch));
                break;
            case HarmonyPatchType.Postfix:
                HarmonyInstance.Patch(target, postfix: new HarmonyMethod(patch));
                break;
            case HarmonyPatchType.Transpiler:
                HarmonyInstance.Patch(target, transpiler: new HarmonyMethod(patch));
                break;
            case HarmonyPatchType.Finalizer:
                HarmonyInstance.Patch(target, finalizer: new HarmonyMethod(patch));
                break;
            default:
                Logger.Error($"Unknown Harmony patch type: {type}");
                break;
        }
    }

    public void LogPatchedMethods() => Logger.Info(GetPatchedMethods());

    public string GetPatchedMethods() {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine("Patched methods:");
        HarmonyInstance.GetPatchedMethods().ForEach(x => stringBuilder.AppendLine($"{x?.DeclaringType?.FullName}.{x?.Name}"));
        return stringBuilder.ToString();
    }
}