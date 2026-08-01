// Optional pathfinding cost on public-transport stop lanes (like road tolls use m_ticketCost).
using System;
using ColossalFramework;
using ImprovedPublicTransport.Util;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.Integration.TicketPriceCustomizer
{
    /// <summary>
    /// Vanilla PathFind reads <see cref="NetLane.m_ticketCost"/> (normally only toll booths write it).
    /// When enabled, map transport ticket prices onto stop-lane ticket costs so cims slightly prefer
    /// cheaper lines — experimental and off by default.
    /// </summary>
    internal static class TicketPathCost
    {
        private const byte MaxCost = 40;

        public static void ApplyFromSettings()
        {
            if (!ModSetting.Instance.EnableTicketPathfindingCost
                || ModSetting.Instance.TicketPriceCustomizerMode != ModSetting.TicketPriceCustomizerModes.Enabled)
            {
                ClearAllStopLaneCosts();
                return;
            }

            try
            {
                var tm = Singleton<TransportManager>.instance;
                var nm = Singleton<NetManager>.instance;
                if (tm == null || nm == null)
                {
                    return;
                }

                // Sample average ticket price across active lines (ushort, vanilla default ~100).
                long sum = 0;
                var count = 0;
                for (ushort i = 1; i < tm.m_lines.m_buffer.Length; i++)
                {
                    ref var line = ref tm.m_lines.m_buffer[i];
                    if ((line.m_flags & TransportLine.Flags.Complete) == 0)
                    {
                        continue;
                    }

                    sum += line.m_ticketPrice;
                    count++;
                }

                var avg = count > 0 ? (int)(sum / count) : 100;
                // Map ~50–200 ticket price into 0–MaxCost path cost.
                var cost = (byte)Mathf.Clamp(avg / 5, 0, MaxCost);

                var applied = 0;
                for (uint lane = 1; lane < nm.m_lanes.m_buffer.Length; lane++)
                {
                    ref var l = ref nm.m_lanes.m_buffer[lane];
                    var flags = (NetLane.Flags)l.m_flags;
                    if ((flags & (NetLane.Flags.Stop | NetLane.Flags.Stop2)) == 0)
                    {
                        continue;
                    }

                    l.m_ticketCost = cost;
                    applied++;
                }

                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log($"TicketPathCost: applied cost={cost} on {applied} stop lanes (avg ticket={avg}).");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPathCost.Apply failed: {ex.Message}");
            }
        }

        public static void ClearAllStopLaneCosts()
        {
            try
            {
                var nm = Singleton<NetManager>.instance;
                if (nm == null)
                {
                    return;
                }

                for (uint lane = 1; lane < nm.m_lanes.m_buffer.Length; lane++)
                {
                    ref var l = ref nm.m_lanes.m_buffer[lane];
                    var flags = (NetLane.Flags)l.m_flags;
                    if ((flags & (NetLane.Flags.Stop | NetLane.Flags.Stop2)) == 0)
                    {
                        continue;
                    }

                    // Do not clear lanes that might be road-toll related without stop flags — we only
                    // touch lanes that already have PT stop flags.
                    if (l.m_ticketCost != 0)
                    {
                        l.m_ticketCost = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPathCost.Clear failed: {ex.Message}");
            }
        }
    }
}
