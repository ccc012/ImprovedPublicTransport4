using System;
using ColossalFramework;
using UnityEngine;
using ImprovedPublicTransport.Util;
using IPTUtils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.Integration.TicketPriceCustomizer
{
    public class PriceCustomization
    {
        /// <summary>
        /// Overload that accepts nested Settings.TicketPriceCustomizerSettings for consolidated configuration.
        /// </summary>
        public static void SetPrices(ImprovedPublicTransport.ModSetting.TicketPriceCustomizerSettings settings)
        {
            // Always apply ticket customizer when requested. IPT manages whether this integration is active.
            if (settings == null) return;

            bool hasAfterDark    = SteamHelper.IsDLCOwned(SteamHelper.DLC.AfterDarkDLC);
            bool hasSnowfall     = SteamHelper.IsDLCOwned(SteamHelper.DLC.SnowFallDLC);
            bool hasMassTransit  = SteamHelper.IsDLCOwned(SteamHelper.DLC.InMotionDLC);
            bool hasSunsetHarbor = SteamHelper.IsDLCOwned(SteamHelper.DLC.UrbanDLC);  // Sunset Harbor = UrbanDLC
            bool hasParklife     = SteamHelper.IsDLCOwned(SteamHelper.DLC.ParksDLC);

            SetBusPrice(settings.BusMultiplier);
            SetMetroPrice(settings.MetroMultiplier);
            SetTrainPrice(settings.TrainMultiplier);
            SetShipPrice(settings.ShipMultiplier);
            SetPlanePrice(settings.PlaneMultiplier);
            if (hasAfterDark)    SetTaxiPrice(settings.TaxiMultiplier);
            if (hasSnowfall)     SetTramPrice(settings.TramMultiplier);
            if (hasMassTransit)  SetBlimpPrice(settings.BlimpMultiplier);
            if (hasMassTransit)  SetFerryPrice(settings.FerryMultiplier);
            if (hasMassTransit)  SetCableCarPrice(settings.CableCarMultiplier);
            if (hasMassTransit)  SetMonorailPrice(settings.MonorailMultiplier);
            if (hasParklife)     SetSightseeingBusPrice(settings.SightseeingBusMultiplier);
            if (hasSunsetHarbor) SetIntercityBusPrice(settings.IntercityBusMultiplier);
            if (hasSunsetHarbor) SetHelicopterPrice(settings.HelicopterMultiplier);
            if (hasSunsetHarbor) SetTrolleybusPrice(settings.TrolleybusMultiplier);
        }

        /// <summary>
        /// Applies the day or night multipliers based on the current simulation time.
        /// Called by DayNightPriceWatcher whenever day/night transitions occur.
        /// </summary>
        public static void ApplyForCurrentTime(ImprovedPublicTransport.ModSetting.TicketPriceCustomizerSettings settings)
        {
            if (settings == null) return;
            bool isNight = Singleton<SimulationManager>.instance.m_isNightTime;

            bool hasAfterDark    = SteamHelper.IsDLCOwned(SteamHelper.DLC.AfterDarkDLC);
            bool hasSnowfall     = SteamHelper.IsDLCOwned(SteamHelper.DLC.SnowFallDLC);
            bool hasMassTransit  = SteamHelper.IsDLCOwned(SteamHelper.DLC.InMotionDLC);
            bool hasSunsetHarbor = SteamHelper.IsDLCOwned(SteamHelper.DLC.UrbanDLC);  // Sunset Harbor = UrbanDLC
            bool hasParklife     = SteamHelper.IsDLCOwned(SteamHelper.DLC.ParksDLC);

            SetBusPrice(isNight   ? settings.BusNightMultiplier   : settings.BusMultiplier);
            SetMetroPrice(isNight ? settings.MetroNightMultiplier : settings.MetroMultiplier);
            SetTrainPrice(isNight ? settings.TrainNightMultiplier : settings.TrainMultiplier);
            SetShipPrice(isNight  ? settings.ShipNightMultiplier  : settings.ShipMultiplier);
            SetPlanePrice(isNight ? settings.PlaneNightMultiplier : settings.PlaneMultiplier);
            if (hasAfterDark)    SetTaxiPrice(isNight           ? settings.TaxiNightMultiplier           : settings.TaxiMultiplier);
            if (hasSnowfall)     SetTramPrice(isNight           ? settings.TramNightMultiplier           : settings.TramMultiplier);
            if (hasMassTransit)  SetBlimpPrice(isNight          ? settings.BlimpNightMultiplier          : settings.BlimpMultiplier);
            if (hasMassTransit)  SetFerryPrice(isNight          ? settings.FerryNightMultiplier          : settings.FerryMultiplier);
            if (hasMassTransit)  SetCableCarPrice(isNight       ? settings.CableCarNightMultiplier       : settings.CableCarMultiplier);
            if (hasMassTransit)  SetMonorailPrice(isNight       ? settings.MonorailNightMultiplier       : settings.MonorailMultiplier);
            if (hasParklife)     SetSightseeingBusPrice(isNight ? settings.SightseeingBusNightMultiplier : settings.SightseeingBusMultiplier);
            if (hasSunsetHarbor) SetIntercityBusPrice(isNight   ? settings.IntercityBusNightMultiplier   : settings.IntercityBusMultiplier);
            if (hasSunsetHarbor) SetHelicopterPrice(isNight     ? settings.HelicopterNightMultiplier     : settings.HelicopterMultiplier);
            if (hasSunsetHarbor) SetTrolleybusPrice(isNight     ? settings.TrolleybusNightMultiplier     : settings.TrolleybusMultiplier);
        }

        public static void SetSightseeingBusPrice(float multiplier)
        {
            SetPrice(multiplier, "Sightseeing Bus");
        }

        public static void SetMetroPrice(float multiplier)
        {
            SetPrice(multiplier, "Metro");
        }

        public static void SetTrainPrice(float multiplier)
        {
            SetPrice(multiplier, "Train");
        }

        public static void SetShipPrice(float multiplier)
        {
            SetPrice(multiplier, "Ship");
        }

        public static void SetPlanePrice(float multiplier)
        {
            SetPrice(multiplier, "Airplane");
        }

        public static void SetTramPrice(float multiplier)
        {
            SetPrice(multiplier, "Tram");
        }

        public static void SetBlimpPrice(float multiplier)
        {
            SetPrice(multiplier, "Blimp");
        }

        public static void SetFerryPrice(float multiplier)
        {
            SetPrice(multiplier, "Ferry");
        }

        public static void SetMonorailPrice(float multiplier)
        {
            SetPrice(multiplier, "Monorail");
        }

        public static void SetCableCarPrice(float multiplier)
        {
            SetPrice(multiplier, "CableCar");
        }

        public static void SetBusPrice(float multiplier)
        {
            SetPrice(multiplier, "Bus");
        }

        public static void SetTaxiPrice(float multiplier)
        {
            SetPrice(multiplier, "Taxi");
        }
        
        public static void SetTrolleybusPrice(float multiplier)
        {
            SetPrice(multiplier, "Trolleybus");
        }

        public static void SetHelicopterPrice(float multiplier)
        {
            SetPrice(multiplier, "Passenger Helicopter");
        }
        
        public static void SetIntercityBusPrice(float multiplier)
        {
            SetPrice(multiplier, "Intercity Bus");
        }

        // Keep track of original base prices to avoid compounding multipliers
        private static readonly System.Collections.Generic.Dictionary<string, int> s_basePrices = new System.Collections.Generic.Dictionary<string, int>();

        // Cache of resolved TransportInfo objects (primary key + successful aliases).
        private static readonly System.Collections.Generic.Dictionary<string, TransportInfo> s_cachedTransportInfos = new System.Collections.Generic.Dictionary<string, TransportInfo>(StringComparer.OrdinalIgnoreCase);

        // Missing TransportInfo is expected for unowned DLC / not-yet-loaded prefabs — warn once per logical type.
        private static readonly System.Collections.Generic.HashSet<string> s_missingTransportInfoWarned = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the ticket price currently in effect for a transport type, as the game charges it.
        /// Used by the Ticket Prices tab so the actual resulting fare is visible, not just the
        /// multiplier percentage.
        /// </summary>
        /// <returns><c>false</c> when the type has no resolvable TransportInfo - e.g. its DLC is not
        /// installed, or no prefab of that type has loaded yet.</returns>
        public static bool TryGetCurrentPrice(string transportType, out int currentPrice, out int basePrice)
        {
            currentPrice = 0;
            basePrice = 0;

            if (!TryGetTransportInfo(transportType, out var info) || info == null)
            {
                return false;
            }

            currentPrice = (int)info.m_ticketPrice;
            basePrice = s_basePrices.TryGetValue(transportType, out var recorded) ? recorded : currentPrice;
            return true;
        }

        public static void ResetToVanilla()
        {
            foreach (var kvp in s_basePrices)
            {
                if (!TryGetTransportInfo(kvp.Key, out var info))
                {
                    // Already warned once (if ever) from TryGetTransportInfo / SetPrice path.
                    continue;
                }

                SetInfoPrice(info, (ushort)Mathf.Clamp(kvp.Value, 0, UInt16.MaxValue));

                // restore line prices for all lines with this transport type
                var transportManager = TransportManager.instance;
                if (transportManager == null) continue;

                for (ushort i = 0; i < transportManager.m_lines.m_size; i++)
                {
                    var line = transportManager.m_lines.m_buffer[i];
                    if ((line.m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None || line.Info != info)
                        continue;

                    SetLinePrice(i, info, ref transportManager.m_lines.m_buffer[i], (ushort)Mathf.Clamp(kvp.Value, 0, UInt16.MaxValue));
                }
            }
        }

        private static void SetPrice(float multiplier, string type)
        {
            try
            {
                if (!TryGetTransportInfo(type, out var info))
                {
                    LogMissingTransportInfoOnce(type);
                    return;
                }

                // Record base price if not already recorded (use original prefab value)
                if (!s_basePrices.TryGetValue(type, out var basePrice))
                {
                    basePrice = (int)info.m_ticketPrice;
                    s_basePrices[type] = basePrice;
                }

                // Calculate final price as base price × multiplier
                int finalPriceInt = Mathf.RoundToInt(basePrice * multiplier);
                finalPriceInt = Math.Max(0, finalPriceInt);
                ushort finalPrice = (ushort)Mathf.Clamp(finalPriceInt, 0, UInt16.MaxValue);
                if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) IPTUtils.Log($"TicketPriceCustomizer: Setting '{type}' multiplier to {multiplier:F1}x (base={basePrice}, final={finalPrice}, prefab='{info.name}')");

                SetInfoPrice(info, finalPrice);

                int modifiedLines = 0;
                int checkedLines = 0;
                var transportManager = TransportManager.instance;
                if (transportManager != null)
                {
                    for (ushort i = 0; i < transportManager.m_lines.m_size; i++)
                    {
                        var line = transportManager.m_lines.m_buffer[i];
                        checkedLines++;
                        if ((line.m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None || line.Info != info)
                        {
                            continue;
                        }

                        var oldPrice = line.m_ticketPrice;
                        SetLinePrice(i, info, ref transportManager.m_lines.m_buffer[i], finalPrice);
                        if (oldPrice != transportManager.m_lines.m_buffer[i].m_ticketPrice)
                        {
                            modifiedLines++;
                        }
                    }
                }
                if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs)
                {
                    IPTUtils.Log($"TicketPriceCustomizer: Applied ticket price for '{type}' (multiplier {multiplier:F1}) - checkedLines={checkedLines}, modifiedLines={modifiedLines}");
                }
            }
            catch (Exception e)
            {
                IPTUtils.LogError($"TicketPriceCustomizer: There was an error applying multiplier {multiplier} for transport type {type}: {e.Message}");
            }
        }

        private static void SetInfoPrice(TransportInfo info, ushort price)
        {
            if (info.m_ticketPrice == price)
            {
               return; 
            }
            if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) IPTUtils.Log($"TicketPriceCustomizer: Transport info: {info.name}, was price: {info.m_ticketPrice}, new price: {price}");
            info.m_ticketPrice = price;
        }

        private static void SetLinePrice(ushort lineId, TransportInfo info, ref TransportLine line, ushort price)
        {
            if ((line.m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None || line.Info != info ||
                price == line.m_ticketPrice)
            {
                return;
            }
            if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) IPTUtils.Log($"TicketPriceCustomizer: Transport line id: {lineId}, #{line.m_lineNumber}, name: {Singleton<TransportManager>.instance.GetLineName(lineId)}, was price: {line.m_ticketPrice}, new price: {price}");
            line.m_ticketPrice = price;
        }

        private static void LogMissingTransportInfoOnce(string transportType)
        {
            if (string.IsNullOrEmpty(transportType) || !s_missingTransportInfoWarned.Add(transportType))
            {
                return;
            }

            IPTUtils.LogWarning(
                $"TicketPriceCustomizer: TransportInfo for '{transportType}' not found " +
                "(DLC missing, prefab not loaded yet, or name mismatch). Further misses for this type are suppressed.");
        }

        /// <summary>
        /// Resolves a TransportInfo for a logical ticket-price key (e.g. "CableCar", "Airplane").
        /// Order: cache → live lines (name/aliases/type) → PrefabCollection.FindLoaded aliases →
        /// PrefabCollection type/vehicle/subservice scan. Successful hits (including alias names)
        /// are cached under the primary key and the alias that found them.
        /// </summary>
        private static bool TryGetTransportInfo(string transportType, out TransportInfo info)
        {
            info = null;
            if (string.IsNullOrEmpty(transportType))
            {
                return false;
            }

            if (s_cachedTransportInfos.TryGetValue(transportType, out info))
            {
                return info != null;
            }

            var aliases = GetTransportInfoNameAliases(transportType);

            // 1) Existing lines — works without PrefabCollection and is safe on the sim thread.
            info = FindTransportInfoFromLines(transportType, aliases);
            if (info != null)
            {
                CacheTransportInfo(transportType, info, matchedName: info.name);
                return true;
            }

            // 2) PrefabCollection by name / aliases. Wrapped — some loading mods can throw if
            // PrefabCollection is touched from unexpected threads; miss is better than a hard crash.
            info = FindTransportInfoByPrefabName(aliases);
            if (info != null)
            {
                CacheTransportInfo(transportType, info, matchedName: info.name);
                return true;
            }

            // 3) Scan loaded TransportInfos by transport type / vehicle type / class.
            info = FindTransportInfoByScan(transportType);
            if (info != null)
            {
                CacheTransportInfo(transportType, info, matchedName: info.name);
                return true;
            }

            // Do not cache misses — a type queried before its DLC prefabs load must remain resolvable later.
            return false;
        }

        private static void CacheTransportInfo(string primaryKey, TransportInfo info, string matchedName)
        {
            if (info == null || string.IsNullOrEmpty(primaryKey))
            {
                return;
            }

            s_cachedTransportInfos[primaryKey] = info;
            if (!string.IsNullOrEmpty(matchedName)
                && !matchedName.Equals(primaryKey, StringComparison.OrdinalIgnoreCase))
            {
                s_cachedTransportInfos[matchedName] = info;
            }

            // Also cache every known alias that equals the matched prefab name so later lookups hit cache.
            foreach (var alias in GetTransportInfoNameAliases(primaryKey))
            {
                if (alias.Equals(matchedName, StringComparison.OrdinalIgnoreCase)
                    || alias.Equals(info.name, StringComparison.OrdinalIgnoreCase))
                {
                    s_cachedTransportInfos[alias] = info;
                }
            }
        }

        /// <summary>
        /// Canonical + alternate prefab names used across vanilla, localised assets, and common renames.
        /// First entry is the logical key used by Set*Price callers.
        /// </summary>
        private static string[] GetTransportInfoNameAliases(string transportType)
        {
            switch (transportType)
            {
                case "Bus":
                    return new[] { "Bus" };
                case "Intercity Bus":
                    return new[] { "Intercity Bus", "IntercityBus", "Intercity bus" };
                case "Metro":
                    return new[] { "Metro", "Metro Train", "Subway" };
                case "Train":
                    return new[] { "Train", "Passenger Train" };
                case "Ship":
                    return new[] { "Ship", "Passenger Ship", "PassengerShip" };
                case "Airplane":
                    return new[] { "Airplane", "Passenger Plane", "PassengerPlane", "Plane" };
                case "Taxi":
                    return new[] { "Taxi" };
                case "Tram":
                    return new[] { "Tram" };
                case "Blimp":
                    return new[] { "Blimp", "Passenger Blimp" };
                case "Ferry":
                    return new[] { "Ferry", "Passenger Ferry", "PassengerFerry" };
                case "CableCar":
                    return new[] { "CableCar", "Cable Car", "Cablecar" };
                case "Monorail":
                    return new[] { "Monorail" };
                case "Sightseeing Bus":
                    return new[] { "Sightseeing Bus", "Tourist Bus", "TouristBus", "SightseeingBus" };
                case "Passenger Helicopter":
                    return new[] { "Passenger Helicopter", "Helicopter", "PassengerHelicopter" };
                case "Trolleybus":
                    return new[] { "Trolleybus", "Trolley Bus" };
                default:
                    return new[] { transportType };
            }
        }

        private static bool NameMatchesAnyAlias(string prefabName, string[] aliases)
        {
            if (string.IsNullOrEmpty(prefabName) || aliases == null)
            {
                return false;
            }

            for (int i = 0; i < aliases.Length; i++)
            {
                if (prefabName.Equals(aliases[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static TransportInfo FindTransportInfoFromLines(string transportType, string[] aliases)
        {
            var transportManager = TransportManager.instance;
            if (transportManager == null)
            {
                return null;
            }

            for (ushort i = 0; i < transportManager.m_lines.m_size; i++)
            {
                var line = transportManager.m_lines.m_buffer[i];
                if ((line.m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None || line.Info == null)
                {
                    continue;
                }

                var candidate = line.Info;
                if (NameMatchesAnyAlias(candidate.name, aliases) || TransportInfoMatchesLogicalType(candidate, transportType))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static TransportInfo FindTransportInfoByPrefabName(string[] aliases)
        {
            if (aliases == null)
            {
                return null;
            }

            try
            {
                for (int i = 0; i < aliases.Length; i++)
                {
                    var loaded = PrefabCollection<TransportInfo>.FindLoaded(aliases[i]);
                    if (loaded != null)
                    {
                        return loaded;
                    }
                }
            }
            catch (Exception e)
            {
                if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs)
                {
                    IPTUtils.LogWarning($"TicketPriceCustomizer: PrefabCollection.FindLoaded failed: {e.Message}");
                }
            }

            return null;
        }

        private static TransportInfo FindTransportInfoByScan(string transportType)
        {
            try
            {
                int count = PrefabCollection<TransportInfo>.LoadedCount();
                for (uint i = 0; i < count; i++)
                {
                    var candidate = PrefabCollection<TransportInfo>.GetLoaded(i);
                    if (candidate == null)
                    {
                        continue;
                    }

                    if (TransportInfoMatchesLogicalType(candidate, transportType))
                    {
                        return candidate;
                    }
                }
            }
            catch (Exception e)
            {
                if (ImprovedPublicTransport.Util.Diagnostics.VerboseRuntimeLogs)
                {
                    IPTUtils.LogWarning($"TicketPriceCustomizer: PrefabCollection scan failed: {e.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Matches a loaded TransportInfo to a logical ticket-price key using transport type,
        /// vehicle type, and ItemClass level/subservice — independent of localised prefab names.
        /// </summary>
        private static bool TransportInfoMatchesLogicalType(TransportInfo info, string transportType)
        {
            if (info == null || string.IsNullOrEmpty(transportType))
            {
                return false;
            }

            var t = info.m_transportType;
            var vehicle = info.m_vehicleType;
            var itemClass = info.m_class;
            var sub = itemClass != null ? itemClass.m_subService : ItemClass.SubService.None;
            var level = itemClass != null ? itemClass.m_level : ItemClass.Level.None;
            var name = info.name ?? string.Empty;

            switch (transportType)
            {
                case "Bus":
                    // City bus only — intercity is Level3 on the same Bus transport type
                    // (mirrors TransportStationAI.IsIntercity / TicketPricesTab).
                    return t == TransportInfo.TransportType.Bus
                           && (sub == ItemClass.SubService.PublicTransportBus || sub == ItemClass.SubService.None)
                           && level != ItemClass.Level.Level3
                           && name.IndexOf("Intercity", StringComparison.OrdinalIgnoreCase) < 0;
                case "Intercity Bus":
                    return t == TransportInfo.TransportType.Bus
                           && (level == ItemClass.Level.Level3
                               || name.IndexOf("Intercity", StringComparison.OrdinalIgnoreCase) >= 0);
                case "Metro":
                    return t == TransportInfo.TransportType.Metro
                           || sub == ItemClass.SubService.PublicTransportMetro;
                case "Train":
                    return t == TransportInfo.TransportType.Train
                           || (sub == ItemClass.SubService.PublicTransportTrain
                               && t != TransportInfo.TransportType.Metro
                               && t != TransportInfo.TransportType.Monorail);
                case "Ship":
                    // Passenger ship harbour traffic — not ferry lines (Ferry vehicle type / "Ferry" name).
                    return t == TransportInfo.TransportType.Ship
                           && vehicle != VehicleInfo.VehicleType.Ferry
                           && name.IndexOf("Ferry", StringComparison.OrdinalIgnoreCase) < 0;
                case "Ferry":
                    return t == TransportInfo.TransportType.Ship
                           && (vehicle == VehicleInfo.VehicleType.Ferry
                               || name.IndexOf("Ferry", StringComparison.OrdinalIgnoreCase) >= 0
                               || sub == ItemClass.SubService.PublicTransportShip && level == ItemClass.Level.Level2);
                case "Airplane":
                    // Intercity planes — not blimps (Blimp vehicle type / "Blimp" name).
                    return t == TransportInfo.TransportType.Airplane
                           && vehicle != VehicleInfo.VehicleType.Blimp
                           && name.IndexOf("Blimp", StringComparison.OrdinalIgnoreCase) < 0;
                case "Blimp":
                    return t == TransportInfo.TransportType.Airplane
                           && (vehicle == VehicleInfo.VehicleType.Blimp
                               || name.IndexOf("Blimp", StringComparison.OrdinalIgnoreCase) >= 0);
                case "Taxi":
                    return t == TransportInfo.TransportType.Taxi
                           || sub == ItemClass.SubService.PublicTransportTaxi;
                case "Tram":
                    return t == TransportInfo.TransportType.Tram
                           || sub == ItemClass.SubService.PublicTransportTram;
                case "CableCar":
                    return t == TransportInfo.TransportType.CableCar
                           || sub == ItemClass.SubService.PublicTransportCableCar;
                case "Monorail":
                    return t == TransportInfo.TransportType.Monorail
                           || sub == ItemClass.SubService.PublicTransportMonorail;
                case "Sightseeing Bus":
                    return t == TransportInfo.TransportType.TouristBus
                           || sub == ItemClass.SubService.PublicTransportTours;
                case "Passenger Helicopter":
                    return t == TransportInfo.TransportType.Helicopter
                           || vehicle == VehicleInfo.VehicleType.Helicopter
                           || name.IndexOf("Helicopter", StringComparison.OrdinalIgnoreCase) >= 0;
                case "Trolleybus":
                    return t == TransportInfo.TransportType.Trolleybus
                           || sub == ItemClass.SubService.PublicTransportTrolleybus;
                default:
                    return name.Equals(transportType, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
