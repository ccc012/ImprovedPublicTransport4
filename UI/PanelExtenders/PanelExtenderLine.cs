// Decompiled with JetBrains decompiler
// Type: ImprovedPublicTransport.PanelExtenderLine
// Assembly: ImprovedPublicTransport, Version=1.0.6177.17409, Culture=neutral, PublicKeyToken=null
// MVID: 76F370C5-F40B-41AE-AA9D-1E3F87E934D3
// Assembly location: C:\Games\Steam\steamapps\workshop\content\255710\424106600\ImprovedPublicTransport.dll

using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.UI;
using ImprovedPublicTransport.Query;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.UI.DontCryJustDieCommons;
using ImprovedPublicTransport.Util;
using ImprovedPublicTransport.UI;
using UnityEngine;
using static ImprovedPublicTransport.ImprovedPublicTransportMod;
using UIUtils = ImprovedPublicTransport.Util.UIUtils;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.UI.PanelExtenders
{
    // See the same attribute on PanelExtenderVehicle - Unity does not guarantee LateUpdate order
    // between different MonoBehaviours, so without this our reapply-cache pattern for
    // m_VehicleAmount could still lose to vanilla's LateUpdate (if it has one) on some frames.
    [DefaultExecutionOrder(32000)]
    public class PanelExtenderLine : MonoBehaviour
    {
        private static readonly MethodInfo OnColorChangedMethod =
            typeof(PublicTransportWorldInfoPanel).GetMethod("OnColorChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private bool _initialized;
        private ushort _cachedLineID;
        private ItemClass.Level _cachedLevel = ItemClass.Level.None;
        private ItemClass.SubService _cachedSubService = ItemClass.SubService.None;
        private Dictionary<ItemClassTriplet, bool> _updateDepots;
        private int _cachedSimCount;
        private int _cachedQueuedCount;
        private int _lastDepotDistrictNamesHash;
        private PublicTransportWorldInfoPanel _publicTransportWorldInfoPanel;
        private UIComponent _mainSubPanel;
        private UIPanel _iptContainer;
        private UIColorField _colorField;
        private UILabel _vehicleAmount;
        private UIPanel _vehicleAmountParent;
        private UILabel _stopCountLabel;
        private UILabel _spawnTimer;
        private UICheckBox _budgetControl;
        private UICheckBox _unbunching;
        private DropDown _depotDropDown;

        private UILabel _lineLengthLabel;

        private VehicleListBox _lineVehicleListBox;
        private VehicleListBox _vehiclesInQueueListBox;
        private UIButton _selectTypes;
        private UIButton _addVehicle;

        private UIPanel _lineVehiclePanel;
        private UIPanel _vehiclesInQueuePanel;

        private UIComponent _budgetButton;
        private UIComponent _vehicleLabel;

        private UITextField _colorTextField;
        private UIButton _autoColorRefreshBtn;

        public PanelExtenderLine()
        {
            Dictionary<ItemClassTriplet, bool> dictionary = new Dictionary<ItemClassTriplet, bool>();
            dictionary.Add(new ItemClassTriplet(ItemClass.Service.PublicTransport,
                ItemClass.SubService.PublicTransportBus,
                ItemClass.Level.Level1), true);
            dictionary.Add(new ItemClassTriplet(ItemClass.Service.PublicTransport,
                ItemClass.SubService.PublicTransportTrain,
                ItemClass.Level.Level1), true);
            dictionary.Add(new ItemClassTriplet(ItemClass.Service.PublicTransport,
                ItemClass.SubService.PublicTransportTrain,
                ItemClass.Level.Level2), true);
            dictionary.Add(new ItemClassTriplet(ItemClass.Service.PublicTransport,
                ItemClass.SubService.PublicTransportMetro,
                ItemClass.Level.Level1), true);
            dictionary.Add(new ItemClassTriplet(ItemClass.Service.PublicTransport,
                ItemClass.SubService.PublicTransportPlane,
                ItemClass.Level.Level1), true);
            dictionary.Add(new ItemClassTriplet(ItemClass.Service.PublicTransport,
                ItemClass.SubService.PublicTransportPlane,
                ItemClass.Level.Level2), true);
            dictionary.Add(new ItemClassTriplet(ItemClass.Service.PublicTransport,
                ItemClass.SubService.PublicTransportPlane,
                ItemClass.Level.Level3), true);
            dictionary.Add(new ItemClassTriplet(ItemClass.Service.PublicTransport,
                ItemClass.SubService.PublicTransportShip,
                ItemClass.Level.Level1), true);
            dictionary.Add(new ItemClassTriplet(ItemClass.Service.PublicTransport,
                ItemClass.SubService.PublicTransportShip,
                ItemClass.Level.Level2), true);
            dictionary.Add(new ItemClassTriplet(ItemClass.Service.PublicTransport,
                ItemClass.SubService.PublicTransportTram,
                ItemClass.Level.Level1), true);
            dictionary.Add(new ItemClassTriplet(ItemClass.Service.PublicTransport,
                ItemClass.SubService.PublicTransportMonorail,
                ItemClass.Level.Level1), true);
            dictionary.Add(new ItemClassTriplet(ItemClass.Service.PublicTransport,
                ItemClass.SubService.PublicTransportTours,
                ItemClass.Level.Level3), true);
            dictionary.Add(new ItemClassTriplet(ItemClass.Service.PublicTransport,
                ItemClass.SubService.PublicTransportTrolleybus,
                ItemClass.Level.Level1), true);
            _updateDepots = dictionary;
        }

        // Full UpdateBindings every frame is expensive (depot dropdown, vehicle list, localization).
        // 4 Hz is enough for spawn timer / counts while the line panel is open.
        private float _nextBindingsRealtime;
        private string _locVehicleCount;
        private string _locStops;
        private string _locUnbunchDisabled;
        private string _locUnbunchEnabled;
        private string _locUnbunchGap;
        private string _locSpawnTimer;
        private string _locDepotWarning;

        private void EnsureLocaleCache()
        {
            if (_locVehicleCount != null)
            {
                return;
            }

            _locVehicleCount = Localization.Get("TRANSPORT_LINE_VEHICLECOUNT");
            _locStops = Localization.Get("LINE_PANEL_STOPS");
            _locUnbunchDisabled = Localization.Get("UNBUNCHING_DISABLED");
            _locUnbunchEnabled = Localization.Get("UNBUNCHING_ENABLED");
            _locUnbunchGap = Localization.Get("UNBUNCHING_TARGET_GAP");
            _locSpawnTimer = Localization.Get("LINE_PANEL_SPAWNTIMER");
            _locDepotWarning = Localization.Get("LINE_PANEL_DEPOT_WARNING");
        }

        // m_VehicleAmount is a vanilla-owned label - PublicTransportWorldInfoPanel writes its own
        // text into it every frame from its own Update(), same as m_Status/m_Type on the vehicle
        // panel (see PanelExtenderVehicle). Update() ordering between different components is not
        // guaranteed, so whichever of us or vanilla ran last for a given frame won - the label
        // flickered between "our" vehicle count and vanilla's own. Re-applying the last computed
        // string every LateUpdate (which always runs after every Update in the frame) is enough to
        // always win, without paying for a full UpdateBindings() every frame - that one stays
        // throttled below since it also rebuilds the depot dropdown and vehicle list.
        private string _cachedVehicleCountText;

        // Cache for total waiting passengers tooltip: avoids expensive re-scanning when mouse
        // re-enters quickly. Each stop scan can inspect up to 150 citizens; cached within 1s per lineId.
        private ushort _cachedWaitingPassengersLineId;
        private int _cachedWaitingPassengersCount;
        private float _cachedWaitingPassengersTime;

        private void LateUpdate()
        {
            if (_initialized && _cachedVehicleCountText != null
                && _publicTransportWorldInfoPanel.component.isVisible && _vehicleAmount != null)
            {
                _vehicleAmount.text = _cachedVehicleCountText;
            }
        }

        private void Update()
        {
            if (!_initialized)
            {
                Init();
            }
            else
            {
                if (!_initialized || !_publicTransportWorldInfoPanel.component.isVisible)
                    return;

                var now = Time.unscaledTime;
                if (now >= _nextBindingsRealtime)
                {
                    _nextBindingsRealtime = now + 0.25f;
                    UpdateBindings();

                    // Layout only when bindings refresh — AlignTo every frame was pure UI thrash.
                    if (_vehicleLabel == null)
                    {
                        Utils.LogError($"{ShortModName}: Vehicle label not found!");
                    }
                    else
                    {
                        _budgetButton.AlignTo(_vehicleLabel.parent, UIAlignAnchor.TopLeft);
                        _budgetButton.relativePosition = _vehicleLabel.relativePosition + new Vector3(0f, _vehicleLabel.height, 0f);
                        _vehicleLabel.isVisible = false;
                    }

                    _lineLengthLabel.AlignTo(_vehicleAmountParent, UIAlignAnchor.TopLeft);
                    _lineLengthLabel.relativePosition = new Vector3(0, 45f, 0f);
                    _stopCountLabel.AlignTo(_vehicleAmountParent, UIAlignAnchor.TopLeft);
                    _stopCountLabel.relativePosition = new Vector3(_lineLengthLabel.width + 16f, 45f, 0f);
                    _vehicleAmount.AlignTo(_vehicleAmountParent, UIAlignAnchor.TopLeft);
                    _vehicleAmount.relativePosition =
                        new Vector3(_lineLengthLabel.width + 16f + _stopCountLabel.width + 16f, 45f, 0f);
                }
            }
        }

        private void Init()
        {
            _publicTransportWorldInfoPanel = GameObject.Find("(Library) PublicTransportWorldInfoPanel")
                .GetComponent<PublicTransportWorldInfoPanel>();
            if (_publicTransportWorldInfoPanel == null)
            {
                return;
            }

            UIComponent passengers = _publicTransportWorldInfoPanel.Find("Passengers");
            passengers.parent.relativePosition = new Vector3(passengers.parent.relativePosition.x,
                76.0f, passengers.parent.relativePosition.z);
            UIComponent agePanel = _publicTransportWorldInfoPanel.Find("AgePanel");
            agePanel.relativePosition = new Vector3(0.0f,
                84.0f, agePanel.relativePosition.z);
            UIComponent tripSaved = _publicTransportWorldInfoPanel.Find("TripSaved");
            tripSaved.parent.relativePosition = new Vector3(tripSaved.parent.relativePosition.x,
                205.0f, tripSaved.parent.relativePosition.z);
            
            _budgetButton = _publicTransportWorldInfoPanel.Find<UIComponent>("Budget");
            _vehicleLabel = _publicTransportWorldInfoPanel.Find<UIComponent>("VehicleLabel");


            var deleteLineButton = _publicTransportWorldInfoPanel.Find("DeleteLine");
            if (deleteLineButton == null)
            {
                Utils.LogError("Could not found Delete button!");
            }
            else
            {
                deleteLineButton.isVisible = false;

                _lineLengthLabel = _publicTransportWorldInfoPanel.Find<UILabel>("LineLengthLabel");

                UIComponent uiComponent2 = _publicTransportWorldInfoPanel.Find("LinesOverview");
                if (uiComponent2 != null)
                    uiComponent2.enabled = false;
                _mainSubPanel = agePanel.parent;
                if (_mainSubPanel == null)
                {
                    Utils.LogError("Could not found Panel!");
                }
                else
                {
                    UIPanel uiPanel = _mainSubPanel.AddUIComponent<UIPanel>();
                    uiPanel.name = "IptContainer";
                    uiPanel.width = 301f;
                    uiPanel.height = 166f;
                    uiPanel.autoLayoutDirection = LayoutDirection.Vertical;
                    uiPanel.autoLayoutStart = LayoutStart.TopLeft;
                    uiPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 5);
                    uiPanel.autoLayout = true;
                    uiPanel.relativePosition = new Vector3(10f, 224.0f);
                    _iptContainer = uiPanel;
                    _vehicleAmount = Utils.GetPrivate<UILabel>(_publicTransportWorldInfoPanel, "m_VehicleAmount");
                    if (_vehicleAmount == null)
                    {
                        Utils.LogError("Could not found m_VehicleAmount!");
                    }
                    else
                    {
                        _vehicleAmountParent = _vehicleAmount.parent as UIPanel;
                        _vehicleAmountParent.autoLayoutPadding = new RectOffset(0, 10, 0, 0);
                        _stopCountLabel = _vehicleAmountParent.AddUIComponent<UILabel>();
                        _stopCountLabel.name = "StopCount";
                        _stopCountLabel.font = _vehicleAmount.font;
                        _stopCountLabel.textColor = _vehicleAmount.textColor;
                        _stopCountLabel.textScale = _vehicleAmount.textScale;
                        _stopCountLabel.eventMouseEnter += OnMouseEnter;

                        _colorField = Utils.GetPrivate<UIColorField>(_publicTransportWorldInfoPanel, "m_ColorField");
                        if (_colorField == null)
                        {
                            Utils.LogError("Could not found m_ColorField!");
                        }
                        else
                        {
                            UITextField uiTextField = _colorField.parent.AddUIComponent<UITextField>();
                            uiTextField.name = "ColorTextField";
                            uiTextField.text = "000000";
                            uiTextField.textColor = Color.black;
                            uiTextField.textScale = 0.7f;
                            uiTextField.selectionSprite = "EmptySprite";
                            uiTextField.normalBgSprite = "TextFieldPanelHovered";
                            uiTextField.focusedBgSprite = "TextFieldPanel";
                            uiTextField.builtinKeyNavigation = true;
                            uiTextField.submitOnFocusLost = true;
                            uiTextField.eventTextSubmitted += OnColorTextSubmitted;
                            uiTextField.width = 50f;
                            uiTextField.height = 23f;
                            uiTextField.maxLength = 6;
                            uiTextField.verticalAlignment = UIVerticalAlignment.Middle;
                            uiTextField.padding = new RectOffset(0, 0, 8, 0);
                            uiTextField.relativePosition = new Vector3(135f, 0.0f);
                            _colorTextField = uiTextField;
                            _colorField.eventSelectedColorReleased += OnColorChanged;
                            CreateLineActionsPanel();
                            CreateSpawnTimerPanel();
                            CreateBudgetControlPanel();
                            CreateUnbunchingPanel();
                            CreateDropDownPanel();
                            CreateButtonPanel1();
                            _publicTransportWorldInfoPanel.component.height = 515f;
                            CreateVehiclesOnLinePanel();
                            CreateVehiclesInQueuePanel();
                            BuildingExtension.OnDepotAdded += OnDepotChanged;
                            BuildingExtension.OnDepotRemoved += OnDepotChanged;
                            _initialized = true;
                        }
                    }
                }
            }
        }

        private void UpdateBindings()
        {
            EnsureLocaleCache();
            ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
            if (lineId != 0)
            {
                int lineVehicleCount = TransportLineUtil.CountLineActiveVehicles(lineId, out int _);
                int targetVehicleCount = CachedTransportLineData.GetTargetVehicleCount(lineId);
                var tm = Singleton<TransportManager>.instance;
                int num1 = tm.m_lines.m_buffer[lineId].CountStops(lineId);
                _cachedVehicleCountText = string.Format(_locVehicleCount,
                    lineVehicleCount + " / " + targetVehicleCount);
                _vehicleAmount.text = _cachedVehicleCountText;
                _stopCountLabel.text = string.Format(_locStops, num1);
                _budgetControl.isChecked = CachedTransportLineData.GetBudgetControlState(lineId);

                if (ModSetting.Instance.IntervalAggressionFactor == 0)
                {
                    _unbunching.Disable();
                    _unbunching.isChecked = false;
                    _unbunching.label.text = _locUnbunchDisabled;
                }
                else
                {
                    _unbunching.Enable();
                    _unbunching.isChecked = CachedTransportLineData.GetUnbunchingState(lineId);

                    if (targetVehicleCount > 1)
                    {
                        _unbunching.label.text =
                            string.Format(
                                _locUnbunchEnabled + " - " +
                                _locUnbunchGap,
                                tm.m_lines.m_buffer[lineId].m_averageInterval);
                    }
                    else
                        _unbunching.label.text = _locUnbunchEnabled;
                }

                bool depotNotValid = false;
                ushort depotID = CachedTransportLineData.GetDepot(lineId);
                TransportInfo info = tm.m_lines.m_buffer[lineId].Info;
                if (!DepotUtil.IsValidDepot(depotID, info))
                {
                    depotNotValid = true;
                }

                bool depotCanAddVehicle = true;
                if (depotID != 0)
                    depotCanAddVehicle = DepotUtil.CanAddVehicle(depotID,
                        ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[depotID], info);
                if (depotCanAddVehicle)
                {
                    var currentlyDisabled = SimulationManager.instance.m_isNightTime
                        ? (tm.m_lines.m_buffer[lineId].m_flags &
                           TransportLine.Flags.DisabledNight) != TransportLine.Flags.None
                        : (tm.m_lines.m_buffer[lineId].m_flags &
                           TransportLine.Flags.DisabledDay) != TransportLine.Flags.None;

                    if (currentlyDisabled || lineVehicleCount >= targetVehicleCount)
                    {
                        _spawnTimer.text = string.Format(_locSpawnTimer, "8");
                    }
                    else
                    {
                        var timeToNext = Mathf.Max(0,
                            Mathf.CeilToInt(CachedTransportLineData.GetNextSpawnTime(lineId) -
                                            SimHelper.SimulationTime));
                        _spawnTimer.text = string.Format(_locSpawnTimer, "=" + timeToNext);
                    }
                }
                else
                    _spawnTimer.text = _locDepotWarning;

                _selectTypes.isEnabled = !depotNotValid;
                _addVehicle.isEnabled = !depotNotValid & depotCanAddVehicle;

                ItemClass.SubService subService = info.GetSubService();
                ItemClass.Service service = info.GetService();
                ItemClass.Level level = info.GetClassLevel();
                ItemClassTriplet triplet = new ItemClassTriplet(service, subService, level);

                // Check if depot district names changed
                int currentDepotDistrictHash = GetDepotDistrictNamesHash(info);
                bool depotDistrictNamesChanged = currentDepotDistrictHash != _lastDepotDistrictNamesHash;
                if (depotDistrictNamesChanged)
                    _lastDepotDistrictNamesHash = currentDepotDistrictHash;

                // TryGetValue, not the indexer: _updateDepots is only pre-seeded for a subset of
                // (service, subService, level) combos in the constructor (e.g. Taxi and CableCar
                // are absent) - the indexer throws KeyNotFoundException for anything missing,
                // which aborted UpdateBindings() every ~0.25s while a Taxi/CableCar line panel
                // was open (vehicle list, panel sizing, depot dropdown all silently skipped).
                bool depotMarkedDirty = false;
                lock (_updateDepots)
                {
                    if (_updateDepots.TryGetValue(triplet, out var dirty) && dirty)
                    {
                        depotMarkedDirty = true;
                        _updateDepots[triplet] = false;
                    }
                }
                if (subService != _cachedSubService || level != _cachedLevel || depotNotValid || depotMarkedDirty || depotDistrictNamesChanged)
                {
                    PopulateDepotDropDown(info);
                    _updateDepots[triplet] = false;
                }

                if (_depotDropDown.Items.Length == 0)
                    _depotDropDown.Text = Localization.Get("LINE_PANEL_NO_DEPOT_FOUND");
                else
                    _depotDropDown.SelectedItem = depotID;
                if (lineId != _cachedLineID)
                {
                    _colorTextField.text = ColorUtility.ToHtmlStringRGB(_colorField.selectedColor);
                    UpdatePanelPositionAndSize();
                }

                if (lineVehicleCount != 0)
                {
                    if (lineId != _cachedLineID || lineVehicleCount != _cachedSimCount)
                    {
                        PopulateLineVehicleListBox(lineId, service, subService, level);
                    }

                    _lineVehiclePanel.Show();
                    UpdatePanelPositionAndSize();
                }
                else
                {
                    _lineVehiclePanel.Hide();
                    _lineVehicleListBox.ClearItems();
                }

                int num3 = CachedTransportLineData.EnqueuedVehiclesCount(lineId);
                if ((uint)num3 > 0U & depotCanAddVehicle)
                {
                    if (lineId != _cachedLineID || num3 != _cachedQueuedCount)
                    {
                        PopulateQueuedVehicleListBox(lineId, service, subService, level);
                    }

                    if (_vehiclesInQueueListBox.Items.Count == 0)
                        _vehiclesInQueuePanel.Hide();
                    else
                        _vehiclesInQueuePanel.Show();
                    UpdatePanelPositionAndSize();
                }
                else
                {
                    _vehiclesInQueuePanel.Hide();
                    _vehiclesInQueueListBox.ClearItems();
                }

                _cachedLevel = level;
                _cachedSubService = subService;
                _cachedSimCount = lineVehicleCount;
                _cachedQueuedCount = num3;
            }
            else
            {
                _publicTransportWorldInfoPanel.Hide();
                _cachedSubService = ItemClass.SubService.None;
                _cachedLevel = ItemClass.Level.None;
            }

            if (lineId != _cachedLineID)
            {
                PrefabPanelManager.Close();
            }

            _cachedLineID = lineId;
        }

        private void UpdatePanelPositionAndSize()
        {
            _lineVehiclePanel.relativePosition = new Vector3((float)(_lineVehiclePanel.parent.width + 1.0), 0.0f);
            if (_vehiclesInQueuePanel.isVisible)
            {
                _lineVehiclePanel.height = ParentHeight() * 0.5f;
                _lineVehicleListBox.height = _lineVehiclePanel.height - 45f;
            }
            else
            {
                _lineVehiclePanel.height = ParentHeight();
                _lineVehicleListBox.height = _lineVehiclePanel.height - 45f;
            }

            if (_lineVehiclePanel.isVisible)
            {
                _vehiclesInQueuePanel.relativePosition = new Vector3((float)(_vehiclesInQueuePanel.parent.width + 1.0),
                    ParentHeight() * 0.5f + 1f);
                _vehiclesInQueuePanel.height = ParentHeight() * 0.5f;
                _vehiclesInQueueListBox.height = _vehiclesInQueuePanel.height - 45f;
            }
            else
            {
                _vehiclesInQueuePanel.relativePosition =
                    new Vector3((float)(_lineVehiclePanel.parent.width + 1.0), 0.0f);
                _vehiclesInQueuePanel.height = ParentHeight();
                _vehiclesInQueueListBox.height = _vehiclesInQueuePanel.height - 45f;
            }
        }

        private float ParentHeight()
        {
            return _lineVehiclePanel.parent.height - 16; //because it has that little thing on the bottom
        }

        private void OnDestroy()
        {
            _initialized = false;
            BuildingExtension.OnDepotAdded -= OnDepotChanged;
            BuildingExtension.OnDepotRemoved -= OnDepotChanged;
            if (_updateDepots != null)
                _updateDepots.Clear();
            if (_colorTextField != null)
            {
                _colorTextField.eventTextSubmitted -= OnColorTextSubmitted;
                _colorField.eventSelectedColorReleased -= OnColorChanged;
                Destroy(_colorTextField.gameObject);
            }

            if (_stopCountLabel != null)
            {
                _stopCountLabel.eventMouseEnter -= OnMouseEnter;
                Destroy(_stopCountLabel.gameObject);
            }
            if (_iptContainer != null)
                Destroy(_iptContainer.gameObject);
            PrefabPanelManager.Close();
            if (_lineVehiclePanel != null)
                Destroy(_lineVehiclePanel.gameObject);
            if (_vehiclesInQueuePanel != null)
                Destroy(_vehiclesInQueuePanel.gameObject);
        }

        private void CreateSpawnTimerPanel()
        {
            UIPanel uiPanel = _iptContainer.AddUIComponent<UIPanel>();
            double width = uiPanel.parent.width;
            uiPanel.width = (float)width;
            double num1 = 14.0;
            uiPanel.height = (float)num1;
            int num2 = 0;
            uiPanel.autoLayoutDirection = (LayoutDirection)num2;
            int num3 = 0;
            uiPanel.autoLayoutStart = (LayoutStart)num3;
            RectOffset rectOffset = new RectOffset(0, 6, 0, 0);
            uiPanel.autoLayoutPadding = rectOffset;
            int num4 = 1;
            uiPanel.autoLayout = num4 != 0;
            UILabel uiLabel = uiPanel.AddUIComponent<UILabel>();
            uiLabel.font = _vehicleAmount.font;
            uiLabel.textColor = _vehicleAmount.textColor;
            uiLabel.textScale = _vehicleAmount.textScale;
            uiLabel.processMarkup = true;
            _spawnTimer = uiLabel;
        }

        private void CreateBudgetControlPanel()
        {
            UIPanel uiPanel = _iptContainer.AddUIComponent<UIPanel>();
            uiPanel.width = uiPanel.parent.width;
            uiPanel.height = 16f;
            uiPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            uiPanel.autoLayoutStart = LayoutStart.TopLeft;
            uiPanel.autoLayoutPadding = new RectOffset(0, 6, 0, 0);
            uiPanel.autoLayout = true;
            UICheckBox uiCheckBox = uiPanel.AddUIComponent<UICheckBox>();
            uiCheckBox.size = uiPanel.size;
            uiCheckBox.clipChildren = true;
            uiCheckBox.tooltip = Localization.Get("LINE_PANEL_BUDGET_CONTROL_TOOLTIP") + Environment.NewLine +
                                 Localization.Get("EXPLANATION_BUDGET_CONTROL");
            uiCheckBox.eventClicked += OnBudgetControlClick;
            UISprite uiSprite = uiCheckBox.AddUIComponent<UISprite>();
            uiSprite.spriteName = "check-unchecked";
            uiSprite.size = new Vector2(16f, 16f);
            uiSprite.relativePosition = Vector3.zero;
            uiCheckBox.checkedBoxObject = uiSprite.AddUIComponent<UISprite>();
            ((UISprite)uiCheckBox.checkedBoxObject).spriteName = "check-checked";
            uiCheckBox.checkedBoxObject.size = new Vector2(16f, 16f);
            uiCheckBox.checkedBoxObject.relativePosition = Vector3.zero;
            uiCheckBox.label = uiCheckBox.AddUIComponent<UILabel>();
            uiCheckBox.label.text = Localization.Get("LINE_PANEL_BUDGET_CONTROL");
            uiCheckBox.label.font = _vehicleAmount.font;
            uiCheckBox.label.textColor = _vehicleAmount.textColor;
            uiCheckBox.label.textScale = _vehicleAmount.textScale;
            uiCheckBox.label.relativePosition = new Vector3(22f, 2f);
            _budgetControl = uiCheckBox;
        }

        private void CreateUnbunchingPanel()
        {
            UIPanel uiPanel = _iptContainer.AddUIComponent<UIPanel>();
            uiPanel.width = uiPanel.parent.width;
            uiPanel.height = 16f;
            uiPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            uiPanel.autoLayoutStart = LayoutStart.TopLeft;
            uiPanel.autoLayoutPadding = new RectOffset(0, 6, 0, 0);
            uiPanel.autoLayout = true;
            UICheckBox uiCheckBox = uiPanel.AddUIComponent<UICheckBox>();
            uiCheckBox.size = uiPanel.size;
            uiCheckBox.clipChildren = true;
            uiCheckBox.tooltip = Localization.Get("LINE_PANEL_UNBUNCHING_TOOLTIP") + Environment.NewLine +
                                 Localization.Get("EXPLANATION_UNBUNCHING");
            uiCheckBox.eventClicked += OnUnbunchingClick;
            UISprite uiSprite = uiCheckBox.AddUIComponent<UISprite>();
            uiSprite.spriteName = "check-unchecked";
            uiSprite.size = new Vector2(16f, 16f);
            uiSprite.relativePosition = Vector3.zero;
            uiCheckBox.checkedBoxObject = uiSprite.AddUIComponent<UISprite>();
            ((UISprite)uiCheckBox.checkedBoxObject).spriteName = "check-checked";
            uiCheckBox.checkedBoxObject.size = new Vector2(16f, 16f);
            uiCheckBox.checkedBoxObject.relativePosition = Vector3.zero;
            uiCheckBox.label = uiCheckBox.AddUIComponent<UILabel>();
            uiCheckBox.label.font = _vehicleAmount.font;
            uiCheckBox.label.textColor = _vehicleAmount.textColor;
            uiCheckBox.label.disabledTextColor = Color.black;
            uiCheckBox.label.textScale = _vehicleAmount.textScale;
            uiCheckBox.label.relativePosition = new Vector3(22f, 2f);
            _unbunching = uiCheckBox;
        }

        private void CreateDropDownPanel()
        {
            UIPanel uiPanel = _iptContainer.AddUIComponent<UIPanel>();
            uiPanel.width = uiPanel.parent.width;
            uiPanel.height = 27f;
            uiPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            uiPanel.autoLayoutStart = LayoutStart.TopLeft;
            uiPanel.autoLayoutPadding = new RectOffset(0, 2, 0, 0);
            uiPanel.autoLayout = true;
            UILabel uiLabel = uiPanel.AddUIComponent<UILabel>();
            string str1 = Localization.Get("LINE_PANEL_DEPOT");
            uiLabel.text = str1;
            UIFont font = _vehicleAmount.font;
            uiLabel.font = font;
            Color32 textColor = _vehicleAmount.textColor;
            uiLabel.textColor = textColor;
            double textScale = _vehicleAmount.textScale;
            uiLabel.textScale = (float)textScale;
            int num1 = 0;
            uiLabel.autoSize = num1 != 0;
            double num2 = 27.0;
            uiLabel.height = (float)num2;
            double num3 = 60.0;
            uiLabel.width = (float)num3;
            int num4 = 1;
            uiLabel.verticalAlignment = (UIVerticalAlignment)num4;
            _depotDropDown = DropDown.Create(uiPanel);
            _depotDropDown.name = "DepotDropDown";
            _depotDropDown.Font = _vehicleAmount.font;
            _depotDropDown.height = 27f;
            _depotDropDown.width = 241f;
            _depotDropDown.DropDownPanelAlignParent = _publicTransportWorldInfoPanel.component;
            _depotDropDown.eventSelectedItemChanged += OnSelectedDepotChanged;
            UIButton uiButton = uiPanel.AddUIComponent<UIButton>();
            string str2 = "DepotMarker";
            uiButton.name = str2;
            string str3 = "LocationMarkerNormal";
            uiButton.normalBgSprite = str3;
            string str4 = "LocationMarkerDisabled";
            uiButton.disabledBgSprite = str4;
            string str5 = "LocationMarkerHovered";
            uiButton.hoveredBgSprite = str5;
            string str6 = "LocationMarkerFocused";
            uiButton.focusedBgSprite = str6;
            string str7 = "LocationMarkerPressed";
            uiButton.pressedBgSprite = str7;
            Vector2 vector2 = new Vector2(27f, 27f);
            uiButton.size = vector2;
            string str8 = Localization.Get("LINE_PANEL_DEPOT_MARKER_TOOLTIP");
            uiButton.tooltip = str8;
            MouseEventHandler mouseEventHandler = OnDepotMarkerClicked;
            uiButton.eventClick += mouseEventHandler;
        }

        /// <summary>
        /// Refresh Nome/Cor + Copy/Paste, as their own row in our own IPT container instead of
        /// sharing vanilla's colour-swatch row. That row is also home to "Atividade da linha"
        /// (vanilla's day/off-peak/night activity icons) - cramming three more buttons onto it
        /// left everything squeezed and clipped against those icons regardless of how carefully
        /// the buttons' own positions were chained (fixed in an earlier pass; the row itself was
        /// simply too narrow for both, not just poorly laid out). _iptContainer auto-lays out its
        /// rows vertically and is full panel width, so a new row here has no vanilla sibling to
        /// contend with.
        /// </summary>
        private void CreateLineActionsPanel()
        {
            var existingPanel = _iptContainer.Find<UIPanel>("IptLineActions");
            if (existingPanel != null)
            {
                _iptContainer.RemoveUIComponent(existingPanel);
                UnityEngine.Object.Destroy(existingPanel.gameObject);
            }

            var panel = _iptContainer.AddUIComponent<UIPanel>();
            panel.name = "IptLineActions";
            panel.width = panel.parent.width;
            // Tall enough for the stacked Lines-overview/Delete pair added below (2x18f + 2f gap).
            panel.height = 38f;
            panel.autoLayoutDirection = LayoutDirection.Horizontal;
            panel.autoLayoutStart = LayoutStart.TopLeft;
            panel.autoLayoutPadding = new RectOffset(0, 6, 0, 0);
            panel.autoLayout = true;

            var btn = UIUtils.CreateButton(panel);
            btn.name = "IptAutoColorRefresh";
            btn.textScale = 0.7f;
            btn.textPadding = new RectOffset(6, 6, 4, 0);
            btn.autoSize = true;
            btn.height = 23f;
            btn.tooltip = Localization.Get("AUTOLINECOLOR_REFRESH_BUTTON_TOOLTIP");
            btn.text = Localization.Get("AUTOLINECOLOR_REFRESH_BUTTON");
            btn.eventClick += (_, __) => OnAutoColorRefreshClick();
            btn.gameObject.AddComponent<AutoLineColor.RefreshButtonStateUpdater>().Button = btn;
            _autoColorRefreshBtn = btn;

            // Copy / Paste line settings (was only on PrefabPanel; CHANGELOG noted unreachable UI).
            CreateCopyPasteButtons(panel);

            // "Visão geral das linhas" / "Excluir linha" - moved here (stacked, top/bottom) instead
            // of their own row at the bottom of _iptContainer: that row's content always overflowed
            // the space vanilla actually left below it (see removed CreateButtonPanel2), and the
            // overflow rendered on top of vanilla's own line-length/stop-count text no matter what
            // height was declared - autoLayout positions children by their own sizes, not by a
            // parent's declared height, so there was no safe height to pick. This row has no vanilla
            // sibling below it to collide with.
            CreateLineOverviewDeleteStack(panel);
        }

        private void CreateLineOverviewDeleteStack(UIComponent parent)
        {
            var stack = parent.AddUIComponent<UIPanel>();
            stack.name = "IptLineOverviewDeleteStack";
            stack.width = 110f;
            stack.height = 38f;
            stack.autoLayoutDirection = LayoutDirection.Vertical;
            stack.autoLayoutStart = LayoutStart.TopLeft;
            stack.autoLayoutPadding = new RectOffset(0, 0, 0, 2);
            stack.autoLayout = true;

            var overviewButton = UIUtils.CreateButton(stack);
            overviewButton.name = "VehicleLinesOverview";
            overviewButton.localeID = "VEHICLE_LINESOVERVIEW";
            overviewButton.textScale = 0.65f;
            overviewButton.textPadding = new RectOffset(4, 4, 2, 0);
            overviewButton.width = 110f;
            overviewButton.height = 18f;
            overviewButton.eventClick += (c, p) => _publicTransportWorldInfoPanel.OnLinesOverviewClicked();

            var deleteButton = UIUtils.CreateButton(stack);
            deleteButton.name = "LineDelete";
            deleteButton.localeID = "LINE_DELETE";
            deleteButton.textScale = 0.65f;
            deleteButton.textPadding = new RectOffset(4, 4, 2, 0);
            deleteButton.width = 110f;
            deleteButton.height = 18f;
            deleteButton.eventClick += OnDeleteLineClick;
        }

        private void CreateCopyPasteButtons(UIComponent parent)
        {
            if (parent == null)
            {
                return;
            }

            UIButton EnsureButton(string name, string textKey, string tipKey, MouseEventHandler onClick)
            {
                var old = parent.Find<UIButton>(name);
                if (old != null)
                {
                    parent.RemoveUIComponent(old);
                    UnityEngine.Object.Destroy(old.gameObject);
                }

                var b = UIUtils.CreateButton(parent);
                b.name = name;
                b.textScale = 0.65f;
                b.textPadding = new RectOffset(4, 4, 3, 0);
                b.autoSize = true;
                b.height = 22f;
                b.text = Localization.Get(textKey);
                b.tooltip = Localization.Get(tipKey);
                b.eventClick += onClick;
                return b;
            }

            EnsureButton("IptLineCopy", "LINE_PANEL_COPY", "COPY_TIP", (c, p) =>
            {
                ushort vehicleId;
                var lineId = WorldInfoCurrentLineIDQuery.Query(out vehicleId);
                if (lineId == 0)
                {
                    return;
                }

                CopyPaste.Instance.Copy(lineId);
            });

            EnsureButton("IptLinePaste", "LINE_PANEL_PASTE", "PASTE_TIP", (c, p) =>
            {
                ushort vehicleId;
                var lineId = WorldInfoCurrentLineIDQuery.Query(out vehicleId);
                if (lineId == 0 || !CopyPaste.Instance.HasData)
                {
                    return;
                }

                CopyPaste.Instance.Paste(lineId);
            });
        }

        private void OnAutoColorRefreshClick()
        {
            var lineId = _cachedLineID;
            if (lineId == 0)
            {
                lineId = WorldInfoCurrentLineIDQuery.Query(out _);
            }

            if (lineId == 0)
            {
                Utils.LogWarning("AutoLineColor: IPT panel refresh — no line id.");
                return;
            }

            Utils.Log($"AutoLineColor: IPT panel refresh for line {lineId}.");
            AutoLineColor.ColorMonitor.ForceRefreshLineNow(lineId);

            // Refresh colour field on next frames after simulation applies the colour.
            StartCoroutine(RefreshColorFieldNextFrames(lineId));
        }

        private System.Collections.IEnumerator RefreshColorFieldNextFrames(ushort lineId)
        {
            // Wait a couple of frames for SimulationManager.AddAction to run.
            yield return null;
            yield return null;
            try
            {
                if (_colorField != null && lineId != 0)
                {
                    var c = Singleton<TransportManager>.instance.GetLineColor(lineId);
                    _colorField.selectedColor = c;
                    if (_colorTextField != null)
                    {
                        _colorTextField.text = ColorUtility.ToHtmlStringRGB(c);
                    }
                }
            }
            catch
            {
                // non-fatal
            }
        }

        private void CreateButtonPanel1()
        {
            UIPanel uiPanel = _iptContainer.AddUIComponent<UIPanel>();
            double width = uiPanel.parent.width;
            uiPanel.width = (float)width;
            double num1 = 32.0;
            uiPanel.height = (float)num1;
            int num2 = 0;
            uiPanel.autoLayoutDirection = (LayoutDirection)num2;
            int num3 = 0;
            uiPanel.autoLayoutStart = (LayoutStart)num3;
            RectOffset rectOffset = new RectOffset(0, 6, 0, 0);
            uiPanel.autoLayoutPadding = rectOffset;
            int num4 = 1;
            uiPanel.autoLayout = num4 != 0;
            UIButton selectTypesButton = UIUtils.CreateButton(uiPanel);
            selectTypesButton.name = "SelectTypes";
            selectTypesButton.textPadding = new RectOffset(10, 10, 4, 0);
            selectTypesButton.text = Localization.Get("LINE_PANEL_SELECT_TYPES");
            selectTypesButton.tooltip = Localization.Get("LINE_PANEL_SELECT_TYPES_TOOLTIP");
            selectTypesButton.textScale = 0.8f;
            selectTypesButton.width = 97f;
            selectTypesButton.height = 32f;
            selectTypesButton.wordWrap = true;
            selectTypesButton.eventClick += (c, p) =>
            {
                if (_depotDropDown.SelectedItem <= 0)
                {
                    return;
                }

                PrefabPanelManager.SetTarget(_cachedLineID);
            };
            _selectTypes = selectTypesButton;
            UIButton button2 = UIUtils.CreateButton(uiPanel);
            button2.name = "AddVehicle";
            button2.textPadding = new RectOffset(10, 10, 4, 0);
            button2.text = Localization.Get("LINE_PANEL_ADD_VEHICLE");
            button2.textScale = 0.8f;
            button2.tooltip = Localization.Get("LINE_PANEL_ADD_VEHICLE_TOOLTIP");
            button2.width = 97f;
            button2.height = 32f;
            button2.wordWrap = true;
            button2.eventClick += OnAddVehicleClick;
            _addVehicle = button2;
            UIButton button3 = UIUtils.CreateButton(uiPanel);
            button3.name = "RemoveVehicle";
            button3.textPadding = new RectOffset(10, 10, 4, 0);
            button3.text = Localization.Get("LINE_PANEL_REMOVE_VEHICLE");
            button3.textScale = 0.8f;
            button3.width = 97f;
            button3.height = 32f;
            button3.wordWrap = true;
            button3.eventClick += OnRemoveVehicleClick;
        }


        private void CreateVehiclesOnLinePanel()
        {
            UIPanel uiPanel = _publicTransportWorldInfoPanel.component.AddUIComponent<UIPanel>();
            uiPanel.name = "VehiclesOnLine";
            uiPanel.AlignTo(uiPanel.parent, UIAlignAnchor.TopRight);
            uiPanel.relativePosition =
                new Vector3((float)(uiPanel.parent.width + /*(double) _prefabPanel.width */+180f + 3.0), 0.0f);
            uiPanel.width = 180f;
            uiPanel.height = 200f; //TODO: do we really need to set?
            uiPanel.backgroundSprite = "UnlockingPanel2";
            uiPanel.opacity = 0.95f;
            _lineVehiclePanel = uiPanel;
            UILabel uiLabel = uiPanel.AddUIComponent<UILabel>();
            uiLabel.text = Localization.Get("LINE_PANEL_LINE_VEHICLES");
            uiLabel.textAlignment = UIHorizontalAlignment.Center;
            uiLabel.font = _vehicleAmount.font;
            uiLabel.position = new Vector3((float)(uiPanel.width / 2.0 - uiLabel.width / 2.0),
                (float)(uiLabel.height / 2.0 - 20.0));
            VehicleListBox vehicleListBox = VehicleListBox.Create(uiPanel);
            vehicleListBox.name = "VehicleListBox";
            vehicleListBox.AlignTo(uiPanel, UIAlignAnchor.TopLeft);
            vehicleListBox.relativePosition = new Vector3(3f, 40f);
            vehicleListBox.width = uiPanel.width - 6f;
            vehicleListBox.height = 162f;
            vehicleListBox.Font = _vehicleAmount.font;
            _lineVehicleListBox = vehicleListBox;
        }

        private void CreateVehiclesInQueuePanel()
        {
            UIPanel uiPanel = _publicTransportWorldInfoPanel.component.AddUIComponent<UIPanel>();
            uiPanel.name = "VehiclesInQueue";
            uiPanel.AlignTo(uiPanel.parent, UIAlignAnchor.TopRight);
            uiPanel.relativePosition = new Vector3(uiPanel.parent.width + 180f + 180f + 1f, +0f);
            uiPanel.width = 180f;
            uiPanel.height = 200f; //TODO: do we really need to set?
            uiPanel.backgroundSprite = "UnlockingPanel2";
            uiPanel.opacity = 0.95f;
            _vehiclesInQueuePanel = uiPanel;
            UILabel uiLabel = uiPanel.AddUIComponent<UILabel>();
            uiLabel.text = Localization.Get("LINE_PANEL_ENQUEUED");
            uiLabel.textAlignment = UIHorizontalAlignment.Center;
            uiLabel.font = _vehicleAmount.font;
            uiLabel.position = new Vector3((float)(uiPanel.width / 2.0 - uiLabel.width / 2.0),
                (float)(uiLabel.height / 2.0 - 20.0));
            VehicleListBox vehicleListBox = VehicleListBox.Create(uiPanel);
            vehicleListBox.name = "VehicleListBox";
            vehicleListBox.AlignTo(uiPanel, UIAlignAnchor.TopLeft);
            vehicleListBox.relativePosition = new Vector3(3f, 40f);
            vehicleListBox.width = uiPanel.width - 6f;
            vehicleListBox.height = 162f;
            vehicleListBox.Font = _vehicleAmount.font;
            _vehiclesInQueueListBox = vehicleListBox;
        }

        private void OnMouseEnter(UIComponent component, UIMouseEventParameter p)
        {
            ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
            if (lineId == 0)
                return;

            // Use cached result if line hasn't changed and cache is still fresh (<1s old)
            if (lineId == _cachedWaitingPassengersLineId &&
                Time.unscaledTime - _cachedWaitingPassengersTime < 1.0f)
            {
                component.tooltip = string.Format(Localization.Get("LINE_PANEL_TOTAL_WAITING_PEOPLE_TOOLTIP"), _cachedWaitingPassengersCount);
                return;
            }

            // Calculate total waiting passengers and cache the result
            TransportLine transportLine = Singleton<TransportManager>.instance.m_lines.m_buffer[lineId];
            ushort num1 = transportLine.m_stops;
            int num2 = 0;
            for (int index = 0; index < transportLine.CountStops(lineId); ++index)
            {
                num2 += WaitingPassengerCountQuery.Query(num1, out var nextStop, out _);
                num1 = nextStop;
            }

            _cachedWaitingPassengersLineId = lineId;
            _cachedWaitingPassengersCount = num2;
            _cachedWaitingPassengersTime = Time.unscaledTime;

            component.tooltip = string.Format(Localization.Get("LINE_PANEL_TOTAL_WAITING_PEOPLE_TOOLTIP"), num2);
        }

        private void OnBudgetControlClick(UIComponent component, UIMouseEventParameter p)
        {
            SimulationManager.instance.AddAction(() =>
            {
                ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
                if (lineId == 0)
                    return;
                bool budgetControlState = CachedTransportLineData.GetBudgetControlState(lineId);
                CachedTransportLineData.SetBudgetControlState(lineId, !budgetControlState);
                if (budgetControlState)
                    return;
                CachedTransportLineData.ClearEnqueuedVehicles(lineId);
                CachedTransportLineData.SetTargetVehicleCount(lineId, 0);
            });
        }

        private void OnUnbunchingClick(UIComponent component, UIMouseEventParameter p)
        {
            SimulationManager.instance.AddAction(() =>
            {
                ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
                if (lineId == 0)
                    return;
                bool unbunchingState = CachedTransportLineData.GetUnbunchingState(lineId);
                CachedTransportLineData.SetUnbunchingState(lineId, !unbunchingState);
            });
        }

        private void OnAddVehicleClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            SimulationManager.instance.AddAction(() =>
            {
                ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
                var transportManager = Singleton<TransportManager>.instance;
                if (lineId == 0 || transportManager == null || lineId >= transportManager.m_lines.m_buffer.Length)
                    return;

                ushort depot = CachedTransportLineData.GetDepot(lineId);
                TransportInfo info = transportManager.m_lines.m_buffer[lineId].Info;
                if (info == null || CachedTransportLineData.GetTargetVehicleCount(lineId) == int.MaxValue)
                    return;

                if (depot == 0)
                {
                    CachedTransportLineData.SetBudgetControlState(lineId, false);
                    CachedTransportLineData.SetTargetVehicleCount(lineId, 1);
                    return;
                }

                var buildingManager = Singleton<BuildingManager>.instance;
                if (buildingManager == null || depot >= buildingManager.m_buildings.m_buffer.Length ||
                    (buildingManager.m_buildings.m_buffer[depot].m_flags & Building.Flags.Created) == 0 ||
                    !DepotUtil.CanAddVehicle(depot, ref buildingManager.m_buildings.m_buffer[depot], info))
                    return;

                var row = component as VehicleListBoxRow;
                string prefabName = row?.Prefab?.Name ?? CachedTransportLineData.GetRandomPrefab(lineId);
                VehicleInfo prefab = string.IsNullOrEmpty(prefabName)
                    ? null
                    : PrefabCollection<VehicleInfo>.FindLoaded(prefabName);
                if (prefab?.m_class == null || info.m_class == null ||
                    prefab.m_class.m_service != info.m_class.m_service ||
                    prefab.m_class.m_subService != info.m_class.m_subService ||
                    prefab.m_class.m_level != info.m_class.m_level)
                    return;

                CachedTransportLineData.SetBudgetControlState(lineId, false);
                CachedTransportLineData.SetTargetVehicleCount(lineId, CachedTransportLineData.GetTargetVehicleCount(lineId) + 1);
                CachedTransportLineData.EnqueueVehicle(lineId, prefabName);
            });
        }

        private void OnRemoveVehicleClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            SimulationManager.instance.AddAction(() =>
            {
                ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
                if (lineId == 0)
                    return;

                TransportInfo info = Singleton<TransportManager>.instance.m_lines.m_buffer[lineId].Info;
                if (info == null)
                    return;

                CachedTransportLineData.SetBudgetControlState(lineId, false);
                
                int[] selectedIndexes = new int[0];
                HashSet<ushort> selectedVehicles = new HashSet<ushort>();
                
                lock (_vehiclesInQueueListBox)
                {
                    if (_vehiclesInQueueListBox != null)
                        selectedIndexes = _vehiclesInQueueListBox.SelectedIndexes;
                }
                lock (_lineVehicleListBox)
                {
                    if (_lineVehicleListBox != null)
                        selectedVehicles = new HashSet<ushort>(_lineVehicleListBox.SelectedVehicles);
                }
                int targetVehicleCount = CachedTransportLineData.GetTargetVehicleCount(lineId);

                if (selectedIndexes.Length > 0)
                {
                    CachedTransportLineData.DequeueVehicles(lineId, selectedIndexes);
                    return;
                }

                if (selectedVehicles.Count > 0)
                {
                    int removedVehicles = 0;
                    foreach (ushort vehicleID in selectedVehicles)
                    {
                        if (vehicleID == 0)
                            continue;

                        Vehicle vehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleID];
                        if (vehicle.Info == null || (vehicle.m_flags & Vehicle.Flags.Deleted) != 0)
                            continue;

                        TransportLineUtil.RemoveVehicle(lineId, vehicleID, true);
                        removedVehicles++;
                    }

                    if (removedVehicles == 0 && targetVehicleCount > 0)
                    {
                        CachedTransportLineData.DecreaseTargetVehicleCount(lineId);
                    }
                    return;
                }

                if (CachedTransportLineData.EnqueuedVehiclesCount(lineId) > 0)
                {
                    CachedTransportLineData.DequeueVehicle(lineId);
                    return;
                }

                int activeVehicles = TransportLineUtil.CountLineActiveVehicles(lineId, out int allVehicles);
                if (activeVehicles > 0)
                {
                    TransportLineUtil.RemoveActiveVehicle(lineId, true, activeVehicles);
                    return;
                }

                if (allVehicles > 0)
                    return;

                if (targetVehicleCount > 0)
                    CachedTransportLineData.DecreaseTargetVehicleCount(lineId);
            });
        }

        private void OnDeleteLineClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            ushort lineID = WorldInfoCurrentLineIDQuery.Query(out _);
            if (lineID == 0)
                return;
            ConfirmPanel.ShowModal("CONFIRM_LINEDELETE", (comp, ret) =>
            {
                if (ret != 1)
                    return;
                Singleton<SimulationManager>.instance.AddAction(() =>
                    Singleton<TransportManager>.instance.ReleaseLine(lineID));
                CachedTransportLineData.SetLineDefaults(lineID);
                _publicTransportWorldInfoPanel.OnCloseButton();
            });
        }

        private void OnSelectedDepotChanged(UIComponent component, ushort selectedItem)
        {
            SimulationManager.instance.AddAction(() =>
            {
                ushort lineId = WorldInfoCurrentLineIDQuery.Query(out _);
                if (lineId == 0)
                    return;
                CachedTransportLineData.SetDepot(lineId, selectedItem);
            });
        }

        private void OnDepotMarkerClicked(UIComponent component, UIMouseEventParameter eventParam)
        {
            component.Unfocus();
            if (_depotDropDown.SelectedItem == 0)
                return;
            InstanceID id = new InstanceID();
            id.Building = _depotDropDown.SelectedItem;
            ToolsModifierControl.cameraController.SetTarget(id,
                ToolsModifierControl.cameraController.transform.position,
                Input.GetKey(KeyCode.LeftShift) | Input.GetKey(KeyCode.RightShift));
            DefaultTool.OpenWorldInfoPanel(id, ToolsModifierControl.cameraController.transform.position);
        }


        private void OnColorChanged(UIComponent component, Color color)
        {
            _colorTextField.text = ColorUtility.ToHtmlStringRGB(color);
        }

        private void OnColorTextSubmitted(UIComponent component, string text)
        {
            Color color;
            if (!ColorUtility.TryParseHtmlString("#" + text, out color))
                return;
            _colorField.selectedColor = color;
            if (OnColorChangedMethod == null || _publicTransportWorldInfoPanel == null)
            {
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.LogWarning("PanelExtenderLine: OnColorChanged method missing; colour text submit ignored.");
                }

                return;
            }

            OnColorChangedMethod.Invoke(
                _publicTransportWorldInfoPanel, new object[2]
                {
                    component,
                    color
                });
        }

        private readonly object _updateDepotsLock = new();

        private void OnDepotChanged(ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level)
        {
            lock (_updateDepotsLock)
            {
                _updateDepots[new ItemClassTriplet(service, subService, level)] = true;
            }
        }

        private void PopulateDepotDropDown(TransportInfo info)
        {
            _depotDropDown.ClearItems();
            if (info == null)
            {
                return;
            }

            ushort[] depots = BuildingExtension.GetDepots(info);
            if (depots == null || depots.Length == 0)
            {
                return;
            }

            // Sort depots alphabetically by their display name
            System.Array.Sort(depots, (a, b) => IDToName(a).CompareTo(IDToName(b)));
            
            _depotDropDown.AddItems(depots, IDToName);
        }

        private int GetDepotDistrictNamesHash(TransportInfo info)
        {
            if (info == null)
                return 0;

            ushort[] depots = BuildingExtension.GetDepots(info);
            if (depots == null || depots.Length == 0)
                return 0;

            int hash = 17;
            DistrictManager dm = Singleton<DistrictManager>.instance;
            BuildingManager bm = Singleton<BuildingManager>.instance;

            foreach (ushort depotId in depots)
            {
                Building depot = bm.m_buildings.m_buffer[depotId];
                byte district = dm.GetDistrict(depot.m_position);
                if (district != 0)
                {
                    string districtName = dm.GetDistrictName(district);
                    if (!string.IsNullOrEmpty(districtName))
                        unchecked
                        {
                            hash = hash * 31 + districtName.GetHashCode();
                        }
                }
            }

            return hash;
        }

        private string IDToName(ushort buildingID)
        {
            if (buildingID == 0)
                return "Invalid Depot";

            BuildingManager bm = Singleton<BuildingManager>.instance;
            Building building = bm.m_buildings.m_buffer[buildingID];
            
            // Handle Untouchable flag
            if ((building.m_flags & Building.Flags.Untouchable) != Building.Flags.None)
            {
                buildingID = bm.FindBuilding(building.m_position, 100f,
                    ItemClass.Service.None, ItemClass.SubService.None, Building.Flags.Active,
                    Building.Flags.Untouchable);
                if (buildingID == 0)
                    return "Invalid Depot";
                building = bm.m_buildings.m_buffer[buildingID];
            }
            
            // Check if building has a custom name (user-assigned)
            if ((building.m_flags & Building.Flags.CustomName) != Building.Flags.None)
            {
                // User-assigned name - return it directly
                InstanceID id = default(InstanceID);
                id.Building = buildingID;
                string customName = Singleton<InstanceManager>.instance.GetName(id);
                if (!string.IsNullOrEmpty(customName))
                {
                    return customName;
                }
            }
            
            // No custom name: use the game's standard building naming logic
            string displayName = bm.GetBuildingName(buildingID, InstanceID.Empty) ?? "";

            // Append district if available (fallback for standard names)
            byte district = Singleton<DistrictManager>.instance.GetDistrict(building.m_position);
            if (district != 0)
            {
                string districtName = Singleton<DistrictManager>.instance.GetDistrictName(district);
                if (!string.IsNullOrEmpty(districtName))
                {
                    if (string.IsNullOrEmpty(displayName))
                    {
                        string baseName = building.Info?.name;
                        if (string.IsNullOrEmpty(baseName))
                            baseName = "Depot " + buildingID;
                        displayName = baseName + " (" + districtName + ")";
                    }
                    else
                    {
                        displayName = displayName + " (" + districtName + ")";
                    }
                }
            }

            // Fall back only if game name is empty
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = building.Info?.name;
                if (string.IsNullOrEmpty(displayName))
                    displayName = "Depot " + buildingID;
            }

            return displayName;
        }


        private void PopulateLineVehicleListBox(ushort lineID, ItemClass.Service service,
            ItemClass.SubService subService, ItemClass.Level level)
        {
            _lineVehicleListBox.ClearItems();
            ActiveVehiclesQuery.Query(lineID, new ItemClassTriplet(service, subService, level))
                .ForEach(result => { _lineVehicleListBox.AddItem(result.PrefabData, result.VehicleID); });
        }

        private void PopulateQueuedVehicleListBox(ushort lineID, ItemClass.Service service,
            ItemClass.SubService subService, ItemClass.Level level)
        {
            _vehiclesInQueueListBox.ClearItems();
            QueuedVehicleQuery.Query(lineID, new ItemClassTriplet(service, subService, level))
                .ForEach(data => { _vehiclesInQueueListBox.AddItem(data); });
        }
    }
}




