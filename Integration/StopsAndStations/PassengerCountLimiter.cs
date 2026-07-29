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
        private readonly NetSegment[] segments;
        private readonly CitizenInstance[] instances;
        private readonly PathUnit[] pathUnits;
        private readonly NetNode[] nodes;
        private readonly TransportLine[] transportLines;

        /// <summary>
        /// Initializes a new instance of the <see cref="PassengerCountLimiter"/> class.
        /// </summary>
        public PassengerCountLimiter()
        {
            if (NetManager.instance == null || CitizenManager.instance == null || 
                PathManager.instance == null || TransportManager.instance == null)
            {
                Utils.LogError("PassengerCountLimiter: One or more game managers not initialized yet");
                segments = null;
                instances = null;
                pathUnits = null;
                nodes = null;
                transportLines = null;
                return;
            }

            segments = NetManager.instance.m_segments.m_buffer;
            instances = CitizenManager.instance.m_instances.m_buffer;
            pathUnits = PathManager.instance.m_pathUnits.m_buffer;
            nodes = NetManager.instance.m_nodes.m_buffer;
            transportLines = TransportManager.instance.m_lines.m_buffer;
        }

        /// <summary>
        /// Gets the Settings instance to read passenger limit configuration from.
        /// </summary>
        private ModSetting Settings => ModSetting.Instance;

        /// <summary>
        /// A method that is called by the game after this instance is created.
        /// </summary>
        /// <param name="threading">A reference to the game's <see cref="IThreading"/> implementation.</param>
        public override void OnCreated(IThreading threading)
        {
            // Instance will be populated later by IPT's OnLevelLoaded
        }

        /// <summary>
        /// A method that is called by the game before each simulation tick.
        /// Each tick contains multiple frames.
        /// Calculates the passenger count for every transport line stop.
        /// </summary>
        public override void OnBeforeSimulationTick()
        {
            if (Settings == null || segments == null || instances == null || pathUnits == null)
            {
                return;
            }

            Array.Clear(passengerCount, 0, passengerCount.Length);

            for (int i = 0; i < instances.Length; ++i)
            {
                ref var instance = ref instances[i];
                uint pathId = instance.m_path;
                if (pathId != 0 && (instance.m_flags & InstanceUsingTransport) == InstanceUsingTransport)
                {
                    var pathPosition = pathUnits[pathId].GetPosition(instance.m_pathPositionIndex >> 1);
                    ushort nodeId = segments[pathPosition.m_segment].m_startNode;
                    ++passengerCount[nodeId];
                }
            }
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
            if (settings == null || segments == null || instances == null || pathUnits == null || nodes == null || transportLines == null)
            {
                return;
            }

            uint step = SimulationManager.instance.m_currentFrameIndex & StepMask;
            uint startIndex = step * StepSize;
            uint endIndex = (step + 1) * StepSize;

            for (uint i = startIndex; i < endIndex; ++i)
            {
                ref var instance = ref instances[i];
                uint pathId = instance.m_path;
                if (pathId != 0
                    && instance.m_waitCounter == 0
                    && (instance.m_flags & InstanceUsingTransport) == InstanceUsingTransport)
                {
                    var pathPosition = pathUnits[pathId].GetPosition(instance.m_pathPositionIndex >> 1);
                    ushort nodeId = segments[pathPosition.m_segment].m_startNode;
                    if (passengerCount[nodeId] > GetMaximumAllowedPassengers(nodeId, settings))
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
