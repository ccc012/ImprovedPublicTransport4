// Adapted from Commuter Destination (MIT, Workshop 2475986859, github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
// The upstream renderer/manager split (IDestinationGraphRenderer + NotificationDestinationGraphRenderer
// + DestinationDisplayManager) existed to support swapping render styles later; IPT4 only ships the
// one "notification icon" style, so it is folded into a single class here.
using ColossalFramework;
using ImprovedPublicTransport;
using UnityEngine;

namespace CommuterDestination
{
    /// <summary>
    /// Draws a red "walking person" problem icon over every destination building reached from the
    /// currently open Commuter Destination panel's stop - the same icon vanilla uses for e.g. "too
    /// long a commute", scaled up when more waiting passengers share that destination. Only ever
    /// draws while <see cref="CommuterDestinationPanel"/> is open, so it costs nothing otherwise.
    /// </summary>
    public sealed class DestinationOverlayManager : SimulationManagerBase<DestinationOverlayManager, MonoBehaviour>, IRenderableManager
    {
        private static readonly Vector3 HeightOffset = new Vector3(0f, 50f, 0f);
        private static readonly Notification.ProblemStruct Icon =
            new Notification.ProblemStruct(Notification.Problem1.TooLong | Notification.Problem1.MajorProblem);

        protected override void BeginOverlayImpl(RenderManager.CameraInfo cameraInfo)
        {
            if (!ModSetting.Instance.EnableCommuterDestination)
            {
                return;
            }

            var panel = CommuterDestinationPanel.Instance;
            if (panel == null || !panel.isVisible || panel.Graph == null)
            {
                return;
            }

            foreach (var stop in panel.Graph.Stops)
            {
                foreach (var journey in stop.Journeys)
                {
                    Notification.RenderInstance(cameraInfo, Icon, journey.Destination + HeightOffset, 1 + journey.Popularity / 5);
                }
            }
        }
    }
}
