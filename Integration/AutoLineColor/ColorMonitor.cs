using System;
using System.Threading;
using AutoLineColor.Coloring;
using AutoLineColor.Naming;
using ColossalFramework;
using ICities;
using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;
using ImprovedPublicTransport;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace AutoLineColor
{
    [UsedImplicitly]
    public class ColorMonitor : ThreadingExtensionBase
    {
        private static readonly Console Logger = Console.Instance;
        private static DateTimeOffset _nextUpdateTime = DateTimeOffset.Now;

        private bool _initialized;
        private IColorStrategy _colorStrategy;
        private INamingStrategy _namingStrategy;
        private IUsedColors _usedColors;
        private int _lastColorStrategy = -1;
        private int _lastNamingStrategy = -1;

        [CanBeNull]
        public static ColorMonitor Instance { get; private set; }

        public override void OnCreated(IThreading threading)
        {
            Logger.Message("===============================");
            Logger.Message("Initializing auto color monitor");
            Logger.Message("Initializing colors");
            GenericNames.Initialize();

            Logger.Message("Loading current config from IPT3 Settings");
            var settings = ModSetting.Instance;
            _colorStrategy = SetColorStrategy((int)settings.AutoLineColorColorStrategy);
            _namingStrategy = SetNamingStrategy((int)settings.AutoLineColorNamingStrategyMode);
            _usedColors = new NullUsedColors();

            Logger.Message("Found color strategy of " + settings.AutoLineColorColorStrategy);
            Logger.Message("Found naming strategy of " + settings.AutoLineColorNamingStrategyMode);

            _lastColorStrategy = (int)settings.AutoLineColorColorStrategy;
            _lastNamingStrategy = (int)settings.AutoLineColorNamingStrategyMode;

            _initialized = true;
            Instance = this;

            Logger.Message("done creating");
            base.OnCreated(threading);
        }

        public override void OnReleased()
        {
            _initialized = false;
            Instance = null;

            base.OnReleased();
        }

        private static INamingStrategy SetNamingStrategy(int namingStrategyValue)
        {
            var namingStrategy = (ModSetting.AutoLineColorNamingStrategy)namingStrategyValue;
            Logger.Message($"Naming Strategy: {namingStrategy}");
            switch (namingStrategy)
            {
                case ModSetting.AutoLineColorNamingStrategy.Disabled:
                    return new NoNamingStrategy();
                case ModSetting.AutoLineColorNamingStrategy.Districts:
                    return new DistrictNamingStrategy();
                case ModSetting.AutoLineColorNamingStrategy.London:
                    return new LondonNamingStrategy();
                case ModSetting.AutoLineColorNamingStrategy.Roads:
                    return new RoadNamingStrategy();
                case ModSetting.AutoLineColorNamingStrategy.NamedColors:
                    return new NamedColorStrategy();
                default:
                    Logger.Error("unknown naming strategy");
                    return new NoNamingStrategy();
            }
        }

        private static IColorStrategy SetColorStrategy(int colorStrategyValue)
        {
            var colorStrategy = (ModSetting.AutoLineColorStrategy)colorStrategyValue;
            Logger.Message($"Color Strategy: {colorStrategy}");
            switch (colorStrategy)
            {
                case ModSetting.AutoLineColorStrategy.Disabled:
                    return null;
                case ModSetting.AutoLineColorStrategy.RandomHue:
                    return new RandomHueStrategy();
                case ModSetting.AutoLineColorStrategy.RandomColor:
                    return new RandomColorStrategy();
                case ModSetting.AutoLineColorStrategy.CategorisedColor:
                    return new CategorisedColorStrategy();
                case ModSetting.AutoLineColorStrategy.NamedColors:
                    return new NamedColorStrategy();
                default:
                    Logger.Error("unknown color strategy");
                    return null;
            }
        }

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            // No city loaded (main menu): nothing to monitor, skip entirely.
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }

            TransportManager theTransportManager;
            SimulationManager theSimulationManager;
            TransportLine[] lines;

            try
            {
                var settings = ModSetting.Instance;
                if (_lastColorStrategy != (int)settings.AutoLineColorColorStrategy ||
                    _lastNamingStrategy != (int)settings.AutoLineColorNamingStrategyMode)
                {
                    Logger.Message("Applying settings changes");
                    _colorStrategy = SetColorStrategy((int)settings.AutoLineColorColorStrategy);
                    _namingStrategy = SetNamingStrategy((int)settings.AutoLineColorNamingStrategyMode);
                    _lastColorStrategy = (int)settings.AutoLineColorColorStrategy;
                    _lastNamingStrategy = (int)settings.AutoLineColorNamingStrategyMode;
                }

                if (_initialized == false)
                    return;

                // Both strategies off — nothing to do this frame (skip manager lookups / locks).
                if (_colorStrategy == null && _namingStrategy is NoNamingStrategy)
                    return;

                if (_nextUpdateTime >= DateTimeOffset.Now)
                    return;

                if (!Singleton<TransportManager>.exists || !Singleton<SimulationManager>.exists)
                    return;

                theTransportManager = Singleton<TransportManager>.instance;
                theSimulationManager = Singleton<SimulationManager>.instance;
                lines = theTransportManager.m_lines.m_buffer;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return;
            }

            if (theSimulationManager.SimulationPaused)
                return;

            var locked = false;

            try
            {
                _nextUpdateTime = DateTimeOffset.Now.AddSeconds(Constants.UpdateIntervalSeconds);

                locked = Monitor.TryEnter(lines, SimulationManager.SYNCHRONIZE_TIMEOUT);

                if (!locked)
                    return;

                // Cheap pre-pass: only build UsedColors + process when at least one line needs work.
                // Avoids allocating a full colour histogram every 10s on a finished transit map.
                var anyWork = false;
                int len = lines.Length;
                for (int i = 0; i < len; i++)
                {
                    if (LineNeedsProcess(lines[i], theTransportManager, (ushort)i))
                    {
                        anyWork = true;
                        break;
                    }
                }

                if (!anyWork)
                    return;

                _usedColors = UsedColors.FromLines(lines);

                for (int i = 0; i < len; i++)
                {
                    if (i < lines.Length)
                        ProcessLine((ushort)i, lines[i], false, theSimulationManager, theTransportManager);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
            finally
            {
                if (locked)
                {
                    Monitor.Exit(lines);
                }
            }
        }

        private bool LineNeedsProcess(in TransportLine transportLine, TransportManager tm, ushort lineId)
        {
            if (transportLine.m_flags == TransportLine.Flags.None)
                return false;
            if (!transportLine.IsComplete() || !transportLine.IsActive())
                return false;
            if (_colorStrategy == null && _namingStrategy is NoNamingStrategy)
                return false;

            if (_colorStrategy != null && !transportLine.HasCustomColor())
                return true;

            if (!(_namingStrategy is NoNamingStrategy)
                && !transportLine.HasCustomName()
                && string.IsNullOrEmpty(tm.GetLineName(lineId)))
                return true;

            return false;
        }

        public void ForceRefreshLine(ushort lineId)
        {
            ForceRefreshLineNow(lineId);
        }

        /// <summary>
        /// Manual "reatribui cor/nome" button.
        /// Always reassigns colour when a colour strategy is enabled.
        /// Only assigns a name when the line currently has no name (empty / whitespace).
        /// </summary>
        public static void ForceRefreshLineNow(ushort lineId)
        {
            if (lineId == 0)
            {
                return;
            }

            if (!Singleton<TransportManager>.exists || !Singleton<SimulationManager>.exists)
            {
                return;
            }

            try
            {
                var settings = ModSetting.Instance;
                // Reuse live strategies when settings unchanged — avoids realloc on every click.
                IColorStrategy colorStrategy;
                INamingStrategy namingStrategy;
                if (Instance != null
                    && Instance._lastColorStrategy == (int)settings.AutoLineColorColorStrategy
                    && Instance._lastNamingStrategy == (int)settings.AutoLineColorNamingStrategyMode)
                {
                    colorStrategy = Instance._colorStrategy;
                    namingStrategy = Instance._namingStrategy;
                }
                else
                {
                    colorStrategy = SetColorStrategy((int)settings.AutoLineColorColorStrategy);
                    namingStrategy = SetNamingStrategy((int)settings.AutoLineColorNamingStrategyMode);
                    if (Instance != null)
                    {
                        Instance._colorStrategy = colorStrategy;
                        Instance._namingStrategy = namingStrategy;
                        Instance._lastColorStrategy = (int)settings.AutoLineColorColorStrategy;
                        Instance._lastNamingStrategy = (int)settings.AutoLineColorNamingStrategyMode;
                    }
                }

                if (colorStrategy == null && namingStrategy is NoNamingStrategy)
                {
                    return;
                }

                var theTransportManager = Singleton<TransportManager>.instance;
                var theSimulationManager = Singleton<SimulationManager>.instance;
                var colorStrat = colorStrategy;
                var nameStrat = namingStrategy;

                theSimulationManager.AddAction(() =>
                {
                    try
                    {
                        ApplyManualRefresh(lineId, colorStrat, nameStrat, theTransportManager);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.ToString());
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }

        private static void ApplyManualRefresh(
            ushort lineId,
            IColorStrategy colorStrategy,
            INamingStrategy namingStrategy,
            TransportManager tm)
        {
            ref var line = ref tm.m_lines.m_buffer[lineId];
            if ((line.m_flags & TransportLine.Flags.Created) == 0)
            {
                return;
            }

            // Complete is preferred but some edge lines still need a recolor — only require Created.
            var currentName = tm.GetLineName(lineId) ?? string.Empty;
            var hasExistingName = !string.IsNullOrEmpty(currentName.Trim());

            // Used colours excluding this line so we can pick a different colour for a re-roll.
            var usedColors = UsedColors.FromLinesExcluding(tm.m_lines.m_buffer, lineId);

            if (colorStrategy != null)
            {
                Color32 newColor;
                if (colorStrategy is ICombinedStrategy combined
                    && namingStrategy != null
                    && !(namingStrategy is NoNamingStrategy)
                    && colorStrategy.GetType() == namingStrategy.GetType()
                    && !hasExistingName)
                {
                    // Combined path only when we will also name.
                    combined.GetColorAndName(line, usedColors, out newColor, out var combinedName);
                    tm.SetLineColor(lineId, newColor);
                    if (!string.IsNullOrEmpty(combinedName))
                    {
                        tm.SetLineName(lineId, combinedName);
                    }

                    return;
                }

                newColor = colorStrategy.GetColor(line, usedColors);
                tm.SetLineColor(lineId, newColor);
                // Ensure CustomColor flag sticks even if SetLineColor path is quirky.
                line.m_flags |= TransportLine.Flags.CustomColor;
                line.m_color = newColor;
            }

            // Only name when the line has no name yet.
            if (!hasExistingName
                && namingStrategy != null
                && !(namingStrategy is NoNamingStrategy))
            {
                var newName = namingStrategy.GetName(line, lineId);
                if (!string.IsNullOrEmpty(newName))
                {
                    tm.SetLineName(lineId, newName);
                }
            }
        }

        private void ProcessLine(ushort lineId, in TransportLine transportLine, bool forceUpdate, SimulationManager theSimulationManager,
            TransportManager theTransportManager)
        {
            if (transportLine.m_flags == TransportLine.Flags.None)
                return;

            if (!transportLine.IsComplete())
                return;

            if (!transportLine.IsActive())
                return;

            if (_colorStrategy == null && _namingStrategy is NoNamingStrategy)
                return;

            var updateColor = (forceUpdate || !transportLine.HasCustomColor()) && _colorStrategy != null;
            // Automatic path: only name brand-new lines without a name.
            var updateName = (forceUpdate || !transportLine.HasCustomName())
                             && !(_namingStrategy is NoNamingStrategy)
                             && (forceUpdate || string.IsNullOrEmpty(theTransportManager.GetLineName(lineId)));

            if (!updateColor && !updateName)
                return;

            Color32 newColor;
            string newName;

            if (_colorStrategy is ICombinedStrategy combinedStrategy && _colorStrategy.GetType() == _namingStrategy.GetType())
            {
                combinedStrategy.GetColorAndName(transportLine, _usedColors, out newColor, out newName);

                theSimulationManager.AddAction(theTransportManager.SetLineColor(lineId, newColor));

                if (updateName && !string.IsNullOrEmpty(newName))
                    theSimulationManager.AddAction(theTransportManager.SetLineName(lineId, newName));

                return;
            }

            if (updateColor && _colorStrategy != null)
            {
                newColor = _colorStrategy.GetColor(transportLine, _usedColors);
                theSimulationManager.AddAction(theTransportManager.SetLineColor(lineId, newColor));
            }

            if (!updateName || _namingStrategy is NoNamingStrategy)
                return;

            newName = _namingStrategy.GetName(transportLine, lineId);

            if (string.IsNullOrEmpty(newName))
                return;

            theSimulationManager.AddAction(theTransportManager.SetLineName(lineId, newName));
        }
    }

    internal static class LineExtensions
    {
        public static bool IsActive(in this TransportLine transportLine)
        {
            if ((transportLine.m_flags & (TransportLine.Flags.Created | TransportLine.Flags.Hidden)) == 0)
                return false;

            return (transportLine.m_flags & TransportLine.Flags.Temporary) == 0;
        }

        public static bool IsComplete(in this TransportLine transportLine)
        {
            return (transportLine.m_flags & TransportLine.Flags.Complete) != 0;
        }

        public static bool HasCustomColor(in this TransportLine transportLine)
        {
            return (transportLine.m_flags & TransportLine.Flags.CustomColor) != 0;
        }

        public static bool HasCustomName(in this TransportLine transportLine)
        {
            return (transportLine.m_flags & TransportLine.Flags.CustomName) != 0;
        }
    }

    internal static class EnumerableExtensions
    {
        public static IEnumerable<TResult> Scan<TSource, TAccumulate, TResult>(
            this IEnumerable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> updater,
            Func<TSource, TAccumulate, TResult> resultSelector)
        {
            var accumulator = seed;
            foreach (var item in source)
            {
                accumulator = updater(accumulator, item);
                yield return resultSelector(item, accumulator);
            }
        }
    }
}
