using ColossalFramework;
using ExpressBusServices.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IPTUtils = ImprovedPublicTransport.Util.Utils;

namespace ExpressBusServices.Util
{
    public class TransportVehicleUtil
    {
        public static void TellVehicleToReturnToBase(ushort vehicleID, ref Vehicle data)
        {
            if (data.m_transportLine == 0)
            {
                // no op
                return;
            }

            // SetTransportLine(0) already unlinks the vehicle from the line.
            // Do NOT call RemoveVehicle first: EmptyBeforeReturnToDepot may block
            // SetTransportLine while RemoveVehicle already orphaned the vehicle from
            // the line list → half-state, repath thrash, U-turn then continue.
            if (data.Info?.m_vehicleAI != null)
            {
                data.Info.m_vehicleAI.SetTransportLine(vehicleID, ref data, 0);
            }
        }

        public static bool VehicleHasProgressPercent(ushort vehicleID, ref Vehicle data)
        {
            if (data.m_transportLine == 0 || data.Info?.m_vehicleAI == null)
            {
                return false;
            }

            // Fast path: only query THIS vehicle. The old path rebuilt progress for every
            // vehicle on the line (GetProgressStatus × N + List/sort) on every bus arrival —
            // a major hitch with dense fleets (dozens of buses × many lines).
            data.Info.m_vehicleAI.GetProgressStatus(vehicleID, ref data, out _, out float max);
            return max != 0f;
        }

        public static void FindFirstVehicleOfVehicleSet(ushort vehicleID, ref Vehicle data, out ushort firstVehicleID, out Vehicle firstVehicleData)
        {
            // assume valid lists
            ushort currentID = data.m_leadingVehicle;
            if (currentID == 0)
            {
                // already first of set
                firstVehicleID = vehicleID;
                firstVehicleData = data;
                return;
            }

            // iterate to the first of the list
            VehicleManager manager = Singleton<VehicleManager>.instance;
            ref Vehicle currentData = ref manager.m_vehicles.m_buffer[currentID];
            int loopGuard = 0;
            while (currentData.m_leadingVehicle != 0)
            {
                if (++loopGuard > 64)
                {
                    IPTUtils.LogError("ExpressBusServices: Invalid vehicle set list detected!");
                    break;
                }
                currentID = currentData.m_leadingVehicle;
                currentData = ref manager.m_vehicles.m_buffer[currentID];
            }
            // at first of list
            firstVehicleID = currentID;
            firstVehicleData = currentData;
        }
    }
}
