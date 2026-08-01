using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;
using ImprovedPublicTransport.UI;
using ImprovedPublicTransport.Util;
using UnityEngine;

namespace ImprovedPublicTransport.HarmonyPatches.PublicTransportWorldInfoPanelPatches
{
    public static class UpdateStopButtonsPatch
    {
        // ___m_stopButtons.items reuses the same pooled UIButton instances across frames, so the
        // "PassengerCount" child lookup below is stable per button - cache it instead of walking
        // the UI tree for every stop button on every frame the line panel is open.
        private static readonly Dictionary<UIComponent, UILabel> PassengerLabelCache =
            new Dictionary<UIComponent, UILabel>();

        private static float _nextFullRefreshRealtime;
        private static ushort _cachedLineId;
        private static readonly Dictionary<UIComponent, string> TooltipCache =
            new Dictionary<UIComponent, string>();

        private const float FullRefreshInterval = 0.4f;

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
            PassengerLabelCache.Clear();
            TooltipCache.Clear();
        }

        public static bool Prefix(UITemplateList<UIButton> ___m_stopButtons, ushort lineID)
        {
            if (___m_stopButtons?.items == null || lineID == 0)
            {
                return false;
            }

            float now = Time.unscaledTime;
            // Passenger counts do not need 60 Hz; tooltips even less so.
            bool doFull = lineID != _cachedLineId || now >= _nextFullRefreshRealtime;
            if (doFull)
            {
                _nextFullRefreshRealtime = now + FullRefreshInterval;
                _cachedLineId = lineID;
            }
            else
            {
                // Still run, but skip expensive name/tooltip rebuilds when possible.
            }

            var tm = Singleton<TransportManager>.instance;
            ref var line = ref tm.m_lines.m_buffer[lineID];
            ushort stop = line.m_stops;
            var instanceManager = Singleton<InstanceManager>.instance;

            foreach (UIComponent uiComponent in ___m_stopButtons.items)
            {
                if (!PassengerLabelCache.TryGetValue(uiComponent, out var passengerLabel) || passengerLabel == null)
                {
                    passengerLabel = uiComponent.Find<UILabel>("PassengerCount");
                    PassengerLabelCache[uiComponent] = passengerLabel;
                }

                if (passengerLabel != null && doFull)
                {
                    passengerLabel.text = line.CalculatePassengerCount(stop).ToString();
                }

                if (doFull)
                {
                    var id = InstanceID.Empty;
                    id.NetNode = stop;
                    var name = instanceManager.GetName(id) ?? string.Empty;
                    if (string.Empty == name)
                    {
                        name = StopListBoxRow.GenerateStopName(name, stop, -1);
                    }

                    var tip = string.Format(Localization.Get("STOP_BUTTON_TOOLTIP"), name);
                    if (!TooltipCache.TryGetValue(uiComponent, out var old) || old != tip)
                    {
                        TooltipCache[uiComponent] = tip;
                        uiComponent.tooltip = tip;
                    }
                }

                stop = TransportLine.GetNextStop(stop);
            }

            return false;
        }
    }
}
