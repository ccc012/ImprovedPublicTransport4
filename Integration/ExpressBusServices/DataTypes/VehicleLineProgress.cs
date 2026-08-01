using ColossalFramework;
using System.Collections.Generic;
using IPTUtils = ImprovedPublicTransport.Util.Utils;

namespace ExpressBusServices.DataTypes
{
    public struct VehicleLineProgress
    {
        public ushort VehicleID { get; private set; }
        public float PercentProgress { get; private set; }

        public VehicleLineProgress(ushort vehicleID, float progress)
        {
            VehicleID = vehicleID;
            PercentProgress = progress;
        }

        /// <summary>
        /// Returns the TransportLineProgress of the given transport line for vehicle progress analysis.
        /// </summary>
        /// <param name="transportLineID"></param>
        /// <returns></returns>
        public static TransportLineVehicleProgress GetTransportLineVehicleProgress(ushort transportLineID)
        {
            TransportLine theLine = Singleton<TransportManager>.instance.m_lines.m_buffer[transportLineID];
            VehicleManager instance = Singleton<VehicleManager>.instance;
            var vBuffer = instance.m_vehicles.m_buffer;
            // assume valid list, so no "exceeded size", etc.
            // we will iterate the list until it reads 0, which indicates "no more vehicles in the line"
            ushort iteratingVehicleID = theLine.m_vehicles;
            List<VehicleLineProgress> progressList = new List<VehicleLineProgress>(16);
            int loopGuard = 0;
            int maxIterations = (int)instance.m_vehicles.m_size;
            while (iteratingVehicleID != 0)
            {
                if (++loopGuard > maxIterations)
                {
                    IPTUtils.LogError("ExpressBusServices: Invalid vehicle list detected!");
                    break;
                }
                ref Vehicle veh = ref vBuffer[iteratingVehicleID];
                VehicleInfo info = veh.Info;
                if (info?.m_vehicleAI != null)
                {
                    info.m_vehicleAI.GetProgressStatus(iteratingVehicleID, ref veh, out float current, out float max);
                    // invalid bus (eg is despawning) will get max = 0
                    if (max != 0)
                    {
                        progressList.Add(new VehicleLineProgress(iteratingVehicleID, current / max));
                    }
                }
                iteratingVehicleID = veh.m_nextLineVehicle;
            }
            // all vehicles found
            // give to dedicated object
            return new TransportLineVehicleProgress(progressList);
        }
    }
}
