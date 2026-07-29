using System;
using ColossalFramework;
using ColossalFramework.IO;
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

        // Cache of resolved transport Info objects for safer simulation-thread operations.
        private static readonly System.Collections.Generic.Dictionary<string, TransportInfo> s_cachedTransportInfos = new System.Collections.Generic.Dictionary<string, TransportInfo>();

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
                    if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs)
                        IPTUtils.LogWarning($"TicketPriceCustomizer: ResetToVanilla skipping '{kvp.Key}' - TransportInfo not found.");
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
                // Prefer already-resolved TransportInfo objects (from existing lines) and avoid PrefabCollection lookups on simulation thread.
                if (!TryGetTransportInfo(type, out var info))
                {
                    if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) IPTUtils.LogWarning($"TicketPriceCustomizer: TransportInfo for '{type}' not found; skipping type.");
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
                if (ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs) IPTUtils.Log($"TicketPriceCustomizer: Setting '{type}' multiplier to {multiplier:F1}x (base={basePrice}, final={finalPrice})");

                SetInfoPrice(info, finalPrice);

                int modifiedLines = 0;
                int checkedLines = 0;
                for (ushort i = 0; i < TransportManager.instance.m_lines.m_size; i++)
                {
                    var line = TransportManager.instance.m_lines.m_buffer[i];
                    checkedLines++;
                    if ((line.m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None || line.Info != info)
                    {
                        continue;
                    }

                    var oldPrice = line.m_ticketPrice;
                    SetLinePrice(i, info, ref TransportManager.instance.m_lines.m_buffer[i], finalPrice);
                    if (oldPrice != TransportManager.instance.m_lines.m_buffer[i].m_ticketPrice)
                    {
                        modifiedLines++;
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

        private static bool TryGetTransportInfo(string transportType, out TransportInfo info)
        {
            if (s_cachedTransportInfos.TryGetValue(transportType, out info))
            {
                return info != null;
            }

            info = null;
            var transportManager = TransportManager.instance;
            if (transportManager != null)
            {
                for (ushort i = 0; i < transportManager.m_lines.m_size; i++)
                {
                    var line = transportManager.m_lines.m_buffer[i];
                    if ((line.m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None || line.Info == null)
                        continue;
                    if (line.Info.name.Equals(transportType, StringComparison.OrdinalIgnoreCase))
                    {
                        info = line.Info;
                        break;
                    }
                }
            }

            // We purposely skip PrefabCollection lookups here in the simulation thread path because some external
            // mods (e.g. LoadingScreenModRevisited custom asset deserializer) can crash when called from worker threads.
            // Use line snapshot data only.
            s_cachedTransportInfos[transportType] = info;
            return info != null;
        }
    }
}
