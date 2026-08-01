using ColossalFramework;
using ImprovedPublicTransport.Util;

namespace ImprovedPublicTransport.HarmonyPatches.TransportLinePatches
{
    /// <summary>
    /// Prevents simulation popups ("Array index is out of range") when vanilla or integration
    /// code calls <see cref="TransportLine.GetNextStop"/> / <see cref="TransportLine.GetPrevStop"/>
    /// with a building ID, a released node, or any index outside the node buffer.
    /// </summary>
    public static class TransportLineStopSafetyPatch
    {
        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(TransportLine), nameof(TransportLine.GetNextStop)),
                new PatchUtil.MethodDefinition(typeof(TransportLineStopSafetyPatch), nameof(GetNextStopPrefix))
            );
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(TransportLine), nameof(TransportLine.GetPrevStop)),
                new PatchUtil.MethodDefinition(typeof(TransportLineStopSafetyPatch), nameof(GetPrevStopPrefix))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(TransportLine), nameof(TransportLine.GetNextStop))
            );
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(TransportLine), nameof(TransportLine.GetPrevStop))
            );
        }

        /// <summary>
        /// True when <paramref name="stopId"/> is a live index into the NetManager node buffer.
        /// Does not require the node to still belong to a transport line (released mid-frame races).
        /// </summary>
        public static bool IsInNodeBuffer(ushort stopId)
        {
            if (stopId == 0)
            {
                return false;
            }

            var nodes = Singleton<NetManager>.instance?.m_nodes.m_buffer;
            return nodes != null && stopId < nodes.Length;
        }

        /// <summary>
        /// Stricter: node exists in buffer and is still marked Created (safe for pathfind / redeploy).
        /// </summary>
        public static bool IsLiveStopNode(ushort stopId)
        {
            if (!IsInNodeBuffer(stopId))
            {
                return false;
            }

            var flags = Singleton<NetManager>.instance.m_nodes.m_buffer[stopId].m_flags;
            return (flags & NetNode.Flags.Created) != 0;
        }

        // Parameter names MUST match the game's method signature (stop), not a made-up name —
        // Harmony otherwise throws "Parameter currentStop not found" and the guard never applies.
        private static bool GetNextStopPrefix(ushort stop, ref ushort __result)
        {
            if (IsInNodeBuffer(stop))
            {
                return true;
            }

            __result = 0;
            return false;
        }

        private static bool GetPrevStopPrefix(ushort stop, ref ushort __result)
        {
            if (IsInNodeBuffer(stop))
            {
                return true;
            }

            __result = 0;
            return false;
        }
    }
}
