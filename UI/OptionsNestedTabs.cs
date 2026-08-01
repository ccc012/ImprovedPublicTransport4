// Nested sub-tabs inside an Options main page (short labels; wrap-safe).
using System;
using ColossalFramework.UI;
using CSLModsCommon.UI;
using CSLModsCommon.UI.Atlas;
using CSLModsCommon.UI.Buttons;
using CSLModsCommon.UI.Containers;
using CSLModsCommon.UI.OptionsPanel;
using CSLModsCommon.UI.Tab;
using UnityEngine;

namespace ImprovedPublicTransport.UI
{
    /// <summary>
    /// Builds a secondary TabBar + scroll pages inside a main Options tab.
    /// Keeps the top bar to ~6 main tabs while grouping dense settings under short sub-labels.
    /// </summary>
    internal static class OptionsNestedTabs
    {
        public const float SubTabBarHeight = 36f;

        public struct TabSpec
        {
            public string Id;
            public string Label;
            public Action<ScrollContainer> Fill;

            public TabSpec(string id, string label, Action<ScrollContainer> fill)
            {
                Id = id;
                Label = label;
                Fill = fill;
            }
        }

        /// <summary>
        /// Host must already be the main tab page (ScrollContainer). Replaces free-scroll content
        /// with a sub TabBar + one scroll pane per sub-tab.
        /// </summary>
        public static void Build(ScrollContainer host, params TabSpec[] tabs)
        {
            if (host == null || tabs == null || tabs.Length == 0)
            {
                return;
            }

            // Nested host: fixed layout (not free-scrolling the tab bar away).
            // Always use the Options panel section width — host.width is often 0 during Awake
            // before layout, which collapsed cards to ~1px and made labels look like single letters.
            host.AutoLayout = false;
            host.clipChildren = true;

            var width = OptionsPanelLayout.SectionWidth;
            var height = OptionsPanelLayout.ContainerSize.y;
            if (host.width < width)
            {
                host.width = width;
            }

            if (host.height < 200f)
            {
                host.height = height;
            }

            var bar = host.AddUIComponent<TabBar>();
            bar.relativePosition = new Vector3(0f, 0f);
            bar.size = new Vector2(width, SubTabBarHeight);
            bar.BgAtlas = Atlases.Shared;
            bar.BgSprites.SetValues(SharedAtlasKeys.RoundRect6);
            bar.BgColors.SetValues(UIColors.GroupBg1);
            bar.ColumnGap = 3;

            var logic = TabGroupLogic.Create().BindingTabBar(bar);
            var contentTop = SubTabBarHeight + 6f;
            var contentHeight = Mathf.Max(120f, height - contentTop);

            for (var i = 0; i < tabs.Length; i++)
            {
                var spec = tabs[i];
                var page = host.AddUIComponent<ScrollContainer>();
                page.relativePosition = new Vector3(0f, contentTop);
                page.size = new Vector2(width, contentHeight);
                page.AutoLayout = true;
                page.AutoLayoutPadding.SetTop(4);
                page.RowGap = 16;
                page.ScrollWheelAmount = 32;
                page.isVisible = false;

                logic.AddTab(spec.Id, spec.Label, page, b =>
                {
                    b.TextPadding.SetTop(2);
                    b.SetStyle(StyleType.OptionPanelStyle);
                    b.WordWrap = true;
                    b.TextScale = 0.85f;
                });

                try
                {
                    spec.Fill?.Invoke(page);
                }
                catch (Exception ex)
                {
                    Util.Utils.LogError($"Options nested tab '{spec.Id}' fill failed: {ex.Message}");
                }
            }

            logic.SelectPage(0);
            bar.RepositionButtons();
        }

        /// <summary>Enable/disable interactive controls on settings cards (parent → child cascade).</summary>
        public static void SetEnabled(UIComponent control, bool enabled)
        {
            if (control == null)
            {
                return;
            }

            control.isEnabled = enabled;
            control.opacity = enabled ? 1f : 0.45f;
        }
    }
}
