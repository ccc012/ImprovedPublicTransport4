using System.Collections.Generic;
using ColossalFramework;
using ImprovedPublicTransport.Util;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.HarmonyPatches.VehicleAIPatches
{
    /// <summary>
    /// When enabled: public-transport vehicles with passengers on board cannot be sent back to the
    /// depot yet. They keep their line until empty, then return. Vanilla unloads only on arrival.
    /// </summary>
    public static class EmptyBeforeDepotPatch
    {
        private static readonly HashSet<ushort> PendingReturn = new HashSet<ushort>();

        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(VehicleAI), nameof(VehicleAI.SetTransportLine)),
                new PatchUtil.MethodDefinition(typeof(EmptyBeforeDepotPatch), nameof(SetTransportLinePrefix))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(VehicleAI), nameof(VehicleAI.SetTransportLine))
            );
            PendingReturn.Clear();
        }

        public static void ForgetVehicle(ushort vehicleID)
        {
            PendingReturn.Remove(vehicleID);
        }

        /// <summary>
        /// Call after unload: if this vehicle was waiting to return and is now empty, send it home.
        /// </summary>
        public static void TryCompletePendingReturn(ushort vehicleID, ref Vehicle data)
        {
            if (!ModSetting.Instance.EnableEmptyBeforeReturnToDepot)
            {
                return;
            }

            if (!PendingReturn.Contains(vehicleID))
            {
                return;
            }

            if (!IsPublicTransport(ref data) || HasPassengers(vehicleID, ref data))
            {
                return;
            }

            PendingReturn.Remove(vehicleID);
            if (data.Info?.m_vehicleAI == null)
            {
                return;
            }

            // Now allowed to leave the line for the depot.
            data.Info.m_vehicleAI.SetTransportLine(vehicleID, ref data, 0);
            if (Diagnostics.VerboseRuntimeLogs)
            {
                Utils.Log($"EmptyBeforeDepot: vehicle {vehicleID} empty — now returning to depot.");
            }
        }

        private static bool SetTransportLinePrefix(
            VehicleAI __instance,
            ushort vehicleID,
            ref Vehicle data,
            ushort transportLine)
        {
            try
            {
                if (!ModSetting.Instance.EnableEmptyBeforeReturnToDepot)
                {
                    return true;
                }

                // Only intercept "leave line / go home" (line → 0), not normal reassignment.
                if (transportLine != 0 || data.m_transportLine == 0)
                {
                    if (transportLine != 0)
                    {
                        PendingReturn.Remove(vehicleID);
                    }

                    return true;
                }

                if (!IsPublicTransport(ref data))
                {
                    return true;
                }

                if (!HasPassengers(vehicleID, ref data))
                {
                    PendingReturn.Remove(vehicleID);
                    return true;
                }

                // Stay on the line until empty; remember intent to return.
                PendingReturn.Add(vehicleID);

                // If something already flipped GoingBack / cleared the stop target before we
                // blocked SetTransportLine, undo that half-return so the bus keeps its route
                // instead of U-turning toward the depot then re-pathing onto the line.
                if ((data.m_flags & Vehicle.Flags.GoingBack) != 0)
                {
                    data.m_flags &= ~Vehicle.Flags.GoingBack;
                }

                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log(
                        $"EmptyBeforeDepot: blocked return of vehicle {vehicleID} with passengers; will return when empty.");
                }

                return false;
            }
            catch
            {
                return true;
            }
        }

        private static bool IsPublicTransport(ref Vehicle data)
        {
            return data.Info?.m_class != null
                   && data.Info.m_class.m_service == ItemClass.Service.PublicTransport;
        }

        private static bool HasPassengers(ushort vehicleID, ref Vehicle data)
        {
            try
            {
                if (data.Info?.m_vehicleAI == null)
                {
                    return false;
                }

                data.Info.m_vehicleAI.GetBufferStatus(vehicleID, ref data, out _, out int passengers, out _);
                if (passengers > 0)
                {
                    return true;
                }

                // Multi-car trains: check trailers.
                var vm = Singleton<VehicleManager>.instance;
                var trailing = data.m_trailingVehicle;
                var guard = 0;
                while (trailing != 0 && guard++ < 32)
                {
                    ref var trail = ref vm.m_vehicles.m_buffer[trailing];
                    if (trail.Info?.m_vehicleAI != null)
                    {
                        trail.Info.m_vehicleAI.GetBufferStatus(trailing, ref trail, out _, out int p, out _);
                        if (p > 0)
                        {
                            return true;
                        }
                    }

                    trailing = trail.m_trailingVehicle;
                }
            }
            catch
            {
                // fail open
            }

            return false;
        }
    }
}
