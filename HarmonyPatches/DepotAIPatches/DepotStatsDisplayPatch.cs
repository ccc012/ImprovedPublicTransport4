using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ColossalFramework;
using ImprovedPublicTransport.ReverseDetours;
using ImprovedPublicTransport.Util;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.HarmonyPatches.DepotAIPatches
{
    /// <summary>
    /// Always rewrites depot occupancy to <c>LABEL: X / Y</c>. Vanilla often prints only X for
    /// ferry/trolley/blimp, and helicopter reused the bus locale ("Buses" / "Ônibus").
    /// Labels prefer LocaleFormatter, then Localization.Get keys, then English fallbacks.
    /// </summary>
    public static class DepotStatsDisplayPatch
    {
        private static readonly Regex TrailingCount = new Regex(
            @"\s*[:：]?\s*\d+\s*(/\s*\d+)?\s*$",
            RegexOptions.Compiled);

        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(DepotAI), nameof(DepotAI.GetLocalizedStats)),
                null,
                new PatchUtil.MethodDefinition(typeof(DepotStatsDisplayPatch), nameof(Postfix))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(DepotAI), nameof(DepotAI.GetLocalizedStats))
            );
        }

        private static void Postfix(DepotAI __instance, ushort buildingID, ref Building data, ref string __result)
        {
            try
            {
                if (__instance is TransportStationAI)
                {
                    if (__result != null && LooksLikeDepotVehicleLine(__result))
                    {
                        __result = StripDepotVehicleLines(__result);
                    }

                    return;
                }

                if (__instance.m_transportInfo == null)
                {
                    return;
                }

                var cap = __instance.m_maxVehicleCount;
                if (cap <= 0 || cap >= 100000)
                {
                    return;
                }

                var count = __instance.GetVehicleCount(buildingID, ref data);
                var budget = Singleton<EconomyManager>.instance.GetBudget(__instance.m_info.m_class);
                var productionRate = PlayerBuildingAI.GetProductionRate(100, budget);
                var max = (productionRate * cap + 99) / 100;
                if (max <= 0)
                {
                    max = cap;
                }

                // Dual-purpose depots (e.g. a combined bus+tram garage) also expose a second
                // vehicle slot via m_secondaryTransportInfo/m_maxVehicleCount2. GetVehicleCount()
                // above only reflects the primary slot's TransferReason, so without this the
                // secondary fleet's occupancy line was silently missing from the panel entirely -
                // not wrong, just absent, same as the un-appended stops in
                // PanelExtenderCityService.CollectStationStops before that fix.
                int? count2 = null;
                int? max2 = null;
                var cap2 = __instance.m_maxVehicleCount2;
                if (__instance.m_secondaryTransportInfo != null && cap2 > 0 && cap2 < 100000)
                {
                    var secondaryCount = GetVehicleCountForSlot(
                        __instance, buildingID, ref data, __instance.m_secondaryTransportInfo);
                    var secondaryMax = (productionRate * cap2 + 99) / 100;
                    if (secondaryMax <= 0)
                    {
                        secondaryMax = cap2;
                    }

                    count2 = secondaryCount;
                    max2 = secondaryMax;
                }

                var line = BuildOccupancyLine(__instance.m_transportInfo, count, max);
                if (count2.HasValue && max2.HasValue)
                {
                    // Combine both lines into a single block before handing it to
                    // AppendOrReplaceVehicleLine - calling it twice in a row would strip the
                    // first line back out, since it also "looks like" a depot vehicle line.
                    var line2 = BuildOccupancyLine(__instance.m_secondaryTransportInfo, count2.Value, max2.Value);
                    line = line + Environment.NewLine + line2;
                }

                __result = AppendOrReplaceVehicleLine(__result, line);
            }
            catch (Exception ex)
            {
                Utils.LogError($"DepotStatsDisplayPatch: {ex.Message}");
            }
        }

        /// <summary>
        /// Counts owned vehicles matching a specific transport slot's <see cref="TransferReason"/>
        /// (primary or secondary), instead of <see cref="DepotAI.GetVehicleCount"/> which only
        /// resolves the primary slot. Goes through the same private
        /// <c>CommonBuildingAI.CalculateOwnVehicles</c> vanilla itself uses, via
        /// <see cref="CommonBuildingAIReverseDetour"/>.
        /// </summary>
        private static int GetVehicleCountForSlot(
            DepotAI depotAi, ushort buildingID, ref Building data, TransportInfo slotInfo)
        {
            int count = 0, cargo = 0, capacity = 0, outside = 0;
            CommonBuildingAIReverseDetour.CalculateOwnVehicles(
                depotAi, buildingID, ref data, slotInfo.m_vehicleReason,
                ref count, ref cargo, ref capacity, ref outside);
            return count;
        }

        /// <summary>
        /// Build a guaranteed "…: count / max" line. Never trust FormatGeneric alone — some
        /// locale templates only have {0} and drop the max.
        /// </summary>
        private static string BuildOccupancyLine(TransportInfo info, int count, int max)
        {
            var label = ResolveLabel(info);
            return label + ": " + count + " / " + max;
        }

        private static string ResolveLabel(TransportInfo info)
        {
            try
            {
                // Probe locale templates with dummy numbers, then strip the trailing count.
                string probe = null;
                if (info.m_vehicleType == VehicleInfo.VehicleType.Ferry
                    || info.m_transportType == TransportInfo.TransportType.Ship)
                {
                    probe = LocaleFormatter.FormatGeneric("AIINFO_FERRYDEPOT_FERRYCOUNT", 0, 0);
                }
                else if (info.m_vehicleType == VehicleInfo.VehicleType.Blimp
                         || info.m_transportType == TransportInfo.TransportType.HotAirBalloon
                         || info.m_transportType == TransportInfo.TransportType.Airplane)
                {
                    probe = LocaleFormatter.FormatGeneric("AIINFO_BLIMPDEPOT_BLIMPCOUNT", 0, 0);
                }
                else if (info.m_vehicleType == VehicleInfo.VehicleType.Helicopter
                         || info.m_transportType == TransportInfo.TransportType.Helicopter)
                {
                    // No dedicated helicopter depot locale; skip FormatGeneric (bus template mislabels).
                    probe = null;
                }
                else
                {
                    switch (info.m_transportType)
                    {
                        case TransportInfo.TransportType.Trolleybus:
                            probe = LocaleFormatter.FormatGeneric("AIINFO_TROLLEYBUSDEPOT_TROLLEYBUSCOUNT", 0, 0);
                            break;
                        case TransportInfo.TransportType.Tram:
                            probe = LocaleFormatter.FormatGeneric("AIINFO_TRAMDEPOT_TRAMCOUNT", 0, 0);
                            break;
                        case TransportInfo.TransportType.Taxi:
                            probe = LocaleFormatter.FormatGeneric("AIINFO_TAXIDEPOT_VEHICLES", 0, 0);
                            break;
                        case TransportInfo.TransportType.Bus:
                        case TransportInfo.TransportType.TouristBus:
                            probe = LocaleFormatter.FormatGeneric("AIINFO_BUSDEPOT_BUSCOUNT", 0, 0);
                            break;
                        default:
                            probe = LocaleFormatter.FormatGeneric("AIINFO_BUSDEPOT_BUSCOUNT", 0, 0);
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(probe))
                {
                    var label = TrailingCount.Replace(probe, string.Empty).Trim().TrimEnd(':', '：');
                    if (!string.IsNullOrEmpty(label)
                        && label.IndexOf("AIINFO_", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        return label;
                    }
                }
            }
            catch
            {
                // fall through
            }

            return FallbackLabel(info);
        }

        /// <summary>
        /// Localization.Get keys first, then English hard fallbacks (never Portuguese literals).
        /// </summary>
        private static string FallbackLabel(TransportInfo info)
        {
            if (info.m_vehicleType == VehicleInfo.VehicleType.Ferry
                || info.m_transportType == TransportInfo.TransportType.Ship)
            {
                return LocalizedOr("DEPOT_STATS_FERRIES_IN_USE", "Ferries in use");
            }

            if (info.m_vehicleType == VehicleInfo.VehicleType.Blimp
                || info.m_transportType == TransportInfo.TransportType.HotAirBalloon
                || info.m_transportType == TransportInfo.TransportType.Airplane)
            {
                return LocalizedOr("DEPOT_STATS_BLIMPS_IN_USE", "Blimps in use");
            }

            if (info.m_vehicleType == VehicleInfo.VehicleType.Helicopter
                || info.m_transportType == TransportInfo.TransportType.Helicopter)
            {
                return LocalizedOr("DEPOT_STATS_HELICOPTERS", "Helicopters in use");
            }

            if (info.m_transportType == TransportInfo.TransportType.Trolleybus)
            {
                return LocalizedOr("DEPOT_STATS_TROLLEYBUSES_IN_USE", "Trolleybuses in use");
            }

            return LocalizedOr("DEPOT_STATS_VEHICLES_IN_USE", "Vehicles in use");
        }

        private static string LocalizedOr(string key, string englishFallback)
        {
            try
            {
                var s = ImprovedPublicTransport.Localization.Get(key);
                if (!string.IsNullOrEmpty(s)
                    && !s.StartsWith("DEPOT_STATS_", StringComparison.Ordinal)
                    && !string.Equals(s, key, StringComparison.Ordinal))
                {
                    return s;
                }
            }
            catch
            {
                // fall through
            }

            return englishFallback;
        }

        private static string AppendOrReplaceVehicleLine(string current, string vehicleLine)
        {
            if (string.IsNullOrEmpty(current))
            {
                return vehicleLine;
            }

            var stripped = StripDepotVehicleLines(current);
            if (string.IsNullOrEmpty(stripped))
            {
                return vehicleLine;
            }

            if (!stripped.EndsWith("\n") && !stripped.EndsWith("\r"))
            {
                stripped += Environment.NewLine;
            }

            return stripped + vehicleLine;
        }

        private static bool LooksLikeDepotVehicleLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            // Match vanilla / prior labels in multiple languages so they can be replaced.
            return text.IndexOf("em uso", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("in use", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Helicópter", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Helicopter", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Dirigív", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Blimp", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Trólebus", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Trolley", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Balsas", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Ferry", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Ônibus em uso", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Buses in use", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string StripDepotVehicleLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var kept = new List<string>();
            foreach (var line in lines)
            {
                if (!LooksLikeDepotVehicleLine(line))
                {
                    kept.Add(line);
                }
            }

            return string.Join(Environment.NewLine, kept.ToArray()).TrimEnd();
        }
    }
}
