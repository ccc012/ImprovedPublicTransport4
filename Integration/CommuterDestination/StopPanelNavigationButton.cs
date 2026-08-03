// Adapted from Commuter Destination (MIT, Workshop 2475986859,
// github.com/Jameskmonger/CSL-ShowCommuterDestination) - see LICENSE.txt.
using ColossalFramework.UI;
using UnityEngine;

namespace CommuterDestination
{
    /// <summary>The "Previous"/"Next" stop buttons on the destination panel.</summary>
    internal sealed class StopPanelNavigationButton : UIButton
    {
        public StopPanelNavigationButton()
        {
            // Upstream pulls the font off the transport info view panel. That panel is not
            // guaranteed to exist yet when this is constructed, so fall back to leaving the
            // default font rather than throwing during UI construction.
            var infoView = GameObject.Find("(Library) PublicTransportInfoViewPanel");
            if (infoView != null)
            {
                var component = infoView.GetComponent<PublicTransportInfoViewPanel>();
                var label = component != null ? component.Find<UILabel>("Label") : null;
                if (label != null)
                {
                    font = label.font;
                }
            }

            size = new Vector2(110f, 30f);
            textPadding = new RectOffset(10, 10, 4, 0);
            textScale = 0.75f;

            normalBgSprite = "ButtonMenu";
            disabledBgSprite = "ButtonMenuDisabled";
            hoveredBgSprite = "ButtonMenuHovered";
            focusedBgSprite = "ButtonMenu";
            pressedBgSprite = "ButtonMenuPressed";

            textColor = new Color32(255, 255, 255, 255);
            disabledTextColor = new Color32(7, 7, 7, 255);
            hoveredTextColor = new Color32(255, 255, 255, 255);
            focusedTextColor = new Color32(255, 255, 255, 255);
            pressedTextColor = new Color32(30, 30, 44, 255);

            wordWrap = true;
        }
    }
}
