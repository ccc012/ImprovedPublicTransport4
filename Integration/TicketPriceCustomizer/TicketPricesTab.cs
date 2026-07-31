using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.UI;
using ImprovedPublicTransport.UI.AlgernonCommons;
using ImprovedPublicTransport.Util;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.Integration.TicketPriceCustomizer
{
    /// <summary>
    /// Adds a "Ticket Prices" tab to the Economy panel with budget-style sliders
    /// for each public transport type (0%–250%), with day/night support matching
    /// the game's budget tab visual style.</summary>
    public static class TicketPricesTab
    {
        private static bool s_initialized;
        private static bool s_localeHooked;
        private static UIScrollablePanel s_ticketPricesContainer;
        private static readonly List<TicketPriceSliderRow> s_sliderRows = new List<TicketPriceSliderRow>();
        private static readonly Dictionary<string, UITextureAtlas> s_customIconAtlases = new Dictionary<string, UITextureAtlas>();

        // This tab's tab-button text, headers, tooltips and per-row transport labels are all set
        // once at build time via Localization.Get(...) - unlike the mod's own Options panel (which
        // CSLModsCommon fully rebuilds on a locale change), nothing ever told this tab to redraw
        // itself, so switching the in-game language left it showing whatever language it happened to
        // be built in until the next level load. Tearing down and rebuilding on the same locale-
        // change events the Options panel already uses is simpler and more robust than patching every
        // individual label/tooltip call site to refresh itself.
        private static void HookLocaleChangeOnce()
        {
            if (s_localeHooked)
            {
                return;
            }

            s_localeHooked = true;
            ColossalFramework.Globalization.LocaleManager.eventLocaleChanged += OnLocaleChanged;
            CSLModsCommon.Manager.LocalizationManager.ModActiveLocaleChanged += (_, __) => OnLocaleChanged();
        }

        private static void OnLocaleChanged()
        {
            if (!s_initialized)
            {
                return;
            }

            var economyPanel = GetEconomyPanel();
            if (economyPanel == null)
            {
                return;
            }

            HideTicketPricesTab(economyPanel);
            InjectTab(economyPanel);
        }

        // Passenger count refresh timer
        private static float s_refreshAccumulator = 0f;
        private const float RefreshInterval = 5.0f; // seconds between passenger count refreshes

        // Transport types with their sprite names and display order
        private static readonly TransportTypeInfo[] s_transportTypes = new TransportTypeInfo[]
        {
            new TransportTypeInfo("Bus",            "SubBarPublicTransportBus",          ItemClass.SubService.PublicTransportBus),
            new TransportTypeInfo("Trolleybus",     "SubBarPublicTransportTrolleybus",   ItemClass.SubService.PublicTransportTrolleybus),
            new TransportTypeInfo("Tram",           "SubBarPublicTransportTram",         ItemClass.SubService.PublicTransportTram),
            new TransportTypeInfo("Metro",          "SubBarPublicTransportMetro",        ItemClass.SubService.PublicTransportMetro),
            new TransportTypeInfo("Train",          "SubBarPublicTransportTrain",        ItemClass.SubService.PublicTransportTrain),
            new TransportTypeInfo("Monorail",       "SubBarPublicTransportMonorail",     ItemClass.SubService.PublicTransportMonorail),
            new TransportTypeInfo("CableCar",       "SubBarPublicTransportCableCar",     ItemClass.SubService.PublicTransportCableCar),
            new TransportTypeInfo("Ship",           "SubBarPublicTransportShip",         ItemClass.SubService.PublicTransportShip),
            new TransportTypeInfo("Ferry",          "SubBarPublicTransportFerry",         ItemClass.SubService.PublicTransportShip), // Custom icon
            new TransportTypeInfo("Plane",          "SubBarPublicTransportPlane",        ItemClass.SubService.PublicTransportPlane),
            new TransportTypeInfo("Blimp",          "SubBarPublicTransportBlimp",        ItemClass.SubService.PublicTransportPlane), // Custom icon
            new TransportTypeInfo("Helicopter",     "SubBarPublicTransportHelicopter",   ItemClass.SubService.PublicTransportPlane), // Custom icon
            new TransportTypeInfo("Taxi",           "SubBarPublicTransportTaxi",         ItemClass.SubService.PublicTransportTaxi),
            new TransportTypeInfo("SightseeingBus", "SubBarPublicTransportTours",        ItemClass.SubService.PublicTransportTours),
            new TransportTypeInfo("IntercityBus",   "SubBarPublicTransportIntercity",    ItemClass.SubService.PublicTransportBus), // Custom icon
        };

        /// <summary>
        /// Called from a Harmony postfix on EconomyPanel.Awake to inject the Ticket Prices tab.
        /// </summary>
        public static void InjectTab(EconomyPanel economyPanel)
        {
            try
            {
                HookLocaleChangeOnce();

                // Honor user setting: don't inject the Ticket Prices UI if disabled
                var currentSettings = ModSetting.Instance;
                if (currentSettings.TicketPriceCustomizerMode != ModSetting.TicketPriceCustomizerModes.Enabled)
                {

                    Cleanup();
                    return;
                }

                if (s_initialized) return;

                // Load custom icon atlases
                LoadCustomIconAtlases();

                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log("TicketPricesTab: Finding EconomyTabstrip and EconomyContainer");
                }
                var tabStrip = economyPanel.Find<UITabstrip>("EconomyTabstrip");
                var tabContainer = economyPanel.Find<UITabContainer>("EconomyContainer");
                if (tabStrip == null || tabContainer == null)
                {
                    Utils.LogError("TicketPricesTab: Could not find EconomyTabstrip or EconomyContainer");
                    return;
                }



                // Preserve the currently selected page index and page reference so we can restore it after adding a new tab.
                int existingSelectedIndex = -1;
                UIComponent existingSelectedPage = null;
                try
                {
                    existingSelectedIndex = tabStrip.selectedIndex;
                    if (existingSelectedIndex >= 0 && existingSelectedIndex < tabContainer.components.Count)
                    {
                        existingSelectedPage = tabContainer.components[existingSelectedIndex];
                    }
                }
                catch { existingSelectedPage = null; }

                // Track pre-injection pages and use diff-based detection to avoid ambiguous component order.
                var existingPages = tabContainer.components.OfType<UIPanel>().ToList();

                // Create the tab button styled like the existing ones
                var tabButton = tabStrip.AddTab("TicketPrices");
                tabButton.name = TabName;

                tabButton.text = Localization.Get("ECONOMY_TAB_TICKET_PRICES");
                // Style it to match the other economy tab buttons
                StyleTabButton(tabButton, tabStrip);

                // Locate newly created page by excluding known existing pages.
                var newPages = tabContainer.components.OfType<UIPanel>().Except(existingPages).ToList();
                var page = newPages.FirstOrDefault();
                if (page == null)
                {
                    // Fallback: try find by name if previous injection left one behind.
                    page = tabContainer.components.OfType<UIPanel>().FirstOrDefault(c => c.name == PageName);
                }
                if (page != null)
                {
                    page.name = PageName;
                }
                if (page == null)
                {
                    Utils.LogError("TicketPricesTab: Could not find newly created tab page");
                    return;
                }

                // Keep this tab hidden until it is actually selected.
                page.isVisible = false;

                // Ensure the previously selected tab remains selected, forcing the tab container to hide newly added page.
                if (existingSelectedIndex >= 0 && existingSelectedIndex < tabStrip.tabCount)
                {
                    tabStrip.selectedIndex = existingSelectedIndex;
                }

                if (existingSelectedIndex >= 0 && existingSelectedIndex < tabContainer.components.Count)
                {
                    tabContainer.selectedIndex = existingSelectedIndex;
                }

                tabStrip.Invalidate();

                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log($"TicketPricesTab: Page found, size={page.width}x{page.height}; building content");
                }
                page.autoLayout = false;
                page.size = tabContainer.size;
                page.isVisible = false; // Hidden until this tab is selected; UITabstrip shows it on click

                // Build the scrollable content
                BuildTicketPricesContent(page);

                s_initialized = true;
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log("TicketPricesTab: Successfully added Ticket Prices tab to Economy panel");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPricesTab: Failed to inject tab: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Cleanup when level unloads.
        /// </summary>
        public static void Cleanup()
        {
            s_initialized = false;

            if (s_ticketPricesContainer != null)
            {
                try
                {
                    UnityEngine.Object.Destroy(s_ticketPricesContainer.gameObject);
                }
                catch (Exception ex)
                {
                    Utils.LogError($"TicketPricesTab: Cleanup could not destroy container: {ex.Message}");
                }
                finally
                {
                    s_ticketPricesContainer = null;
                }
            }

            s_sliderRows.Clear();
            s_customIconAtlases.Clear();
            s_refreshAccumulator = 0f;
        }

        private const string TabName = "TicketPrices";
        private const string PageName = "TicketPricesPanel";

        /// <summary>
        /// Find the active EconomyPanel instance via the UI view.
        /// </summary>
        private static EconomyPanel GetEconomyPanel()
        {
            var uiView = UIView.GetAView();
            if (uiView == null)
            {
                return null;
            }
            return uiView.GetComponentsInChildren<EconomyPanel>().FirstOrDefault();
        }

        private static UITabstrip GetEconomyTabstrip(EconomyPanel economyPanel)
        {
            return economyPanel?.Find<UITabstrip>("EconomyTabstrip");
        }

        private static UITabContainer GetEconomyTabContainer(EconomyPanel economyPanel)
        {
            return economyPanel?.Find<UITabContainer>("EconomyContainer");
        }

        private static void HideTicketPricesTab(EconomyPanel economyPanel)
        {
            try
            {
                var tabStrip = GetEconomyTabstrip(economyPanel);
                var tabContainer = GetEconomyTabContainer(economyPanel);
                if (tabStrip != null)
                {
                    var tab = tabStrip.tabs.FirstOrDefault(t => t.name == TabName);
                    if (tab != null)
                    {
                        UnityEngine.Object.Destroy(tab.gameObject);
                    }
                    tabStrip.Invalidate();
                }
                if (tabContainer != null)
                {
                    var page = tabContainer.components.FirstOrDefault(c => c.name == PageName);
                    if (page != null)
                    {
                        UnityEngine.Object.Destroy(page.gameObject);
                    }
                }

                // In the unlikely case we still have a direct reference to the container, destroy it too.
                if (s_ticketPricesContainer != null)
                {
                    try
                    {
                        UnityEngine.Object.Destroy(s_ticketPricesContainer.gameObject);
                    }
                    catch (Exception ex2)
                    {
                        Utils.LogError($"TicketPricesTab: HideTicketPricesTab could not destroy ticketPricesContainer: {ex2.Message}");
                    }
                    finally
                    {
                        s_ticketPricesContainer = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPricesTab: HideTicketPricesTab failed: {ex.Message}");
            }
            finally
            {
                // Always cleanup UI state and watcher as soon as tab is hidden.
                Cleanup();
            }
        }

        private static void ShowTicketPricesTab(EconomyPanel economyPanel)
        {
            if (economyPanel == null)
                return;

            var tabStrip = GetEconomyTabstrip(economyPanel);
            var tabContainer = GetEconomyTabContainer(economyPanel);
            var tab = tabStrip?.tabs.FirstOrDefault(t => t.name == TabName);
            var page = tabContainer?.components.FirstOrDefault(c => c.name == PageName);

            if (tab != null && page != null)
            {
                // Show tab button; do not force the page visible here, because tab selection should drive visibility.
                tab.isVisible = true;

                // Make sure existing selected tab remains selected in container so new ticket page doesn't appear over existing tab.
                if (tabStrip != null && tabContainer != null)
                {
                    tabContainer.selectedIndex = tabStrip.selectedIndex;
                }

                tabStrip?.Invalidate();
                return;
            }

            // If we do not have a tab/page, inject it.
            InjectTab(economyPanel);
        }

        public static void UpdateTabState()
        {
            var economyPanel = GetEconomyPanel();
            if (economyPanel == null)
            {
                return;
            }

            var enabled = ModSetting.Instance.TicketPriceCustomizerMode == ModSetting.TicketPriceCustomizerModes.Enabled;
            if (enabled)
            {
                if (!s_initialized)
                {
                    InjectTab(economyPanel);
                }
                else
                {
                    ShowTicketPricesTab(economyPanel);
                }
            }
            else
            {
                HideTicketPricesTab(economyPanel);
            }
        }

        /// <summary>
        /// Called every frame from ColorMonitor.OnUpdate. Refreshes passenger count labels
        /// at most every <see cref="RefreshInterval"/> seconds, only when the tab is visible.
        /// </summary>
        public static void OnUpdate(float realTimeDelta)
        {
            if (!s_initialized || s_sliderRows.Count == 0) return;
            if (s_ticketPricesContainer == null || !s_ticketPricesContainer.isVisible) return;

            s_refreshAccumulator += realTimeDelta;
            if (s_refreshAccumulator < RefreshInterval) return;
            s_refreshAccumulator = 0f;

            foreach (var row in s_sliderRows)
            {
                if (row.TotalLabel != null)
                    UpdateTotalLabel(row.TransportType.Name, row.TotalLabel);
            }
        }

        /// <summary>
        /// Loads custom icons (Ferry, Blimp, Heli) into per-sprite atlases.
        /// Uses PluginManager to get the correct mod folder path (Assembly.Location/CodeBase
        /// return the game directory in CS1's Mono runtime, not the mod folder).
        /// </summary>
        private static void LoadCustomIconAtlases()
        {
            if (s_customIconAtlases.Count > 0) return;
            try
            {
                // PluginManager is the correct CS1 way to get the mod's folder path
                var modDir = TranslationFramework.Util.AssemblyPath(typeof(Mod));
                var iconsDir = Path.Combine(modDir, "Resources");

                if (!Directory.Exists(iconsDir))
                {
                    Utils.LogError($"TicketPricesTab: Resources directory not found at {iconsDir}");
                    return;
                }

                var iconFiles = new[]
                {
                    new { SpriteName = "SubBarPublicTransportFerry", FileName = "SubBarPublicTransportFerry.png" },
                    new { SpriteName = "SubBarPublicTransportBlimp", FileName = "SubBarPublicTransportBlimp.png" },
                    new { SpriteName = "SubBarPublicTransportHelicopter",  FileName = "SubBarPublicTransportHelicopter.png" },
                    new { SpriteName = "SubBarPublicTransportIntercity", FileName = "SubBarPublicTransportIntercity.png" },
                };

                foreach (var info in iconFiles)
                {
                    var pngPath = Path.Combine(iconsDir, info.FileName);
                    if (!File.Exists(pngPath))
                    {
                        Utils.LogError($"TicketPricesTab: Icon file not found: {pngPath}");
                        continue;
                    }

                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!texture.LoadImage(File.ReadAllBytes(pngPath)))
                    {
                        Utils.LogError($"TicketPricesTab: LoadImage failed for {pngPath}");
                        continue;
                    }
                    texture.name = info.SpriteName;

                    // Create a single-sprite atlas: clone the default atlas material (same shader/blend),
                    // set its texture to our PNG, add the sprite with full-UV region, rebuild the index.
                    var atlas = ScriptableObject.CreateInstance<UITextureAtlas>();
                    atlas.name = info.SpriteName;

                    var uiView = UIView.GetAView();
                    if (uiView == null || uiView.defaultAtlas == null)
                    {
                        Utils.LogError($"TicketPricesTab: UIView or defaultAtlas not available for icon {info.SpriteName}");
                        continue;
                    }

                    atlas.material = UnityEngine.Object.Instantiate(uiView.defaultAtlas.material);
                    atlas.material.mainTexture = texture;
                    atlas.AddSprite(new UITextureAtlas.SpriteInfo
                    {
                        name    = info.SpriteName,
                        texture = texture,
                        region  = new Rect(0f, 0f, 1f, 1f),
                    });
                    atlas.RebuildIndexes();

                    s_customIconAtlases[info.SpriteName] = atlas;
                    if (Diagnostics.VerboseRuntimeLogs)
                    {
                        Utils.Log($"TicketPricesTab: Loaded icon {info.SpriteName} ({texture.width}x{texture.height})");
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPricesTab: Failed loading custom icon atlases: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void StyleTabButton(UIButton tabButton, UITabstrip tabStrip)
        {
            // Copy style from an existing tab button
            var budgetBtn = tabStrip.Find<UIButton>("Budget");
            if (budgetBtn != null)
            {
                tabButton.normalBgSprite = budgetBtn.normalBgSprite;
                tabButton.focusedBgSprite = budgetBtn.focusedBgSprite;
                tabButton.hoveredBgSprite = budgetBtn.hoveredBgSprite;
                tabButton.pressedBgSprite = budgetBtn.pressedBgSprite;
                tabButton.disabledBgSprite = budgetBtn.disabledBgSprite;
                tabButton.textColor = budgetBtn.textColor;
                tabButton.hoveredTextColor = budgetBtn.hoveredTextColor;
                tabButton.pressedTextColor = budgetBtn.pressedTextColor;
                tabButton.focusedTextColor = budgetBtn.focusedTextColor;
                tabButton.disabledTextColor = budgetBtn.disabledTextColor;
                tabButton.textScale = budgetBtn.textScale;
                tabButton.textPadding = budgetBtn.textPadding;
                tabButton.autoSize = budgetBtn.autoSize;
                tabButton.size = budgetBtn.size;
            }
        }

        private static void ApplyTransportIcon(UISprite icon, TransportTypeInfo transportType)
        {
            var spriteName = transportType.SpriteName;
            var atlas = UIView.GetAView().defaultAtlas;

            if (s_customIconAtlases.TryGetValue(spriteName, out var customAtlas) && customAtlas != null)
            {
                atlas = customAtlas;
            }
            else if (transportType.Name == "IntercityBus")
            {
                // Prefer the dedicated icon, but never leave the row blank if that sprite fails.
                spriteName = "SubBarPublicTransportBus";
            }

            icon.atlas = atlas;
            icon.spriteName = spriteName;
            icon.color = Color.white;
        }

        private static void BuildTicketPricesContent(UIPanel page)
        {
            if (Diagnostics.VerboseRuntimeLogs)
            {
                Utils.Log($"TicketPricesTab: BuildTicketPricesContent entered, page={page.width}x{page.height}");
            }
            page.autoLayout = false;
            // Ensure page has a valid size (if not set, defer initialization)
            if (page.width <= 0 || page.height <= 0)
            {
                Utils.LogError("TicketPricesTab: Page has invalid size at initialization");
                return;
            }

            // Load current settings
            var settings = ModSetting.Instance;
            if (settings.TicketPriceCustomizer == null)
                settings.TicketPriceCustomizer = new ModSetting.TicketPriceCustomizerSettings();

            // Create main container with padding matching budget panel
            const float SIDE_PAD = 45f;        // Outer left/right padding
            const float COL_GAP = 55f;         // Gap between columns
            
            var mainContainer = page.AddUIComponent<UIPanel>();
            mainContainer.autoLayout = false;
            mainContainer.relativePosition = new Vector3(SIDE_PAD, 10f);
            mainContainer.size = new Vector2(page.width - SIDE_PAD * 2f, page.height - 60f);
            // Keep sizing in sync if the economy panel resizes
            page.eventSizeChanged += (c, s) =>
            {
                if (mainContainer != null)
                {
                    mainContainer.size = new Vector2(page.width - SIDE_PAD * 2f, page.height - 60f);
                }
            };

            // Separate transport types into land (left) and air/water (right)
            var landTransport = new List<TransportTypeInfo>();
            var airWaterTransport = new List<TransportTypeInfo>();

            foreach (var transportType in s_transportTypes)
            {
                if (!IsTransportEnabled(transportType)) continue;
                if (!HasRequiredDlc(transportType)) continue;
                if (!IsTransportLoaded(transportType)) continue;

                if (transportType.Name == "Ship" || transportType.Name == "Ferry" || 
                    transportType.Name == "Plane" || transportType.Name == "Blimp" || transportType.Name == "Helicopter")
                {
                    airWaterTransport.Add(transportType);
                }
                else
                {
                    landTransport.Add(transportType);
                }
            }

            float columnWidth = (mainContainer.width - COL_GAP) / 2f; // Two equal-width columns
            float columnHeight = mainContainer.height;

            // Left column (Land transport)
            var leftColumn = mainContainer.AddUIComponent<UIScrollablePanel>();
            leftColumn.autoLayout = true;
            leftColumn.autoLayoutDirection = LayoutDirection.Vertical;
            // Was 2px - rows read as one solid block at that gap. 8px gives each row visible
            // breathing room without needing to scroll noticeably more to see the same rows.
            leftColumn.autoLayoutPadding = new RectOffset(0, 0, 0, 8);
            leftColumn.clipChildren = true;
            leftColumn.relativePosition = Vector3.zero;
            leftColumn.size = new Vector2(columnWidth, columnHeight);
            leftColumn.scrollWheelDirection = UIOrientation.Vertical;
            leftColumn.builtinKeyNavigation = true;

            // Right column (Air/Water transport) 
            var rightColumn = mainContainer.AddUIComponent<UIScrollablePanel>();
            rightColumn.autoLayout = true;
            rightColumn.autoLayoutDirection = LayoutDirection.Vertical;
            rightColumn.autoLayoutPadding = new RectOffset(0, 0, 0, 8);
            rightColumn.clipChildren = true;
            rightColumn.relativePosition = new Vector3(columnWidth + COL_GAP, 0);
            rightColumn.size = new Vector2(columnWidth, columnHeight);
            rightColumn.scrollWheelDirection = UIOrientation.Vertical;
            rightColumn.builtinKeyNavigation = true;

            if (Diagnostics.VerboseRuntimeLogs)
            {
                Utils.Log($"TicketPricesTab: Creating {landTransport.Count} land rows, {airWaterTransport.Count} air/water rows");
            }
            int landIndex = 0;
            foreach (var transportType in landTransport)
            {

                var row = CreateSliderRow(leftColumn, transportType, landIndex);
                if (row != null)
                {
                    s_sliderRows.Add(row);
                    landIndex++;
                }
            }

            int airIndex = 0;
            foreach (var transportType in airWaterTransport)
            {

                var row = CreateSliderRow(rightColumn, transportType, airIndex);
                if (row != null)
                {
                    s_sliderRows.Add(row);
                    airIndex++;
                }
            }


            s_ticketPricesContainer = leftColumn; // Store reference to main container

            // Trigger an immediate passenger-count refresh the first time the tab is opened
            // (and again every subsequent open), rather than waiting the full RefreshInterval.
            page.eventVisibilityChanged += (c, visible) =>
            {
                if (visible) s_refreshAccumulator = RefreshInterval;
            };
        }

        private static bool IsTransportEnabled(TransportTypeInfo info)
        {
            switch (info.Name)
            {
                case "IntercityBus":
                    return ModSetting.Instance.EnableIntercityBusControl;
                default:
                    return true;
            }
        }

        private static bool IsTransportLoaded(TransportTypeInfo info)
        {
            try
            {
                // Ferry/blimp/helicopter should always show in the ticket prices tab if this integration is active,
                // even when no vehicle is currently instantiated, so users can configure them.
                if (info.Name == "Ferry" || info.Name == "Blimp" || info.Name == "Helicopter")
                {
                    return true;
                }

                // Check if the transport info prefab exists for this type
                string transportInfoName = GetTransportInfoName(info.Name);
                if (transportInfoName == null) return true; // Unknown types always shown
                var prefab = PrefabCollection<TransportInfo>.FindLoaded(transportInfoName);
                return prefab != null;
            }
            catch
            {
                return true; // Default to showing if we can't check
            }
        }

        /// <summary>
        /// Returns true if the DLC required for this transport type is owned.
        /// Types with no DLC requirement always return true.
        /// </summary>
        private static bool HasRequiredDlc(TransportTypeInfo info)
        {
            switch (info.Name)
            {
                case "Tram":
                    return SteamHelper.IsDLCOwned(SteamHelper.DLC.SnowFallDLC);
                case "Taxi":
                    return SteamHelper.IsDLCOwned(SteamHelper.DLC.AfterDarkDLC);
                case "Ferry":
                case "Blimp":
                case "Monorail":
                case "CableCar":
                    return SteamHelper.IsDLCOwned(SteamHelper.DLC.InMotionDLC);
                case "Trolleybus":
                case "IntercityBus":
                case "Helicopter":
                    return SteamHelper.IsDLCOwned(SteamHelper.DLC.UrbanDLC);  // Sunset Harbor = UrbanDLC
                case "SightseeingBus":
                    return SteamHelper.IsDLCOwned(SteamHelper.DLC.ParksDLC);
                default:
                    return true; // Bus, Metro, Train, Ship, Plane — base game
            }
        }

        private static TicketPriceSliderRow CreateSliderRow(UIScrollablePanel container, TransportTypeInfo transportType, int index)
        {
            // Try the game's BudgetItem prefab first — gives the correct dual-handle visual for free.
            // Falls back to a custom row if the template is unavailable.
            var row = CreateSliderRowFromTemplate(container, transportType, index);
            if (row != null) return row;
            if (Diagnostics.VerboseRuntimeLogs)
            {
                Utils.Log($"TicketPricesTab: Template unavailable for {transportType.Name}, using fallback");
            }
            return CreateSliderRowFallback(container, transportType, index);
        }

        // Uses the game's own BudgetItem prefab — visually identical to the Budget panel.
        // Reflects into BudgetItem for slider/label refs, then destroys the MonoBehaviour so
        // EconomyPanel cannot call Init() and override our range/values.
        private static TicketPriceSliderRow CreateSliderRowFromTemplate(
            UIScrollablePanel container, TransportTypeInfo transportType, int index)
        {

            var templateGO   = UITemplateManager.GetAsGameObject("BudgetItem");

            var rowComponent = container.AttachUIComponent(templateGO);
            
            // Make rows narrower than container to prevent cutoff (leave 30px margin)
            rowComponent.width = container.width - 30f;

            // Reflect into BudgetItem to grab the serialized UI references
            var budgetItem   = ((Component)rowComponent).GetComponent<BudgetItem>();
            if (budgetItem == null)
            {
                Utils.LogError($"TicketPricesTab: BudgetItem component not found on row for {transportType.Name}");
                UnityEngine.Object.Destroy(rowComponent.gameObject);
                return null;
            }

            var biType       = typeof(BudgetItem);
            var flags        = BindingFlags.Instance | BindingFlags.NonPublic;
            
            UISlider daySlider = null;
            UISlider nightSlider = null;
            UILabel dayLabel = null;
            UILabel nightLabel = null;
            UILabel totalLabel = null;
            
            try
            {
                daySlider    = (UISlider)biType.GetField("m_DaySlider",           flags).GetValue(budgetItem);
                nightSlider  = (UISlider)biType.GetField("m_NightSlider",          flags).GetValue(budgetItem);
                dayLabel     = (UILabel) biType.GetField("m_DayPercentageLabel",  flags).GetValue(budgetItem);
                nightLabel   = (UILabel) biType.GetField("m_NightPercentageLabel",flags).GetValue(budgetItem);
                totalLabel   = (UILabel) biType.GetField("m_TotalLabel",          flags).GetValue(budgetItem);
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPricesTab: BudgetItem reflection failed for {transportType.Name}: {ex.Message}");
                UnityEngine.Object.Destroy(rowComponent.gameObject);
                return null;
            }

            if (daySlider == null || dayLabel == null || totalLabel == null)
            {
                Utils.LogError($"TicketPricesTab: BudgetItem reflection returned nulls for {transportType.Name} — daySlider={daySlider}, dayLabel={dayLabel}, totalLabel={totalLabel}");
                UnityEngine.Object.Destroy(rowComponent.gameObject);
                return null;
            }
            ((Behaviour)budgetItem).enabled = false;
            UnityEngine.Object.Destroy(budgetItem);

            // Alternating row background — use the same colors as the Budget tab itself
            var backDivider = rowComponent.Find<UISlicedSprite>("BackDivider");
            if (backDivider != null)
            {
                var ep = ToolsModifierControl.economyPanel;
                // Vanilla's own divider colors are tuned to blend into the Budget tab's background -
                // exactly what makes them hard to tell apart from the Economy panel behind this tab.
                // Bumping the alpha (not swapping to a different hue) keeps the same vanilla look but
                // makes the alternating rows actually read as a distinct row, not a wash of one color.
                var baseColor = (index % 2 == 0) ? ep.m_BackDividerColor : ep.m_BackDividerAltColor;
                backDivider.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Min(1f, baseColor.a + 60f / 255f));
            }

            // Transport icon
            var icon = rowComponent.Find<UISprite>("Icon");
            if (icon != null)
            {
                ApplyTransportIcon(icon, transportType);
            }
            else
            {
                Utils.LogError($"TicketPricesTab: Could not find Icon sprite in row for {transportType.Name}");
            }

            // Day slider — our range / initial value
            // Note: BudgetItem template already has a static "%" label next to the value;
            // set text to the number only to avoid showing "100%%".
            float dayPercent             = GetMultiplier(transportType.Name, false) * 100f;
            daySlider.minValue           = 0f;
            daySlider.maxValue           = 250f;
            daySlider.stepSize           = 5f;
            daySlider.scrollWheelAmount  = 5f;
            daySlider.value              = dayPercent;
            dayLabel.text                = Mathf.RoundToInt(dayPercent).ToString();
            daySlider.tooltip            = GetTransportTooltip(transportType.Name);

            // Night slider — always configure (day/night cycle is vanilla, not After Dark)
            if (nightSlider != null)
            {
                float nightPercent           = GetMultiplier(transportType.Name, true) * 100f;
                nightSlider.minValue         = 0f;
                nightSlider.maxValue         = 250f;
                nightSlider.stepSize         = 5f;
                nightSlider.scrollWheelAmount = 5f;
                nightSlider.value            = nightPercent;
                if (nightLabel != null) nightLabel.text = Mathf.RoundToInt(nightPercent).ToString();
                nightSlider.tooltip          = GetTransportTooltip(transportType.Name);
            }

            // Income total — deferred: populated by OnUpdate once the game is running,
            // to avoid scanning the vehicle buffer (huge with MoreVehicles) during loading.
            // Shrunk from the template's own default scale since the text is now "fare (passengers)"
            // rather than just a bare fare number - UNCONFIRMED whether this is small enough to avoid
            // clipping against the template's fixed label width; needs an in-game check.
            totalLabel.textScale *= 0.8f;
            totalLabel.text = "-";
            
            // Set tooltip on the parent panel since UILabel doesn't support tooltips natively.
            if (totalLabel.parent != null)
            {
                totalLabel.parent.tooltip = Localization.Get("ECONOMY_TAB_TICKET_PRICES_TOOLTIP_PASSENGER_COUNT");
            }

            var row = new TicketPriceSliderRow
            {
                TransportType = transportType,
                DaySlider     = daySlider,
                DayLabel      = dayLabel,
                NightSlider   = nightSlider,
                NightLabel    = nightLabel,
                TotalLabel    = totalLabel,
            };

            daySlider.eventValueChanged += (comp, value) =>
            {
                dayLabel.text = Mathf.RoundToInt(value).ToString();
                ApplyMultiplier(row);
            };

            if (nightSlider != null)
            {
                nightSlider.eventValueChanged += (comp, value) =>
                {
                    if (nightLabel != null) nightLabel.text = Mathf.RoundToInt(value).ToString();
                    ApplyMultiplier(row);
                };
            }

            return row;
        }

        // Fallback custom row (used if BudgetItem template is unavailable).
        // Custom row layout matching the Budget panel style:
        // [Icon] | [Day slider ────────] 100% | [₡income]
        //        | [Night slider ──────] 100% |
        private static TicketPriceSliderRow CreateSliderRowFallback(UIScrollablePanel container, TransportTypeInfo transportType, int index)
        {
            const float ICON_W   = 26f;
            const float PCT_W    = 38f;   // "250%" label width
            const float TOTAL_W  = 100f;  // fare + "(passengers)" display
            const float PAD      = 3f;
            const float SLD_H    = 18f;   // height of one slider track (matches BudgetItem)
            const float SLD_GAP  = 3f;    // gap between day and night sliders

            // Always use two-row height — day/night cycle is vanilla, not After Dark
            float rowH = SLD_H * 2f + SLD_GAP + PAD * 2f;
            rowH = Mathf.Max(rowH, 28f);

            var rowPanel = container.AddUIComponent<UIPanel>();
            rowPanel.autoLayout  = false;
            rowPanel.width       = container.width;
            rowPanel.height      = rowH;
            rowPanel.clipChildren = true;

            // Alternating row background
            var bg = rowPanel.AddUIComponent<UISlicedSprite>();
            bg.spriteName        = "GenericPanelWhite";
            bg.relativePosition  = Vector2.zero;
            bg.size              = rowPanel.size;
            // Widened the gap between the two shades (was 56/61/75 vs 49/52/64, barely distinguishable
            // against the Economy panel's own dark background) so alternating rows are actually easy
            // to tell apart at a glance.
            bg.color = (index % 2 == 0)
                ? new Color32((byte)68, (byte)74, (byte)92, (byte)255)
                : new Color32((byte)38, (byte)41, (byte)52, (byte)255);

            // Transport icon – centred vertically (use custom atlas if available)
            var icon = rowPanel.AddUIComponent<UISprite>();
            ApplyTransportIcon(icon, transportType);
            
            icon.size             = new Vector2(ICON_W, ICON_W);
            icon.relativePosition = new Vector3(PAD, (rowH - ICON_W) / 2f);

            // Slider area runs from icon to the labels+total on the right
            float sliderAreaX = PAD + ICON_W + PAD;
            float sliderAreaW = rowPanel.width - sliderAreaX - PAD - PCT_W - PAD - TOTAL_W - PAD;
            sliderAreaW = Mathf.Max(50f, sliderAreaW);

            float currentPercent = GetMultiplier(transportType.Name, false) * 100f;

            float daySliderY   = PAD;
            float nightSliderY = PAD + SLD_H + SLD_GAP;

            // ── Day slider ──────────────────────────────────────────────
            var daySlider = CreateTicketSlider(rowPanel, sliderAreaX, daySliderY, sliderAreaW);
            daySlider.value = currentPercent;
            daySlider.tooltip = GetTransportTooltip(transportType.Name);

            float pctX = sliderAreaX + sliderAreaW + PAD;
            var dayLabel = CreatePercentLabel(rowPanel, pctX, daySliderY, PCT_W);
            dayLabel.text = Mathf.RoundToInt(currentPercent) + "%";

            // ── Night slider — always shown (day/night cycle is vanilla, not After Dark) ───
            float nightPercent = GetMultiplier(transportType.Name, true) * 100f;
            UISlider nightSlider = CreateTicketSlider(rowPanel, sliderAreaX, nightSliderY, sliderAreaW);
            nightSlider.value = nightPercent;
            nightSlider.tooltip = GetTransportTooltip(transportType.Name);

            UILabel nightLabel = CreatePercentLabel(rowPanel, pctX, nightSliderY, PCT_W);
            nightLabel.text = Mathf.RoundToInt(nightPercent) + "%";

            // ── Income total box ────────────────────────────────────────
            float totalX = rowPanel.width - TOTAL_W - PAD;
            var totalBg = rowPanel.AddUIComponent<UISlicedSprite>();
            totalBg.spriteName        = "GenericPanelWhite";
            totalBg.color             = new Color32((byte)25, (byte)28, (byte)38, (byte)230);
            totalBg.size              = new Vector2(TOTAL_W, rowH - 4f);
            totalBg.relativePosition  = new Vector3(totalX, 2f);

            var totalLabel = rowPanel.AddUIComponent<UILabel>();
            totalLabel.name              = "TotalLabel";
            totalLabel.autoSize          = false;
            totalLabel.wordWrap          = false;
            totalLabel.textAlignment     = UIHorizontalAlignment.Center;
            totalLabel.textScale         = 0.75f; // was 0.85 - text is longer now (fare + passenger count)
            totalLabel.textColor         = new Color32((byte)206, (byte)248, (byte)0, (byte)255);
            totalLabel.size              = new Vector2(TOTAL_W, rowH - 4f);
            totalLabel.relativePosition  = new Vector3(totalX, 2f);
            // UILabel.verticalAlignment is unreliable in CS1 — use padding to vertically centre.
            // Approximate single-line height for scale 0.85 ≈ 12 px.
            totalLabel.padding           = new RectOffset(0, 0, Mathf.Max(0, Mathf.RoundToInt((rowH - 4f - 12f) / 2f)), 0);
            totalLabel.text = "-"; // populated by OnUpdate once the game is running
            
            // Set tooltip on the parent panel since UILabel doesn't support tooltips natively.
            if (totalLabel.parent != null)
            {
                totalLabel.parent.tooltip = Localization.Get("ECONOMY_TAB_TICKET_PRICES_TOOLTIP_PASSENGER_COUNT");
            }

            // ── Wire up ─────────────────────────────────────────────────
            var row = new TicketPriceSliderRow
            {
                TransportType = transportType,
                DaySlider     = daySlider,
                DayLabel      = dayLabel,
                NightSlider   = nightSlider,
                NightLabel    = nightLabel,
                TotalLabel    = totalLabel,
            };

            daySlider.eventValueChanged += (comp, value) =>
            {
                dayLabel.text = Mathf.RoundToInt(value) + "%";
                ApplyMultiplier(row);
            };

            if (nightSlider != null)
            {
                nightSlider.eventValueChanged += (comp, value) =>
                {
                    if (nightLabel != null) nightLabel.text = Mathf.RoundToInt(value) + "%";
                    ApplyMultiplier(row);
                };
            }

            return row;
        }

        private static UISlider CreateTicketSlider(UIPanel parent, float x, float y, float width)
        {
            // Wrapper panel for positioning within the row
            var sliderPanel = parent.AddUIComponent<UIPanel>();
            sliderPanel.autoLayout = false;
            sliderPanel.relativePosition = new Vector3(x, y);
            sliderPanel.size = new Vector2(width, 18f);

            var slider = sliderPanel.AddUIComponent<UISlider>();
            slider.relativePosition = Vector2.zero;
            slider.size = new Vector2(width, 18f);
            slider.minValue = 0f;
            slider.maxValue = 250f;
            slider.stepSize = 5f;
            slider.scrollWheelAmount = 5f;
            slider.orientation = UIOrientation.Horizontal;

            // Track — 9px tall, offset 4px down to centre in 18px slider (matches BudgetItem)
            var track = slider.AddUIComponent<UISlicedSprite>();
            track.atlas = UITextures.InGameAtlas;
            track.spriteName = "BudgetSlider";
            track.relativePosition = new Vector2(0f, 4f);
            track.size = new Vector2(width, 9f);

            // Thumb — same "SliderBudget" handle sprite AlgernonCommons' own generic slider helper
            // uses throughout the rest of this mod (Train Display overlay scale/opacity sliders,
            // etc.), instead of a plain custom rectangle, so this row matches the game's own slider
            // look instead of looking hand-rolled.
            var thumb = slider.AddUIComponent<UISlicedSprite>();
            thumb.atlas = UITextures.InGameAtlas;
            thumb.spriteName = "SliderBudget";
            thumb.size = new Vector2(20f, 18f);

            slider.thumbObject = thumb;

            return slider;
        }

        private static UILabel CreatePercentLabel(UIPanel parent, float x, float y, float width)
        {
            var label = parent.AddUIComponent<UILabel>();
            label.relativePosition = new Vector3(x, y);
            label.size = new Vector2(width, 18f);
            label.textAlignment = UIHorizontalAlignment.Right;
            label.textScale = 0.75f;
            label.textColor = new Color32((byte)206, (byte)248, (byte)0, (byte)255); // Match budget green color
            return label;
        }

        private static void ApplyMultiplier(TicketPriceSliderRow row)
        {
            try
            {
                float dayMultiplier   = row.DaySlider.value / 100f;
                float nightMultiplier = row.NightSlider != null
                    ? row.NightSlider.value / 100f
                    : dayMultiplier;

                SetMultiplier(row.TransportType.Name, false, dayMultiplier);
                SetMultiplier(row.TransportType.Name, true,  nightMultiplier);

                // Apply whichever multiplier is active right now
                bool isNight = Singleton<SimulationManager>.instance.m_isNightTime;
                ApplyPriceForType(row.TransportType.Name, isNight ? nightMultiplier : dayMultiplier);

                // Save settings
                CSLModsCommon.Manager.Domain.DefaultDomain.GetOrCreateManager<CSLModsCommon.Manager.SettingManager>().SaveSettings();
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPricesTab: Error applying multiplier for {row.TransportType.Name}: {ex.Message}");
            }
        }

        private static void UpdateTotalLabel(string transportName, UILabel totalLabel)
        {
            try
            {
                // Show the resulting fare, which is the whole point of this tab - the sliders only
                // show a percentage, so without this there was no way to see what the multiplier
                // actually produced. (This used to show the live passenger count instead, which
                // reads as a permanent "0" on a small or paused city and told you nothing about the
                // price.) The passenger count moves to the tooltip, where it is still useful.
                if (PriceCustomization.TryGetCurrentPrice(transportName, out int currentPrice, out int basePrice))
                {
                    int passengers = CalculateCurrentPassengerLoad(transportName);
                    // Passenger count used to be tooltip-only (hover required, easy to miss) - now
                    // shown inline too, so both numbers are visible at a glance without needing to
                    // hover every row. "|" instead of "(42)" per feedback - reads cleaner at a glance.
                    totalLabel.text = $"{currentPrice:N0} | {passengers:N0}";
                    // White at the base fare (slider untouched, 100%), fading toward red as the
                    // slider approaches its 250% maximum - a continuous gauge of "how far from
                    // default" rather than a flat two-colour on/off flag.
                    var ratio = basePrice > 0 ? (float)currentPrice / basePrice : 1f;
                    totalLabel.textColor = GetPriceGaugeColor(ratio);

                    totalLabel.tooltip = string.Format(
                        Localization.Get("TICKET_PRICE_LABEL_TOOLTIP"),
                        currentPrice.ToString("N0"),
                        basePrice.ToString("N0"),
                        passengers.ToString("N0"));
                }
                else
                {
                    // No prefab of this type loaded (e.g. its DLC is missing) - nothing to price.
                    totalLabel.text = "-";
                    totalLabel.textColor = new Color32(150, 150, 150, 255);
                    totalLabel.tooltip = string.Empty;
                }
            }
            catch (Exception ex)
            {
                totalLabel.text = "-";
                Utils.LogWarning($"TicketPricesTab: Failed to update price label for {transportName}: {ex.Message}");
            }
        }

        // A continuous gauge of "how far the current fare is from the game's own default", rather
        // than a flat two-colour on/off flag: white at the base fare (100%, slider untouched), warming
        // through orange to red as it climbs toward the 250% maximum, and cooling toward green as it
        // drops toward 0% (a cheaper fare reads as a deliberate, positive choice, not a warning).
        private static Color32 GetPriceGaugeColor(float ratio)
        {
            var white = new Color32(255, 255, 255, 255);
            if (ratio >= 1f)
            {
                var orange = new Color32(255, 170, 40, 255);
                var red = new Color32(230, 40, 40, 255);
                // 1.0-1.75 fades white->orange, 1.75-2.5 fades orange->red - a straight white->red
                // lerp passes through a muddy pink that doesn't read as "getting more expensive".
                if (ratio <= 1.75f)
                {
                    return Color32.Lerp(white, orange, Mathf.InverseLerp(1f, 1.75f, ratio));
                }

                return Color32.Lerp(orange, red, Mathf.InverseLerp(1.75f, 2.5f, ratio));
            }

            var green = new Color32(90, 220, 120, 255);
            return Color32.Lerp(green, white, Mathf.Clamp01(ratio));
        }

        private static int CalculateCurrentPassengerLoad(string transportName)
        {
            try
            {
                // Outside-connection vehicles (Plane, Ship) and non-line vehicles (Taxi, CableCar)
                // have m_transportLine = 0 and are not in TransportManager.m_lines, so scan
                // VehicleManager directly. Blimp, Ferry, and Helicopter are strictly player
                // transport lines and stay on their line during normal operation.
                switch (transportName)
                {
                    case "Taxi":        return CountVehiclesByAI<TaxiAI>();
                    case "Plane":       return CountVehiclesByAI<PassengerPlaneAI>();
                    case "Ship":        return CountVehiclesByAI<PassengerShipAI>();
                    case "CableCar":    return CountVehiclesByAI<CableCarAI>();
                    case "IntercityBus": return CountIntercityBusPassengers();
                }

                // For types that reliably stay on transport lines, iterate lines
                int totalPassengers = 0;
                var vehicleManager = Singleton<VehicleManager>.instance;
                var transportManager = TransportManager.instance;

                for (ushort lineId = 0; lineId < transportManager.m_lines.m_size; lineId++)
                {
                    var line = transportManager.m_lines.m_buffer[lineId];
                    if ((line.m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None) continue;
                    if (line.Info == null) continue;
                    if (!LineMatchesTransport(line.Info, transportName)) continue;

                    int vehiclesSeen = 0;
                    for (ushort vehicleId = line.m_vehicles;
                         vehicleId != 0 && vehiclesSeen < vehicleManager.m_vehicles.m_size;
                         vehicleId = vehicleManager.m_vehicles.m_buffer[vehicleId].m_nextLineVehicle, vehiclesSeen++)
                    {
                        if (vehicleId >= vehicleManager.m_vehicles.m_size) break;
                        var vehicle = vehicleManager.m_vehicles.m_buffer[vehicleId];
                        int vehiclePassengers = vehicle.m_transferSize;
                        var trailingId = vehicle.m_trailingVehicle;
                        int trailingsSeen = 0;
                        while (trailingId != 0 && trailingId < vehicleManager.m_vehicles.m_size && trailingsSeen < 64)
                        {
                            trailingsSeen++;
                            var trailingVehicle = vehicleManager.m_vehicles.m_buffer[trailingId];
                            vehiclePassengers += trailingVehicle.m_transferSize;
                            trailingId = trailingVehicle.m_trailingVehicle;
                        }
                        totalPassengers += vehiclePassengers;
                    }
                }

                return totalPassengers;
            }
            catch (Exception ex)
            {
                Utils.LogWarning($"TicketPricesTab: Error calculating passenger load for {transportName}: {ex.Message}");
                return 0;
            }
        }

        // Generic vehicle scan by AI type — counts m_transferSize of all active vehicles
        // whose AI is exactly TAI. This correctly handles types that set m_transportLine=0.
        // NOTE: loop variable must be int, not ushort — MoreVehicles sets m_size to 65536,
        //       which causes ushort to wrap from 65535 → 0, creating an infinite loop.
        private static int CountVehiclesByAI<TAI>() where TAI : VehicleAI
        {
            int count = 0;
            var vm = Singleton<VehicleManager>.instance;
            for (int i = 1; i < vm.m_vehicles.m_size; i++)
            {
                var v = vm.m_vehicles.m_buffer[i];
                if ((v.m_flags & Vehicle.Flags.Created) == 0) continue;
                if (v.Info?.m_vehicleAI is TAI)
                    count += v.m_transferSize;
            }
            return count;
        }

        // Intercity buses use BusAI like regular buses, but their VehicleInfo has m_class.m_level == Level3.
        // This mirrors the game's own TransportStationAI.IsIntercity(itemClass) check exactly.
        private static int CountIntercityBusPassengers()
        {
            int count = 0;
            var vm = Singleton<VehicleManager>.instance;
            for (int i = 1; i < vm.m_vehicles.m_size; i++)
            {
                var v = vm.m_vehicles.m_buffer[i];
                if ((v.m_flags & Vehicle.Flags.Created) == 0) continue;
                if (v.Info?.m_vehicleAI is BusAI
                    && v.Info.m_class != null
                    && v.Info.m_class.m_level == ItemClass.Level.Level3)
                    count += v.m_transferSize;
            }
            return count;
        }

        // Match a TransportInfo against a transport name using the TransportType enum,
        // which is reliable regardless of the exact prefab name in any given game install.
        private static bool LineMatchesTransport(TransportInfo info, string transportName)
        {
            var t  = info.m_transportType;
            var ss = info.m_class != null ? info.m_class.m_subService : ItemClass.SubService.None;
            switch (transportName)
            {
                case "Bus":
                    // Regular city buses: TransportType.Bus, PublicTransportBus sub-service, NOT level 3 (intercity)
                    // Matches game's own TransportStationAI.IsIntercity check: level 3 = intercity
                    return t == TransportInfo.TransportType.Bus
                        && ss == ItemClass.SubService.PublicTransportBus
                        && (info.m_class == null || info.m_class.m_level != ItemClass.Level.Level3);
                case "IntercityBus":
                    // Intercity buses: same TransportType and sub-service as regular bus, but level 3
                    // This mirrors TransportStationAI.IsIntercity(itemClass) exactly
                    return t == TransportInfo.TransportType.Bus
                        && ss == ItemClass.SubService.PublicTransportBus
                        && info.m_class != null && info.m_class.m_level == ItemClass.Level.Level3;
                case "Trolleybus":
                    return t == TransportInfo.TransportType.Trolleybus;
                case "Tram":
                    return t == TransportInfo.TransportType.Tram;
                case "Metro":
                    return t == TransportInfo.TransportType.Metro;
                case "Train":
                    return t == TransportInfo.TransportType.Train;
                case "Monorail":
                    return t == TransportInfo.TransportType.Monorail;
                case "Ferry":
                    // Ferries use TransportType.Ship; passenger ships are outside-connection
                    // vehicles and do not create player transport lines, so any Ship line is a ferry.
                    return t == TransportInfo.TransportType.Ship;
                case "Blimp":
                    // Blimps use TransportType.Airplane; intercity planes are outside-connection
                    // vehicles and do not create player transport lines, so any Airplane line is a blimp.
                    return t == TransportInfo.TransportType.Airplane;
                case "Helicopter":
                    return t == TransportInfo.TransportType.Helicopter;
                case "SightseeingBus":
                    return t == TransportInfo.TransportType.TouristBus;
                default:
                    return false;
            }
        }

        private static TransportInfo GetTransportInfo(string transportName)
        {
            try
            {
                string infoPrefabName = GetTransportInfoName(transportName);
                if (infoPrefabName == null)
                    return null;
                return PrefabCollection<TransportInfo>.FindLoaded(infoPrefabName);
            }
            catch
            {
                return null;
            }
        }

        private static void OnResetClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            try
            {
                var defaults = new ModSetting.TicketPriceCustomizerSettings();
                var current = ModSetting.Instance.TicketPriceCustomizer;
                if (current == null)
                    current = ModSetting.Instance.TicketPriceCustomizer = new ModSetting.TicketPriceCustomizerSettings();

                // Reset all properties to defaults
                foreach (var prop in typeof(ModSetting.TicketPriceCustomizerSettings).GetProperties())
                {
                    if (prop.CanWrite && prop.PropertyType == typeof(float))
                    {
                        prop.SetValue(current, prop.GetValue(defaults, null), null);
                    }
                }

                CSLModsCommon.Manager.Domain.DefaultDomain.GetOrCreateManager<CSLModsCommon.Manager.SettingManager>().SaveSettings();
                PriceCustomization.SetPrices(current);

                // Update all slider positions
                foreach (var row in s_sliderRows)
                {
                    float defaultDay   = GetMultiplier(row.TransportType.Name, false) * 100f;
                    float defaultNight = GetMultiplier(row.TransportType.Name, true)  * 100f;
                    row.DaySlider.value = defaultDay;
                    row.DayLabel.text = Mathf.RoundToInt(defaultDay) + "%";
                    if (row.NightSlider != null)
                    {
                        row.NightSlider.value = defaultNight;
                        row.NightLabel.text = Mathf.RoundToInt(defaultNight) + "%";
                    }
                    if (row.TotalLabel != null)
                    {
                        UpdateTotalLabel(row.TransportType.Name, row.TotalLabel);
                    }
                }

                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log("TicketPricesTab: All ticket prices reset to defaults");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPricesTab: Error resetting prices: {ex.Message}");
            }
        }

        #region Multiplier Mapping

        private static float GetMultiplier(string transportName, bool isNight)
        {
            var settings = ModSetting.Instance.TicketPriceCustomizer;
            if (settings == null) return 1.0f;
            if (isNight)
            {
                switch (transportName)
                {
                    case "Bus":           return settings.BusNightMultiplier;
                    case "Trolleybus":    return settings.TrolleybusNightMultiplier;
                    case "Tram":          return settings.TramNightMultiplier;
                    case "Metro":         return settings.MetroNightMultiplier;
                    case "Train":         return settings.TrainNightMultiplier;
                    case "Monorail":      return settings.MonorailNightMultiplier;
                    case "CableCar":      return settings.CableCarNightMultiplier;
                    case "Ship":          return settings.ShipNightMultiplier;
                    case "Ferry":         return settings.FerryNightMultiplier;
                    case "Plane":         return settings.PlaneNightMultiplier;
                    case "Blimp":         return settings.BlimpNightMultiplier;
                    case "Helicopter":    return settings.HelicopterNightMultiplier;
                    case "Taxi":          return settings.TaxiNightMultiplier;
                    case "SightseeingBus":return settings.SightseeingBusNightMultiplier;
                    case "IntercityBus":  return settings.IntercityBusNightMultiplier;
                    default:              return 1.0f;
                }
            }
            else
            {
                switch (transportName)
                {
                    case "Bus":           return settings.BusMultiplier;
                    case "Trolleybus":    return settings.TrolleybusMultiplier;
                    case "Tram":          return settings.TramMultiplier;
                    case "Metro":         return settings.MetroMultiplier;
                    case "Train":         return settings.TrainMultiplier;
                    case "Monorail":      return settings.MonorailMultiplier;
                    case "CableCar":      return settings.CableCarMultiplier;
                    case "Ship":          return settings.ShipMultiplier;
                    case "Ferry":         return settings.FerryMultiplier;
                    case "Plane":         return settings.PlaneMultiplier;
                    case "Blimp":         return settings.BlimpMultiplier;
                    case "Helicopter":    return settings.HelicopterMultiplier;
                    case "Taxi":          return settings.TaxiMultiplier;
                    case "SightseeingBus":return settings.SightseeingBusMultiplier;
                    case "IntercityBus":  return settings.IntercityBusMultiplier;
                    default:              return 1.0f;
                }
            }
        }

        private static void SetMultiplier(string transportName, bool isNight, float value)
        {
            var settings = ModSetting.Instance.TicketPriceCustomizer;
            if (settings == null) return;
            if (isNight)
            {
                switch (transportName)
                {
                    case "Bus":           settings.BusNightMultiplier            = value; break;
                    case "Trolleybus":    settings.TrolleybusNightMultiplier     = value; break;
                    case "Tram":          settings.TramNightMultiplier           = value; break;
                    case "Metro":         settings.MetroNightMultiplier          = value; break;
                    case "Train":         settings.TrainNightMultiplier          = value; break;
                    case "Monorail":      settings.MonorailNightMultiplier       = value; break;
                    case "CableCar":      settings.CableCarNightMultiplier       = value; break;
                    case "Ship":          settings.ShipNightMultiplier           = value; break;
                    case "Ferry":         settings.FerryNightMultiplier          = value; break;
                    case "Plane":         settings.PlaneNightMultiplier          = value; break;
                    case "Blimp":         settings.BlimpNightMultiplier          = value; break;
                    case "Helicopter":    settings.HelicopterNightMultiplier     = value; break;
                    case "Taxi":          settings.TaxiNightMultiplier           = value; break;
                    case "SightseeingBus":settings.SightseeingBusNightMultiplier = value; break;
                    case "IntercityBus":  settings.IntercityBusNightMultiplier   = value; break;
                }
            }
            else
            {
                switch (transportName)
                {
                    case "Bus":           settings.BusMultiplier            = value; break;
                    case "Trolleybus":    settings.TrolleybusMultiplier     = value; break;
                    case "Tram":          settings.TramMultiplier           = value; break;
                    case "Metro":         settings.MetroMultiplier          = value; break;
                    case "Train":         settings.TrainMultiplier          = value; break;
                    case "Monorail":      settings.MonorailMultiplier       = value; break;
                    case "CableCar":      settings.CableCarMultiplier       = value; break;
                    case "Ship":          settings.ShipMultiplier           = value; break;
                    case "Ferry":         settings.FerryMultiplier          = value; break;
                    case "Plane":         settings.PlaneMultiplier          = value; break;
                    case "Blimp":         settings.BlimpMultiplier          = value; break;
                    case "Helicopter":    settings.HelicopterMultiplier     = value; break;
                    case "Taxi":          settings.TaxiMultiplier           = value; break;
                    case "SightseeingBus":settings.SightseeingBusMultiplier = value; break;
                    case "IntercityBus":  settings.IntercityBusMultiplier   = value; break;
                }
            }
        }

        private static void ApplyPriceForType(string transportName, float multiplier)
        {
            switch (transportName)
            {
                case "Bus": PriceCustomization.SetBusPrice(multiplier); break;
                case "Trolleybus": PriceCustomization.SetTrolleybusPrice(multiplier); break;
                case "Tram": PriceCustomization.SetTramPrice(multiplier); break;
                case "Metro": PriceCustomization.SetMetroPrice(multiplier); break;
                case "Train": PriceCustomization.SetTrainPrice(multiplier); break;
                case "Monorail": PriceCustomization.SetMonorailPrice(multiplier); break;
                case "CableCar": PriceCustomization.SetCableCarPrice(multiplier); break;
                case "Ship": PriceCustomization.SetShipPrice(multiplier); break;
                case "Ferry": PriceCustomization.SetFerryPrice(multiplier); break;
                case "Plane": PriceCustomization.SetPlanePrice(multiplier); break;
                case "Blimp": PriceCustomization.SetBlimpPrice(multiplier); break;
                case "Helicopter": PriceCustomization.SetHelicopterPrice(multiplier); break;
                case "Taxi": PriceCustomization.SetTaxiPrice(multiplier); break;
                case "SightseeingBus": PriceCustomization.SetSightseeingBusPrice(multiplier); break;
                case "IntercityBus": PriceCustomization.SetIntercityBusPrice(multiplier); break;
            }
        }

        private static string GetTransportInfoName(string transportName)
        {
            switch (transportName)
            {
                case "Bus": return "Bus";
                case "Trolleybus": return "Trolleybus";
                case "Tram": return "Tram";
                case "Metro": return "Metro";
                case "Train": return "Train";
                case "Monorail": return "Monorail";
                case "CableCar": return "CableCar";
                case "Ship": return "Ship";
                case "Ferry": return "Ferry";
                case "Plane": return "Airplane";
                case "Blimp": return "Blimp";
                case "Helicopter": return "Passenger Helicopter";
                case "Taxi": return "Taxi";
                case "SightseeingBus": return "Sightseeing Bus";
                case "IntercityBus": return "Intercity Bus";
                default: return null;
            }
        }

        private static string GetLocalizedTransportName(string transportName)
        {
            try
            {
                switch (transportName)
                {
                    case "Bus": return Localization.Get("TICKET_PRICE_BUS");
                    case "Trolleybus": return Localization.Get("TICKET_PRICE_TROLLEYBUS");
                    case "Tram": return Localization.Get("TICKET_PRICE_TRAM");
                    case "Metro": return Localization.Get("TICKET_PRICE_METRO");
                    case "Train": return Localization.Get("TICKET_PRICE_TRAIN");
                    case "Monorail": return Localization.Get("TICKET_PRICE_MONORAIL");
                    case "CableCar": return Localization.Get("TICKET_PRICE_CABLECAR");
                    case "Ship": return Localization.Get("TICKET_PRICE_SHIP");
                    case "Ferry": return Localization.Get("TICKET_PRICE_FERRY");
                    case "Plane": return Localization.Get("TICKET_PRICE_PLANE");
                    case "Blimp": return Localization.Get("TICKET_PRICE_BLIMP");
                    case "Helicopter": return Localization.Get("TICKET_PRICE_HELICOPTER");
                    case "Taxi":
                    {
                        bool isMph = ModSetting.Instance.SpeedUnit == ModSetting.VehicleSpeedUnits.MPH;
                        return Localization.Get(isMph ? "TICKET_PRICE_TAXI_MILE" : "TICKET_PRICE_TAXI_KILOMETER");
                    }
                    case "SightseeingBus": return Localization.Get("TICKET_PRICE_SIGHTSEEING_BUS");
                    case "IntercityBus": return Localization.Get("TICKET_PRICE_INTERCITY_BUS");
                    default: return transportName;
                }
            }
            catch
            {
                return transportName;
            }
        }

        private static string GetTransportTooltip(string transportName)
        {
            try
            {
                switch (transportName)
                {
                    case "Bus": return Localization.Get("TICKET_PRICE_BUS");
                    case "Trolleybus": return Localization.Get("TICKET_PRICE_TROLLEYBUS");
                    case "Tram": return Localization.Get("TICKET_PRICE_TRAM");
                    case "Metro": return Localization.Get("TICKET_PRICE_METRO");
                    case "Train": return Localization.Get("TICKET_PRICE_TRAIN");
                    case "Monorail": return Localization.Get("TICKET_PRICE_MONORAIL");
                    case "CableCar": return Localization.Get("TICKET_PRICE_CABLECAR");
                    case "Ship": return Localization.Get("TICKET_PRICE_SHIP");
                    case "Ferry": return Localization.Get("TICKET_PRICE_FERRY");
                    case "Plane": return Localization.Get("TICKET_PRICE_PLANE");
                    case "Blimp": return Localization.Get("TICKET_PRICE_BLIMP");
                    case "Helicopter": return Localization.Get("TICKET_PRICE_HELICOPTER");
                    case "Taxi":
                    {
                        bool isMph = ModSetting.Instance.SpeedUnit == ModSetting.VehicleSpeedUnits.MPH;
                        return Localization.Get(isMph ? "TICKET_PRICE_TAXI_MILE" : "TICKET_PRICE_TAXI_KILOMETER");
                    }
                    case "SightseeingBus": return Localization.Get("TICKET_PRICE_SIGHTSEEING_BUS");
                    case "IntercityBus": return Localization.Get("TICKET_PRICE_INTERCITY_BUS");
                    default: return "";
                }
            }
            catch
            {
                return "";
            }
        }

        #endregion

        #region Data Types

        private class TransportTypeInfo
        {
            public string Name;
            public string SpriteName;
            public ItemClass.SubService SubService;

            public TransportTypeInfo(string name, string spriteName, ItemClass.SubService subService)
            {
                Name = name;
                SpriteName = spriteName;
                SubService = subService;
            }
        }

        private class TicketPriceSliderRow
        {
            public TransportTypeInfo TransportType;
            public UISlider DaySlider;
            public UILabel DayLabel;
            public UISlider NightSlider;
            public UILabel NightLabel;
            public UILabel TotalLabel;
        }

        #endregion
    }
}



