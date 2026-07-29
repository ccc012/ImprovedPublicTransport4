using System;
using System.Threading;
using AutoLineColor.Coloring;
using AutoLineColor.Naming;
using ColossalFramework;
using ICities;
using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;
using ImprovedPublicTransport.Integration.TicketPriceCustomizer;
using ImprovedPublicTransport;

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
        private static bool _ticketPricesTabFailed;

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

        // TODO: make this whole thing a coroutine?
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            TransportManager theTransportManager;
            SimulationManager theSimulationManager;
            TransportLine[] lines;

            // Refresh ticket prices tab passenger counts (throttled internally).
            // This runs every frame, so an empty catch here would hide a recurring failure forever
            // while still paying the exception cost 60x/second. Log it once, then stop calling it.
            if (!_ticketPricesTabFailed)
            {
                try
                {
                    TicketPricesTab.OnUpdate(realTimeDelta);
                }
                catch (Exception ex)
                {
                    _ticketPricesTabFailed = true;
                    Logger.Error($"TicketPricesTab.OnUpdate failed - disabling its per-frame refresh: {ex}");
                }
            }

            try
            {
                //Check for settings changes
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

                // try and limit how often we are scanning for lines. this ain't that important
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

                _usedColors = UsedColors.FromLines(lines);

                for (ushort i = 0; i < lines.Length - 1; i++)
                {
                    ProcessLine(i, lines[i], false, theSimulationManager, theTransportManager);
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

        public void ForceRefreshLine(ushort lineId)
        {
            ForceRefreshLineNow(lineId);
        }

        public static void ForceRefreshLineNow(ushort lineId)
        {
            Logger.Message($"Force refresh for line {lineId}");

            if (lineId == 0)
            {
                Logger.Error("Skipping force refresh because lineId is 0");
                return;
            }

            if (!Singleton<TransportManager>.exists || !Singleton<SimulationManager>.exists)
            {
                Logger.Error($"Skipping force refresh for line {lineId} because managers are missing");
                return;
            }

            try
            {
                var theTransportManager = Singleton<TransportManager>.instance;
                var theSimulationManager = Singleton<SimulationManager>.instance;
                var lines = theTransportManager.m_lines.m_buffer;
                var colorStrategy = Instance?._colorStrategy ?? SetColorStrategy((int)ModSetting.Instance.AutoLineColorColorStrategy);
                var namingStrategy = Instance?._namingStrategy ?? SetNamingStrategy((int)ModSetting.Instance.AutoLineColorNamingStrategyMode);
                var usedColors = UsedColors.FromLines(lines);

                if (colorStrategy == null && namingStrategy is NoNamingStrategy)
                {
                    Logger.Error($"Skipping force refresh for line {lineId} because both color and naming strategies are disabled");
                    return;
                }

                theSimulationManager.AddAction(() =>
                {
                    try
                    {
                        ref var line = ref lines[lineId];
                        if (line.m_flags == TransportLine.Flags.None || !line.IsComplete() || !line.IsActive())
                        {
                            return;
                        }

                        var updateColor = colorStrategy != null;
                        var updateName = !(namingStrategy is NoNamingStrategy);
                        if (!updateColor && !updateName)
                        {
                            return;
                        }

                        if (updateColor && updateName && colorStrategy is ICombinedStrategy combinedStrategy && colorStrategy.GetType() == namingStrategy.GetType())
                        {
                            combinedStrategy.GetColorAndName(line, usedColors, out var combinedColor, out var combinedName);
                            theTransportManager.SetLineColor(lineId, combinedColor);
                            if (!string.IsNullOrEmpty(combinedName))
                            {
                                theTransportManager.SetLineName(lineId, combinedName);
                            }

                            return;
                        }

                        if (updateColor)
                        {
                            var newColor = colorStrategy.GetColor(line, usedColors);
                            theTransportManager.SetLineColor(lineId, newColor);
                        }

                        if (updateName)
                        {
                            var newName = namingStrategy.GetName(line, lineId);
                            if (!string.IsNullOrEmpty(newName))
                            {
                                theTransportManager.SetLineName(lineId, newName);
                            }
                        }
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
        private void ProcessLine(ushort lineId, in TransportLine transportLine, bool forceUpdate, SimulationManager theSimulationManager,
            TransportManager theTransportManager)
        {
            //logger.Message(string.Format("Starting on line {0}", num));

            if (transportLine.m_flags == TransportLine.Flags.None)
                return;

            if (!transportLine.IsComplete())
                return;

            // only worry about fully created lines
            if (!transportLine.IsActive())
                return;

            // If both color and naming are disabled, skip processing
            if (_colorStrategy == null && _namingStrategy is NoNamingStrategy)
                return;

            // only worry about newly created lines, unless forcing the update
            var updateColor = (forceUpdate || !transportLine.HasCustomColor()) && _colorStrategy != null;
            var updateName = (forceUpdate || !transportLine.HasCustomName()) && !(_namingStrategy is NoNamingStrategy);

            if (!updateColor && !updateName)
                return;

            Logger.Message($"Working on line {lineId} (m_flags={transportLine.m_flags} m_color={transportLine.m_color})");

            var currentColor = (Color32)theTransportManager.GetLineColor(lineId);
            var currentName = theTransportManager.GetLineName(lineId);
            Color32 newColor;
            string newName;

            if (_colorStrategy is ICombinedStrategy combinedStrategy && _colorStrategy.GetType() == _namingStrategy.GetType())  //XXX ugly
            {
                combinedStrategy.GetColorAndName(transportLine, _usedColors, out newColor, out newName);

                theSimulationManager.AddAction(theTransportManager.SetLineColor(lineId, newColor));

                if (!string.IsNullOrEmpty(newName))
                    theSimulationManager.AddAction(theTransportManager.SetLineName(lineId, newName));

                return;
            }

            if (updateColor && _colorStrategy != null)
            {
                newColor = _colorStrategy.GetColor(transportLine, _usedColors);

                Logger.Message($"Changing line {lineId} color from {currentColor} to {newColor}");

                theSimulationManager.AddAction(theTransportManager.SetLineColor(lineId, newColor));
            }

            if (!updateName || _namingStrategy is NoNamingStrategy)
                return;

            newName = _namingStrategy.GetName(transportLine, lineId);

            if (string.IsNullOrEmpty(newName))
                return;

            Logger.Message($"Changing line {lineId} name from '{currentName}' to '{newName}'");

            theSimulationManager.AddAction(theTransportManager.SetLineName(lineId, newName));
        }
    }

    internal static class LineExtensions
    {
        //// TODO: check default colors for other line types? is there a better way to do this?
        //private static readonly Color32 BlackColor = new Color32(0, 0, 0, 0);
        //private static readonly Color32 DefaultBusColor = new Color32(44, 142, 191, 255);
        //private static readonly Color32 DefaultMetroColor = new Color32(0, 184, 0, 255);
        //private static readonly Color32 DefaultTrainColor = new Color32(219, 86, 0, 255);

        //public static bool IsDefaultColor(this Color32 color)
        //{
        //    return color.IsColorEqual(BlackColor) ||
        //           color.IsColorEqual(DefaultBusColor) ||
        //           color.IsColorEqual(DefaultMetroColor) ||
        //           color.IsColorEqual(DefaultTrainColor);
        //}

        public static bool IsActive(in this TransportLine transportLine)
        {
            if ((transportLine.m_flags & (TransportLine.Flags.Created | TransportLine.Flags.Hidden)) == 0)
                return false;

            // stations are marked with this flag
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

        public static TItem MinBy<TItem, TKey>([NotNull] this IEnumerable<TItem> source, [NotNull] Func<TItem, TKey> selector)
            where TKey : IComparable<TKey>
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new ArgumentException("Sequence is empty", nameof(source));

                var best = enumerator.Current;
                var bestScore = selector(best);

                while (enumerator.MoveNext())
                {
                    var next = enumerator.Current;
                    var nextScore = selector(next);

                    if (nextScore.CompareTo(bestScore) >= 0)
                        continue;

                    best = next;
                    bestScore = nextScore;
                }

                return best;
            }
        }

        public static TItem MaxBy<TItem, TKey>([NotNull] this IEnumerable<TItem> source, [NotNull] Func<TItem, TKey> selector)
            where TKey : IComparable<TKey>
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new ArgumentException("Sequence is empty", nameof(source));

                var best = enumerator.Current;
                var bestScore = selector(best);

                while (enumerator.MoveNext())
                {
                    var next = enumerator.Current;
                    var nextScore = selector(next);

                    if (nextScore.CompareTo(bestScore) <= 0)
                        continue;

                    best = next;
                    bestScore = nextScore;
                }

                return best;
            }
        }
    }
}




