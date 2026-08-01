// <copyright file="PassengerCountLimiter.cs" company="dymanoid">
using ImprovedPublicTransport;
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace StopsAndStations
{
    using System;
    using ICities;
    using ImprovedPublicTransport.Util;

    /// <summary>
    /// A service that observes the stops in the city and calculates their current passenger count.
    /// It reads maximum waiting passenger limits from the centralized IPT3 settings
    /// and limits the number of citizens waiting for transport at stops by setting the passenger status to 'cannot use transport'.
    /// </summary>
    public sealed class PassengerCountLimiter : ThreadingExtensionBase
    {
        private const int StepMask = 0xF;
        private const int StepSize = CitizenManager.MAX_INSTANCE_COUNT / (StepMask + 1);
        private const CitizenInstance.Flags InstanceUsingTransport = CitizenInstance.Flags.OnPath | CitizenInstance.Flags.WaitingTransport;

        private readonly ushort[] passengerCount = new ushort[NetManager.MAX_NODE_COUNT];
        private NetSegment[] segments;
        private CitizenInstance[] instances;
        private PathUnit[] pathUnits;
        private NetNode[] nodes;
        private TransportLine[] transportLines;
        // Counts are rebuilt over 16 frames (same StepMask as the enforce pass) instead of a full
        // 65k citizen scan every simulation tick — that full scan was a major FPS sink in big cities.
        private int _countRebuildStep = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="PassengerCountLimiter"/> class.
        /// </summary>
        public PassengerCountLimiter()
        {
            // Managers may not exist at ThreadingExtension construction — resolve lazily in-game.
        }

        /// <summary>
        /// Gets the Settings instance to read passenger limit configuration from.
        /// </summary>
        private ModSetting Settings => ModSetting.Instance;

        private bool EnsureBuffers()
        {
            if (instances != null && segments != null && pathUnits != null && nodes != null && transportLines != null)
            {
                return true;
            }

            try
            {
                if (NetManager.instance == null || CitizenManager.instance == null
                    || PathManager.instance == null || TransportManager.instance == null)
                {
                    return false;
                }

                segments = NetManager.instance.m_segments.m_buffer;
                instances = CitizenManager.instance.m_instances.m_buffer;
                pathUnits = PathManager.instance.m_pathUnits.m_buffer;
                nodes = NetManager.instance.m_nodes.m_buffer;
                transportLines = TransportManager.instance.m_lines.m_buffer;
                return instances != null && segments != null && pathUnits != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// A method that is called by the game after this instance is created.
        /// </summary>
        /// <param name="threading">A reference to the game's <see cref="IThreading"/> implementation.</param>
        public override void OnCreated(IThreading threading)
        {
            // Buffers resolved lazily once the level is loaded.
        }

        /// <summary>
        /// A method that is called by the game before each simulation tick.
        /// Each tick contains multiple frames.
        /// Starts a spread count rebuild (completed across the next 16 frames).
        /// </summary>
        public override void OnBeforeSimulationTick()
        {
            if (!EnsureBuffers() || Settings == null)
            {
                return;
            }

            Array.Clear(passengerCount, 0, passengerCount.Length);
            _countRebuildStep = 0;
        }

        /// <summary>
        /// A method that is called by the game before each simulation frame.
        /// </summary>
        public override void OnBeforeSimulationFrame()
        {
            // Resolved once per frame instead of per citizen instance: ModSetting.Instance walks two
            // dictionaries (manager lookup + setting lookup) on every access, and this loop runs
            // thousands of iterations per frame.
            var settings = Settings;
            if (settings == null || !EnsureBuffers())
            {
                return;
            }

            // Spread the count rebuild across the same 16 frame buckets as enforcement.
            if (_countRebuildStep >= 0 && _countRebuildStep <= StepMask)
            {
                uint startIndex = (uint)_countRebuildStep * StepSize;
                uint endIndex = (uint)(_countRebuildStep + 1) * StepSize;
                for (uint i = startIndex; i < endIndex; ++i)
                {
                    ref var instance = ref instances[i];
                    uint pathId = instance.m_path;
                    if (pathId != 0 && (instance.m_flags & InstanceUsingTransport) == InstanceUsingTransport)
                    {
                        var pathPosition = pathUnits[pathId].GetPosition(instance.m_pathPositionIndex >> 1);
                        ushort nodeId = segments[pathPosition.m_segment].m_startNode;
                        if (nodeId < passengerCount.Length)
                        {
                            ++passengerCount[nodeId];
                        }
                    }
                }

                _countRebuildStep++;
                if (_countRebuildStep > StepMask)
                {
                    _countRebuildStep = -1;
                }
            }

            uint step = SimulationManager.instance.m_currentFrameIndex & StepMask;
            uint enforceStart = step * StepSize;
            uint enforceEnd = (step + 1) * StepSize;

            for (uint i = enforceStart; i < enforceEnd; ++i)
            {
                ref var instance = ref instances[i];
                uint pathId = instance.m_path;
                if (pathId != 0
                    && instance.m_waitCounter == 0
                    && (instance.m_flags & InstanceUsingTransport) == InstanceUsingTransport)
                {
                    var pathPosition = pathUnits[pathId].GetPosition(instance.m_pathPositionIndex >> 1);
                    ushort nodeId = segments[pathPosition.m_segment].m_startNode;
                    if (nodeId < passengerCount.Length
                        && passengerCount[nodeId] > GetMaximumAllowedPassengers(nodeId, settings))
                    {
                        --passengerCount[nodeId];
                        instance.m_flags |= CitizenInstance.Flags.BoredOfWaiting;
                        instance.m_waitCounter = byte.MaxValue;
                    }
                }
            }
        }

        private int GetMaximumAllowedPassengers(ushort nodeId, ModSetting settings)
        {
            if (nodes == null || transportLines == null)
            {
                return int.MaxValue;
            }

            ushort transportLineId = nodes[nodeId].m_transportLine;
            if (transportLineId == 0)
            {
                return int.MaxValue;
            }

            TransportLine line = transportLines[transportLineId];
            switch (line.Info?.m_transportType)
            {
                case TransportInfo.TransportType.EvacuationBus:
                    return settings.MaxWaitingPassengersEvacuationBus;

                case TransportInfo.TransportType.Bus:
                    return settings.MaxWaitingPassengersBus;

                case TransportInfo.TransportType.Trolleybus:
                    return settings.MaxWaitingPassengersTrolleybus;

                case TransportInfo.TransportType.TouristBus:
                    return settings.MaxWaitingPassengersTouristBus;

                case TransportInfo.TransportType.Tram:
                    return settings.MaxWaitingPassengersTram;

                case TransportInfo.TransportType.Metro:
                    return settings.MaxWaitingPassengersMetro;

                case TransportInfo.TransportType.Train:
                    return settings.MaxWaitingPassengersTrain;

                case TransportInfo.TransportType.Monorail:
                    return settings.MaxWaitingPassengersMonorail;

                case TransportInfo.TransportType.Airplane:
                    // Both blimps and commercial airplanes use TransportType.Airplane — they cannot be distinguished
                    // at the TransportType level. MaxWaitingPassengersAirplane applies to both.
                    return settings.MaxWaitingPassengersAirplane;

                case TransportInfo.TransportType.Ship:
                    // Both ferries and cargo ships use TransportType.Ship — they cannot be distinguished
                    // at the TransportType level. MaxWaitingPassengersShip applies to both.
                    return settings.MaxWaitingPassengersShip;

                case TransportInfo.TransportType.CableCar:
                    return settings.MaxWaitingPassengersCableCar;

                case TransportInfo.TransportType.HotAirBalloon:
                    return settings.MaxWaitingPassengersHotAirBalloon;

                case TransportInfo.TransportType.Helicopter:
                    return settings.MaxWaitingPassengersHelicopter;

                default:
                    return int.MaxValue;
            }
        }
    }
}
