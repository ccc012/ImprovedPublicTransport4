using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.Util;

namespace ImprovedPublicTransport.HarmonyPatches.NetManagerPatches
{
    public class ReleaseNodePatch
    {
        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(NetManager), nameof(NetManager.ReleaseNode)),
                null,
                new PatchUtil.MethodDefinition(typeof(ReleaseNodePatch), nameof(ReleaseNodePost))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(NetManager), nameof(NetManager.ReleaseNode))
            );
        }

        public static void ReleaseNodePost(ushort node)
        {
            var cache = CachedNodeData.m_cachedNodeData;
            if (cache == null || node == 0 || node >= cache.Length || cache[node].IsEmpty)
            {
                return;
            }

            cache[node] = new NodeData();
        }
    }
}