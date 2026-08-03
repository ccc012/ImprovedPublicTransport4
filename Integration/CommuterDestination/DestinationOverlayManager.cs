// Adapted from Commuter Destination (MIT, Workshop 2475986859,
// github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
using ColossalFramework;
using ImprovedPublicTransport;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace CommuterDestination
{
    /// <summary>
    /// Port of upstream's DestinationDisplayManager + NotificationDestinationGraphRenderer, kept
    /// deliberately identical: same gate (upstream's own panel open, with a graph), same icon,
    /// same height offset, same popularity-driven size, every journey of every stop drawn.
    /// </summary>
    public sealed class DestinationOverlayManager
        : SimulationManagerBase<DestinationOverlayManager, MonoBehaviour>, IRenderableManager
    {
        /// <summary>How high above the destination building the notification is rendered.</summary>
        private static readonly Vector3 HeightOffset = new Vector3(0f, 50f, 0f);

        /// <summary>The "Major" variant of the "Too Long" problem - the red walking man.</summary>
        private static readonly Notification.ProblemStruct DestinationIcon =
            new Notification.ProblemStruct(Notification.Problem1.TooLong | Notification.Problem1.MajorProblem);

        private static bool _registered;

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

        protected override void BeginOverlayImpl(RenderManager.CameraInfo cameraInfo)
        {
            // Not upstream's, and kept: this runs inside RenderManager's overlay pass, and an
            // exception escaping here aborts the rest of that pass every frame, leaving the game
            // in a state where the camera still moves but nothing is selectable. Failing closed
            // costs a frame of icons; failing open costs the session.
            try
            {
                RenderIcons(cameraInfo);
            }
            catch (System.Exception ex)
            {
                Utils.LogError($"CommuterDestination: overlay render failed: {ex.Message}");
            }
        }

        private void RenderIcons(RenderManager.CameraInfo cameraInfo)
        {
            if (!ModSetting.Instance.EnableCommuterDestination)
            {
                return;
            }

            var panel = StopDestinationInfoPanel.instance;
            if (panel == null || !panel.isVisible)
            {
                return;
            }

            var graph = panel.DestinationGraph;
            if (graph == null)
            {
                return;
            }

            foreach (var stop in graph.Stops)
            {
                foreach (var journey in stop.GetJourneys())
                {
                    Notification.RenderInstance(
                        cameraInfo,
                        DestinationIcon,
                        journey.Destination + HeightOffset,
                        1 + (journey.Popularity / 5));
                }
            }
        }
    }
}
