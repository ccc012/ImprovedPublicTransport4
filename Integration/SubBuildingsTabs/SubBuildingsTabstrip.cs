// Ported from BloodyPenguin's Sub-Buildings Tabs (Workshop 608517757, MIT - see LICENSE.txt),
// adapted to attach via a Harmony postfix instead of the original's polling MonoBehaviour, and
// hardened against UI elements ("RelocateAction") that do not exist on every panel type.
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using UnityEngine;

namespace SubBuildingsTabs
{
    /// <summary>
    /// A tab strip shown at the top of a building's info panel when that building has
    /// sub-buildings (e.g. an airport with a built-in metro station), letting the player switch
    /// the panel between the main building and each of its sub-buildings.
    /// </summary>
    public class SubBuildingsTabstrip : UITabstrip
    {
        public const string ComponentName = "IPT4_SubBuildingsTabstrip";

        private List<ushort> _idList;
        private UITextureAtlas _ingame;
        private UITextureAtlas _thumbnails;

        public override void Start()
        {
            base.Start();
            size = new Vector2(432, 25);
            // relativePosition is deliberately NOT set here - it used to be hardcoded to (13, -25)
            // in this method, which conflicted with Patch_BuildingWorldInfoPanel_OnSetTarget's own
            // re-assertion of the same property on every OnSetTarget call. Start() is deferred by
            // Unity to a later frame than the postfix that creates this component, so whichever one
            // ran last silently won - which meant tuning the postfix's offset value had no visible
            // effect, since this line always ran after it and stomped it back to the old value. The
            // postfix is now the single place that sets it.
            name = ComponentName;
            startSelectedIndex = 0;
            padding = new RectOffset(0, 3, 0, 0);
            _ingame = Resources.FindObjectsOfTypeAll<UITextureAtlas>().FirstOrDefault(a => a.name == "Ingame");
            _thumbnails = Resources.FindObjectsOfTypeAll<UITextureAtlas>().FirstOrDefault(a => a.name == "Thumbnails");
        }

        /// <summary>
        /// Rebuilds the tab list for the given building, or clears it if the building has no
        /// sub-buildings (or none are selected). Cheap to call every UpdateBindings - it bails out
        /// immediately if the currently shown building is already in the tab list.
        /// </summary>
        public void UpdateInfoPanelTabs(ushort buildingId)
        {
            if (buildingId == 0)
            {
                RemoveAllTabs();
                return;
            }

            var buildingManager = BuildingManager.instance;

            if (_idList != null && _idList.Contains(buildingId))
            {
                // Same cluster as last time - IF the cache is still actually valid. Building IDs are
                // recycled by the game once a building is demolished, so a stale _idList entry could
                // otherwise be silently reused for an unrelated new building that happens to land on
                // a recycled slot from the last cluster the player inspected (rare - needs a demolish
                // + rebuild to hit that exact ID - which matches the "hard to reproduce" reports of
                // the tab strip appearing on buildings that shouldn't have one). Cheaply re-walk the
                // parent chain to confirm buildingId still actually belongs to the cached main
                // building before trusting the cache.
                var checkId = buildingId;
                var walkGuard = 0;
                while (buildingManager.m_buildings.m_buffer[checkId].m_parentBuilding != 0)
                {
                    checkId = buildingManager.m_buildings.m_buffer[checkId].m_parentBuilding;
                    if (++walkGuard > 49152)
                    {
                        break;
                    }
                }

                if (checkId == _idList[0])
                {
                    // Skip rebuilding the tab buttons themselves (expensive, and unnecessary since
                    // the same buildings are still there), but the highlighted tab still has to track
                    // whichever building is ACTUALLY being shown now. Returning here unconditionally
                    // used to leave selectedIndex at whatever it was from the player's previous visit
                    // - e.g. reopening the main building after last leaving on its "Metro" sub-tab
                    // would show the main building's data while the tab strip kept "Metro"
                    // highlighted, since nothing here ever re-synced the two.
                    var currentIndex = _idList.IndexOf(buildingId);
                    if (currentIndex >= 0 && selectedIndex != currentIndex)
                    {
                        selectedIndex = currentIndex;
                    }

                    return;
                }
                // Cache is stale (buildingId's actual parent chain no longer matches) - fall through
                // and rebuild from scratch below.
            }

            RemoveAllTabs();

            var selectedBuilding = buildingManager.m_buildings.m_buffer[buildingId];
            if (selectedBuilding.m_parentBuilding == 0 && selectedBuilding.m_subBuilding == 0)
            {
                return;
            }

            var mainBuildingId = buildingId;
            var guard = 0;
            while (buildingManager.m_buildings.m_buffer[mainBuildingId].m_parentBuilding != 0)
            {
                mainBuildingId = buildingManager.m_buildings.m_buffer[mainBuildingId].m_parentBuilding;
                // A building chain cannot legitimately be longer than the building buffer itself;
                // this only trips on a corrupted m_parentBuilding cycle, and bails instead of hanging.
                if (++guard > 49152)
                {
                    return;
                }
            }

            _idList = new List<ushort> { mainBuildingId };
            var subBuildingId = buildingManager.m_buildings.m_buffer[mainBuildingId].m_subBuilding;
            guard = 0;
            while (subBuildingId != 0)
            {
                var building = buildingManager.m_buildings.m_buffer[subBuildingId];
                if (!IsDummy(ref building))
                {
                    _idList.Add(subBuildingId);
                }
                subBuildingId = building.m_subBuilding;
                if (++guard > 49152)
                {
                    break;
                }
            }

            if (_idList.Count == 1)
            {
                RemoveAllTabs();
                return;
            }

            var counter = 0;
            foreach (var id in _idList)
            {
                var info = buildingManager.m_buildings.m_buffer[id].Info;
                if (info == null)
                {
                    counter++;
                    continue;
                }

                var tabButton = AddUIComponent<UIButton>();
                tabButton.size = new Vector2(58, 25);
                tabButton.normalBgSprite = "SubBarButtonBase";
                tabButton.disabledBgSprite = "SubBarButtonBaseDisabled";
                tabButton.pressedBgSprite = "SubBarButtonBasePressed";
                tabButton.hoveredBgSprite = "SubBarButtonBaseHovered";
                tabButton.focusedBgSprite = "SubBarButtonBaseFocused";
                tabButton.name = ComponentName + "_Tab" + counter;

                var sprite = tabButton.AddUIComponent<UISprite>();
                sprite.size = new Vector2(35, 25);
                sprite.isInteractive = false;
                sprite.relativePosition = new Vector2(12, 0);
                SetTabIcon(sprite, info);

                tabButton.eventClicked += SelectSub;
                tabButton.isVisible = true;

                if (_idList[counter] == buildingId)
                {
                    selectedIndex = counter;
                }
                counter++;
            }
        }

