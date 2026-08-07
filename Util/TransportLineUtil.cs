using System;
using System.Collections.Generic;
using ColossalFramework;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.ReverseDetours;

namespace ImprovedPublicTransport.Util
{
    public static class TransportLineUtil
    {


        public static ushort GetNextVehicle(ushort lineID, ushort vehicleID)
        {
            ushort vehicles = Singleton<TransportManager>.instance.m_lines.m_buffer[(int) lineID].m_vehicles;
            ushort nextLineVehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[(int) vehicleID]
                .m_nextLineVehicle;
            if ((int) nextLineVehicle == 0)
                return vehicles;
            return nextLineVehicle;
        }

        public static ushort GetPreviousVehicle(ushort lineID, ushort vehicleID)
        {
            TransportLine transportLine = Singleton<TransportManager>.instance.m_lines.m_buffer[(int) lineID];
            int num1 = transportLine.CountVehicles(lineID);
            ushort num2 = 0;
            for (int index = 0; index < num1; ++index)
            {
                ushort vehicle = transportLine.GetVehicle(index);
                if ((int) vehicle == (int) vehicleID)
                {
                    if ((int) num2 == 0)
                        return transportLine.GetVehicle(num1 - 1);
                    return num2;
                }

                num2 = vehicle;
            }

            return transportLine.m_vehicles;
        }

        public static int GetStopIndex(ushort lineID, ushort stopID)
        {
            ushort stop = Singleton<TransportManager>.instance.m_lines.m_buffer[(int) lineID].m_stops;
            int num1 = 0;
            int num2 = 0;
            while ((int) stop != 0)
            {
                if ((int) stopID == (int) stop)
                    return num1;
                ++num1;
                stop = TransportLine.GetNextStop(stop);
                if (++num2 >= 32768)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core,
                        "Invalid list detected!\n" + System.Environment.StackTrace);
                    break;
                }
            }

            return 0;
        }

        //based off code in the SimulationStep of TransportLine
        public static int CountLineActiveVehicles(ushort lineID, out int allVehicles, Action<Int32> callback = null)
        {
            TransportLine thisLine = TransportManager.instance.m_lines.m_buffer[lineID];
            int activeVehicles = 0;
            allVehicles = 0;
            //this part is directly taken from beginning of vanilla SimulationStep method (except for marked part)

            if (thisLine.Complete)
            {
                int num2 = 0;
                int num3 = 0;
                if ((int) thisLine.m_vehicles != 0)
                {
                    VehicleManager instance3 = Singleton<VehicleManager>.instance;
                    ushort num4 = thisLine.m_vehicles;
                    int num5 = 0;
                    while ((int) num4 != 0)
                    {
                        ushort nextLineVehicle = instance3.m_vehicles.m_buffer[(int) num4].m_nextLineVehicle;
                        ++num2;
                        // Decompiler soup for "(flags & GoingBack) == 0" — use a real bit test.
                        if ((instance3.m_vehicles.m_buffer[(int) num4].m_flags & Vehicle.Flags.GoingBack) == 0)
                        {
                            //begin mod(+): callback
                            callback?.Invoke(num4);
                            //end mod
                            ++num3;
                        }

                        num4 = nextLineVehicle;
                        if (++num5 > CachedVehicleData.MaxVehicleCount)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core,
                                "Invalid list detected!\n" + System.Environment.StackTrace);
                            break;
                        }
                    }
                }

                //end of vanilla part
                activeVehicles = num3;
                allVehicles = num2;
            }

            return activeVehicles;
        }

        //based off code in TransportLine.SimulationStep
        //based off code in TransportLine.SimulationStep
        public static void RemoveActiveVehicle(ushort lineID, bool descreaseTargetVehicleCount, int activeVehiclesCount)
        {
            if (activeVehiclesCount <= 0)
            {
                return;
            }

            var activeVehicles = new List<ushort>(Math.Max(1, activeVehiclesCount));
            CountLineActiveVehicles(lineID, out _, vehicleID => activeVehicles.Add((ushort)vehicleID));
            if (activeVehicles.Count == 0)
            {
                return;
            }

            int selectedIndex = (int)Singleton<SimulationManager>.instance.m_randomizer.Int32((uint)activeVehicles.Count);
            TransportLineUtil.RemoveVehicle(lineID, activeVehicles[selectedIndex], descreaseTargetVehicleCount);
        }

        //based off code in TransportLine.SimulationStep
        /// <summary>
        /// Detach a vehicle from its line so it returns to the depot/garage (not despawned mid-route).
        /// </summary>
        public static void RemoveVehicle(ushort lineID, ushort vehicleID, bool descreaseTargetVehicleCount)
        {
            VehicleManager instance = Singleton<VehicleManager>.instance;
            ref var vehicle = ref instance.m_vehicles.m_buffer[(int)vehicleID];
            if ((vehicle.m_flags & Vehicle.Flags.GoingBack) != 0)
            {
                return;
            }

            if (descreaseTargetVehicleCount)
            {
                CachedTransportLineData.DecreaseTargetVehicleCount(lineID);
            }

            var info = vehicle.Info;
            if (info?.m_vehicleAI == null)
            {
                return;
            }

            // Clear line assignment — vehicle AI then heads home to its depot/garage.
            info.m_vehicleAI.SetTransportLine(vehicleID, ref vehicle, (ushort)0);

            // If the AI did not set GoingBack (some custom vehicle AIs), force a home return.
            if ((vehicle.m_flags & Vehicle.Flags.GoingBack) == 0
                && (vehicle.m_flags & Vehicle.Flags.Deleted) == 0)
            {
                vehicle.m_flags |= Vehicle.Flags.GoingBack;
                if (vehicle.m_sourceBuilding != 0)
                {
                    try
                    {
                        info.m_vehicleAI.SetTarget(vehicleID, ref vehicle, vehicle.m_sourceBuilding);
                    }
                    catch
                    {
                        // Non-fatal: SetTransportLine alone is enough for vanilla public-transport AIs.
                    }
                }
            }
        }
    }
}

