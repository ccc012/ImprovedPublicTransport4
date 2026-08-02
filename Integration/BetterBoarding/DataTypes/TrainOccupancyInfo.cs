using System;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework;
using IPTUtils = ImprovedPublicTransport.Util.Utils;

namespace BetterBoarding.DataTypes
{
    public class TrainOccupancyInfo
    {
        public ushort FirstVehicleID { get; private set; }

        private Dictionary<ushort, VehicleOccupancyInfo> _compartmentOccupancy = new Dictionary<ushort, VehicleOccupancyInfo>();

        public List<VehicleOccupancyInfo> FreeCompartments => _compartmentOccupancy.Values.Where((item) => !item.IsFull).ToList();

        public int FreeCapacity
        {
            get
            {
                var counter = 0;
                foreach (var compartment in FreeCompartments)
                {
                    if (compartment.IsProxy)
                    {
                        continue;
                    }
                    counter += compartment.ActualCapacity - compartment.Occupancy;
                }
                return counter;
            }
        }

        public TrainOccupancyInfo(ushort vehicleID)
        {
            // find the vehicle ID of the first vehicle first
            SetFirstVehicleID(vehicleID);

            // then, iterate through the train to find per-vehicle occupancy
            var vehicleManager = Singleton<VehicleManager>.instance;
            var currentVehicleID = FirstVehicleID;
            var iterationCount = 0;
            while (true)
            {
                if (++iterationCount > 16384)
                {
                    IPTUtils.LogError("BetterBoarding: Invalid vehicle list detected!\n" + Environment.StackTrace);
                    break;
                }
                var currentVehicleInstance = vehicleManager.m_vehicles.m_buffer[currentVehicleID];
                _compartmentOccupancy[currentVehicleID] = new VehicleOccupancyInfo(currentVehicleID);
                var nextVehicleID = currentVehicleInstance.m_trailingVehicle;
                if (nextVehicleID == 0)
                {
                    break;
                }
                currentVehicleID = nextVehicleID;
            }
        }

        private void SetFirstVehicleID(ushort vehicleID)
        {
            var currentVehicleID = vehicleID;
            var vehicleManager = Singleton<VehicleManager>.instance;
            var currentVehicleInstance = vehicleManager.m_vehicles.m_buffer[currentVehicleID];
            var guard = 0;
            while (currentVehicleInstance.m_leadingVehicle != 0 && guard++ < 16384)
            {
                // move 1 vehicle to the front
                currentVehicleID = currentVehicleInstance.m_leadingVehicle;
                currentVehicleInstance = vehicleManager.m_vehicles.m_buffer[currentVehicleID];
            }
            // we are at the front
            FirstVehicleID = currentVehicleID;
        }
    }
}
