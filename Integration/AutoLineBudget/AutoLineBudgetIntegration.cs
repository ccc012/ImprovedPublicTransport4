using System;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.Util;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.Integration.AutoLineBudget
{
    /// <summary>
    /// Ported from AutoLineBudget 21 (GPL-3.0, github.com/jakeluba/AutoLineBudget21).
    /// Unlike the original - which wrote TransportLine.m_budget directly and raced
    /// with IPT's own vehicle spawn queue, causing runaway maintenance costs - this
    /// drives CachedTransportLineData.SetTargetVehicleCount/SetBudgetControlState,
    /// the same API IPT's own SimulationStepPatch uses to decide vehicle counts.
    /// </summary>
    public class AutoLineBudgetIntegration : MonoBehaviour
    {
        private const float UpdateFrequencySeconds = 5f;
        private const float Inertia = 0.625f;
        private const float TargetOccupancy = 0.5f;

        private float _timeSinceUpdate;
        private int _prevHour = -1;

        // Lines this integration has taken over from vanilla budget control.
        // Needed because taking over sets BudgetControl=false, which would
        // otherwise look identical to the user manually choosing Manual mode.
        private readonly HashSet<ushort> _managedLines = new();

        private readonly Dictionary<ushort, Dictionary<int, float>> _lineHourFlow = new();
        private readonly Dictionary<ushort, SortedDictionary<int, float>> _avgIntervalByVehCount = new();
        private readonly Dictionary<ushort, SortedDictionary<int, float>> _avgIntervalCountByVehCount = new();

        private void Update()
        {
            _timeSinceUpdate += Time.unscaledDeltaTime;
            if (_timeSinceUpdate < UpdateFrequencySeconds)
            {
                return;
            }
            _timeSinceUpdate = 0f;

            if (!Singleton<TransportManager>.exists)
            {
                return;
            }

            var hour = SimulationManager.instance.m_currentGameTime.Hour;
            if (hour == _prevHour)
            {
                return;
            }
            _prevHour = hour;

            var transportManager = Singleton<TransportManager>.instance;
            var lineCount = transportManager.m_lines.m_buffer.Length;
            for (ushort lineID = 1; lineID < lineCount; ++lineID)
            {
                // Incomplete / released slots recycle lineIDs - drop any stale demand
                // samples so a brand-new line on that slot does not inherit old hour-flow data.
                if (!transportManager.m_lines.m_buffer[lineID].Complete)
                {
                    ForgetLine(lineID);
                    continue;
                }

                var underVanillaBudgetControl = CachedTransportLineData.GetBudgetControlState(lineID);
                if (underVanillaBudgetControl)
                {
                    // Either untouched so far, or the user just flipped this line
                    // back to Budget mode in the UI - relinquish it either way and
                    // let it run vanilla for one cycle before we consider it again.
                    if (_managedLines.Remove(lineID))
                    {
                        continue;
                    }
                }
                else if (!_managedLines.Contains(lineID))
                {
                    // BudgetControl is off and we never took it over - the user set
                    // this line to Manual on purpose. Never touch it.
                    continue;
                }

                ProcessLine(lineID);
            }
        }

        private void ForgetLine(ushort lineID)
        {
            _managedLines.Remove(lineID);
            _lineHourFlow.Remove(lineID);
            _avgIntervalByVehCount.Remove(lineID);
            _avgIntervalCountByVehCount.Remove(lineID);
        }

        private void ProcessLine(ushort lineID)
        {
            var vehicleManager = Singleton<VehicleManager>.instance;
            var line = TransportManager.instance.m_lines.m_buffer[lineID];

            var occupiedWeighted = 0f;
            var capacity = 0f;
            var vehCount = TransportLineUtil.CountLineActiveVehicles(lineID, out _, vehicleID =>
            {
                var info = vehicleManager.m_vehicles.m_buffer[vehicleID].Info;
                if (info?.m_vehicleAI == null)
                {
                    return;
                }
                info.m_vehicleAI.GetBufferStatus((ushort)vehicleID, ref vehicleManager.m_vehicles.m_buffer[vehicleID], out _, out var current, out var max);
                if (max <= 0)
                {
                    return;
                }
                var occShare = current / (float)max;
                var occFactor = 1f;
                if (occShare > 0.5f)
                {
                    occFactor = 4f * occShare * occShare - 6f * occShare + 4f - 0.5f / occShare;
                }
                occupiedWeighted += current * occFactor;
                capacity += max;
            });

            if (vehCount == 0 || capacity <= 0f)
            {
                return;
            }

            var avgOccupancy = occupiedWeighted / capacity;
            var lineFlow = capacity * avgOccupancy;
            const float maxWaitingTime = 120f;
            lineFlow *= Mathf.Max(1f, line.m_averageInterval / maxWaitingTime);

            TrackIntervalStability(lineID, vehCount, line.m_averageInterval, out var intervalByVehCount);

            if (!_lineHourFlow.TryGetValue(lineID, out var hourFlow))
            {
                _lineHourFlow[lineID] = hourFlow = new Dictionary<int, float>();
            }
            hourFlow[_prevHour] = lineFlow;
            if (hourFlow.Count > 24)
            {
                hourFlow.Clear();
                return;
            }
            var avgFlow = hourFlow.Values.Sum() / hourFlow.Count;
            if (avgFlow <= 0f)
            {
                return;
            }

            var demandOccupancy = avgFlow / capacity;
            // Correct for having less than a full day of data yet.
            var correctedTarget = (TargetOccupancy - demandOccupancy) * hourFlow.Count / 24f + demandOccupancy;
            if (correctedTarget <= 0f)
            {
                return;
            }

            var vehChange = demandOccupancy * vehCount / correctedTarget - vehCount;
            if (Mathf.Abs(vehChange + Inertia) < Inertia)
            {
                return;
            }

            var candidateCount = vehCount + Mathf.RoundToInt(vehChange);
            if (vehChange > 0 && intervalByVehCount.Count > 0)
            {
                // Anti-jam: don't grow toward a vehicle count that historically ran worse
                // (higher average interval) than the current one, unless it's the best we've seen.
                var best = intervalByVehCount.OrderBy(kv => kv.Value).First().Key;
                if (best < vehCount + vehChange && best != intervalByVehCount.Keys.Last())
                {
                    candidateCount = best;
                }
            }

            var newTarget = Math.Max(1, candidateCount);
            if (newTarget == CachedTransportLineData.GetTargetVehicleCount(lineID))
            {
                return;
            }

            CachedTransportLineData.SetTargetVehicleCount(lineID, newTarget);
            CachedTransportLineData.SetBudgetControlState(lineID, false);
            _managedLines.Add(lineID);
            if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs)
            {
                Utils.Log($"AutoLineBudget: line {lineID} vehCount={vehCount} avgOccupancy={avgOccupancy:F2} -> target={newTarget}");
            }
        }

        private void TrackIntervalStability(ushort lineID, int vehCount, float averageInterval, out SortedDictionary<int, float> intervalByVehCount)
        {
            if (!_avgIntervalByVehCount.TryGetValue(lineID, out intervalByVehCount))
            {
                _avgIntervalByVehCount[lineID] = intervalByVehCount = new SortedDictionary<int, float>();
            }
            if (!_avgIntervalCountByVehCount.TryGetValue(lineID, out var sampleCount))
            {
                _avgIntervalCountByVehCount[lineID] = sampleCount = new SortedDictionary<int, float>();
            }

            if (!intervalByVehCount.ContainsKey(vehCount))
            {
                intervalByVehCount[vehCount] = 0f;
            }
            if (!sampleCount.ContainsKey(vehCount))
            {
                sampleCount[vehCount] = 1f;
            }

            if (averageInterval != 0)
            {
                intervalByVehCount[vehCount] = (intervalByVehCount[vehCount] * sampleCount[vehCount] + averageInterval) / (sampleCount[vehCount] + 1);
                if (sampleCount[vehCount] <= 23f)
                {
                    sampleCount[vehCount]++;
                }
            }

            foreach (var key in intervalByVehCount.Keys.ToArray())
            {
                var diff = key - vehCount;
                if (diff == 0)
                {
                    continue;
                }
                var aging = 1f - 1f / (100f * diff * diff);
                intervalByVehCount[key] *= aging;
                sampleCount[key] *= aging;
            }
        }
    }
}
