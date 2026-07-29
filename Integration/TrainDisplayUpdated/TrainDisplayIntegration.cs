// <copyright file="TrainDisplayIntegration.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the GPL-3.0 license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace ImprovedPublicTransport.Integration.TrainDisplayUpdated
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using ColossalFramework;
    using UnityEngine;
    using Utils = ImprovedPublicTransport.Util.Utils;

    internal static class TrainDisplayIntegration
    {
        private static readonly BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static FieldInfo _cameraModeField;
        private static PropertyInfo _cameraModeProperty;
        private static FieldInfo _cameraTargetField;
        private static MethodInfo _cameraTargetMethod;
        private static PropertyInfo _cameraTargetProperty;
        private static bool _cached;
        private static GUIStyle _bodyStyle;
        private static GUIStyle _headerStyle;
        private static GUIStyle _metricStyle;
        private static GUIStyle _smallStyle;
        private static GUIStyle _valueStyle;

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
        }

        public static void EnsureCached()
        {
            if (_cached)
            {
                return;
            }

            _cached = true;

            try
            {
                var controller = ToolsModifierControl.cameraController;
                if (controller == null)
                {
                    return;
                }

                var type = controller.GetType();
                // CameraController.m_targetInstance is the field the game actually uses for the
                // followed instance, and it is PRIVATE - the generic "target"/"GetTarget" probes
                // below never matched it, so the overlay could never find the followed vehicle.
                // Look it up by its real name first, walking up the hierarchy in case a camera mod
                // subclasses CameraController.
                for (var t = type; t != null && _cameraTargetField == null; t = t.BaseType)
                {
                    _cameraTargetField = t.GetField("m_targetInstance", InstanceFlags);
                }

                _cameraTargetProperty = type.GetProperty("target", InstanceFlags) ?? type.GetProperty("m_target", InstanceFlags);
                if (_cameraTargetField == null)
                {
                    _cameraTargetField = type.GetField("target", InstanceFlags) ?? type.GetField("m_target", InstanceFlags);
                }
                _cameraTargetMethod = type.GetMethod("GetTarget", InstanceFlags, null, Type.EmptyTypes, null);
                _cameraModeProperty = type.GetProperty("cameraMode", InstanceFlags) ?? type.GetProperty("m_cameraMode", InstanceFlags);
                _cameraModeField = type.GetField("cameraMode", InstanceFlags) ?? type.GetField("m_cameraMode", InstanceFlags);
            }
            catch (Exception ex)
            {
                Utils.LogError($"TrainDisplayUpdated: failed to inspect camera controller: {ex.Message}");
            }
        }

        internal static bool IsFirstPersonCameraActive()
        {
            if (!TrainDisplayRuntimeConfig.FirstPersonOnly)
            {
                return true;
            }

            EnsureCached();

            try
            {
                var controller = ToolsModifierControl.cameraController;
                if (controller == null)
                {
                    return false;
                }

                var typeName = controller.GetType().Name;
                if (typeName.IndexOf("first", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                object mode = null;
                if (_cameraModeProperty != null)
                {
                    mode = _cameraModeProperty.GetValue(controller, null);
                }
                else if (_cameraModeField != null)
                {
                    mode = _cameraModeField.GetValue(controller);
                }

                if (mode != null)
                {
                    var modeText = mode.ToString();
                    if (!string.IsNullOrEmpty(modeText) && modeText.IndexOf("first", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Utils.LogError($"TrainDisplayUpdated: failed to inspect camera mode: {ex.Message}");
                return false;
            }
        }

        internal static bool TryGetFollowTarget(out InstanceID target)
        {
            EnsureCached();
            target = default(InstanceID);

            try
            {
                var controller = ToolsModifierControl.cameraController;
                if (controller == null)
                {
                    return false;
                }

                object value = null;
                if (_cameraTargetMethod != null)
                {
                    value = _cameraTargetMethod.Invoke(controller, null);
                }
                else if (_cameraTargetProperty != null)
                {
                    value = _cameraTargetProperty.GetValue(controller, null);
                }
                else if (_cameraTargetField != null)
                {
                    value = _cameraTargetField.GetValue(controller);
                }

                if (value == null)
                {
                    return false;
                }

                if (value is InstanceID instanceId)
                {
                    target = instanceId;
                    return target.Vehicle != 0;
                }

                var valueType = value.GetType();
                var vehicleField = valueType.GetField("m_vehicle", InstanceFlags) ?? valueType.GetField("vehicle", InstanceFlags) ?? valueType.GetField("Vehicle", InstanceFlags);
                if (vehicleField == null)
                {
                    return false;
                }

                target = new InstanceID { Vehicle = Convert.ToUInt16(vehicleField.GetValue(value), CultureInfo.InvariantCulture) };
                return target.Vehicle != 0;
            }
            catch (Exception ex)
            {
                Utils.LogError($"TrainDisplayUpdated: failed to inspect follow target: {ex.Message}");
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
            var info = vehicle.Info;
            return info?.m_class?.m_service == ItemClass.Service.PublicTransport
                || info?.m_class?.m_service == ItemClass.Service.Road;
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
            return true;
        }

        internal static void DrawOverlay(OverlayData data)
        {
            if (!data.HasContent)
            {
                return;
            }

            EnsureGuiStyles();

            var rect = GetOverlayRect();
            var previousColor = GUI.color;
            var previousContentColor = GUI.contentColor;
            var previousBackgroundColor = GUI.backgroundColor;

            GUI.color = new Color(1f, 1f, 1f, TrainDisplayIntegration.GetOverlayOpacity());
            GUI.backgroundColor = TrainDisplayRuntimeConfig.BackgroundColor;
            GUI.contentColor = TrainDisplayRuntimeConfig.TextColor;
            GUI.Box(rect, GUIContent.none);

            var padding = 18f * TrainDisplayRuntimeConfig.OverlayScale;
            var lineHeight = 24f * TrainDisplayRuntimeConfig.LineSpacing;
            var labelWidth = 110f * TrainDisplayRuntimeConfig.OverlayScale;
            var top = rect.y + padding;
            var leftColumn = rect.x + padding;
            var metricColumn = rect.x + rect.width * 0.46f;
            var rightColumn = rect.x + rect.width - (235f * TrainDisplayRuntimeConfig.OverlayScale);

            GUI.Label(new Rect(leftColumn, top, labelWidth, lineHeight), Localization.Get("TRAINDISPLAY_LABEL_NAME"), _smallStyle);
            GUI.Label(new Rect(leftColumn + labelWidth, top, 250f * TrainDisplayRuntimeConfig.OverlayScale, lineHeight), data.Name, _headerStyle);

            GUI.Label(new Rect(leftColumn, top + lineHeight, labelWidth, lineHeight), Localization.Get("TRAINDISPLAY_LABEL_STATUS"), _smallStyle);
            GUI.Label(new Rect(leftColumn + labelWidth, top + lineHeight, 250f * TrainDisplayRuntimeConfig.OverlayScale, lineHeight), data.Status, _bodyStyle);

            GUI.Label(new Rect(metricColumn, top, 210f * TrainDisplayRuntimeConfig.OverlayScale, 48f * TrainDisplayRuntimeConfig.OverlayScale), data.Speed, _metricStyle);
            GUI.Label(new Rect(metricColumn + 6f, top + 50f * TrainDisplayRuntimeConfig.OverlayScale, 160f * TrainDisplayRuntimeConfig.OverlayScale, lineHeight), data.Elapsed, _smallStyle);

            GUI.Label(new Rect(rightColumn, top, 220f * TrainDisplayRuntimeConfig.OverlayScale, lineHeight), data.Breadcrumb, _headerStyle);
            GUI.Label(new Rect(rightColumn, top + lineHeight, 130f * TrainDisplayRuntimeConfig.OverlayScale, lineHeight), Localization.Get("VEHICLE_PANEL_STATUS_NEXT_STOP"), _smallStyle);
            GUI.Label(new Rect(rightColumn + 130f * TrainDisplayRuntimeConfig.OverlayScale, top + lineHeight, 90f * TrainDisplayRuntimeConfig.OverlayScale, lineHeight), data.NextStop, _valueStyle);
            GUI.Label(new Rect(rightColumn, top + lineHeight * 2f, 130f * TrainDisplayRuntimeConfig.OverlayScale, lineHeight), Localization.Get("VEHICLE_PANEL_PASSENGERS"), _smallStyle);
            GUI.Label(new Rect(rightColumn + 130f * TrainDisplayRuntimeConfig.OverlayScale, top + lineHeight * 2f, 90f * TrainDisplayRuntimeConfig.OverlayScale, lineHeight), data.Passengers, _valueStyle);

            GUI.contentColor = previousContentColor;
            GUI.backgroundColor = previousBackgroundColor;
            GUI.color = previousColor;
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
                var stopName = Singleton<InstanceManager>.instance.GetName(id);
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

            switch (info.m_class.m_subService)
            {
                case ItemClass.SubService.PublicTransportBus:
                    return Localization.Get("TICKET_PRICE_BUS");
                case ItemClass.SubService.PublicTransportTrolleybus:
                    return Localization.Get("TICKET_PRICE_TROLLEYBUS");
                case ItemClass.SubService.PublicTransportTram:
                    return Localization.Get("TICKET_PRICE_TRAM");
                case ItemClass.SubService.PublicTransportMetro:
                    return Localization.Get("TICKET_PRICE_METRO");
                case ItemClass.SubService.PublicTransportTrain:
                    return Localization.Get("TICKET_PRICE_TRAIN");
                case ItemClass.SubService.PublicTransportMonorail:
                    return Localization.Get("TICKET_PRICE_MONORAIL");
                case ItemClass.SubService.PublicTransportCableCar:
                    return Localization.Get("TICKET_PRICE_CABLECAR");
                case ItemClass.SubService.PublicTransportShip:
                    return Localization.Get("TICKET_PRICE_SHIP");
                case ItemClass.SubService.PublicTransportPlane:
                    return Localization.Get("TICKET_PRICE_PLANE");
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
