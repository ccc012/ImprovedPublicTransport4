using ColossalFramework;
using UnityEngine;

namespace ImprovedPublicTransport.Integration.TicketPriceCustomizer
{
    /// <summary>
    /// MonoBehaviour that detects day/night transitions and re-applies the correct
    /// ticket price multipliers. Attached to IptGameObject on level load.
    /// </summary>
    public class DayNightPriceWatcher : MonoBehaviour
    {
        private bool _lastIsNight;
        private bool _ticketPricesTabFailed;

        private void Start()
        {
            _lastIsNight = Singleton<SimulationManager>.instance.m_isNightTime;
        }

        private void Update()
        {
            if (!_ticketPricesTabFailed)
            {
                try
                {
                    TicketPricesTab.OnUpdate(Time.deltaTime);
                }
                catch (System.Exception ex)
                {
                    _ticketPricesTabFailed = true;
                    Util.Utils.LogError($"TicketPricesTab.OnUpdate failed - disabling its per-frame refresh: {ex}");
                }
            }

            bool isNight = Singleton<SimulationManager>.instance.m_isNightTime;
            if (isNight == _lastIsNight) return;

            _lastIsNight = isNight;
            var settings = ModSetting.Instance.TicketPriceCustomizer;
            if (settings != null)
                PriceCustomization.ApplyForCurrentTime(settings);
        }
    }
}
