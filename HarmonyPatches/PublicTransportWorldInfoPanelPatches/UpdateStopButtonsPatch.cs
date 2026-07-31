using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;
using ImprovedPublicTransport.UI;
using ImprovedPublicTransport.Util;

namespace ImprovedPublicTransport.HarmonyPatches.PublicTransportWorldInfoPanelPatches
{
    public static class UpdateStopButtonsPatch
    {
        // ___m_stopButtons.items reuses the same pooled UIButton instances across frames, so the
        // "PassengerCount" child lookup below is stable per button - cache it instead of walking
        // the UI tree for every stop button on every frame the line panel is open.
        private static readonly Dictionary<UIComponent, UILabel> PassengerLabelCache = new Dictionary<UIComponent, UILabel>();

        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(PublicTransportWorldInfoPanel), "UpdateStopButtons"),
                new PatchUtil.MethodDefinition(typeof(UpdateStopButtonsPatch),
                    nameof(Prefix))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(PublicTransportWorldInfoPanel), "UpdateStopButtons")
            );
        }
        
        public static bool Prefix(UITemplateList<UIButton> ___m_stopButtons, ushort lineID)
        {
            ushort stop = Singleton<TransportManager>.instance.m_lines.m_buffer[(int)lineID].m_stops;
            foreach (UIComponent uiComponent in ___m_stopButtons.items)
            {
                if (!PassengerLabelCache.TryGetValue(uiComponent, out var passengerLabel) || passengerLabel == null)
                {
                    passengerLabel = uiComponent.Find<UILabel>("PassengerCount");
                    PassengerLabelCache[uiComponent] = passengerLabel;
                }
                if (passengerLabel != null)
                {
                    passengerLabel.text = Singleton<TransportManager>.instance.m_lines.m_buffer[(int)lineID].CalculatePassengerCount(stop).ToString();
                }
                //begin mod(+): add tooltip with stop name
                var id = InstanceID.Empty;
                id.NetNode = stop;
                var name = Singleton<InstanceManager>.instance.GetName(id) ?? string.Empty;
                if (string.Empty == name)
                {
                    name = StopListBoxRow.GenerateStopName(name, stop, -1);
                }
                uiComponent.tooltip = string.Format(Localization.Get("STOP_BUTTON_TOOLTIP"), name);
                //end mod

                stop = TransportLine.GetNextStop(stop);
            }

            return false;
        }
    }
}