using System.Reflection;
using ColossalFramework;
using HarmonyLib;

namespace ImprovedPublicTransport.Query
{
    public static class WorldInfoCurrentLineIDQuery
    {
        private static readonly FieldInfo LinePanelInstanceIdField =
            AccessTools.Field(typeof(PublicTransportWorldInfoPanel), "m_InstanceID");

        /// <summary>
        /// Resolves the transport line currently shown in the world-info UI.
        /// Prefers the line panel's own InstanceID (reliable when IPT extends the panel), then
        /// falls back to the global WorldInfoPanel selection (line or vehicle).
        /// </summary>
        public static ushort Query(out ushort firstVehicle)
        {
            firstVehicle = 0;

            // 1) Line panel open — read its target even if WorldInfo focus is still a vehicle.
            try
            {
                var panels = UnityEngine.Object.FindObjectsOfType<PublicTransportWorldInfoPanel>();
                for (int i = 0; i < panels.Length; i++)
                {
                    var panel = panels[i];
                    if (panel?.component == null || !panel.component.isVisible)
                    {
                        continue;
                    }

                    if (LinePanelInstanceIdField?.GetValue(panel) is InstanceID panelId)
                    {
                        if (panelId.Type == InstanceType.TransportLine && IsValidLine(panelId.TransportLine))
                        {
                            return panelId.TransportLine;
                        }

                        if (panelId.Type == InstanceType.Vehicle && TryGetVehicleLine(panelId.Vehicle, out firstVehicle, out ushort line))
                        {
                            return line;
                        }
                    }
                }
            }
            catch
            {
                // fall through to global selection
            }

            // 2) Global world-info selection.
            try
            {
                var currentInstanceId = WorldInfoPanel.GetCurrentInstanceID();
                if (currentInstanceId.Type == InstanceType.TransportLine)
                {
                    return IsValidLine(currentInstanceId.TransportLine) ? currentInstanceId.TransportLine : (ushort)0;
                }

                return currentInstanceId.Type == InstanceType.Vehicle &&
                       TryGetVehicleLine(currentInstanceId.Vehicle, out firstVehicle, out ushort line)
                    ? line
                    : (ushort)0;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"WorldInfoCurrentLineIDQuery: Exception in global selection: {ex.Message}");
                firstVehicle = 0;
                return 0;
            }
        }

        private static bool IsValidLine(ushort line)
        {
            var manager = Singleton<TransportManager>.instance;
            return line != 0 && manager != null && line < manager.m_lines.m_buffer.Length &&
                   (manager.m_lines.m_buffer[line].m_flags & TransportLine.Flags.Created) != 0 &&
                   manager.m_lines.m_buffer[line].Info != null;
        }

        private static bool TryGetVehicleLine(ushort vehicle, out ushort firstVehicle, out ushort line)
        {
            firstVehicle = 0;
            line = 0;
            var manager = Singleton<VehicleManager>.instance;
            if (vehicle == 0 || manager == null || vehicle >= manager.m_vehicles.m_buffer.Length)
            {
                return false;
            }

            if ((manager.m_vehicles.m_buffer[vehicle].m_flags &
                 (Vehicle.Flags.Created | Vehicle.Flags.Deleted)) != Vehicle.Flags.Created)
            {
                return false;
            }

            firstVehicle = manager.m_vehicles.m_buffer[vehicle].GetFirstVehicle(vehicle);
            if (firstVehicle == 0 || firstVehicle >= manager.m_vehicles.m_buffer.Length ||
                (manager.m_vehicles.m_buffer[firstVehicle].m_flags &
                 (Vehicle.Flags.Created | Vehicle.Flags.Deleted)) != Vehicle.Flags.Created)
            {
                firstVehicle = 0;
                return false;
            }

            line = manager.m_vehicles.m_buffer[firstVehicle].m_transportLine;
            return IsValidLine(line);
        }

    }
}
