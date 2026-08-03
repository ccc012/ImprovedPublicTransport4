// Adapted from Commuter Destination (MIT, Workshop 2475986859,
// github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
using ColossalFramework;
using ColossalFramework.UI;
using ImprovedPublicTransport;
using ImprovedPublicTransport.Util;
using UnityEngine;

namespace CommuterDestination
{
    /// <summary>
    /// Upstream's own destination panel, restored as-is. The map icons are drawn only while this
    /// is open (see DestinationOverlayManager), exactly as upstream gates them - which is also why
    /// it is back: an earlier attempt replaced it with IPT4's stop panel and buttons, and the
    /// feature stopped showing anything.
    /// </summary>
    internal sealed class StopDestinationInfoPanel : UIPanel
    {
        private static class PanelConfig
        {
            public const float PanelWidth = 300f;
            public const float PanelHeight = 150f;
            public const float TitleWidth = 250f;
            public const float TitleHeight = 36f;
            public const float CloseButtonSize = 32f;
            public const float CloseButtonY = 3f;
            public const float CloseButtonX = PanelWidth - CloseButtonSize - CloseButtonY;
            public const int CyclesPerUpdate = 50;
        }

        public static StopDestinationInfoPanel instance;

        private ushort _stopId;
        private int _cyclesSinceGraphGenerated;

        private UILabel _lineNameLabel;
        private UILabel _stopNameLabel;
        private UILabel _passengerCountLabel;

        /// <summary>The destinations for the stop currently shown. Read by the overlay manager.</summary>
        public DestinationGraph DestinationGraph { get; private set; }

        public StopDestinationInfoPanel()
        {
            name = "CommuterDestinationStopPanel";
            canFocus = true;
            isInteractive = true;
            width = PanelConfig.PanelWidth;
            height = PanelConfig.PanelHeight;
            backgroundSprite = "MenuPanel";
            padding = new RectOffset(10, 10, 5, 5);
        }

        public override void Start()
        {
            instance = this;
            base.Start();
            SetupPanel();
        }

        public override void Update()
        {
            base.Update();

            if (Input.GetKey(KeyCode.Escape))
            {
                Hide();
            }

            if (!isVisible)
            {
                return;
            }

            if (++_cyclesSinceGraphGenerated > PanelConfig.CyclesPerUpdate)
            {
                DestinationGraph = DestinationGraphGenerator.GenerateGraph(_stopId);
                _cyclesSinceGraphGenerated = 0;
            }
        }

        public void Show(ushort stopId)
        {
            if (stopId == 0)
            {
                return;
            }

            _stopId = stopId;

            DestinationGraph = DestinationGraphGenerator.GenerateGraph(_stopId);
            _cyclesSinceGraphGenerated = 0;

            var netManager = Singleton<NetManager>.instance;
            var lineId = netManager.m_nodes.m_buffer[_stopId].m_transportLine;
            var lineName = lineId != 0
                ? Singleton<TransportManager>.instance.GetLineName(lineId)
                : string.Empty;

            _lineNameLabel.text = lineName + " " + Localization.Get("COMMUTERDESTINATION_PANEL_DESTINATIONS");
            _stopNameLabel.text = string.Format(
                Localization.Get("COMMUTERDESTINATION_PANEL_STOP"),
                lineId != 0 ? TransportLineUtil.GetStopIndex(lineId, _stopId) + 1 : 0);
            _passengerCountLabel.text = string.Format(
                Localization.Get("COMMUTERDESTINATION_PANEL_WAITING"), CountWaiting());

            relativePosition = new Vector3(400f, 400f);
            Show();
        }

        /// <summary>
        /// Waiting passengers, taken straight from the graph that drew the icons, so the number and
        /// the icons can never disagree.
        /// </summary>
        private int CountWaiting()
        {
            var graph = DestinationGraph;
            if (graph == null)
            {
                return 0;
            }

            var total = 0;
            foreach (var stop in graph.Stops)
            {
                foreach (var journey in stop.GetJourneys())
                {
                    total += journey.Popularity;
                }
            }

            return total;
        }

        private void MoveToPrevStop()
        {
            var prevStop = global::TransportLine.GetPrevStop(_stopId);
            if (prevStop != 0)
            {
                Show(prevStop);
            }
        }

        private void MoveToNextStop()
        {
            var nextStop = global::TransportLine.GetNextStop(_stopId);
            if (nextStop != 0)
            {
                Show(nextStop);
            }
        }

