using HarmonyLib;
using ImprovedPublicTransport.Util;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace SingleTrainTrackAI
{
    /// <summary>
    /// Patches that invalidate the section/segment caches when the rail network changes during gameplay.
    /// </summary>
    internal static class NetworkChangePatch
    {
        public static void Apply()
        {
            // CreateSegment has two overloads (with/without TreeInfo); CreateNode has one.
            // Specifying argument types makes GetMethod resolve the exact overload instead of
            // throwing AmbiguousMatchException on Mono.
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(NetManager), nameof(NetManager.CreateSegment),
                    argumentTypes: new[] { typeof(ushort).MakeByRefType(), typeof(Randomizer).MakeByRefType(), typeof(NetInfo), typeof(TreeInfo), typeof(ushort), typeof(ushort), typeof(Vector3), typeof(Vector3), typeof(uint), typeof(uint), typeof(bool) }),
                null,
                new PatchUtil.MethodDefinition(typeof(NetworkChangePatch), nameof(Postfix)));

            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(NetManager), nameof(NetManager.CreateSegment),
                    argumentTypes: new[] { typeof(ushort).MakeByRefType(), typeof(Randomizer).MakeByRefType(), typeof(NetInfo), typeof(ushort), typeof(ushort), typeof(Vector3), typeof(Vector3), typeof(uint), typeof(uint), typeof(bool) }),
                null,
                new PatchUtil.MethodDefinition(typeof(NetworkChangePatch), nameof(Postfix)));

            // ReleaseSegment(ushort, bool) and ReleaseNode(ushort) — single overloads, but
            // specifying types is still safer against future game changes.
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(NetManager), nameof(NetManager.ReleaseSegment),
                    argumentTypes: new[] { typeof(ushort), typeof(bool) }),
                null,
                new PatchUtil.MethodDefinition(typeof(NetworkChangePatch), nameof(Postfix)));

            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(NetManager), nameof(NetManager.CreateNode),
                    argumentTypes: new[] { typeof(ushort).MakeByRefType(), typeof(Randomizer).MakeByRefType(), typeof(NetInfo), typeof(Vector3), typeof(uint) }),
                null,
                new PatchUtil.MethodDefinition(typeof(NetworkChangePatch), nameof(Postfix)));

            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(NetManager), nameof(NetManager.ReleaseNode),
                    argumentTypes: new[] { typeof(ushort) }),
                null,
                new PatchUtil.MethodDefinition(typeof(NetworkChangePatch), nameof(Postfix)));
        }

        public static void Undo()
        {
            // Must pass the same argumentTypes as Apply(): CreateSegment is overloaded, so
            // GetMethod without explicit types throws AmbiguousMatchException on Mono.
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(typeof(NetManager), nameof(NetManager.CreateSegment),
                argumentTypes: new[] { typeof(ushort).MakeByRefType(), typeof(Randomizer).MakeByRefType(), typeof(NetInfo), typeof(TreeInfo), typeof(ushort), typeof(ushort), typeof(Vector3), typeof(Vector3), typeof(uint), typeof(uint), typeof(bool) }));
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(typeof(NetManager), nameof(NetManager.CreateSegment),
                argumentTypes: new[] { typeof(ushort).MakeByRefType(), typeof(Randomizer).MakeByRefType(), typeof(NetInfo), typeof(ushort), typeof(ushort), typeof(Vector3), typeof(Vector3), typeof(uint), typeof(uint), typeof(bool) }));
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(typeof(NetManager), nameof(NetManager.ReleaseSegment),
                argumentTypes: new[] { typeof(ushort), typeof(bool) }));
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(typeof(NetManager), nameof(NetManager.CreateNode),
                argumentTypes: new[] { typeof(ushort).MakeByRefType(), typeof(Randomizer).MakeByRefType(), typeof(NetInfo), typeof(Vector3), typeof(uint) }));
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(typeof(NetManager), nameof(NetManager.ReleaseNode),
                argumentTypes: new[] { typeof(ushort) }));
        }

        public static void Postfix()
        {
            SectionClassifier.Clear();
            SegmentClassifier.Clear();
        }
    }
}