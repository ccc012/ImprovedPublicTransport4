// Adapted from Commuter Destination (MIT, Workshop 2475986859, github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
// Rebuilt on AlgernonCommons.UI.StandalonePanel (title bar, drag, close button all handled by the
// base class) instead of upstream's hand-built UIPanel/UIDragHandle/close-button setup, matching how
// IPT4's other standalone panels (e.g. Integration/FlightTracker/UI/TrackerPanel.cs) are built.
using System;
using System.Linq;
using System.Text;
using AlgernonCommons.UI;
using ColossalFramework;
using ColossalFramework.UI;
using ImprovedPublicTransport;
using ImprovedPublicTransport.Util;
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
        private const float LabelsTopY = 40f;
        private const float LabelGap = 4f;
        private const float ButtonGap = 12f;
        private const float ButtonHeight = 30f;
        private const float BottomMargin = 16f;

        private const int MaxDestinationsShown = 5;

        private UILabel _lineNameLabel;
        private UILabel _stopLabel;
        private UILabel _passengerCountLabel;
        private UILabel _destinationsLabel;
        private UIButton _prevButton;
        private UIButton _nextButton;
        private ushort _stopId;
        private int _framesSinceRefresh;
        private float _dynamicHeight = PanelContentHeight;

        public static CommuterDestinationPanel Instance => StandalonePanelManager<CommuterDestinationPanel>.Panel;

        public override float PanelWidth => PanelContentWidth;

        // Awake() only reads this once to set the initial size (see AlgernonCommons.StandalonePanel)
        // - actual resizing after that happens in ReflowContent(), called every time label text
        // changes, since the panel's true content height depends on how many lines the (variable-
        // length, sometimes translated) line name and stop labels wrap to.
        public override float PanelHeight => _dynamicHeight;

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
                _lineNameLabel = UILabels.AddLabel(this, Margin, LabelsTopY, string.Empty, PanelContentWidth - Margin * 2);
                _stopLabel = UILabels.AddLabel(this, Margin, LabelsTopY, string.Empty, PanelContentWidth - Margin * 2);
                _passengerCountLabel = UILabels.AddLabel(this, Margin, LabelsTopY, string.Empty, PanelContentWidth - Margin * 2);
                // Text fallback for "where are they going" - the map icons DestinationOverlayManager
                // draws can be easy to miss depending on zoom/camera angle, so the panel itself should
                // never rely on them alone to answer the question it exists to answer.
                _destinationsLabel = UILabels.AddLabel(this, Margin, LabelsTopY, string.Empty, PanelContentWidth - Margin * 2, textScale: 0.8f);

                _prevButton = UIButtons.AddButton(this, Margin, LabelsTopY, Localization.Get("STOP_PANEL_PREVIOUS"), 140f, ButtonHeight, tooltip: Localization.Get("STOP_PANEL_PREVIOUS_TOOLTIP"));
                _prevButton.eventClick += (_, __) => Show(global::TransportLine.GetPrevStop(_stopId));

                _nextButton = UIButtons.AddButton(this, Margin + 150f, LabelsTopY, Localization.Get("STOP_PANEL_NEXT"), 140f, ButtonHeight, tooltip: Localization.Get("STOP_PANEL_NEXT_TOOLTIP"));
                _nextButton.eventClick += (_, __) => Show(global::TransportLine.GetNextStop(_stopId));
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
                var stopName = StopAutoNamer.EnsureNamed(new InstanceID { NetNode = _stopId });
                _stopLabel.text = string.IsNullOrEmpty(stopName) ? "#" + _stopId : stopName;
            }

            if (_passengerCountLabel != null)
            {
                var passengerCount = Singleton<TransportManager>.instance.m_lines.m_buffer[lineId].CalculatePassengerCount(_stopId);
                _passengerCountLabel.text = string.Format(Localization.Get("STOP_PANEL_WAITING_PEOPLE"), passengerCount);
            }

            if (_destinationsLabel != null)
            {
                _destinationsLabel.text = BuildDestinationsText();
            }

            ReflowContent();
        }

        // Top destinations by popularity across every stop the graph reached (the graph already
        // walks a bounded number of downstream stops from the one being shown - see
        // DestinationGraphGenerator), formatted as plain building-name lines so the panel answers
        // "where are they going" in text even if the player never looks at the map icons.
        private string BuildDestinationsText()
        {
            if (Graph == null)
            {
                return Localization.Get("COMMUTER_DESTINATION_LOADING");
            }

            var buildingManager = Singleton<BuildingManager>.instance;
            var topJourneys = Graph.Stops
                .SelectMany(stop => stop.Journeys)
                .GroupBy(journey => journey.DestinationBuildingId)
                .Select(group => (BuildingId: group.Key, Popularity: group.Sum(j => j.Popularity)))
                .OrderByDescending(entry => entry.Popularity)
                .Take(MaxDestinationsShown)
                .ToList();

            if (topJourneys.Count == 0)
            {
                return Localization.Get("COMMUTER_DESTINATION_NONE");
            }

            var sb = new StringBuilder();
            sb.AppendLine(Localization.Get("COMMUTER_DESTINATION_HEADER"));
            foreach (var (buildingId, popularity) in topJourneys)
            {
                var name = buildingManager.GetBuildingName(buildingId, InstanceID.Empty);
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                sb.AppendLine($"{name} ({popularity})");
            }

            return sb.ToString().TrimEnd();
        }

        // Stacks the labels top-to-bottom by their actual (word-wrapped) height instead of fixed Y
        // offsets, then places the prev/next buttons below whichever label ends up lowest, and
        // resizes the panel itself to fit - a long line name (long custom names, some translations)
        // would otherwise get clipped or overlap the buttons at a fixed height.
        private void ReflowContent()
        {
            if (_lineNameLabel == null || _stopLabel == null || _passengerCountLabel == null || _destinationsLabel == null)
            {
                return;
            }

            var currentY = LabelsTopY;
            foreach (var label in new[] { _lineNameLabel, _stopLabel, _passengerCountLabel, _destinationsLabel })
            {
                label.relativePosition = new Vector2(Margin, currentY);
                currentY += label.height + LabelGap;
            }

            currentY += ButtonGap - LabelGap;
            if (_prevButton != null)
            {
                _prevButton.relativePosition = new Vector2(Margin, currentY);
            }

            if (_nextButton != null)
            {
                _nextButton.relativePosition = new Vector2(Margin + 150f, currentY);
            }

            currentY += ButtonHeight + BottomMargin;
            _dynamicHeight = Mathf.Max(PanelContentHeight, currentY);
            height = _dynamicHeight;
        }
    }
}
