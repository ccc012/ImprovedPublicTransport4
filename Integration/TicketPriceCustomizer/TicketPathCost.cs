// Optional pathfinding cost on public-transport stop lanes (like road tolls use m_ticketCost).
using System;
using System.Collections.Generic;
using System.Threading;
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
        private static readonly Dictionary<uint, byte> PreviousCosts = new Dictionary<uint, byte>();
        private static readonly Dictionary<uint, byte> AppliedCosts = new Dictionary<uint, byte>();
        private static int _applyQueued;

        public static void ApplyFromSettings()
        {
            if (Interlocked.CompareExchange(ref _applyQueued, 1, 0) == 0)
            {
                Singleton<SimulationManager>.instance.AddAction(Apply);
            }
        }

        private static void Apply()
        {
            Interlocked.Exchange(ref _applyQueued, 0);
            if (!ModSetting.Instance.EnableTicketPathfindingCost
                || ModSetting.Instance.TicketPriceCustomizerMode != ModSetting.TicketPriceCustomizerModes.Enabled)
            {
                ClearAllStopLaneCosts();
                return;
            }

            try
            {
                ClearAllStopLaneCosts();
                var nm = Singleton<NetManager>.instance;
                if (nm == null)
                {
                    return;
                }

                var settings = ModSetting.Instance.TicketPriceCustomizer;
                if (settings == null)
                {
                    return;
                }

                var isNight = Singleton<SimulationManager>.instance.m_isNightTime;
                var applied = 0;
                for (ushort segmentId = 1; segmentId < nm.m_segments.m_buffer.Length; segmentId++)
                {
                    ref var segment = ref nm.m_segments.m_buffer[segmentId];
                    var info = segment.Info;
                    if (info == null || info.m_lanes == null)
                    {
                        continue;
                    }

                    var laneId = segment.m_lanes;
                    for (var laneIndex = 0; laneIndex < info.m_lanes.Length && laneId != 0; laneIndex++)
                    {
                        ref var lane = ref nm.m_lanes.m_buffer[laneId];
                        var nextLane = lane.m_nextLane;
                        var flags = (NetLane.Flags)lane.m_flags;
                        var cost = GetCost(info.m_lanes[laneIndex], settings, isNight);
                        if ((flags & (NetLane.Flags.Stop | NetLane.Flags.Stop2)) != 0 && cost != 0)
                        {
                            if (!PreviousCosts.ContainsKey(laneId))
                            {
                                PreviousCosts.Add(laneId, lane.m_ticketCost);
                            }

                            lane.m_ticketCost = cost;
                            AppliedCosts[laneId] = cost;
                            applied++;
                        }

                        laneId = nextLane;
                    }
                }

                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log($"TicketPathCost: applied modality costs on {applied} stop lanes.");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPathCost.Apply failed: {ex.Message}");
            }
        }

        private static byte GetCost(NetInfo.Lane laneInfo, ModSetting.TicketPriceCustomizerSettings settings, bool isNight)
        {
            var vehicleType = laneInfo.m_vehicleType;
            float multiplier;
            if ((vehicleType & VehicleInfo.VehicleType.Car) != 0)
            {
                multiplier = isNight ? settings.BusNightMultiplier : settings.BusMultiplier;
            }
            else if ((vehicleType & VehicleInfo.VehicleType.Metro) != 0)
            {
                multiplier = isNight ? settings.MetroNightMultiplier : settings.MetroMultiplier;
            }
            else if ((vehicleType & VehicleInfo.VehicleType.Train) != 0)
            {
                multiplier = isNight ? settings.TrainNightMultiplier : settings.TrainMultiplier;
            }
            else if ((vehicleType & VehicleInfo.VehicleType.Tram) != 0)
            {
                multiplier = isNight ? settings.TramNightMultiplier : settings.TramMultiplier;
            }
            else if ((vehicleType & VehicleInfo.VehicleType.Monorail) != 0)
            {
                multiplier = isNight ? settings.MonorailNightMultiplier : settings.MonorailMultiplier;
            }
            else if ((vehicleType & VehicleInfo.VehicleType.Ferry) != 0)
            {
                multiplier = isNight ? settings.FerryNightMultiplier : settings.FerryMultiplier;
            }
            else if ((vehicleType & VehicleInfo.VehicleType.CableCar) != 0)
            {
                multiplier = isNight ? settings.CableCarNightMultiplier : settings.CableCarMultiplier;
            }
            else if ((vehicleType & VehicleInfo.VehicleType.Trolleybus) != 0)
            {
                multiplier = isNight ? settings.TrolleybusNightMultiplier : settings.TrolleybusMultiplier;
            }
            else
            {
                return 0;
            }

            return (byte)Mathf.Clamp(Mathf.RoundToInt(20f * multiplier), 1, MaxCost);
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

                foreach (var appliedCost in AppliedCosts)
                {
                    if (appliedCost.Key >= nm.m_lanes.m_buffer.Length)
                    {
                        continue;
                    }

                    ref var lane = ref nm.m_lanes.m_buffer[appliedCost.Key];
                    if (lane.m_ticketCost == appliedCost.Value && PreviousCosts.TryGetValue(appliedCost.Key, out var previousCost))
                    {
                        lane.m_ticketCost = previousCost;
                    }
                }

                AppliedCosts.Clear();
                PreviousCosts.Clear();
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPathCost.Clear failed: {ex.Message}");
            }
        }
    }
}
