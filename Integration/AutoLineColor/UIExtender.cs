using ColossalFramework.UI;
using ICities;
using ImprovedPublicTransport.Query;
using ImprovedPublicTransport.Util;
using JetBrains.Annotations;
using UnityEngine;

namespace AutoLineColor
{
    [UsedImplicitly]
    public class UIExtender : LoadingExtensionBase
    {
        private LoadMode _mode;

        private UIButton _refreshBtn;
        private MouseEventHandler _refreshBtnClick;

        private static bool ActiveInMode(LoadMode mode)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (mode)
            {
                case LoadMode.NewGame:
                case LoadMode.NewGameFromScenario:
                case LoadMode.LoadGame:
                    return true;

                default:
                    return false;
            }
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            this._mode = mode;

            if (ActiveInMode(mode))
                AttachUI();
        }

        public override void OnLevelUnloading()
        {
            if (ActiveInMode(_mode))
                DetachUI();
        }

        private sealed class RefreshButtonStateUpdater : MonoBehaviour
        {
            internal UIButton Button { get; set; }

            private void Update()
            {
                if (Button == null)
                {
                    return;
                }

                var settings = ImprovedPublicTransport.ModSetting.Instance;
                var enabled = settings.AutoLineColorColorStrategy != ImprovedPublicTransport.ModSetting.AutoLineColorStrategy.Disabled
                    || settings.AutoLineColorNamingStrategyMode != ImprovedPublicTransport.ModSetting.AutoLineColorNamingStrategy.Disabled;

                Button.isEnabled = enabled;
                Button.tooltip = ImprovedPublicTransport.Localization.Get(
                    enabled ? "AUTOLINECOLOR_REFRESH_BUTTON_TOOLTIP" : "AUTOLINECOLOR_REFRESH_DISABLED_TOOLTIP");
            }
        }
        private void AttachUI()
        {
            //Console.Instance.Message("Attaching UI");

            var uiViewGO = GameObject.Find("UIView");
            if (uiViewGO == null)
            {
                Utils.LogError("UIExtender: UIView GameObject not found");
                return;
            }

            var ptwip = uiViewGO.transform.GetComponentInChildren<PublicTransportWorldInfoPanel>();
            if (ptwip == null)
            {
                Utils.LogError("UIExtender: PublicTransportWorldInfoPanel not found");
                return;
            }

            var ptwipUiPanel = ptwip.gameObject.GetComponent<UIPanel>();
            if (ptwipUiPanel == null)
            {
                Utils.LogError("UIExtender: UIPanel component not found on PublicTransportWorldInfoPanel");
                return;
            }

            var linesOverview = (UIButton)ptwipUiPanel.Find("LinesOverview");
            if (linesOverview == null)
            {
                Utils.LogError("UIExtender: LinesOverview button not found");
                return;
            }

            var buttonPanel = (UIPanel)linesOverview.parent;
            if (buttonPanel == null)
            {
                Utils.LogError("UIExtender: Button panel not found");
                return;
            }

            // Robust cleanup: search all child buttons for any existing RefreshNameAndColor button
            foreach (UIButton btn in buttonPanel.GetComponentsInChildren<UIButton>())
            {
                if (btn.name == "RefreshNameAndColor")
                {
                    buttonPanel.RemoveUIComponent(btn);
                }
            }

            _refreshBtn = buttonPanel.AddUIComponent<UIButton>();
            _refreshBtn.name = "RefreshNameAndColor";
            _refreshBtn.gameObject.AddComponent<RefreshButtonStateUpdater>().Button = _refreshBtn;

            _refreshBtn.text = ImprovedPublicTransport.Localization.Get("AUTOLINECOLOR_REFRESH_BUTTON");
            _refreshBtn.tooltip = ImprovedPublicTransport.Localization.Get("AUTOLINECOLOR_REFRESH_BUTTON_TOOLTIP");
            _refreshBtn.textScale = .75f;
            _refreshBtn.font = linesOverview.font;
            _refreshBtn.hoveredTextColor = new Color32(7, 132, 255, 255);
            _refreshBtn.textPadding = new RectOffset(10, 10, 4, 2);
            _refreshBtn.autoSize = true;
            _refreshBtn.anchor = UIAnchorStyle.Left | UIAnchorStyle.Right | UIAnchorStyle.CenterVertical;
            _refreshBtn.normalBgSprite = "ButtonMenu";
            _refreshBtn.hoveredBgSprite = "ButtonMenuHovered";
            _refreshBtn.disabledBgSprite = "ButtonMenuDisabled";
            //refreshBtn.focusedBgSprite = "ButtonMenuFocused";
            _refreshBtn.pressedBgSprite = "ButtonMenuPressed";

            _refreshBtn.SendToBack();
            buttonPanel.ResetLayout();

            _refreshBtnClick = (x, y) =>
            {
                ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
                if (lineId == 0) return;

                ColorMonitor.ForceRefreshLineNow(lineId);
            };

            _refreshBtn.eventClick += _refreshBtnClick;

            //Console.Instance.Message("Attached UI");
        }

        private void DetachUI()
        {
            //Console.Instance.Message("Detaching UI");

            // ReSharper disable once InvertIf
            if (_refreshBtn != null)
            {
                _refreshBtn.eventClick -= _refreshBtnClick;
                _refreshBtnClick = null;

                _refreshBtn.parent.RemoveUIComponent(_refreshBtn);
                Object.Destroy(_refreshBtn.gameObject);
                _refreshBtn = null;
            }

            //Console.Instance.Message("Detached UI");
        }
    }
}




