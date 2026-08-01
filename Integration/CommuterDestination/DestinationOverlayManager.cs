// Adapted from Commuter Destination (MIT, Workshop 2475986859) - see LICENSE.txt.
using ColossalFramework;
using ImprovedPublicTransport;
using ImprovedPublicTransport.Util;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace CommuterDestination
{
    /// <summary>
    /// Map icons for destination buildings. Colour bands by waiting count (same size).
    /// </summary>
    public sealed class DestinationOverlayManager
        : SimulationManagerBase<DestinationOverlayManager, MonoBehaviour>, IRenderableManager
    {
        private static readonly Vector3 HeightOffset = new Vector3(0f, 40f, 0f);

        // Different Notification problem bits → distinct colours in CS1 (same size always).
        // Low ≈ blue/info, mid ≈ yellow/warn, high ≈ red/major.
        private static readonly Notification.ProblemStruct IconLow =
            new Notification.ProblemStruct(Notification.Problem1.NoCustomers);
        private static readonly Notification.ProblemStruct IconMid =
            new Notification.ProblemStruct(Notification.Problem1.Noise);
        private static readonly Notification.ProblemStruct IconHigh =
            new Notification.ProblemStruct(Notification.Problem1.Death | Notification.Problem1.MajorProblem);

        private static bool _registered;

        internal static DestinationGraph ActiveGraph { get; set; }

        internal static ushort ActiveStopId { get; set; }

        public static void EnsureRegistered()
        {
            try
            {
                var mgr = instance;
                if (mgr == null)
                {
                    return;
                }

                if (!_registered)
                {
                    SimulationManager.RegisterManager(mgr);
                    _registered = true;
                }
            }
            catch (System.Exception ex)
            {
                Utils.LogError($"CommuterDestination: failed to register overlay manager: {ex.Message}");
            }
        }

        public static void ClearOverlay()
        {
            ActiveGraph = null;
            ActiveStopId = 0;
        }

        protected override void BeginOverlayImpl(RenderManager.CameraInfo cameraInfo)
        {
            if (!ModSetting.Instance.EnableCommuterDestination)
            {
                return;
            }

            var graph = ActiveGraph;
            var panel = CommuterDestinationPanel.Instance;
            if (panel != null && panel.isVisible && panel.Graph != null)
            {
                graph = panel.Graph;
                ActiveGraph = graph;
            }

            if (graph == null || graph.Entries == null || graph.Entries.Length == 0)
            {
                return;
            }

            // Icons while stop panel is open (secondary list optional).
            var stopPanel = ImprovedPublicTransport.UI.PublicTransportStopWorldInfoPanel.instance;
            var stopOpen = stopPanel != null && stopPanel.isVisible;
            var panelOpen = panel != null && panel.isVisible;
            if (!stopOpen && !panelOpen)
            {
                return;
            }

            var entries = graph.Entries;
            var maxIcons = DestinationGraphGenerator.OverlayIconLimit;
            var n = entries.Length < maxIcons ? entries.Length : maxIcons;
            const float scale = 1f; // colour carries popularity, not size
            for (var i = 0; i < n; i++)
            {
                var e = entries[i];
                if (e.BuildingId == 0 || e.Count <= 0)
                {
                    continue;
                }

                var icon = PickIcon(e.Count);
                Notification.RenderInstance(cameraInfo, icon, e.Position + HeightOffset, scale);
            }
        }

        private static Notification.ProblemStruct PickIcon(int count)
        {
            // Low / mid / high popularity → different colours (same footprint).
            if (count >= 10)
            {
                return IconHigh;
            }

            if (count >= 4)
            {
                return IconMid;
            }

            return IconLow;
        }
    }
}
