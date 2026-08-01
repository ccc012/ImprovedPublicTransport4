using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.HarmonyPatches.VehicleAIPatches;
using ImprovedPublicTransport.Util;

namespace ImprovedPublicTransport.HarmonyPatches.VehicleManagerPatches
{
    public class ReleaseWaterSourcePatch
    {
        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(VehicleManager), "ReleaseWaterSource"),
                null,
                new PatchUtil.MethodDefinition(typeof(ReleaseWaterSourcePatch), nameof(ReleaseWaterSourcePost))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(VehicleManager), "ReleaseWaterSource")
            );
        }


        //the method is called from within ReleaseVehicle method. Patching it leads to the least chance of conflict
        public static void ReleaseWaterSourcePost(ushort vehicle, ref Vehicle data)
        {
            // Always drop empty-before-depot pending state — previously only Express Bus unspawn
            // forgot IDs, so recycled vehicle slots could inherit "return when empty".
            EmptyBeforeDepotPatch.ForgetVehicle(vehicle);

            var cache = CachedVehicleData.m_cachedVehicleData;
            if (cache == null || vehicle == 0 || vehicle >= cache.Length)
            {
                return;
            }

            // Always clear IPT vehicle cache on release (flags-only slots used to keep CurrentStop).
            cache[vehicle] = new VehicleData();
        }
    }
}