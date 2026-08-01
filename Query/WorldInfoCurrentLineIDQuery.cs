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
                if (panels != null)
                {
                    foreach (var panel in panels)
                    {
                        if (panel == null || !panel.component.isVisible)
                        {
                            continue;
                        }

                        if (LinePanelInstanceIdField?.GetValue(panel) is InstanceID panelId)
                        {
                            if (panelId.Type == InstanceType.TransportLine && panelId.TransportLine != 0)
                            {
                                return panelId.TransportLine;
                            }

                            if (panelId.Type == InstanceType.Vehicle && panelId.Vehicle != 0)
                            {
                                firstVehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[panelId.Vehicle]
                                    .GetFirstVehicle(panelId.Vehicle);
                                if (firstVehicle != 0)
                                {
                                    var line = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[firstVehicle]
                                        .m_transportLine;
                                    if (line != 0)
                                    {
                                        return line;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // fall through to global selection
            }

            // 2) Global world-info selection.
            var currentInstanceId = WorldInfoPanel.GetCurrentInstanceID();
            if (currentInstanceId.Type == InstanceType.TransportLine)
            {
                return currentInstanceId.TransportLine;
            }

            if (currentInstanceId.Type != InstanceType.Vehicle || currentInstanceId.Vehicle == 0)
            {
                return 0;
            }

            firstVehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[currentInstanceId.Vehicle]
                .GetFirstVehicle(currentInstanceId.Vehicle);
            return firstVehicle != 0
                ? Singleton<VehicleManager>.instance.m_vehicles.m_buffer[firstVehicle].m_transportLine
                : (ushort)0;
        }
    }
}
