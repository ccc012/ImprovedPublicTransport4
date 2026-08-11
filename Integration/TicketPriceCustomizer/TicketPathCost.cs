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
        private static readonly object CommandLock = new object();
        private static readonly Queue<Command> Commands = new Queue<Command>();
        private static int _desiredGeneration;
        private static int _generation;
        private static bool _active;
        private static bool _workerQueued;
        private static int _queuedApplyGeneration;

        public static void BeginLevel()
        {
            lock (CommandLock)
            {
                var generation = Interlocked.Increment(ref _desiredGeneration);
                EnqueueLocked(new Command(CommandType.Begin, generation));
            }
        }

        public static void EndLevel()
        {
            lock (CommandLock)
            {
                var generation = Interlocked.Increment(ref _desiredGeneration);
                EnqueueLocked(new Command(CommandType.End, generation));
            }
        }

        public static void RequestClear()
        {
            Enqueue(CommandType.Clear, Interlocked.CompareExchange(ref _desiredGeneration, 0, 0));
        }

        public static void ApplyFromSettings()
        {
            var generation = Interlocked.CompareExchange(ref _desiredGeneration, 0, 0);
            lock (CommandLock)
            {
                if (_queuedApplyGeneration == generation)
                {
                    return;
                }

                _queuedApplyGeneration = generation;
                EnqueueLocked(new Command(CommandType.Apply, generation));
            }
        }

        private static void Enqueue(CommandType type, int generation)
        {
            Enqueue(new Command(type, generation));
        }

        private static void Enqueue(Command command)
        {
            lock (CommandLock)
            {
                EnqueueLocked(command);
            }
        }

        private static void EnqueueLocked(Command command)
        {
            Commands.Enqueue(command);
            if (!_workerQueued)
            {
                _workerQueued = true;
                Singleton<SimulationManager>.instance.AddAction(ProcessCommands);
            }
        }

        private static void ProcessCommands()
        {
            while (true)
            {
                Command command;
                lock (CommandLock)
                {
                    if (Commands.Count == 0)
                    {
                        _workerQueued = false;
                        return;
                    }

                    command = Commands.Dequeue();
                    if (command.Type == CommandType.Apply)
                    {
                        _queuedApplyGeneration = 0;
                    }
                }

                ProcessCommand(command);
            }
        }

        private static void ProcessCommand(Command command)
        {
            switch (command.Type)
            {
                case CommandType.Begin:
                    _generation = command.Generation;
                    _active = true;
                    break;
                case CommandType.End:
                    if (Interlocked.CompareExchange(ref _desiredGeneration, 0, 0) != command.Generation)
                    {
                        break;
                    }

                    _generation = command.Generation;
                    _active = false;
                    ClearAndReport(command.Generation);
                    break;
                case CommandType.Apply:
                    if (CanApply(_active, _generation, command.Generation))
                    {
                        Apply(command.Generation);
                    }
                    break;
                case CommandType.Clear:
                    if (CanClear(_active, _generation, command.Generation))
                    {
                        ClearAndReport(command.Generation);
                    }
                    break;
            }
        }

        private static void ClearAndReport(int generation)
        {
            if (ClearAllStopLaneCosts(generation))
            {
                Utils.Log("TicketPathCost: owned lane costs restored.");
            }
            else
            {
                Utils.LogError("TicketPathCost: unable to restore owned lane costs; ownership retained.");
            }
        }

        private static void Apply(int generation)
        {
            if (!IsActiveGeneration(generation))
            {
                return;
            }

            if (!ModSetting.Instance.EnableTicketPathfindingCost
                || ModSetting.Instance.TicketPriceCustomizerMode != ModSetting.TicketPriceCustomizerModes.Enabled)
            {
                ClearAllStopLaneCosts(generation);
                return;
            }

            try
            {
                if (!ClearAllStopLaneCosts(generation))
                {
                    return;
                }

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
                var candidates = new Dictionary<uint, byte>();
                var tm = Singleton<TransportManager>.instance;
                if (tm == null)
                {
                    return;
                }

                var lines = 0;
                for (ushort lineId = 1; lineId < tm.m_lines.m_size; lineId++)
                {
                    ref var line = ref tm.m_lines.m_buffer[lineId];
                    var info = line.Info;
                    if ((line.m_flags & TransportLine.Flags.Created) == 0 || info == null || line.m_stops == 0)
                    {
                        continue;
                    }

                    var cost = GetCost(info, settings, isNight);
                    if (!cost.HasValue)
                    {
                        continue;
                    }

                    lines++;
                    var firstStop = line.m_stops;
                    var stop = firstStop;
                    var guard = 0;
                    do
                    {
                        var nextStop = TransportLine.GetNextStop(stop);
                        if (nextStop == 0)
                        {
                            break;
                        }

                        AddLinePathLaneCosts(nm, stop, info.m_vehicleType, cost.Value, candidates);
                        stop = nextStop;
                    }
                    while (stop != firstStop && ++guard < 32768);
                }

                var applied = 0;
                foreach (var candidate in candidates)
                {
                    if (!IsActiveGeneration(generation))
                    {
                        return;
                    }

                    ref var lane = ref nm.m_lanes.m_buffer[candidate.Key];
                    if (!ShouldOwn(lane.m_ticketCost, candidate.Value, PreviousCosts.ContainsKey(candidate.Key)))
                    {
                        continue;
                    }

                    if (!PreviousCosts.ContainsKey(candidate.Key))
                    {
                        PreviousCosts.Add(candidate.Key, lane.m_ticketCost);
                    }

                    lane.m_ticketCost = candidate.Value;
                    AppliedCosts[candidate.Key] = candidate.Value;
                    applied++;
                }

                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log($"TicketPathCost: applied {applied} public-transport lane costs from {lines} transport lines.");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPathCost.Apply failed: {ex.Message}");
            }
        }

        private static bool IsActiveGeneration(int generation)
        {
            return CanMutate(_active, _generation,
                Interlocked.CompareExchange(ref _desiredGeneration, 0, 0), generation);
        }

        private static bool CanMutate(bool active, int generation, int desiredGeneration, int commandGeneration)
        {
            return active && generation == commandGeneration && desiredGeneration == commandGeneration;
        }

        private static bool CanApply(bool active, int generation, int commandGeneration)
        {
            return active && commandGeneration == generation;
        }

        private static bool CanClear(bool active, int generation, int commandGeneration)
        {
            return !active || commandGeneration == generation;
        }

        private static bool IsEligibleLane(NetInfo.LaneType laneType, VehicleInfo.VehicleType laneVehicleType,
            VehicleInfo.VehicleType lineVehicleType, NetInfo.Direction direction, bool forward)
        {
            var oriented = forward
                ? (direction & NetInfo.Direction.Forward) != 0
                : (direction & NetInfo.Direction.Backward) != 0;
            return oriented
                && (laneType & NetInfo.LaneType.PublicTransport) != 0
                && (lineVehicleType != VehicleInfo.VehicleType.Car
                    || (laneType & NetInfo.LaneType.Vehicle) == 0)
                && (laneVehicleType & lineVehicleType) != 0;
        }

        private static bool ShouldOwn(byte laneCost, byte candidateCost, bool alreadyOwned)
        {
            return alreadyOwned || laneCost != candidateCost;
        }

        private static void AddLinePathLaneCosts(NetManager nm, ushort stop,
            VehicleInfo.VehicleType vehicleType, byte cost, Dictionary<uint, byte> candidates)
        {
            var lineSegment = TransportLine.GetNextSegment(stop);
            if (lineSegment == 0 || lineSegment >= nm.m_segments.m_size
                || lineSegment >= nm.m_segments.m_buffer.Length)
            {
                return;
            }

            ref var lineSegmentData = ref nm.m_segments.m_buffer[lineSegment];
            if ((lineSegmentData.m_flags & NetSegment.Flags.Created) == 0
                || (lineSegmentData.m_flags & NetSegment.Flags.Deleted) != 0)
            {
                return;
            }

            var pathManager = Singleton<PathManager>.instance;
            if (pathManager == null)
            {
                return;
            }

            WalkPathPositions(pathManager.m_pathUnits.m_buffer, pathManager.m_pathUnits.m_size,
                lineSegmentData.m_path, delegate(ushort segmentId, byte laneIndex)
                {
                    AddPhysicalLaneCost(nm, segmentId, laneIndex, vehicleType, cost, candidates);
                });
        }

        private static void WalkPathPositions(PathUnit[] pathUnits, uint pathUnitSize, uint path,
            Action<ushort, byte> visitor)
        {
            if (pathUnits == null || visitor == null)
            {
                return;
            }

            var visited = new HashSet<uint>();
            var unitLimit = Math.Min((uint)pathUnits.Length, pathUnitSize);
            while (path != 0 && path < unitLimit && visited.Add(path) && visited.Count <= 65536)
            {
                ref var unit = ref pathUnits[path];
                var positionCount = Math.Min((int)unit.m_positionCount, PathUnit.MAX_POSITIONS);
                for (var positionIndex = 0; positionIndex < positionCount; positionIndex++)
                {
                    var position = GetPathPosition(ref unit, positionIndex);
                    visitor(position.m_segment, position.m_lane);
                }

                path = unit.m_nextPathUnit;
            }
        }

        private static PathUnit.Position GetPathPosition(ref PathUnit unit, int index)
        {
            switch (index)
            {
                case 0: return unit.m_position00;
                case 1: return unit.m_position01;
                case 2: return unit.m_position02;
                case 3: return unit.m_position03;
                case 4: return unit.m_position04;
                case 5: return unit.m_position05;
                case 6: return unit.m_position06;
                case 7: return unit.m_position07;
                case 8: return unit.m_position08;
                case 9: return unit.m_position09;
                case 10: return unit.m_position10;
                default: return unit.m_position11;
            }
        }

        private static void AddPhysicalLaneCost(NetManager nm, ushort segmentId, byte laneIndex,
            VehicleInfo.VehicleType vehicleType, byte cost, Dictionary<uint, byte> candidates)
        {
            if (segmentId == 0 || segmentId >= nm.m_segments.m_size
                || segmentId >= nm.m_segments.m_buffer.Length)
            {
                return;
            }

            ref var segment = ref nm.m_segments.m_buffer[segmentId];
            if ((segment.m_flags & NetSegment.Flags.Created) == 0
                || (segment.m_flags & NetSegment.Flags.Deleted) != 0)
            {
                return;
            }

            var segmentInfo = segment.Info;
            if (segmentInfo == null || segmentInfo.m_lanes == null || laneIndex >= segmentInfo.m_lanes.Length)
            {
                return;
            }

            var laneId = segment.m_lanes;
            var visited = new HashSet<uint>();
            for (var index = 0; index < laneIndex && laneId != 0; index++)
            {
                if (laneId >= nm.m_lanes.m_size || laneId >= nm.m_lanes.m_buffer.Length || !visited.Add(laneId))
                {
                    return;
                }

                laneId = nm.m_lanes.m_buffer[laneId].m_nextLane;
            }

            if (laneId == 0 || laneId >= nm.m_lanes.m_size || laneId >= nm.m_lanes.m_buffer.Length
                || !visited.Add(laneId))
            {
                return;
            }

            var laneInfo = segmentInfo.m_lanes[laneIndex];
            AddPathPositionLaneCost(laneId, laneInfo.m_laneType, laneInfo.m_vehicleType,
                vehicleType, cost, candidates);
        }

        private static void AddPathPositionLaneCost(uint laneId, NetInfo.LaneType laneType,
            VehicleInfo.VehicleType laneVehicleType, VehicleInfo.VehicleType lineVehicleType,
            byte cost, Dictionary<uint, byte> candidates)
        {
            if ((laneType & NetInfo.LaneType.PublicTransport) == 0
                || (lineVehicleType == VehicleInfo.VehicleType.Car && (laneType & NetInfo.LaneType.Vehicle) != 0)
                || (laneVehicleType & lineVehicleType) == 0)
            {
                return;
            }

            byte current;
            candidates[laneId] = candidates.TryGetValue(laneId, out current)
                ? Math.Min(current, cost)
                : cost;
        }

        private static byte? GetCost(TransportInfo info, ModSetting.TicketPriceCustomizerSettings settings, bool isNight)
        {
            return GetCost(info.GetSubService(), info.GetClassLevel(), info.m_vehicleType, settings, isNight);
        }

        private static byte? GetCost(ItemClass.SubService subService, ItemClass.Level level,
            VehicleInfo.VehicleType vehicleType, ModSetting.TicketPriceCustomizerSettings settings, bool isNight)
        {
            float multiplier;
            switch (subService)
            {
                case ItemClass.SubService.PublicTransportBus:
                    multiplier = level == ItemClass.Level.Level2
                        ? (isNight ? settings.IntercityBusNightMultiplier : settings.IntercityBusMultiplier)
                        : (isNight ? settings.BusNightMultiplier : settings.BusMultiplier);
                    break;
                case ItemClass.SubService.PublicTransportMetro:
                    multiplier = isNight ? settings.MetroNightMultiplier : settings.MetroMultiplier;
                    break;
                case ItemClass.SubService.PublicTransportTrain:
                    multiplier = isNight ? settings.TrainNightMultiplier : settings.TrainMultiplier;
                    break;
                case ItemClass.SubService.PublicTransportTram:
                    multiplier = isNight ? settings.TramNightMultiplier : settings.TramMultiplier;
                    break;
                case ItemClass.SubService.PublicTransportMonorail:
                    multiplier = isNight ? settings.MonorailNightMultiplier : settings.MonorailMultiplier;
                    break;
                case ItemClass.SubService.PublicTransportShip:
                    multiplier = level == ItemClass.Level.Level1
                        ? (isNight ? settings.ShipNightMultiplier : settings.ShipMultiplier)
                        : (isNight ? settings.FerryNightMultiplier : settings.FerryMultiplier);
                    break;
                case ItemClass.SubService.PublicTransportPlane:
                    if (vehicleType == VehicleInfo.VehicleType.Blimp)
                    {
                        multiplier = isNight ? settings.BlimpNightMultiplier : settings.BlimpMultiplier;
                    }
                    else if (vehicleType == VehicleInfo.VehicleType.Helicopter)
                    {
                        multiplier = isNight ? settings.HelicopterNightMultiplier : settings.HelicopterMultiplier;
                    }
                    else
                    {
                        multiplier = isNight ? settings.PlaneNightMultiplier : settings.PlaneMultiplier;
                    }
                    break;
                case ItemClass.SubService.PublicTransportCableCar:
                    multiplier = isNight ? settings.CableCarNightMultiplier : settings.CableCarMultiplier;
                    break;
                case ItemClass.SubService.PublicTransportTrolleybus:
                    multiplier = isNight ? settings.TrolleybusNightMultiplier : settings.TrolleybusMultiplier;
                    break;
                case ItemClass.SubService.PublicTransportTours:
                    if (vehicleType != VehicleInfo.VehicleType.Car)
                    {
                        return null;
                    }

                    multiplier = isNight ? settings.SightseeingBusNightMultiplier : settings.SightseeingBusMultiplier;
                    break;
                default:
                    return null;
            }

            return (byte)Mathf.Clamp(Mathf.RoundToInt(20f * multiplier), 0, MaxCost);
        }

        private enum CommandType
        {
            Begin,
            End,
            Apply,
            Clear
        }

        private struct Command
        {
            internal readonly CommandType Type;
            internal readonly int Generation;

            internal Command(CommandType type, int generation)
            {
                Type = type;
                Generation = generation;
            }
        }

        private static bool ClearAllStopLaneCosts(int generation)
        {
            try
            {
                if (Interlocked.CompareExchange(ref _desiredGeneration, 0, 0) != generation)
                {
                    return false;
                }

                var nm = Singleton<NetManager>.instance;
                if (nm == null)
                {
                    return false;
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
                return true;
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPathCost.Clear failed: {ex.Message}");
                return false;
            }
        }
    }
}
