// Adapted from Commuter Destination (MIT, Workshop 2475986859, github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
// Rebuilt on AlgernonCommons.UI.StandalonePanel (title bar, drag, close button all handled by the
// base class) instead of upstream's hand-built UIPanel/UIDragHandle/close-button setup, matching how
// IPT4's other standalone panels (e.g. Integration/FlightTracker/UI/TrackerPanel.cs) are built.
using System;
using AlgernonCommons.UI;
using ColossalFramework;
using ColossalFramework.UI;
using ImprovedPublicTransport;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace CommuterDestination
{
    /// <summary>Shows where the passengers currently waiting at a stop are headed.</summary>
    internal sealed class CommuterDestinationPanel : StandalonePanel
    {
        private const float PanelContentWidth = 320f;
        private const float PanelContentHeight = 150f;
        private const int RefreshIntervalFrames = 30;

        private UILabel _lineNameLabel;
        private UILabel _stopLabel;
        private UILabel _passengerCountLabel;
        private ushort _stopId;
        private int _framesSinceRefresh;

        public static CommuterDestinationPanel Instance => StandalonePanelManager<CommuterDestinationPanel>.Panel;

        public override float PanelWidth => PanelContentWidth;

        public override float PanelHeight => PanelContentHeight;

        protected override string PanelTitle => Localization.Get("COMMUTER_DESTINATION_PANEL_TITLE");

        /// <summary>The destination breakdown for the currently shown stop, read by
        /// <see cref="DestinationOverlayManager"/> to draw the map icons. Null until the first
        /// refresh after <see cref="Show"/>.</summary>
        public DestinationGraph Graph { get; private set; }

        public static void ShowForStop(ushort stopId)
        {
            StandalonePanelManager<CommuterDestinationPanel>.Create();
            StandalonePanelManager<CommuterDestinationPanel>.Panel?.Show(stopId);
        }

        public static void CloseIfOpen() => StandalonePanelManager<CommuterDestinationPanel>.Panel?.Hide();

        public override void Awake()
        {
            base.Awake();

            try
            {
                _lineNameLabel = UILabels.AddLabel(this, Margin, 40f, string.Empty, PanelContentWidth - Margin * 2);
                _stopLabel = UILabels.AddLabel(this, Margin, 60f, string.Empty, PanelContentWidth - Margin * 2);
                _passengerCountLabel = UILabels.AddLabel(this, Margin, 80f, string.Empty, PanelContentWidth - Margin * 2);

                var prevButton = UIButtons.AddButton(this, Margin, 110f, Localization.Get("STOP_PANEL_PREVIOUS"), 140f, 30f, tooltip: Localization.Get("STOP_PANEL_PREVIOUS_TOOLTIP"));
                prevButton.eventClick += (_, __) => Show(global::TransportLine.GetPrevStop(_stopId));

                var nextButton = UIButtons.AddButton(this, Margin + 150f, 110f, Localization.Get("STOP_PANEL_NEXT"), 140f, 30f, tooltip: Localization.Get("STOP_PANEL_NEXT_TOOLTIP"));
                nextButton.eventClick += (_, __) => Show(global::TransportLine.GetNextStop(_stopId));
            }
            catch (Exception ex)
            {
                Utils.LogError($"CommuterDestination: failed to build panel: {ex.Message}");
            }
        }

        public override void Update()
        {
            base.Update();

            if (!isVisible || _stopId == 0)
            {
                return;
            }

            if (++_framesSinceRefresh < RefreshIntervalFrames)
            {
                return;
            }

            _framesSinceRefresh = 0;
            RefreshGraph();
        }

        public void Show(ushort stopId)
        {
            if (stopId == 0)
            {
                Hide();
                return;
            }

            _stopId = stopId;
            _framesSinceRefresh = RefreshIntervalFrames;
            RefreshGraph();
            UpdateLabels();
            Show();
        }

        private void RefreshGraph()
        {
            try
            {
                Graph = DestinationGraphGenerator.GenerateGraph(_stopId);
            }
            catch (Exception ex)
            {
                Utils.LogError($"CommuterDestination: failed to generate destination graph: {ex.Message}");
            }
        }

        private void UpdateLabels()
        {
            var lineId = Singleton<NetManager>.instance.m_nodes.m_buffer[_stopId].m_transportLine;
            if (_lineNameLabel != null)
            {
                _lineNameLabel.text = Singleton<TransportManager>.instance.GetLineName(lineId);
            }

            if (_stopLabel != null)
            {
                _stopLabel.text = "#" + _stopId;
            }

            if (_passengerCountLabel != null)
            {
                var passengerCount = Singleton<TransportManager>.instance.m_lines.m_buffer[lineId].CalculatePassengerCount(_stopId);
                _passengerCountLabel.text = string.Format(Localization.Get("STOP_PANEL_WAITING_PEOPLE"), passengerCount);
            }
        }
    }
}
