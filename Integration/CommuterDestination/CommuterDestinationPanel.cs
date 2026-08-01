// Adapted from Commuter Destination (MIT, Workshop 2475986859) - see LICENSE.txt.
// Secondary panel only: opened from the Destinations button, not auto on every stop click.
// No Prev/Next (main stop panel already has those). X (Close) dismisses panel fully.
using System;
using System.Text;
using AlgernonCommons.UI;
using ColossalFramework;
using ColossalFramework.UI;
using ImprovedPublicTransport;
using ImprovedPublicTransport.UI;
using ImprovedPublicTransport.Util;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace CommuterDestination
{
    /// <summary>Secondary list of destination buildings for a stop (map icons are independent).</summary>
    internal sealed class CommuterDestinationPanel : StandalonePanel
    {
        private const float PanelContentWidth = 320f;
        private const float PanelContentHeight = 120f;
        private const float LabelsTopY = 40f;
        private const float LabelGap = 4f;
        private const float BottomMargin = 12f;

        private UILabel _lineNameLabel;
        private UILabel _stopLabel;
        private UILabel _destinationsLabel;
        private ushort _stopId;
        private int _framesSinceRefresh;
        private float _dynamicHeight = PanelContentHeight;

        private string _cachedStopName = string.Empty;
        private ushort _cachedStopNameId;
        private string _cachedHeader;
        private string _cachedNone;
        private string _lastDestinationsText = string.Empty;

        private readonly StringBuilder _destSb = new StringBuilder(256);
        private readonly UILabel[] _reflowLabels = new UILabel[3];

        /// <summary>User closed with X — do not auto-retarget until Destinations is clicked again.</summary>
        public static bool UserDismissed { get; private set; }

        /// <summary>Mark secondary list dismissed (toggle button / soft hide). Icons may remain while stop is open.</summary>
        public static void UserDismissedSet()
        {
            UserDismissed = true;
        }

        public static CommuterDestinationPanel Instance => StandalonePanelManager<CommuterDestinationPanel>.Panel;

        public override float PanelWidth => PanelContentWidth;

        public override float PanelHeight => _dynamicHeight;

        protected override string PanelTitle => Localization.Get("COMMUTER_DESTINATION_PANEL_TITLE");

        public DestinationGraph Graph { get; private set; }

        public ushort CurrentStopId => _stopId;

        public static void ShowForStop(ushort stopId)
        {
            try
            {
                UserDismissed = false;
                StandalonePanelManager<CommuterDestinationPanel>.Create();
                var panel = StandalonePanelManager<CommuterDestinationPanel>.Panel;
                if (panel == null)
                {
                    Utils.LogError("CommuterDestination: panel Create returned null.");
                    return;
                }

                panel.Show(stopId);
            }
            catch (Exception ex)
            {
                Utils.LogError($"CommuterDestination: ShowForStop failed: {ex.Message}");
            }
        }

        public static void CloseIfOpen()
        {
            try
            {
                var p = StandalonePanelManager<CommuterDestinationPanel>.Panel;
                if (p != null)
                {
                    p.Hide();
                }
            }
            catch
            {
                // non-fatal
            }
        }

        /// <summary>X button path: destroy panel and mark dismissed (icons may stay while stop open).</summary>
        protected override bool PreClose()
        {
            UserDismissed = true;
            _stopId = 0;
            isVisible = false;
            return true;
        }

        public override void Awake()
        {
            base.Awake();

            try
            {
                _lineNameLabel = UILabels.AddLabel(this, Margin, LabelsTopY, string.Empty, PanelContentWidth - Margin * 2);
                // Stop name + waiting count on one line (not a separate "destination" header for pax).
                _stopLabel = UILabels.AddLabel(this, Margin, LabelsTopY, string.Empty, PanelContentWidth - Margin * 2);
                _destinationsLabel = UILabels.AddLabel(this, Margin, LabelsTopY, string.Empty, PanelContentWidth - Margin * 2, textScale: 0.8f);

                // No Prev/Next — main IPT stop panel already has those.

                _reflowLabels[0] = _lineNameLabel;
                _reflowLabels[1] = _stopLabel;
                _reflowLabels[2] = _destinationsLabel;

                CacheLocaleStrings();
            }
            catch (Exception ex)
            {
                Utils.LogError($"CommuterDestination: failed to build panel: {ex.Message}");
            }
        }

        private void CacheLocaleStrings()
        {
            _cachedHeader = Localization.Get("COMMUTER_DESTINATION_HEADER");
            _cachedNone = Localization.Get("COMMUTER_DESTINATION_NONE");
        }

        public override void Update()
        {
            base.Update();

            if (!isVisible || _stopId == 0)
            {
                return;
            }

            var interval = PerformanceProfile.CommuterRefreshFrames;
            if (DestinationGraphGenerator.IsFullMapMode)
            {
                interval = Mathf.Max(interval, interval + 40);
            }

            if (++_framesSinceRefresh < interval)
            {
                return;
            }

            _framesSinceRefresh = 0;
            RefreshGraph();
            UpdateLabels();
        }

        public void Show(ushort stopId)
        {
            if (stopId == 0)
            {
                Hide();
                return;
            }

            UserDismissed = false;

            if (stopId != _stopId)
            {
                _cachedStopNameId = 0;
                _cachedStopName = string.Empty;
                _lastDestinationsText = string.Empty;
            }

            _stopId = stopId;
            _framesSinceRefresh = 0;
            if (_cachedHeader == null)
            {
                CacheLocaleStrings();
            }

            RefreshGraph();
            UpdateLabels();
            isVisible = true;
            Show();
            BringToFront();
            TryDockNearStopPanel();
        }

        private void TryDockNearStopPanel()
        {
            try
            {
                var stopPanel = PublicTransportStopWorldInfoPanel.instance;
                if (stopPanel == null || !stopPanel.isVisible)
                {
                    return;
                }

                absolutePosition = new Vector3(
                    stopPanel.absolutePosition.x + stopPanel.width + 8f,
                    stopPanel.absolutePosition.y,
                    0f);
            }
            catch
            {
                // keep default
            }
        }

        private void RefreshGraph()
        {
            try
            {
                Graph = DestinationGraphGenerator.GenerateGraph(_stopId);
                DestinationOverlayManager.ActiveGraph = Graph;
                DestinationOverlayManager.ActiveStopId = _stopId;
                DestinationOverlayManager.EnsureRegistered();
            }
            catch (Exception ex)
            {
                Utils.LogError($"CommuterDestination: failed to generate destination graph: {ex.Message}");
                Graph = DestinationGraph.Empty;
                DestinationOverlayManager.ActiveGraph = Graph;
            }
        }

        private void UpdateLabels()
        {
            var lineId = Singleton<NetManager>.instance.m_nodes.m_buffer[_stopId].m_transportLine;

            if (_lineNameLabel != null)
            {
                _lineNameLabel.text = lineId != 0
                    ? Singleton<TransportManager>.instance.GetLineName(lineId)
                    : string.Empty;
            }

            if (_stopLabel != null)
            {
                if (_cachedStopNameId != _stopId || string.IsNullOrEmpty(_cachedStopName))
                {
                    var stopName = StopAutoNamer.EnsureNamed(new InstanceID { NetNode = _stopId });
                    _cachedStopName = string.IsNullOrEmpty(stopName) ? "#" + _stopId : stopName;
                    _cachedStopNameId = _stopId;
                }

                var waiting = Graph != null ? Graph.WaitingCount : 0;
                if (waiting == 0 && lineId != 0)
                {
                    waiting = Singleton<TransportManager>.instance.m_lines.m_buffer[lineId]
                        .CalculatePassengerCount(_stopId);
                }

                // Passengers on the stop name line — not as a separate "destination" field.
                _stopLabel.text = waiting > 0
                    ? string.Format(Localization.Get("COMMUTER_STOP_WITH_WAITING"), _cachedStopName, waiting)
                    : _cachedStopName;
            }

            if (_destinationsLabel != null)
            {
                var destText = BuildDestinationsText();
                if (destText != _lastDestinationsText)
                {
                    _lastDestinationsText = destText;
                    _destinationsLabel.text = destText;
                }
            }

            ReflowContent();
        }

        private string BuildDestinationsText()
        {
            if (Graph == null)
            {
                return Localization.Get("COMMUTER_DESTINATION_LOADING");
            }

            var entries = Graph.Entries;
            if (entries == null || entries.Length == 0)
            {
                return _cachedNone ?? Localization.Get("COMMUTER_DESTINATION_NONE");
            }

            var buildingManager = Singleton<BuildingManager>.instance;
            var districtManager = Singleton<DistrictManager>.instance;
            _destSb.Length = 0;
            _destSb.AppendLine(_cachedHeader ?? Localization.Get("COMMUTER_DESTINATION_HEADER"));

            var shown = 0;
            var listLimit = PerformanceProfile.CommuterPanelListRows;
            if (DestinationGraphGenerator.IsFullMapMode)
            {
                listLimit = Mathf.Max(listLimit, 8);
            }

            var n = entries.Length < listLimit ? entries.Length : listLimit;
            for (var i = 0; i < n; i++)
            {
                ref var e = ref entries[i];
                if (e.BuildingId == 0 || e.Count <= 0)
                {
                    continue;
                }

                var name = buildingManager.GetBuildingName(e.BuildingId, InstanceID.Empty);
                if (string.IsNullOrEmpty(name))
                {
                    var district = districtManager.GetDistrict(e.Position);
                    name = district != 0
                        ? districtManager.GetDistrictName(district)
                        : ("#" + e.BuildingId);
                }

                _destSb.Append(name);
                _destSb.Append(" (");
                _destSb.Append(e.Count);
                _destSb.AppendLine(")");
                shown++;
            }

            return shown > 0
                ? _destSb.ToString().TrimEnd()
                : (_cachedNone ?? Localization.Get("COMMUTER_DESTINATION_NONE"));
        }

        private void ReflowContent()
        {
            if (_lineNameLabel == null || _stopLabel == null || _destinationsLabel == null)
            {
                return;
            }

            var currentY = LabelsTopY;
            for (var i = 0; i < _reflowLabels.Length; i++)
            {
                var label = _reflowLabels[i];
                if (label == null)
                {
                    continue;
                }

                label.relativePosition = new Vector2(Margin, currentY);
                currentY += label.height + LabelGap;
            }

            currentY += BottomMargin;
            _dynamicHeight = Mathf.Max(PanelContentHeight, currentY);
            height = _dynamicHeight;
        }
    }
}
