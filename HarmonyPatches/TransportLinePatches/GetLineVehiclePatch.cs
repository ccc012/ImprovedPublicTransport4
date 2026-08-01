using HarmonyLib;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.Util;

namespace ImprovedPublicTransport.HarmonyPatches.TransportLinePatches
{
    public class GetLineVehiclePatch
    {
        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(TransportLine),
                    nameof(TransportLine.GetLineVehicle)),
                new PatchUtil.MethodDefinition(typeof(GetLineVehiclePatch),
                    nameof(Prefix), priority: Priority.Normal)
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(TransportLine),
                    nameof(TransportLine.GetLineVehicle))
            );
        }

        public static bool Prefix(ushort lineID, ref VehicleInfo __result)
        {
            var info = TransportManager.instance.m_lines.m_buffer[lineID].Info;
            if (lineID <= 0 || info?.m_class == null || info.m_class.m_service == ItemClass.Service.Disaster)
            {
                return true; //if it's not a proper transport line, let's not modify the behavior
            }

            if (!CachedTransportLineData._init)
            {
                return true; // fall back to vanilla until line cache is ready
            }

            var dequeuedVehicle = CachedTransportLineData.Dequeue(lineID);
            var name = dequeuedVehicle ?? CachedTransportLineData.GetRandomPrefab(lineID);
            if (string.IsNullOrEmpty(name))
            {
                // No prefab filter / empty queue — do not force null (breaks spawn).
                return true;
            }

            __result = PrefabCollection<VehicleInfo>.FindLoaded(name);
            if (__result == null)
            {
                return true; // missing asset — vanilla may still pick a default
            }

            return false;
        }
    }
}