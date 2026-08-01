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
        private static GUIStyle _lineNameStyle;
        private static GUIStyle _stopLabelStyle;

        internal struct OverlayData
        {
            internal bool HasContent;
            /// <summary>Next stop / primary destination title (max 2 lines in UI).</summary>
            internal string Destination;
            /// <summary>Current line name (IPT change vs original "next" subtitle) — large italic.</summary>
            internal string LineName;
            internal Color32 LineColor;
            internal string[] RouteStopNames;
            internal int RouteCurrentIndex;
            // Optional extras (default off / unused in base panel; reserved for expand-up later).
            internal string Name;
            internal string Status;
            internal string Passengers;
            internal string Speed;
            internal string Elapsed;
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
            if (vehicle.Info?.m_class == null
                || vehicle.Info.m_class.m_service != ItemClass.Service.PublicTransport)
            {
                return false;
            }

            return IsVehicleTypeEnabled(vehicle.Info.m_class.m_subService);
        }

        private static bool IsVehicleTypeEnabled(ItemClass.SubService sub)
        {
            var mask = ModSetting.Instance.TrainDisplayEnabledVehicleTypes;
            if (mask == ModSetting.TrainDisplayVehicleTypes.None)
            {
                return false;
            }

            if (mask == ModSetting.TrainDisplayVehicleTypes.All)
            {
                return true;
            }

            ModSetting.TrainDisplayVehicleTypes bit;
            switch (sub)
            {
                case ItemClass.SubService.PublicTransportBus:
                    bit = ModSetting.TrainDisplayVehicleTypes.Bus;
                    break;
                case ItemClass.SubService.PublicTransportTrolleybus:
                    bit = ModSetting.TrainDisplayVehicleTypes.Trolleybus;
                    break;
                case ItemClass.SubService.PublicTransportTram:
                    bit = ModSetting.TrainDisplayVehicleTypes.Tram;
                    break;
                case ItemClass.SubService.PublicTransportMetro:
                    bit = ModSetting.TrainDisplayVehicleTypes.Metro;
                    break;
                case ItemClass.SubService.PublicTransportTrain:
                    bit = ModSetting.TrainDisplayVehicleTypes.Train;
                    break;
                case ItemClass.SubService.PublicTransportMonorail:
                    bit = ModSetting.TrainDisplayVehicleTypes.Monorail;
                    break;
                case ItemClass.SubService.PublicTransportShip:
                    bit = ModSetting.TrainDisplayVehicleTypes.Ship;
                    break;
                case ItemClass.SubService.PublicTransportPlane:
                    bit = ModSetting.TrainDisplayVehicleTypes.Plane;
                    break;
                case ItemClass.SubService.PublicTransportTaxi:
                    bit = ModSetting.TrainDisplayVehicleTypes.Taxi;
                    break;
                case ItemClass.SubService.PublicTransportCableCar:
                    bit = ModSetting.TrainDisplayVehicleTypes.CableCar;
                    break;
                case ItemClass.SubService.PublicTransportTours:
                    bit = ModSetting.TrainDisplayVehicleTypes.Tours;
                    break;
                default:
                    return true; // unknown PT subtypes still show
            }

            return (mask & bit) != 0;
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
            // No line (returning to depot / unassigned): hide the whole display.
            if (lineId == 0 || (vehicle.m_flags & Vehicle.Flags.GoingBack) != 0)
            {
                return false;
            }

            var lineName = transportManager.GetLineName(lineId);
            var terminal = ResolveTerminalStopName(lineId);

            data.HasContent = true;
            // Destination = line terminus (final station), not the next stop.
            data.Destination = string.IsNullOrEmpty(terminal)
                ? Localization.Get("TRAINDISPLAY_NO_DESTINATION")
                : terminal;
            data.LineName = lineName ?? string.Empty;
            data.Name = ResolveVehicleDisplayName(vehicleId, ref vehicle);
            data.Status = ResolveVehicleState(vehicle);

            // Always compute extras (cheap); strip only draws when ExtrasMask is on.
            TryGetPassengerCounts(vehicleId, ref vehicle, out var currentPassengers, out var capacity);
            data.Passengers = capacity > 0
                ? $"{currentPassengers} / {capacity}"
                : currentPassengers.ToString(CultureInfo.InvariantCulture);
            data.Speed = FormatSpeed(ref vehicle);
            data.Elapsed = FormatElapsedTime(trackedSeconds);

            BuildRouteStrip(lineId, vehicle.m_targetBuilding, ref data);
            return true;
        }

        // Reused route strip buffer — no List/ToArray allocation every poll (Train Display hot path).
        private static readonly string[] RouteStripScratch = new string[5];
        private static string[] _emptyRouteStops = new string[0];

        /// <summary>
        /// Current stop + next few names for the LCD route rail. Uses InstanceManager name first;
        /// auto-names at most one unnamed stop per poll so building scans cannot hitch the frame.
        /// </summary>
        private static void BuildRouteStrip(ushort lineId, ushort targetStop, ref OverlayData data)
        {
            if (lineId == 0)
            {
                data.RouteStopNames = _emptyRouteStops;
                data.RouteCurrentIndex = 0;
                return;
            }

            var transportManager = Singleton<TransportManager>.instance;
            ref TransportLine line = ref transportManager.m_lines.m_buffer[lineId];
            data.LineColor = line.m_color;

            var firstStop = line.m_stops;
            if (firstStop == 0)
            {
                data.RouteStopNames = _emptyRouteStops;
                data.RouteCurrentIndex = 0;
                return;
            }

            // 1 current + 4 upcoming = 5 total.
            const int maxStops = 5;
            var startStop = targetStop != 0 ? targetStop : firstStop;
            var stop = startStop;
            var guard = 0;
            var count = 0;
            var autoNamedThisPoll = 0;
            var instanceManager = Singleton<InstanceManager>.instance;
            do
            {
                var id = new InstanceID { NetNode = stop };
                var stopName = instanceManager.GetName(id);
                if (string.IsNullOrEmpty(stopName) && autoNamedThisPoll < 1)
                {
                    // Cap to one EnsureNamed (spatial scan) per poll — unnamed lines used to
                    // scan up to 5 stops every interval and hitch.
                    stopName = StopAutoNamer.EnsureNamed(id);
                    if (!string.IsNullOrEmpty(stopName))
                    {
                        autoNamedThisPoll++;
                    }
                }

                RouteStripScratch[count++] = string.IsNullOrEmpty(stopName) ? "?" : stopName;
                stop = global::TransportLine.GetNextStop(stop);
                guard++;
            }
            while (stop != startStop && stop != 0 && count < maxStops && guard < 64);

            // Copy into a stable array owned by this overlay frame (scratch is reused next poll).
            var result = new string[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = RouteStripScratch[i];
            }

            data.RouteStopNames = result;
            data.RouteCurrentIndex = 0; // startStop is "current" (approaching)
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
            // ONE panel only (Workshop 3233229958 style). Themes only recolour the face —
            // never a second full-screen HUD bar.
            DrawSingleCornerPanel(data);
        }

        private static Color MultiplyAlpha(Color color, float alphaMultiplier) =>
            new Color(color.r, color.g, color.b, color.a * alphaMultiplier);

        /// <summary>
        /// Single corner LCD (Workshop 3233229958 style). No second top-of-screen panel.
        /// Themes only recolour the face. Destination = line terminus; line name italic full width.
        /// </summary>
        private static void DrawSingleCornerPanel(OverlayData data)
        {
            var scale = TrainDisplayRuntimeConfig.DrawScale; // 100% user = former ~165%
            var opacity = GetOverlayOpacity();
            var face = TrainDisplayRuntimeConfig.PanelFaceColor;
            var ink = TrainDisplayRuntimeConfig.PanelInkColor;
            var lineColor = new Color(data.LineColor.r / 255f, data.LineColor.g / 255f, data.LineColor.b / 255f, 1f);

            var fields = TrainDisplayRuntimeConfig.VisibleFields;
            // Honour Options checkboxes — never hardcode showDest/showLine true (that made
            // "Mostrar nome da linha" / "Mostrar destino" appear broken in the menu).
            var showDest = (fields & ModSetting.TrainDisplayFields.Destination) != 0;
            var showLine = (fields & ModSetting.TrainDisplayFields.Line) != 0
                           && !string.IsNullOrEmpty(data.LineName);
            var extrasOn = TrainDisplayRuntimeConfig.ShowExtrasStrip;

            var panelW = 320f * scale;
            // Shrink base panel when destination / line name are hidden so empty space
            // does not remain under the extras strip.
            var panelH = 72f * scale; // route rail + padding minimum
            if (showDest)
            {
                panelH += 44f * scale;
            }

            if (showLine)
            {
                panelH += 26f * scale;
            }

            // Compact extras strip height (not too tall — first version).
            var extrasH = extrasOn ? 52f * scale : 0f;
            var totalH = panelH + extrasH;
            var panelRect = GetCornerPanelRect(panelW, totalH);
            // Base LCD sits at the bottom; extras strip (if any) stacks above it, same width.
            var baseRect = new Rect(panelRect.x, panelRect.y + extrasH, panelW, panelH);
            var extrasRect = extrasOn
                ? new Rect(panelRect.x, panelRect.y, panelW, extrasH)
                : default(Rect);

            var pad = 6f * scale;
            var stripW = 36f * scale;
            var contentLeft = baseRect.x + stripW + pad;
            var contentRight = baseRect.xMax - pad;
            var contentW = Mathf.Max(8f, contentRight - contentLeft);
            var contentTop = baseRect.y + pad;
            var contentBottom = baseRect.yMax - pad;

            var previousColor = GUI.color;
            var previousContentColor = GUI.contentColor;

            // —— Optional extras strip (line colour) — only when extras options are on ——
            if (extrasOn)
            {
                DrawSolidBox(extrasRect, MultiplyAlpha(lineColor, Mathf.Clamp01(opacity + 0.05f)));
                // Soft join: thin darker edge between extras and base.
                DrawSolidBox(new Rect(extrasRect.x, extrasRect.yMax - 2f * scale, panelW, 2f * scale),
                    MultiplyAlpha(new Color(0f, 0f, 0f, 0.25f), opacity));

                GUI.color = new Color(1f, 1f, 1f, opacity);
                GUI.contentColor = Color.white;
                _smallStyle.fontSize = Mathf.RoundToInt(11f * scale);
                _smallStyle.normal.textColor = Color.white;
                _smallStyle.alignment = TextAnchor.MiddleLeft;
                _valueStyle.fontSize = Mathf.RoundToInt(13f * scale);
                _valueStyle.fontStyle = FontStyle.Bold;
                _valueStyle.normal.textColor = Color.white;
                _valueStyle.alignment = TextAnchor.MiddleLeft;

                var labels = new string[4];
                var values = new string[4];
                var n = 0;
                if ((fields & ModSetting.TrainDisplayFields.State) != 0 && !string.IsNullOrEmpty(data.Status) && n < 4)
                {
                    labels[n] = Localization.Get("TRAINDISPLAY_LABEL_STATUS");
                    values[n++] = data.Status;
                }

                if ((fields & ModSetting.TrainDisplayFields.Speed) != 0 && !string.IsNullOrEmpty(data.Speed) && n < 4)
                {
                    labels[n] = Localization.Get("TRAINDISPLAY_LABEL_SPEED");
                    values[n++] = data.Speed;
                }

                if ((fields & ModSetting.TrainDisplayFields.Passengers) != 0 && !string.IsNullOrEmpty(data.Passengers) && n < 4)
                {
                    labels[n] = Localization.Get("VEHICLE_PANEL_PASSENGERS");
                    values[n++] = data.Passengers;
                }

                if ((fields & ModSetting.TrainDisplayFields.Elapsed) != 0 && !string.IsNullOrEmpty(data.Elapsed) && n < 4)
                {
                    labels[n] = Localization.Get("TRAINDISPLAY_LABEL_TIME");
                    values[n++] = data.Elapsed;
                }

                // No fallback fill — if the player disabled every extras field that produces a
                // value, leave the strip empty rather than re-showing speed/name against their will.

                var ex = extrasRect.x + pad;
                var ey = extrasRect.y + pad * 0.5f;
                var colW = (panelW - pad * 2f) * 0.5f;
                var rowH = 20f * scale;
                for (var i = 0; i < n; i++)
                {
                    var col = i % 2;
                    var row = i / 2;
                    var cellX = ex + col * colW;
                    var cellY = ey + row * rowH;
                    GUI.Label(new Rect(cellX, cellY, colW * 0.42f, rowH), labels[i], _smallStyle);
                    GUI.Label(new Rect(cellX + colW * 0.42f, cellY, colW * 0.55f, rowH), values[i], _valueStyle);
                }
            }

            // —— Base LCD (theme face colour) ——
            DrawSolidBox(baseRect, face);
            DrawSolidBox(new Rect(baseRect.x, baseRect.y, stripW, panelH), MultiplyAlpha(lineColor, opacity));

            GUI.color = new Color(1f, 1f, 1f, opacity);
            GUI.contentColor = ink;

            var y = contentTop;

            if (showDest)
            {
                _headerStyle.fontSize = Mathf.RoundToInt(20f * scale);
                _headerStyle.fontStyle = FontStyle.Bold;
                _headerStyle.normal.textColor = ink;
                _headerStyle.wordWrap = true;
                _headerStyle.alignment = TextAnchor.UpperLeft;
                _headerStyle.clipping = TextClipping.Clip;
                var destH = 42f * scale;
                GUI.Label(new Rect(contentLeft, y, contentW, destH), TruncateToLines(data.Destination, 2, 28), _headerStyle);
                y += destH + 2f * scale;
            }

            if (showLine)
            {
                _lineNameStyle.fontSize = Mathf.RoundToInt(16f * scale);
                _lineNameStyle.fontStyle = FontStyle.Italic | FontStyle.Bold;
                _lineNameStyle.normal.textColor = ink;
                _lineNameStyle.wordWrap = true;
                _lineNameStyle.clipping = TextClipping.Clip;
                var lineH = 24f * scale;
                GUI.Label(new Rect(contentLeft, y, contentW, lineH), TruncateToLines(data.LineName, 1, 36), _lineNameStyle);
                y += lineH + 2f * scale;
            }

            var railArea = new Rect(contentLeft, y, contentW, Mathf.Max(8f, contentBottom - y));
            DrawRouteRail(data, railArea, scale, opacity, ink, lineColor);

            GUI.contentColor = previousContentColor;
            GUI.color = previousColor;
        }

        private static void DrawRouteRail(OverlayData data, Rect area, float scale, float opacity, Color ink, Color lineColor)
        {
            var names = data.RouteStopNames;
            if (names == null || names.Length == 0)
            {
                return;
            }

            var count = names.Length;
            var markerSize = 12f * scale;
            var trackH = 8f * scale;
            // Leave room under the track for vertical labels (text must not sit on the dots).
            var gapBelowMarker = 5f * scale;
            var trackY = area.y + 2f * scale;
            var trackLeft = area.x + markerSize * 0.5f;
            var trackWidth = Mathf.Max(markerSize, area.width - markerSize);
            var trackRect = new Rect(trackLeft, trackY + markerSize * 0.5f - trackH * 0.5f, trackWidth, trackH);
            DrawSolidBox(trackRect, MultiplyAlpha(lineColor, opacity));

            // Vertical text column: starts AFTER the gap under the marker, ends at panel bottom.
            var textTop = trackY + markerSize + gapBelowMarker;
            var textBottom = area.yMax - 2f * scale;
            var textRun = Mathf.Max(16f * scale, textBottom - textTop); // vertical length available
            // Thickness of the rotated strip ≈ room for 2 lines of glyphs side-by-side after wrap.
            var fontPx = Mathf.RoundToInt(10f * scale);
            var twoLineThick = fontPx * 2.4f + 4f * scale;
            var slotW = count <= 1 ? area.width : trackWidth / (count - 1);
            var stripThick = Mathf.Min(twoLineThick, Mathf.Max(14f * scale, slotW - 4f * scale));

            var nextIndex = data.RouteCurrentIndex + 1;

            for (var i = 0; i < count; i++)
            {
                var t = count == 1 ? 0f : i / (float)(count - 1);
                var cx = trackLeft + t * trackWidth;
                cx = Mathf.Clamp(cx, area.x + markerSize * 0.5f, area.xMax - markerSize * 0.5f);

                var markerRect = new Rect(cx - markerSize * 0.5f, trackY, markerSize, markerSize);
                var isCurrent = i == data.RouteCurrentIndex;
                var isNext = i == nextIndex;
                var isPast = i < data.RouteCurrentIndex;
                var fill = isCurrent || isPast
                    ? MultiplyAlpha(lineColor, opacity)
                    : MultiplyAlpha(new Color(0.78f, 0.78f, 0.82f, 1f), opacity);
                DrawSolidBox(markerRect, fill);

                if (isCurrent && i + 1 < count)
                {
                    var nextT = (i + 1) / (float)(count - 1);
                    var nx = trackLeft + nextT * trackWidth;
                    var midX = (cx + nx) * 0.5f;
                    var arrowCol = new Color(0.9f, 0.15f, 0.12f, opacity);
                    DrawSolidBox(new Rect(midX - 6f * scale, trackY + markerSize * 0.5f - 3.5f * scale, 12f * scale, 7f * scale), arrowCol);
                    DrawSolidBox(new Rect(midX + 4f * scale, trackY + markerSize * 0.5f - 6f * scale, 5f * scale, 5f * scale), arrowCol);
                    DrawSolidBox(new Rect(midX + 4f * scale, trackY + markerSize * 0.5f + 1.5f * scale, 5f * scale, 5f * scale), arrowCol);
                }

                // —— Vertical label ——
                // Unrotated: wrap to 2 lines in a tall thin box (width = vertical run, height = 2 lines).
                // Then rotate +90° so text stands upright under the stop, starting BELOW the marker
                // (gap) and growing downward — never overlaps the dots.
                var charsPerLine = Mathf.Max(4, Mathf.FloorToInt(textRun / (fontPx * 0.62f)));
                var label = WrapTwoLines(names[i], charsPerLine);

                _stopLabelStyle.fontSize = fontPx;
                _stopLabelStyle.fontStyle = isNext ? FontStyle.Bold : FontStyle.Normal;
                _stopLabelStyle.normal.textColor = ink;
                _stopLabelStyle.alignment = TextAnchor.UpperLeft;
                _stopLabelStyle.wordWrap = true;
                _stopLabelStyle.clipping = TextClipping.Clip;

                // Pivot = top-center of vertical column, just under the marker (+ gap).
                var pivot = new Vector2(cx, textTop);
                var saved = GUI.matrix;
                // +90°: unrotated +x becomes up-screen in Unity GUI (y-down); we place the rect so
                // text reads top→bottom under the stop after rotation.
                GUIUtility.RotateAroundPivot(90f, pivot);
                // After +90 around pivot: rect (pivot.x, pivot.y - thick/2, run, thick) maps to a
                // vertical band under the marker. Width=textRun becomes the vertical extent.
                var textRect = new Rect(pivot.x, pivot.y - stripThick * 0.5f, textRun, stripThick);
                // Keep strip inside white content (horizontal clamp via thick half-width).
                if (textRect.y < area.y)
                {
                    textRect.y = area.y;
                }

                if (textRect.yMax > area.yMax)
                {
                    textRect.height = Mathf.Max(4f, area.yMax - textRect.y);
                }

                GUI.Label(textRect, label, _stopLabelStyle);
                GUI.matrix = saved;
            }
        }

        /// <summary>
        /// Split into at most 2 lines by character budget; leftover ends with ….
        /// </summary>
        private static string WrapTwoLines(string text, int charsPerLine)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            charsPerLine = Mathf.Max(3, charsPerLine);
            if (text.Length <= charsPerLine)
            {
                return text;
            }

            // Prefer break at space near the middle of first line.
            var breakAt = charsPerLine;
            for (var i = Math.Min(charsPerLine, text.Length - 1); i >= charsPerLine / 2; i--)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    breakAt = i;
                    break;
                }
            }

            var line1 = text.Substring(0, breakAt).TrimEnd();
            var rest = text.Substring(breakAt).TrimStart();
            if (rest.Length <= charsPerLine)
            {
                return line1 + "\n" + rest;
            }

            return line1 + "\n" + rest.Substring(0, charsPerLine - 1).TrimEnd() + "…";
        }

        /// <summary>Soft-wrap budget by chars/line; if still too long, end with …</summary>
        private static string TruncateToLines(string text, int maxLines, int charsPerLine)
        {
            if (string.IsNullOrEmpty(text) || maxLines < 1)
            {
                return text ?? string.Empty;
            }

            var budget = maxLines * Mathf.Max(4, charsPerLine);
            if (text.Length <= budget)
            {
                return text;
            }

            if (budget <= 1)
            {
                return "…";
            }

            return text.Substring(0, budget - 1).TrimEnd() + "…";
        }

        internal static Rect GetCornerPanelRect(float width, float height)
        {
            var margin = 16f + TrainDisplayRuntimeConfig.Padding;
            var screen = new Vector2(Screen.width, Screen.height);
            var offset = TrainDisplayRuntimeConfig.OverlayOffset;

            switch (TrainDisplayRuntimeConfig.OverlayPosition)
            {
                case ModSetting.TrainDisplayOverlayPositions.TopRight:
                    return new Rect(screen.x - width - margin + offset.x, margin + offset.y, width, height);
                case ModSetting.TrainDisplayOverlayPositions.TopLeft:
                    return new Rect(margin + offset.x, margin + offset.y, width, height);
                case ModSetting.TrainDisplayOverlayPositions.BottomRight:
                    return new Rect(screen.x - width - margin + offset.x, screen.y - height - margin + offset.y, width, height);
                default:
                    return new Rect(margin + offset.x, screen.y - height - margin + offset.y, width, height);
            }
        }

        internal static Rect GetOverlayRect(float extraHeight = 0f)
        {
            var scale = TrainDisplayRuntimeConfig.DrawScale;
            return GetCornerPanelRect(320f * scale, 150f * scale + extraHeight);
        }

        internal static float GetOverlayOpacity() => Mathf.Clamp01(TrainDisplayRuntimeConfig.OverlayOpacity);
        // Floor 0.1s — sub-100ms polls were part of the 4.8 freeze reports.
        internal static float GetUpdateInterval() => Mathf.Clamp(TrainDisplayRuntimeConfig.UpdateInterval, 0.1f, 2f);

        private static void EnsureGuiStyles()
        {
            _headerStyle = _headerStyle ?? new GUIStyle(GUI.skin.label);
            _bodyStyle = _bodyStyle ?? new GUIStyle(GUI.skin.label);
            _metricStyle = _metricStyle ?? new GUIStyle(GUI.skin.label);
            _smallStyle = _smallStyle ?? new GUIStyle(GUI.skin.label);
            _valueStyle = _valueStyle ?? new GUIStyle(GUI.skin.label);
            _tinyStyle = _tinyStyle ?? new GUIStyle(GUI.skin.label);
            _lineNameStyle = _lineNameStyle ?? new GUIStyle(GUI.skin.label);
            _stopLabelStyle = _stopLabelStyle ?? new GUIStyle(GUI.skin.label);
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
                // CS1: GetLastFrameVelocity is in game units ≈ m/s. Convert with * 3.6 for km/h.
                // The old * 8f was wrong and showed ~230–300 km/h on trams/buses (Workshop
                // 3233229958 original shows ~30–60 km/h for the same motion).
                var speed = vehicle.GetLastFrameVelocity().magnitude * 3.6f;

                // Soft-cap to prefab max (vanilla UI uses ≈ m_maxSpeed * 5 as km/h) so spikes
                // from pathfinding/teleport never read as 200+ km/h city buses.
                if (vehicle.Info != null && vehicle.Info.m_maxSpeed > 0.01f)
                {
                    var maxKmh = vehicle.Info.m_maxSpeed * 5f * 1.15f;
                    if (speed > maxKmh)
                    {
                        speed = maxKmh;
                    }
                }

                if (ModSetting.Instance.SpeedUnit == ModSetting.VehicleSpeedUnits.MPH)
                {
                    speed *= 0.621371f;
                }

                if (speed < 0f)
                {
                    speed = 0f;
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

        /// <summary>Line terminus (final station) — not the next intermediate stop.</summary>
        private static string ResolveTerminalStopName(ushort lineId)
        {
            if (lineId == 0)
            {
                return string.Empty;
            }

            try
            {
                ref var line = ref Singleton<TransportManager>.instance.m_lines.m_buffer[lineId];
                var terminal = line.GetLastStop();
                if (terminal == 0)
                {
                    terminal = line.m_stops;
                }

                if (terminal == 0)
                {
                    return string.Empty;
                }

                var id = new InstanceID { NetNode = terminal };
                var name = Singleton<InstanceManager>.instance.GetName(id);
                if (string.IsNullOrEmpty(name))
                {
                    name = StopAutoNamer.EnsureNamed(id);
                }

                return name ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ResolveNextStopName(ushort lineId, Vehicle vehicle)
        {
            if ((TrainDisplayRuntimeConfig.VisibleFields & ModSetting.TrainDisplayFields.Destination) == 0)
            {
                return string.Empty;
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
            // Hidden field = omit entirely (empty), never show "Oculto"/HIDDEN as placeholder text.
            if ((TrainDisplayRuntimeConfig.VisibleFields & ModSetting.TrainDisplayFields.State) == 0)
            {
                return string.Empty;
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