        private void SetupPanel()
        {
            isVisible = false;
            anchor = UIAnchorStyle.None;
            pivot = UIPivotPoint.MiddleCenter;
            relativePosition = Vector3.zero;

            CreateTitleBar(this, "TitleBar", Localization.Get("COMMUTERDESTINATION_PANEL_TITLE"));

            var container = AddUIComponent<UIPanel>();
            container.name = "Container";
            container.width = width;
            container.height = 100f;
            container.autoLayout = true;
            container.autoLayoutDirection = LayoutDirection.Vertical;
            container.autoLayoutPadding = new RectOffset(0, 0, 5, 5);
            container.autoLayoutStart = LayoutStart.TopLeft;
            container.relativePosition = new Vector3(0f, 40f);
            container.padding = new RectOffset(10, 10, 5, 5);

            _lineNameLabel = CreateLabel(container, "LineNameLabel", string.Empty);
            _stopNameLabel = CreateLabel(container, "StopNameLabel", string.Empty);
            CreateStopNavigation(container);
            _passengerCountLabel = CreateLabel(container, "PassengerCountLabel", string.Empty);
        }

        private void OnCloseButtonClick(UIComponent component, UIMouseEventParameter eventParam) => Hide();

        private void OnPrevStopButtonClick(UIComponent component, UIMouseEventParameter eventParam) => MoveToPrevStop();

        private void OnNextStopButtonClick(UIComponent component, UIMouseEventParameter eventParam) => MoveToNextStop();

        private static UILabel CreateLabel(UIComponent container, string labelName, string text)
        {
            var label = container.AddUIComponent<UILabel>();
            label.name = labelName;
            label.text = text;
            label.textScale = 0.8f;
            return label;
        }

        private UIPanel CreateStopNavigation(UIComponent container)
        {
            var stopNavigation = container.AddUIComponent<UIPanel>();
            stopNavigation.name = "StopNavigation";
            stopNavigation.width = container.width;
            stopNavigation.height = 30f;
            stopNavigation.autoLayout = true;
            stopNavigation.autoLayoutDirection = LayoutDirection.Horizontal;
            stopNavigation.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            stopNavigation.autoLayoutStart = LayoutStart.TopLeft;
            stopNavigation.padding = new RectOffset(0, 0, 0, 0);

            var previousStop = stopNavigation.AddUIComponent<StopPanelNavigationButton>();
            previousStop.name = "PreviousStop";
            previousStop.text = Localization.Get("COMMUTERDESTINATION_PANEL_PREVIOUS");
            previousStop.eventClick += OnPrevStopButtonClick;

            var nextStop = stopNavigation.AddUIComponent<StopPanelNavigationButton>();
            nextStop.name = "NextStop";
            nextStop.text = Localization.Get("COMMUTERDESTINATION_PANEL_NEXT");
            nextStop.eventClick += OnNextStopButtonClick;

            return stopNavigation;
        }

        private UIPanel CreateTitleBar(UIComponent container, string barName, string text)
        {
            var titleBar = container.AddUIComponent<UIPanel>();
            titleBar.name = barName;
            titleBar.width = PanelConfig.TitleWidth;
            titleBar.height = PanelConfig.TitleHeight;
            titleBar.relativePosition = Vector3.zero;

            var title = titleBar.AddUIComponent<UILabel>();
            title.name = barName + "Title";
            title.text = text;
            title.isInteractive = false;
            title.width = titleBar.width;
            title.relativePosition = new Vector3(10f, 10f);
            title.textColor = new Color32(231, 220, 161, 255);

            var dragHandle = titleBar.AddUIComponent<UIDragHandle>();
            dragHandle.width = titleBar.width;
            dragHandle.height = titleBar.height;
            dragHandle.relativePosition = Vector3.zero;
            dragHandle.target = titleBar.parent;

            var closeButton = titleBar.AddUIComponent<UIButton>();
            closeButton.name = "CloseButton";
            closeButton.size = new Vector2(PanelConfig.CloseButtonSize, PanelConfig.CloseButtonSize);
            closeButton.normalBgSprite = "buttonclose";
            closeButton.hoveredBgSprite = "buttonclosehover";
            closeButton.pressedBgSprite = "buttonclosepressed";
            closeButton.relativePosition = new Vector3(PanelConfig.CloseButtonX, PanelConfig.CloseButtonY);
            closeButton.eventClick += OnCloseButtonClick;

            return titleBar;
        }
    }
}
