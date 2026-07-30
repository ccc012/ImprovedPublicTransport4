// Adapted from SharedStopEnabler (GPL-3.0, Workshop 2096382380, github.com/CodeBardian/SharedStopEnabler) - see LICENSE.txt.
using System;
using ColossalFramework;
using HarmonyLib;
using ImprovedPublicTransport;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace SharedStopEnabler
{
    /// <summary>Clears shared-stop bookkeeping for a line's stops when the line itself is deleted,
    /// so a deleted line's ID does not linger in the registry forever.</summary>
    [HarmonyPatch(typeof(TransportManager), "ReleaseLine", new[] { typeof(ushort) })]
    internal static class Patch_TransportManager_ReleaseLine
    {
        [HarmonyPrefix]
        public static void Prefix(ushort lineID)
        {
            try
            {
                if (!ModSetting.Instance.EnableSharedStopEnabler || lineID == 0)
                {
                    return;
                }

                var line = Singleton<TransportManager>.instance.m_lines.m_buffer[lineID];
                if (!line.Info.m_vehicleType.IsSharedStopTransport())
                {
                    return;
                }

                var stops = line.m_stops;
                if (stops == 0)
                {
                    return;
                }

                var netManager = Singleton<NetManager>.instance;
                var stopCount = line.CountStops(lineID);
                for (var i = 0; i < stopCount; i++)
                {
                    stops = global::TransportLine.GetNextStop(stops);
                    var lane = netManager.m_nodes.m_buffer[stops].m_lane;
                    var segment = netManager.m_lanes.m_buffer[lane].m_segment;
                    if (lane != 0 && segment != 0)
                    {
                        SharedStopRegistry.RemoveSharedStop(segment, lineID, lane);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"SharedStopEnabler: failed in ReleaseLine prefix: {ex.Message}");
            }
        }
    }
}
