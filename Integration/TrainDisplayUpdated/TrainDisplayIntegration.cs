// <copyright file="TrainDisplayIntegration.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the GPL-3.0 license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace ImprovedPublicTransport.Integration.TrainDisplayUpdated
{
    using System;
    using System.Globalization;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;
    using ImprovedPublicTransport.Util;
    using Utils = ImprovedPublicTransport.Util.Utils;

    internal static class TrainDisplayIntegration
    {
        private static GUIStyle _bodyStyle;
        private static GUIStyle _headerStyle;
        private static GUIStyle _metricStyle;
        private static GUIStyle _smallStyle;
        private static GUIStyle _valueStyle;
        private static GUIStyle _tinyStyle;

        internal struct OverlayData
        {
            internal bool HasContent;
            internal string Name;
            internal string Status;
            internal string Breadcrumb;
            internal string NextStop;
            internal string Passengers;
            internal string Speed;
            internal string Elapsed;

            // Only populated for the Original theme's route strip - gathering a line's stop names
            // is cheap (bounded by stop count, same throttled interval as everything else here) but
            // pointless work for the other three themes, which do not use it.
            internal Color32 LineColor;
            internal string[] RouteStopNames;
            internal int RouteCurrentIndex;
        }

        /// <summary>
        /// Gets the vehicle selected in the game's own world-info UI. This avoids camera
        /// reflection and means the overlay works without any first-person camera mod.
        /// </summary>
        internal static bool TryGetSelectedVehicle(out ushort vehicleId)
        {
            vehicleId = 0;

            try
            {
                var selected = WorldInfoPanel.GetCurrentInstanceID();
                var vehicleManager = Singleton<VehicleManager>.instance;
                if (vehicleManager == null)
                {
                    return false;
                }

                if (selected.Vehicle != 0)
                {
                    vehicleId = vehicleManager.m_vehicles.m_buffer[selected.Vehicle].GetFirstVehicle(selected.Vehicle);
                    if (vehicleId == 0)
                    {
                        vehicleId = selected.Vehicle;
                    }
                }
                else if (selected.TransportLine != 0)
                {
                    // Opening a transport line is also an explicit public-transport selection.
                    // Use its first live vehicle without requiring a camera mode.
                    var line = Singleton<TransportManager>.instance.m_lines.m_buffer[selected.TransportLine];
                    vehicleId = line.m_vehicles;
                    if (vehicleId != 0)
                    {
                        vehicleId = vehicleManager.m_vehicles.m_buffer[vehicleId].GetFirstVehicle(vehicleId);
                    }
                }

                return vehicleId != 0 && IsSupportedVehicle(vehicleId);
            }
            catch (Exception ex)
            {
                Utils.LogError($"TrainDisplayUpdated: failed to read the selected vehicle: {ex.Message}");
                vehicleId = 0;
                return false;
            }
        }
        internal static bool IsSupportedVehicle(ushort vehicleId)
        {
            if (vehicleId == 0)
            {
                return false;
            }

            var vehicleManager = Singleton<VehicleManager>.instance;
            if (vehicleManager == null)
            {
                return false;
            }

            ref Vehicle vehicle = ref vehicleManager.m_vehicles.m_buffer[vehicleId];
            return vehicle.Info?.m_class?.m_service == ItemClass.Service.PublicTransport;
        }

        internal static bool TryBuildOverlayData(ushort vehicleId, float trackedSeconds, out OverlayData data)
        {
            data = default(OverlayData);

            var vehicleManager = Singleton<VehicleManager>.instance;
            var transportManager = Singleton<TransportManager>.instance;
            if (vehicleManager == null || transportManager == null || vehicleId == 0)
            {
                return false;
            }

            var rootVehicleId = vehicleManager.m_vehicles.m_buffer[vehicleId].GetFirstVehicle(vehicleId);
            if (rootVehicleId != 0)
            {
                vehicleId = rootVehicleId;
            }

            ref Vehicle vehicle = ref vehicleManager.m_vehicles.m_buffer[vehicleId];
            if (vehicle.Info == null)
            {
                return false;
            }

            var lineId = vehicle.m_transportLine;
            var lineName = lineId != 0 ? transportManager.GetLineName(lineId) : Localization.Get("TRAINDISPLAY_NO_LINE");
            var nextStop = ResolveNextStopName(lineId, vehicle);
            var currentPassengers = 0;
            var capacity = 0;
            TryGetPassengerCounts(vehicleId, ref vehicle, out currentPassengers, out capacity);

            data.HasContent = true;
            data.Name = ResolveVehicleDisplayName(vehicleId, ref vehicle);
            data.Status = ResolveVehicleState(vehicle);
            data.Breadcrumb = BuildBreadcrumb(ref vehicle, lineName);
            data.NextStop = nextStop;
            data.Passengers = capacity > 0 ? $"{currentPassengers} / {capacity}" : currentPassengers.ToString(CultureInfo.InvariantCulture);
            data.Speed = FormatSpeed(ref vehicle);
            data.Elapsed = FormatElapsedTime(trackedSeconds);

            if (TrainDisplayRuntimeConfig.ColorTheme == ModSetting.TrainDisplayColorThemes.Original)
            {
                BuildRouteStrip(lineId, vehicle.m_targetBuilding, ref data);
            }

            return true;
        }

        /// <summary>Gathers the line's stop names and which one the vehicle is currently headed to,
        /// for the Original theme's route strip. Read-only: only walks the existing stop chain via
        /// TransportLine.GetNextStop, same as the game's own line UI does.</summary>
        private static void BuildRouteStrip(ushort lineId, ushort targetStop, ref OverlayData data)
        {
            if (lineId == 0)
            {
                data.RouteStopNames = new string[0];
                return;
            }

            var transportManager = Singleton<TransportManager>.instance;
            ref TransportLine line = ref transportManager.m_lines.m_buffer[lineId];
            data.LineColor = line.m_color;

            var firstStop = line.m_stops;
            if (firstStop == 0)
            {
                data.RouteStopNames = new string[0];
                return;
            }

            // Route strips on real displays show the current stop plus a handful of upcoming ones,
            // not the whole line - a big rail line's full stop list would not fit legibly in one
            // strip. Walking from the line's first stop and stopping after N regardless of where the
            // vehicle actually is meant a vehicle past stop N never had its position found at all
            // (currentIndex stayed -1) - starting the walk from the vehicle's own next stop instead
            // always puts "where it is now" at index 0.
            const int maxStops = 5; // current stop + next 4
            var names = new System.Collections.Generic.List<string>(maxStops);
            var currentIndex = 0;
            var startStop = targetStop != 0 ? targetStop : firstStop;
            var stop = startStop;
            var guard = 0;
            do
            {
                var id = new InstanceID { NetNode = stop };
                // Most stops on a line are never individually clicked by the player, so they would
                // never get named by AutoNameStopPatch (which only fires when a stop's own world info
                // panel is opened) and would permanently render as "?" here. Name them proactively
                // the first time the route strip needs to show one, same nearest-building fallback.
                var stopName = StopAutoNamer.EnsureNamed(id);
                names.Add(string.IsNullOrEmpty(stopName) ? "?" : stopName);

                stop = global::TransportLine.GetNextStop(stop);
                guard++;
            }
            while (stop != startStop && stop != 0 && names.Count < maxStops && guard < 32768);

            data.RouteStopNames = names.ToArray();
            data.RouteCurrentIndex = currentIndex;
        }

        // A 1x1 white pixel, tinted via GUI.color to draw a flat, accurate solid-colour box.
        // GUI.Box's default skin texture is not flat white, so tinting it via GUI.backgroundColor
        // (the previous approach) produced a muddied/incorrect result instead of the requested
        // colour - this was the "wrong colours" bug in the Simple/Dark/Light themes.
        private static Texture2D _solidTexture;

        private static Texture2D SolidTexture
        {
            get
            {
                if (_solidTexture == null)
                {
                    _solidTexture = new Texture2D(1, 1);
                    _solidTexture.SetPixel(0, 0, Color.white);
                    _solidTexture.Apply();
                }

                return _solidTexture;
            }
        }

        private static void DrawSolidBox(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, SolidTexture);
            GUI.color = previous;
        }

        internal static void DrawOverlay(OverlayData data)
        {
            if (!data.HasContent)
            {
                return;
            }

            EnsureGuiStyles();
            DrawUnifiedOverlay(data);
        }

        private static Color MultiplyAlpha(Color color, float alphaMultiplier) =>
            new Color(color.r, color.g, color.b, color.a * alphaMultiplier);

        /// <summary>
        /// Single layout for every theme - a header strip (name/status on the left, big speed
        /// readout centred, line/next-stop/passengers on the right) plus a route strip below it,
        /// coloured to match the vehicle's actual line colour, with a marker per upcoming stop and a
        /// chevron at the current one. Themes only ever change the header's background/text colour
        /// (see <see cref="TrainDisplayRuntimeConfig.BackgroundColor"/>/<c>TextColor</c>) - there is
        /// no separate simplified layout anymore, since having one meant Simple/Dark/Light looked
        /// like a completely different, smaller panel instead of a colour variant of the same one.
        /// </summary>
        private static void DrawUnifiedOverlay(OverlayData data)
        {
            var scale = Mathf.Clamp(TrainDisplayRuntimeConfig.OverlayScale, 0.75f, 2.0f);
            var opacity = GetOverlayOpacity();
            var headerRect = GetOverlayRect();
            var isOriginal = TrainDisplayRuntimeConfig.ColorTheme == ModSetting.TrainDisplayColorThemes.Original;
            var backgroundColor = isOriginal ? new Color(0.08f, 0.08f, 0.1f, 0.85f) : TrainDisplayRuntimeConfig.BackgroundColor;
            var textColor = isOriginal ? Color.white : TrainDisplayRuntimeConfig.TextColor;

            DrawSolidBox(headerRect, MultiplyAlpha(backgroundColor, opacity));

            var previousColor = GUI.color;
            var previousContentColor = GUI.contentColor;
            GUI.color = new Color(1f, 1f, 1f, opacity);
            GUI.contentColor = textColor;

            var padding = 14f * scale;
            var lineHeight = 22f * scale;
            var labelWidth = 90f * scale;
            var top = headerRect.y + padding;
            var leftColumn = headerRect.x + padding;
            var metricColumn = headerRect.x + headerRect.width * 0.46f;
            var rightColumn = headerRect.x + headerRect.width - (230f * scale);
            // How much room the name/status value actually has before it would run into the speed
            // readout - was a flat 240px regardless of available space, clipping longer vehicle/line
            // names even though there was room to spare.
            var nameFieldWidth = Mathf.Max(120f * scale, metricColumn - (leftColumn + labelWidth) - 10f * scale);

            GUI.Label(new Rect(leftColumn, top, labelWidth, lineHeight), Localization.Get("TRAINDISPLAY_LABEL_NAME"), _smallStyle);
            GUI.Label(new Rect(leftColumn + labelWidth, top, nameFieldWidth, lineHeight), data.Name, _headerStyle);
            GUI.Label(new Rect(leftColumn, top + lineHeight, labelWidth, lineHeight), Localization.Get("TRAINDISPLAY_LABEL_STATUS"), _smallStyle);
            GUI.Label(new Rect(leftColumn + labelWidth, top + lineHeight, nameFieldWidth, lineHeight), data.Status, _bodyStyle);

            GUI.Label(new Rect(metricColumn, top, 210f * scale, 44f * scale), data.Speed, _metricStyle);
            GUI.Label(new Rect(metricColumn + 6f, top + 46f * scale, 160f * scale, lineHeight), data.Elapsed, _smallStyle);

            GUI.Label(new Rect(rightColumn, top, 220f * scale, lineHeight), data.Breadcrumb, _headerStyle);
            GUI.Label(new Rect(rightColumn, top + lineHeight, 130f * scale, lineHeight), Localization.Get("VEHICLE_PANEL_STATUS_NEXT_STOP"), _smallStyle);
            GUI.Label(new Rect(rightColumn + 130f * scale, top + lineHeight, 90f * scale, lineHeight), data.NextStop, _valueStyle);
            GUI.Label(new Rect(rightColumn, top + lineHeight * 2f, 130f * scale, lineHeight), Localization.Get("VEHICLE_PANEL_PASSENGERS"), _smallStyle);
            GUI.Label(new Rect(rightColumn + 130f * scale, top + lineHeight * 2f, 90f * scale, lineHeight), data.Passengers, _valueStyle);

            DrawRouteStrip(data, headerRect, scale, opacity);

            GUI.contentColor = previousContentColor;
            GUI.color = previousColor;
        }

        private static void DrawRouteStrip(OverlayData data, Rect headerRect, float scale, float opacity)
        {
            if (data.RouteStopNames == null || data.RouteStopNames.Length == 0)
            {
                return;
            }

            var lineColor = new Color(data.LineColor.r / 255f, data.LineColor.g / 255f, data.LineColor.b / 255f, 1f);
            var stripWidth = 260f * scale;
            var badgeHeight = 46f * scale;
            var trackHeight = 40f * scale;
            var stripRect = new Rect(headerRect.x, headerRect.yMax, stripWidth, badgeHeight + trackHeight);

            // Solid white backing so semi-transparent line colours (e.g. a pale line) still read
            // clearly, matching the upstream mod's white-backed strip.
            DrawSolidBox(stripRect, new Color(1f, 1f, 1f, 0.9f * opacity));
            var badgeRect = new Rect(stripRect.x, stripRect.y, stripRect.width, badgeHeight);
            DrawSolidBox(badgeRect, new Color(lineColor.r, lineColor.g, lineColor.b, opacity));

            GUI.Label(new Rect(badgeRect.x + 8f, badgeRect.y + 4f, badgeRect.width - 16f, badgeHeight * 0.4f),
                Localization.Get("VEHICLE_PANEL_STATUS_NEXT_STOP"), _smallStyle);
            var nextStopName = data.RouteCurrentIndex >= 0 && data.RouteCurrentIndex < data.RouteStopNames.Length
                ? data.RouteStopNames[data.RouteCurrentIndex]
                : data.NextStop;
            GUI.Label(new Rect(badgeRect.x + 8f, badgeRect.y + badgeHeight * 0.35f, badgeRect.width - 16f, badgeHeight * 0.6f),
                nextStopName, _headerStyle);

            var trackRect = new Rect(stripRect.x, badgeRect.yMax, stripRect.width, trackHeight);
            var count = data.RouteStopNames.Length;
            var markerY = trackRect.y + trackHeight * 0.28f;
            var markerSize = 12f * scale;
            var trackLineHeight = 4f * scale;
            var trackLineRect = new Rect(trackRect.x + 6f, markerY + markerSize / 2f - trackLineHeight / 2f, trackRect.width - 12f, trackLineHeight);
            DrawSolidBox(trackLineRect, lineColor);

            for (var i = 0; i < count; i++)
            {
                var x = trackRect.x + 6f + (count == 1 ? 0f : i * (trackRect.width - 12f - markerSize) / (count - 1));
                var markerRect = new Rect(x, markerY, markerSize, markerSize);
                var passed = data.RouteCurrentIndex >= 0 && i <= data.RouteCurrentIndex;
                DrawSolidBox(markerRect, passed ? lineColor : Color.white);
                if (passed)
                {
                    // A thin ring around filled markers keeps them visible against a same-colour track.
                    GUI.Box(markerRect, GUIContent.none);
                }

                var nameRect = new Rect(x - 20f, markerY + markerSize + 2f, markerSize + 40f, trackHeight - markerSize - 4f);
                GUI.Label(nameRect, data.RouteStopNames[i], _tinyStyle);
            }
        }

        internal static Rect GetOverlayRect()
        {
            var scale = Mathf.Clamp(TrainDisplayRuntimeConfig.OverlayScale, 0.75f, 2.0f);
            var width = 820f * scale;
            var height = 150f * scale;
            var margin = 20f + TrainDisplayRuntimeConfig.Padding;
            var screen = new Vector2(Screen.width, Screen.height);
            var offset = TrainDisplayRuntimeConfig.OverlayOffset;

            switch (TrainDisplayRuntimeConfig.OverlayPosition)
            {
                case ModSetting.TrainDisplayOverlayPositions.TopRight:
                    return new Rect(screen.x - width - margin + offset.x, margin + offset.y, width, height);
                case ModSetting.TrainDisplayOverlayPositions.BottomLeft:
                    return new Rect(margin + offset.x, screen.y - height - margin + offset.y, width, height);
                case ModSetting.TrainDisplayOverlayPositions.BottomRight:
                    return new Rect(screen.x - width - margin + offset.x, screen.y - height - margin + offset.y, width, height);
                default:
                    return new Rect(margin + offset.x, margin + offset.y, width, height);
            }
        }

        internal static float GetOverlayOpacity() => Mathf.Clamp01(TrainDisplayRuntimeConfig.OverlayOpacity);
        internal static float GetUpdateInterval() => Mathf.Clamp(TrainDisplayRuntimeConfig.UpdateInterval, 0.05f, 2f);

        private static string BuildBreadcrumb(ref Vehicle vehicle, string lineName)
        {
            var transportLabel = ResolveTransportModeName(vehicle.Info);
            if ((TrainDisplayRuntimeConfig.VisibleFields & ModSetting.TrainDisplayFields.Line) == 0)
            {
                return transportLabel;
            }

            return $"{transportLabel} > {lineName}";
        }

        private static void EnsureGuiStyles()
        {
            var scale = Mathf.Clamp(TrainDisplayRuntimeConfig.OverlayScale, 0.75f, 2f);
            var textColor = TrainDisplayRuntimeConfig.TextColor;

            _headerStyle = _headerStyle ?? new GUIStyle(GUI.skin.label);
            _bodyStyle = _bodyStyle ?? new GUIStyle(GUI.skin.label);
            _metricStyle = _metricStyle ?? new GUIStyle(GUI.skin.label);
            _smallStyle = _smallStyle ?? new GUIStyle(GUI.skin.label);
            _valueStyle = _valueStyle ?? new GUIStyle(GUI.skin.label);
            _tinyStyle = _tinyStyle ?? new GUIStyle(GUI.skin.label);

            _headerStyle.fontSize = Mathf.RoundToInt(18f * scale);
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.normal.textColor = textColor;

            _bodyStyle.fontSize = Mathf.RoundToInt(17f * scale);
            _bodyStyle.normal.textColor = textColor;

            _metricStyle.fontSize = Mathf.RoundToInt(32f * scale);
            _metricStyle.fontStyle = FontStyle.Bold;
            _metricStyle.normal.textColor = textColor;

            _smallStyle.fontSize = Mathf.RoundToInt(12f * scale);
            _smallStyle.normal.textColor = new Color(textColor.r, textColor.g, textColor.b, 0.82f);

            _valueStyle.fontSize = Mathf.RoundToInt(16f * scale);
            _valueStyle.fontStyle = FontStyle.Bold;
            _valueStyle.alignment = TextAnchor.UpperRight;
            _valueStyle.normal.textColor = textColor;

            // Always dark, regardless of theme text colour: this style only labels the Original
            // theme's route strip, which is drawn on a solid white/light backing (see
            // DrawRouteStrip), so it needs to stay readable independent of the header's own colour.
            _tinyStyle.fontSize = Mathf.RoundToInt(10f * scale);
            _tinyStyle.alignment = TextAnchor.UpperCenter;
            _tinyStyle.wordWrap = true;
            _tinyStyle.normal.textColor = new Color(0.1f, 0.1f, 0.12f, 1f);
        }

        private static string FormatElapsedTime(float trackedSeconds)
        {
            var totalSeconds = Mathf.Max(0, Mathf.RoundToInt(trackedSeconds));
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", minutes, seconds);
        }

        private static string FormatSpeed(ref Vehicle vehicle)
        {
            try
            {
                var speed = vehicle.GetLastFrameVelocity().magnitude * 3.6f * 8f;
                if (ModSetting.Instance.SpeedUnit == ModSetting.VehicleSpeedUnits.MPH)
                {
                    speed *= 0.621371f;
                }

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.0} {1}",
                    speed,
                    ModSetting.Instance.SpeedString);
            }
            catch
            {
                return string.Format(CultureInfo.InvariantCulture, "0.0 {0}", ModSetting.Instance.SpeedString);
            }
        }

        private static string ResolveNextStopName(ushort lineId, Vehicle vehicle)
        {
            if ((TrainDisplayRuntimeConfig.VisibleFields & ModSetting.TrainDisplayFields.Destination) == 0)
            {
                return Localization.Get("TRAINDISPLAY_HIDDEN");
            }

            var stop = vehicle.m_targetBuilding;
            if (stop != 0)
            {
                var id = new InstanceID { NetNode = stop };
                var stopName = StopAutoNamer.EnsureNamed(id);
                if (!string.IsNullOrEmpty(stopName))
                {
                    return stopName;
                }
            }

            if (lineId == 0)
            {
                return Localization.Get("TRAINDISPLAY_NO_DESTINATION");
            }

            return Localization.Get("TRAINDISPLAY_NO_DESTINATION");
        }

        private static string ResolveTransportModeName(VehicleInfo info)
        {
            if (info?.m_class == null)
            {
                return Localization.Get("TRAINDISPLAY_VEHICLE");
            }

            // Was accidentally pulling from the TICKET_PRICE_* family (Economy panel labels like
            // "Ticket price of train: ") instead of a plain vehicle-type noun - same subService switch
            // shape as a ticket-price lookup nearby probably got copy-pasted without updating the
            // keys. INFO_PUBLICTRANSPORT_* are vanilla's own labels (already used successfully
            // elsewhere in this project - see the Delete Lines checkboxes in
            // UI/CSLModsCommonOptionsPanel.cs); the three subServices without a confirmed vanilla key
            // fall back to the existing generic "Vehicle" string rather than guessing a new one.
            switch (info.m_class.m_subService)
            {
                case ItemClass.SubService.PublicTransportBus:
                    return Localization.Get("INFO_PUBLICTRANSPORT_BUS");
                case ItemClass.SubService.PublicTransportTrolleybus:
                    return Localization.Get("INFO_PUBLICTRANSPORT_TROLLEYBUS");
                case ItemClass.SubService.PublicTransportTram:
                    return Localization.Get("INFO_PUBLICTRANSPORT_TRAM");
                case ItemClass.SubService.PublicTransportMetro:
                    return Localization.Get("INFO_PUBLICTRANSPORT_METRO");
                case ItemClass.SubService.PublicTransportTrain:
                    return Localization.Get("INFO_PUBLICTRANSPORT_TRAIN");
                case ItemClass.SubService.PublicTransportMonorail:
                    return Localization.Get("INFO_PUBLICTRANSPORT_MONORAIL");
                default:
                    return Localization.Get("TRAINDISPLAY_VEHICLE");
            }
        }

        private static string ResolveVehicleDisplayName(ushort vehicleId, ref Vehicle vehicle)
        {
            var id = new InstanceID { Vehicle = vehicleId };
            var customName = Singleton<InstanceManager>.instance.GetName(id);
            if (!string.IsNullOrEmpty(customName))
            {
                return customName;
            }

            return ResolveTransportModeName(vehicle.Info);
        }

        private static string ResolveVehicleState(Vehicle vehicle)
        {
            if ((TrainDisplayRuntimeConfig.VisibleFields & ModSetting.TrainDisplayFields.State) == 0)
            {
                return Localization.Get("TRAINDISPLAY_HIDDEN");
            }

            if ((vehicle.m_flags & Vehicle.Flags.GoingBack) != 0)
            {
                return Localization.Get("TRAINDISPLAY_STATE_RETURNING");
            }

            if ((vehicle.m_flags & Vehicle.Flags.Stopped) != 0)
            {
                return Localization.Get("TRAINDISPLAY_STATE_STOPPED");
            }

            if (vehicle.m_path != 0)
            {
                return Localization.Get("TRAINDISPLAY_STATE_EN_ROUTE");
            }

            return vehicle.m_transportLine != 0
                ? Localization.Get("TRAINDISPLAY_STATE_ON_LINE")
                : Localization.Get("TRAINDISPLAY_STATE_IDLE");
        }

        private static void TryGetPassengerCounts(ushort vehicleId, ref Vehicle vehicle, out int passengers, out int capacity)
        {
            passengers = 0;
            capacity = 0;

            try
            {
                vehicle.Info.m_vehicleAI.GetBufferStatus(vehicleId, ref vehicle, out _, out passengers, out capacity);
            }
            catch
            {
            }
        }
    }
}
