using System;
using System.Reflection;
using ColossalFramework.UI;
using HarmonyLib;
using ImprovedPublicTransport.Util;
using UnityEngine;

namespace ImprovedPublicTransport.HarmonyPatches.EngineHardening
{
    /// <summary>
    /// Breaks stack-overflow recursion in Colossal/Unity dynamic fonts.
    /// Not an IPT feature bug - engine armour so a dirty font atlas cannot freeze the game.
    ///
    /// Observed (player logs, including issue #2 4.8.9):
    ///
    ///   d3d11: failed to create staging 2D texture ... [80070057]  (often tiny/broken assets)
    ///   ...
    ///   StackOverflowException
    ///     (wrapper delegate-invoke) Action&lt;Font&gt;:invoke_void__this___Font
    ///     ... hundreds of identical frames (Font.textureRebuilt re-entrancy)
    ///   and/or the longer form:
    ///     Font:RequestCharactersInTexture
    ///     UIDynamicFont:RequestCharacters
    ///     DynamicFontRenderer:MeasureString
    ///     InstanceManager:RefreshNameData → NetManager:RenderName → OverlayEffect
    ///
    /// Failed atlas alloc keeps the font dirty. textureRebuilt handlers re-request glyphs,
    /// which rebuild again, which fires the static event again - until the stack dies and
    /// the process often follows with a native Access Violation.
    ///
    /// Two independent re-entrancy guards (separate flags - MUST stay separate):
    /// 1) UIDynamicFont.RequestCharacters - Colossal managed path (original 4.8.x fix).
    /// 2) Font.RequestCharactersInTexture - Unity native choke (issue #2 stack only showed
    ///    Action&lt;Font&gt;; path never re-entered UIDynamicFont so (1) alone was not enough).
    ///
    /// Nested skip cost: glyphs for that label a frame late - self-heals next frame.
    /// Finalizers clear ownership even if the original throws, so a stuck guard cannot
    /// permanently mute all font requests for the session.
    /// </summary>
    public static class FontRequestCharactersGuardPatch
    {
        private static bool _inUiDynamicRequest;
        private static bool _inNativeRequest;

        private static readonly Type[] UiRequestArgs =
            { typeof(string), typeof(int), typeof(FontStyle) };

        private static readonly Type[][] NativeRequestArgSets =
        {
            new[] { typeof(string), typeof(int), typeof(FontStyle) },
            new[] { typeof(string), typeof(int) },
            new[] { typeof(string) }
        };

        public static void Apply()
        {
            // Keep PatchUtil path for UIDynamicFont (logs + failure summary).
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(UIDynamicFont), nameof(UIDynamicFont.RequestCharacters),
                    argumentTypes: UiRequestArgs),
                new PatchUtil.MethodDefinition(typeof(FontRequestCharactersGuardPatch), nameof(UiPrefix)),
                postfix: null,
                transpiler: null);

            // PatchUtil has no finalizer hook - attach finalizer + native overloads via Harmony.
            var harmony = new Harmony(HarmonyId.Value);
            var uiMethod = AccessTools.Method(typeof(UIDynamicFont), nameof(UIDynamicFont.RequestCharacters), UiRequestArgs);
            if (uiMethod != null)
            {
                harmony.Patch(uiMethod,
                    finalizer: new HarmonyMethod(typeof(FontRequestCharactersGuardPatch), nameof(UiFinalizer)));
            }

            var nativePrefix = new HarmonyMethod(typeof(FontRequestCharactersGuardPatch), nameof(NativePrefix));
            var nativeFinalizer = new HarmonyMethod(typeof(FontRequestCharactersGuardPatch), nameof(NativeFinalizer));
            foreach (var args in NativeRequestArgSets)
            {
                var method = AccessTools.Method(typeof(Font), nameof(Font.RequestCharactersInTexture), args);
                if (method == null)
                {
                    continue;
                }

                try
                {
                    PatchUtil.LogExistingPatches(method);
                    harmony.Patch(method, prefix: nativePrefix, finalizer: nativeFinalizer);
                }
                catch (Exception e)
                {
                    Debug.LogError("IPT: Failed to patch Font.RequestCharactersInTexture");
                    Debug.LogException(e);
                }
            }
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(UIDynamicFont), nameof(UIDynamicFont.RequestCharacters),
                    argumentTypes: UiRequestArgs));

            var harmony = new Harmony(HarmonyId.Value);
            foreach (var args in NativeRequestArgSets)
            {
                var method = AccessTools.Method(typeof(Font), nameof(Font.RequestCharactersInTexture), args);
                if (method != null)
                {
                    harmony.Unpatch(method, HarmonyPatchType.All, HarmonyId.Value);
                }
            }

            _inUiDynamicRequest = false;
            _inNativeRequest = false;
        }

        // --- UIDynamicFont.RequestCharacters ---

        public static bool UiPrefix(out bool __state)
        {
            if (_inUiDynamicRequest)
            {
                __state = false;
                return false;
            }

            _inUiDynamicRequest = true;
            __state = true;
            return true;
        }

        // Harmony finalizer always runs (success or exception). Clears ownership so a throw
        // cannot leave the guard stuck true and mute every later RequestCharacters call.
        public static Exception UiFinalizer(Exception __exception, bool __state)
        {
            if (__state)
            {
                _inUiDynamicRequest = false;
            }

            return __exception;
        }

        // --- Font.RequestCharactersInTexture (all overloads share one flag) ---

        public static bool NativePrefix(out bool __state)
        {
            if (_inNativeRequest)
            {
                __state = false;
                return false;
            }

            _inNativeRequest = true;
            __state = true;
            return true;
        }

        public static Exception NativeFinalizer(Exception __exception, bool __state)
        {
            if (__state)
            {
                _inNativeRequest = false;
            }

            return __exception;
        }
    }
}