        private void SetTabIcon(UISprite sprite, BuildingInfo info)
        {
            var itemClass = info.m_class;
            if (itemClass != null && itemClass.GetZone() != ItemClass.Zone.None)
            {
                sprite.spriteName = "Zoning" + itemClass.GetZone() + "Focused";
                sprite.atlas = _thumbnails;
                return;
            }

            var service = info.GetService();
            if (service == ItemClass.Service.None)
            {
                return;
            }

            var subService = info.GetSubService();
            sprite.atlas = _ingame;
            sprite.spriteName = subService == ItemClass.SubService.None
                ? "ToolbarIcon" + service
                : "SubBar" + subService;
        }

        public void RemoveAllTabs()
        {
            _idList = null;
            if (tabs == null || tabs.Count == 0)
            {
                return;
            }

            ShowRelocateButton(true);
            foreach (var child in tabs.Clone().Where(child => child != null))
            {
                child.Hide();
                RemoveUIComponent(child);
                Destroy(child.gameObject);
            }
        }

        private static bool IsDummy(ref Building building)
        {
            var info = building.Info;
            return info == null || info.m_buildingAI is DummyBuildingAI;
        }

        private void SelectSub(UIComponent component, UIMouseEventParameter param)
        {
            if (_idList == null || selectedIndex < 0 || selectedIndex >= _idList.Count)
            {
                return;
            }

            var buildingId = _idList[selectedIndex];
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            if (building.Info != null)
            {
                EnsureLocalizedName(building.Info.name);
            }

            // Vanilla only shows the "relocate" action for the root building - a sub-building
            // (e.g. an airport's built-in metro) is not independently movable.
            ShowRelocateButton(selectedIndex == 0);

            DefaultTool.OpenWorldInfoPanel(new InstanceID { Building = buildingId }, building.m_position);
        }

        /// <summary>
        /// Some building prefabs (mainly sub-buildings never meant to be selected directly) have
        /// no BUILDING_TITLE/DESC locale entry of their own, which would otherwise show the raw
        /// prefab name in the panel. Registers empty/name fallbacks the same way the original mod
        /// did, so opening the tab does not show a raw asset name.
        /// </summary>
        // LocaleManager exposes no public accessor for the active Locale (only supportedLocales /
        // supportedLocaleIDs) - m_Locale is private in every game version checked. Cached once
        // rather than re-reflected on every tab click.
        private static readonly FieldInfo LocaleField =
            typeof(LocaleManager).GetField("m_Locale", BindingFlags.NonPublic | BindingFlags.Instance);

        private static void EnsureLocalizedName(string buildingInfoName)
        {
            if (string.IsNullOrEmpty(buildingInfoName) || LocaleField == null)
            {
                return;
            }

            var locale = LocaleField.GetValue(SingletonLite<LocaleManager>.instance) as Locale;
            if (locale == null)
            {
                return;
            }

            AddIfMissing(locale, "BUILDING_TITLE", buildingInfoName, buildingInfoName);
            AddIfMissing(locale, "BUILDING_DESC", buildingInfoName, string.Empty);
            AddIfMissing(locale, "BUILDING_SHORT_DESC", buildingInfoName, string.Empty);
        }

        private static void AddIfMissing(Locale locale, string identifier, string key, string value)
        {
            var localeKey = new Locale.Key { m_Identifier = identifier, m_Key = key };
            if (!locale.Exists(localeKey))
            {
                locale.AddLocalizedString(localeKey, value);
            }
        }

        private static void ShowRelocateButton(bool show)
        {
            // Not every panel type has a "RelocateAction" button (it only exists on panels for
            // player-placed service buildings) - GameObject.Find returns null on the rest, and the
            // original mod's unconditional GetComponent<UIButton>() would throw a
            // NullReferenceException there. Guard it instead of assuming it always exists.
            var relocateObject = GameObject.Find("RelocateAction");
            var button = relocateObject != null ? relocateObject.GetComponent<UIButton>() : null;
            if (button == null)
            {
                return;
            }

            if (show)
            {
                button.Show();
            }
            else
            {
                button.Hide();
            }
        }
    }
}
