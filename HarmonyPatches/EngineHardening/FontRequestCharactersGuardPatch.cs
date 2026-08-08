using ColossalFramework.UI;
using ImprovedPublicTransport.Util;
using UnityEngine;

namespace ImprovedPublicTransport.HarmonyPatches.EngineHardening
{
    /// <summary>
    /// Breaks a stack-overflow-causing recursion inside ColossalFramework's own dynamic font
    /// system - not our bug, but one that reliably freezes the whole game and is worth armouring
    /// against since Harmony can reach it too.
    ///
    /// Observed crash (from a player output_log, twice, same signature both times):
    ///
    ///   d3d11: failed to create 2D texture ... [D3D error was 80070057]
    ///   ...
    ///   StackOverflowException
    ///     UnityEngine.Font:RequestCharactersInTexture
    ///     ColossalFramework.UI.UIDynamicFont:RequestCharacters
    ///     ColossalFramework.UI.DynamicFontRenderer:MeasureString
    ///     InstanceManager:RefreshNameData
    ///     NetManager:RenderName
    ///     NetManager:EndOverlayImpl
    ///     RenderManager:Managers_RenderOverlay
    ///     OverlayEffect:OnPostRender
    ///     + ~1000 further Action&lt;Font&gt; invoke frames
    ///
    /// On a low-VRAM integrated GPU under heavy asset load, the native texture allocation for the
    /// font atlas can fail outright (the d3d11 error above). Font.RequestCharactersInTexture then
    /// marks the atlas dirty and fires the static Font.textureRebuilt event; Colossal's own world
    /// name-label refresh (InstanceManager.RefreshNameData, called every frame for every visible
    /// road/building/stop name) reacts by measuring the string again, which requests characters
    /// again, which retries the same allocation, fails again, and fires the event again - entirely
    /// inside ColossalFramework's own classes, with no mod code in the loop. It recurses until the
    /// stack overflows, which happens inside the overlay render pass - killing that pass for the
    /// frame, and the next, forever: the camera still moves, but nothing is clickable and the city
    /// looks frozen.
    ///
    /// This can't be fixed by not going through the atlas rebuild path - the recursion never
    /// leaves vanilla code. The only mod-side option is a reentrancy guard at the narrowest choke
    /// point Harmony can reach: RequestCharacters, the last managed method before the native call
    /// that actually fails. Worst case if a nested call is skipped: one label's glyphs render a
    /// frame later than usual - self-healing on the very next call, never a visible or permanent
    /// change in behaviour.
    /// </summary>
    public static class FontRequestCharactersGuardPatch
    {
        private static bool _inRequestCharacters;

        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(UIDynamicFont), nameof(UIDynamicFont.RequestCharacters),
                    argumentTypes: new[] { typeof(string), typeof(int), typeof(FontStyle) }),
                new PatchUtil.MethodDefinition(typeof(FontRequestCharactersGuardPatch), nameof(Prefix)),
                new PatchUtil.MethodDefinition(typeof(FontRequestCharactersGuardPatch), nameof(Postfix))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(UIDynamicFont), nameof(UIDynamicFont.RequestCharacters),
                    argumentTypes: new[] { typeof(string), typeof(int), typeof(FontStyle) })
            );
        }

        // __state marks whether THIS call is the one that owns the guard, so a blocked nested
        // call's Postfix (which still runs even though Prefix returned false) does not clear a
        // still-active outer guard.
        public static bool Prefix(out bool __state)
        {
            if (_inRequestCharacters)
            {
                __state = false;
                return false;
            }

            _inRequestCharacters = true;
            __state = true;
            return true;
        }

        public static void Postfix(bool __state)
        {
            if (__state)
            {
                _inRequestCharacters = false;
            }
        }
    }
}
