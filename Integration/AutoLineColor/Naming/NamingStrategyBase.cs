using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace AutoLineColor.Naming
{
    internal struct LineAnalysis
    {
        public int StopCount;
        [NotNull, ItemNotNull]
        public List<string> Districts;
        public bool HasNonDistrictStop;

        /// <summary>
        /// May be null if pathfinding wasn't successful at analysis time.
        /// </summary>
        public Dictionary<string, float> DistanceOnSegments;
    }

    [SuppressMessage("ReSharper", "VirtualMemberNeverOverridden.Global")]
    internal abstract class NamingStrategyBase : INamingStrategy
    {
        public string GetName(in TransportLine transportLine, ushort lineId = 0)
        {
            try
            {
                string result;

                switch (transportLine.Info.m_transportType)
                {
                    case TransportInfo.TransportType.Bus:
                        result = GetBusLineName(transportLine, lineId);
                        break;

                    case TransportInfo.TransportType.Metro:
                        result = GetMetroLineName(transportLine, lineId);
                        break;

                    case TransportInfo.TransportType.Train:
                        result = GetTrainLineName(transportLine, lineId);
                        break;

                    case TransportInfo.TransportType.Ship:
                        result = GetShipLineName(transportLine, lineId);
                        break;

                    case TransportInfo.TransportType.Tram:
                        result = GetTramLineName(transportLine, lineId);
                        break;

                    case TransportInfo.TransportType.Monorail:
                        result = GetMonorailLineName(transportLine, lineId);
                        break;

                    case TransportInfo.TransportType.CableCar:
                        result = GetCableCarLineName(transportLine, lineId);
                        break;

                    case TransportInfo.TransportType.Airplane:
                        result = GetBlimpLineName(transportLine, lineId);
                        break;

                    case TransportInfo.TransportType.Taxi:
                    case TransportInfo.TransportType.EvacuationBus:
                    case TransportInfo.TransportType.HotAirBalloon:
                    case TransportInfo.TransportType.TouristBus:
                    case TransportInfo.TransportType.Pedestrian:
                        // TODO: handle more line types
                        goto default;

                    default:
                        result = null;
                        break;
                }

                return result ?? GetGenericLineName(transportLine);
            }
            catch (Exception e)
            {
                Console.Instance.Error(e.ToString());
                return null;
            }
        }

        protected virtual string GetGenericLineName(in TransportLine transportLine)
        {
            return null;
        }

        protected virtual string GetBusLineName(in TransportLine transportLine, ushort lineId = 0)
        {
            return null;
        }

        protected virtual string GetTrainLineName(in TransportLine transportLine, ushort lineId = 0)
        {
            return null;
        }

        protected virtual string GetMetroLineName(in TransportLine transportLine, ushort lineId = 0)
        {
            return null;
        }

        // ReSharper disable once UnusedParameter.Global
        protected virtual string GetCableCarLineName(in TransportLine transportLine, ushort lineId = 0)
        {
            return null;
        }

        protected virtual string GetTramLineName(in TransportLine transportLine, ushort lineId = 0)
        {
            return GetBusLineName(transportLine, lineId);
        }

        protected virtual string GetShipLineName(in TransportLine transportLine, ushort lineId = 0)
        {
            return GetTrainLineName(transportLine, lineId);
        }

        protected virtual string GetMonorailLineName(in TransportLine transportLine, ushort lineId = 0)
        {
            return GetTrainLineName(transportLine, lineId);
        }

        protected virtual string GetBlimpLineName(in TransportLine transportLine, ushort lineId = 0)
        {
            return GetShipLineName(transportLine);
        }

        protected static LineAnalysis AnalyzeLine(in TransportLine transportLine)
        {
            var theNetManager = Singleton<NetManager>.instance;
            var theDistrictManager = Singleton<DistrictManager>.instance;
            var stop = transportLine.m_stops;
            var firstStop = stop;
            var collectSegments = true;

            var result = new LineAnalysis
            {
                Districts = new List<string>(),
                HasNonDistrictStop = false,
                StopCount = 0,
                DistanceOnSegments = new Dictionary<string, float>(),
            };

            var segments = new List<ushort>();

            do
            {
                var stopInfo = theNetManager.m_nodes.m_buffer[stop];

                // record the name of the district containing the stop
                var district = theDistrictManager.GetDistrict(stopInfo.m_position);

                if (district == 0)
                {
                    result.HasNonDistrictStop = true;
                }
                else
                {
                    var districtName = theDistrictManager.GetDistrictName(district).Trim();
                    if (!result.Districts.Contains(districtName))
                    {
                        result.Districts.Add(districtName);
                    }
                }

                var nextStop = TransportLine.GetNextStop(stop);

                if (collectSegments)
                {
                    // record the segment names and distances from this stop to the next
                    segments.Clear();
                    if (GetSegmentsBetweenStops(stop, nextStop, segments))
                    {
                        foreach (var segment in segments)
                        {
                            var name = theNetManager.GetSegmentName(segment).Trim();

                            if (string.IsNullOrEmpty(name))
                                continue;

                            var length = theNetManager.m_segments.m_buffer[segment].m_averageLength;

                            if (result.DistanceOnSegments.TryGetValue(name, out var curLength))
                            {
                                result.DistanceOnSegments[name] = curLength + length;
                            }
                            else
                            {
                                result.DistanceOnSegments[name] = length;
                            }
                        }
                    }
                    else
                    {
                        collectSegments = false;
                    }
                }

                stop = nextStop;
                result.StopCount++;
            } while (result.StopCount < Constants.MaxLineAnalysisStops && stop != firstStop);

            if (!collectSegments)
                result.DistanceOnSegments = null;

            return result;
        }

        private static bool GetSegmentsBetweenStops(ushort stop1, ushort stop2, List<ushort> segments)
        {
            var theNetManager = Singleton<NetManager>.instance;

            // Do NOT call TransportManager.UpdateLinesNow() here.
            // It recalculates paths for every line in the city and was invoked once per stop pair
            // during AnalyzeLine — a single "refresh color/name" click could freeze for seconds.
            // Complete lines already have segment paths; if a path is missing we just skip roads.

            while (stop1 != stop2)
            {
                // these are the transport-network segments that make up the logical line
                var transportSegment = TransportLine.GetNextSegment(stop1);

                if (transportSegment == 0)
                    return false;

                var path = theNetManager.m_segments.m_buffer[transportSegment].m_path;

                if (path == 0)
                    return false;

                // these are the road/track segments that make up the physical line
                segments.AddRange(GetPathPositions(path).Select(pp => pp.m_segment));

                stop1 = TransportLine.GetNextStop(stop1);
            }

            return true;
        }

        private static IEnumerable<PathUnit.Position> GetPathPositions(uint pathUnit)
        {
            var thePathManager = Singleton<PathManager>.instance;

            var unit = thePathManager.m_pathUnits.m_buffer[pathUnit];

            if (!unit.GetPosition(0, out var position))
                yield break;

            var posIndex = 0;

            do
            {
                yield return position;
            } while (PathUnit.GetNextPosition(ref pathUnit, ref posIndex, out position, out _));
        }

        protected static List<string> GetExistingNames(ushort excludeLineId = 0)
        {
            var names = new List<string>();
            var theTransportManager = Singleton<TransportManager>.instance;
            var lines = theTransportManager.m_lines.m_buffer;
            int len = lines.Length;
            for (int lineIndex = 0; lineIndex < len; lineIndex++)
            {
                if (lineIndex == excludeLineId)
                    continue;
                if (lineIndex >= len)
                    continue;
                if (!lines[lineIndex].HasCustomName())
                    continue;
                var name = theTransportManager.GetLineName((ushort)lineIndex);
                if (!string.IsNullOrEmpty(name))
                {
                    names.Add(name);
                }
            }
            return names;
        }
    }

    internal static class StringExtensions
    {
        public static string GetInitials(this string words)
        {
            var initials = words[0].ToString();
            for (var i = 0; i < words.Length - 1; i++)
            {
                if (words[i] == ' ')
                {
                    initials += words[i + 1];
                }
            }
            return initials;
        }

        public static string FirstWord(this string words)
        {
            var pos = words.IndexOf(' ');
            return pos >= 0 ? words.Substring(0, pos) : words;
        }

        private static string LastWord(this string words)
        {
            var pos = words.LastIndexOf(' ');
            return pos >= 0 ? words.Substring(pos + 1) : words;
        }

        private static string AllButLastWord(this string words)
        {
            var pos = words.LastIndexOf(' ');
            return pos >= 0 ? words.Substring(0, pos) : words;
        }

        // TODO: refactor district/road suffix methods
        // TODO: strip or abbreviate prefix too ("The Valley" -> "Valley", "East Palo Alto" -> "E Palo Alto")
        public static string StripDistrictSuffix(this string name)
        {
            return name.AllButLastWord();
        }

        public static string StripRoadSuffix(this string name)
        {
            return name.AllButLastWord();
        }

        public static string AbbreviateDistrictSuffix(this string name)
        {
            // TODO: make localizable

            if (name.IndexOf(' ') < 0)
                return name;

            var suffix = LastWord(name);

            switch (suffix)
            {
                case "District":
                    suffix = "Dist";
                    break;

                case "Heights":
                    suffix = "Hts";
                    break;

                case "Hills":
                    suffix = "Hls";
                    break;

                case "Park":
                    suffix = "Pk";
                    break;

                case "Square":
                    suffix = "Sq";
                    break;

                default:
                    suffix = suffix.AutoShorten().Substring(0, 3);
                    break;
            }

            return name.AllButLastWord() + " " + suffix;
        }

        // TODO: make localizable
        /* Loosely based on the US Postal Service street suffix abbreviations:
         * https://pe.usps.com/text/pub28/28apc_002.htm */
        private static readonly Dictionary<string, string> RoadSuffixAbbreviations =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Alley", "Aly" },
                { "Avenue", "Ave" },
                { "Boulevard", "Blvd" },
                { "Bridge", "Br" },
                { "Circle", "Cir" },
                { "Court", "Ct" },
                { "Crossing", "Xing" },
                { "Drive", "Dr" },
                { "Expressway", "Expy" },
                { "Freeway", "Fwy" },
                { "Highway", "Hwy" },
                { "Junction", "Jct" },
                { "Lane", "Ln" },
                { "Parkway", "Pkwy" },
                { "Road", "Rd" },
                { "Station", "Stn" },
                { "Street", "St" },
                { "Track", "Trk" },
                { "Tunnel", "Tunl" },
                { "Way", "Wy" },
            };

        public static string AbbreviateRoadSuffix(this string name)
        {
            if (name.IndexOf(' ') < 0)
                return name;

            var suffix = LastWord(name);

            if (!RoadSuffixAbbreviations.TryGetValue(suffix, out var abbrev))
                abbrev = AutoShorten(suffix).Substring(0, 3);

            return AllButLastWord(name) + " " + abbrev;
        }

        private static readonly HashSet<char> Vowels = new HashSet<char>
        {
            'a', 'e', 'i', 'o', 'u',
            'A', 'E', 'I', 'O', 'U',
        };

        /// <summary>
        /// Tries to shorten a word by removing unneeded letters.
        /// </summary>
        /// <param name="word">The word.</param>
        /// <returns>A new string with length less than or equal to the length of <paramref name="word"/>.</returns>
        /// <remarks>
        /// <para>The first character is always preserved, even if it's a vowel.</para>
        /// <para>It was hard to resist the temptation to call this <c>Abrvt</c>.</para>
        /// </remarks>
        private static string AutoShorten(this string word)
        {
            var sb = new StringBuilder(word);

            for (var i = sb.Length - 1; i > 0; i--)
            {
                if (Vowels.Contains(sb[i]) ||
                    sb[i] == sb[i - 1] ||
                    sb[i] == 'c' && i + 1 < sb.Length && sb[i + 1] == 'k')
                {
                    sb.Remove(i, 1);
                }
            }

            return sb.ToString();
        }

        private static readonly char[] SpaceDelimiter = { ' ' };

        public static string AutoShortenWords(this string words)
        {
            var split = words.Split(SpaceDelimiter);

            for (var i = 0; i < split.Length; i++)
                split[i] = split[i].AutoShorten();

            return string.Join(" ", split);
        }
    }
}