using ColossalFramework.UI;
using UnityEngine;

namespace AutoLineColor
{
    /// <summary>
    /// Greys out the "refresh name/colour" button when AutoLineColor's strategies are both
    /// Disabled, and swaps its tooltip accordingly. Attached to whichever button currently
    /// owns the feature - see PanelExtenderLine.CreateAutoColorRefreshButton.
    /// </summary>
    internal sealed class RefreshButtonStateUpdater : MonoBehaviour
    {
        internal UIButton Button { get; set; }

        // This button is only visible while the line panel is open, but Update() still runs
        // every frame for the lifetime of the loaded level - only touch the button (and pay for
        // the Localization.Get lookup) when the enabled state has actually changed since last
        // frame, instead of reassigning isEnabled/tooltip unconditionally every frame. Also
        // invalidated on a language change (see OnEnable/OnDisable below), or the tooltip would
        // keep showing stale text after switching languages until the enabled state next flips.
        private bool? _lastEnabled;

        private void OnEnable()
        {
            CSLModsCommon.Manager.LocalizationManager.ModActiveLocaleChanged += OnLocaleChanged;
        }

        private void OnDisable()
        {
            CSLModsCommon.Manager.LocalizationManager.ModActiveLocaleChanged -= OnLocaleChanged;
        }

        private void OnLocaleChanged(string localeId, CSLModsCommon.Manager.LocalizationManager manager)
        {
            _lastEnabled = null;
        }

        private void Update()
        {
            if (Button == null)
            {
                return;
            }

            var settings = ImprovedPublicTransport.ModSetting.Instance;
            var enabled = settings.AutoLineColorColorStrategy != ImprovedPublicTransport.ModSetting.AutoLineColorStrategy.Disabled
                || settings.AutoLineColorNamingStrategyMode != ImprovedPublicTransport.ModSetting.AutoLineColorNamingStrategy.Disabled;

            if (_lastEnabled == enabled)
            {
                return;
            }
            _lastEnabled = enabled;

            Button.isEnabled = enabled;
            Button.tooltip = ImprovedPublicTransport.Localization.Get(
                enabled ? "AUTOLINECOLOR_REFRESH_BUTTON_TOOLTIP" : "AUTOLINECOLOR_REFRESH_DISABLED_TOOLTIP");
        }
    }
}
