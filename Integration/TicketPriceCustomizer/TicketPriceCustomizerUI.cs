using System;
using UnityEngine;
using ColossalFramework.UI;

namespace ImprovedPublicTransport.Integration.TicketPriceCustomizer
{
    public static class TicketPriceCustomizerUI
    {
        private static UIPanel s_ticketsPanel;

        public static void SetPanel(UIPanel panel)
        {
            try
            {
                s_ticketsPanel = panel;
                // Panel is always enabled under IPT (ticket price customizing is always active)
                SetVisible(true);
            }
            catch { }
        }

        public static void SetVisible(bool isEnabled)
        {
            try
            {
                if (s_ticketsPanel == null) return;
                s_ticketsPanel.isEnabled = isEnabled;
                // Dim the whole panel when disabled so controls appear greyed-out
                try
                {
                    s_ticketsPanel.opacity = isEnabled ? 1f : 0.5f;
                }
                catch { }
                // Also set enabled state for children to preserve interactive behaviour
                foreach (var comp in s_ticketsPanel.GetComponentsInChildren<UIComponent>(true))
                {
                    try
                    {
                        comp.isEnabled = isEnabled;
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}
