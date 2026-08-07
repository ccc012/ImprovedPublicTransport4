// Decompiled with JetBrains decompiler
// Type: ImprovedPublicTransport.PanelExtenderVehicle
// Assembly: ImprovedPublicTransport, Version=1.0.6177.17409, Culture=neutral, PublicKeyToken=null
// MVID: 76F370C5-F40B-41AE-AA9D-1E3F87E934D3
// Assembly location: C:\Games\Steam\steamapps\workshop\content\255710\424106600\ImprovedPublicTransport.dll

using System;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ImprovedPublicTransport.HarmonyPatches.TransportLinePatches;
using ImprovedPublicTransport.Query;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.Util;
using UnityEngine;
using UIUtils = ImprovedPublicTransport.Util.UIUtils;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.UI.PanelExtenders
{
  // Unity does not guarantee LateUpdate ordering between different MonoBehaviours unless told to -
  // "LateUpdate always runs after every Update" (see the comment on our own LateUpdate below) is
  // only true for OUR LateUpdate vs vanilla's Update. If the vanilla vehicle panel (or another mod)
  // also has its own LateUpdate touching these same labels, whichever LateUpdate happens to run
  // later that frame wins, and text kept flickering even with the reapply-cache pattern in place.
  // Forcing this about as late as the execution order range allows makes us win deterministically.
  [DefaultExecutionOrder(32000)]
  public class PanelExtenderVehicle : MonoBehaviour
  {
    private bool _initialized;
    private PublicTransportVehicleWorldInfoPanel _publicTransportVehicleWorldInfoPanel;
    private UIButton _editType;
    private UIPanel _passengerPanel;
    private UILabel _lastStopExchange;
    private UIPanel _statsPanel;
    private UILabel _passengersCurrentWeek;
    private UILabel _passengersLastWeek;
    private UILabel _passengersAverage;
    private UILabel _earningsCurrentWeek;
    private UILabel _earningsLastWeek;
    private UILabel _earningsAverage;
    private UIPanel _buttonPanel;
    private UILabel _status;
    // How _status is hidden while it has nothing worth showing (moving between stops - see the
    // else branch in UpdateBindings): its text colour is made fully transparent, and left that way.
    //
    // Every earlier attempt fought vanilla for _status.text / .isVisible and lost intermittently
    // (that IS the flicker: vanilla rewrites the label every frame from its own Update). Covering
    // the label with an opaque sibling panel failed too, because the cover was positioned/sized
    // from _status itself and parented next to it, so it inherited the very layout vanilla was
    // churning. Transparency sidesteps the race completely instead of trying to win it: vanilla
    // writes the label's *text*, never its colour, so it does not matter who writes the text last -
    // invisible text is invisible either way. ReapplyCachedFields re-asserts the colour anyway, so
    // even a future game/mod that did touch textColor could not bring it back.
    private Color32 _statusVisibleTextColor;
    private bool _statusTextHidden;
    private UIButton _target;
    private UILabel _distance;
    private UIProgressBar _distanceTraveled;
    private UILabel _distanceProgress;
    // Re-applied every LateUpdate (see below) so we always win the race against vanilla's own
    // Update() writing the same labels - see the comment on LateUpdate(). Populated inside
    // UpdateBindings() every time _distance.text is set there. _cachedStatusText is ONLY set
    // while the vehicle is stopped/boarding (_status shows real info there, e.g. "unbunching",
    // and is visible, so this race still matters) - UpdateBindings clears it back to null while
    // moving, since _status is transparent then and there is nothing left to defend.
    private string _cachedStatusText;
    private string _cachedDistanceText;
    private float _nextBindingsRealtime;

    // Same race as _cachedStatusText/_cachedDistanceText above, but for the progress bar - vanilla
    // writes m_DistanceTraveled's value/color every frame too, and our own writes only happen on
    // the 0.2s UpdateBindings throttle, so most frames showed vanilla's own progress/colour with
    // ours flashing in only on the throttled frames (worst while boarding/stopped: vanilla and our
    // green boarding-progress bar fight every frame). Null/unset means "we don't own this field in
    // the current state" - LateUpdate only reapplies when a value is actually cached.
    private float? _cachedProgressValue;
    private Color32? _cachedProgressColor;
    private string _cachedProgressText;
    private ushort _observedVehicleId;
    private ushort _observedLineId;
    private bool _observedStopped;
    private bool _snapshotInitialized;

    private bool _endOfFrameLoopRunning;

    public void LateUpdate()
    {
      if (!this._initialized)
      {
        this.Init();
        return;
      }

      if (!this._publicTransportVehicleWorldInfoPanel.component.isVisible)
      {
        this.ClearOwnedFields();
        this._snapshotInitialized = false;
        return;
      }

      var now = UnityEngine.Time.unscaledTime;
      if (!this.TryObserveVehicle(out var vehicleId, out var lineId, out var stopped, out var routeProgress))
      {
        this.ClearOwnedFields();
        this._snapshotInitialized = false;
        return;
      }

      var stateChanged = !this._snapshotInitialized
        || this._observedVehicleId != vehicleId
        || this._observedLineId != lineId
        || this._observedStopped != stopped;
      this._observedVehicleId = vehicleId;
      this._observedLineId = lineId;
      this._observedStopped = stopped;
      this._snapshotInitialized = true;

      if (stateChanged || now >= this._nextBindingsRealtime)
      {
        // Passenger stats, earnings and other panel bindings remain throttled; vehicle identity and
        // progress-bar transitions are observed separately each frame below.
        this._nextBindingsRealtime = now + 0.2f;
        if (stateChanged)
        {
          this.ClearOwnedFields();
        }
        this.UpdateBindings();
      }

      if (stopped)
      {
        this.UpdateBoardingProgress(vehicleId);
      }
      else if (routeProgress)
      {
        this.UpdateProgress();
        if (this._cachedProgressValue.HasValue)
        {
          this._cachedProgressColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        }
      }

      this.ReapplyCachedFields();

      if (!this._endOfFrameLoopRunning)
      {
        this._endOfFrameLoopRunning = true;
        this.StartCoroutine(this.EndOfFrameReapplyLoop());
      }
    }

    // [DefaultExecutionOrder] on this class forces our LateUpdate to run after every other
    // MonoBehaviour's LateUpdate this frame - but the vanilla panel's own field-clearing turned out
    // to still win sometimes even so (confirmed: text flickers between real value and BLANK, not
    // between two different real values - so this was never a stale-cache issue, always a "who
    // wrote last" issue). WaitForEndOfFrame runs after Unity has finished ALL Update/LateUpdate
    // calls for every object in the scene for that frame, which is later than LateUpdate can ever
    // be pushed via execution order alone - the only later point before the frame actually renders.
    private System.Collections.IEnumerator EndOfFrameReapplyLoop()
    {
      while (this._initialized)
      {
        yield return new UnityEngine.WaitForEndOfFrame();
        if (this._publicTransportVehicleWorldInfoPanel != null
            && this._publicTransportVehicleWorldInfoPanel.component.isVisible)
        {
          if (this.IsSnapshotCurrent())
          {
            this.ReapplyCachedFields();
          }
          else
          {
            this.ClearOwnedFields();
            this._snapshotInitialized = false;
          }
        }
      }

      this._endOfFrameLoopRunning = false;
    }

    private void ReapplyCachedFields()
    {
      // The vanilla panel's own Update() writes the Status/Distance labels every frame with its
      // own text (e.g. it can decide a vehicle is "not on route" the instant it isn't strictly
      // between two stops) - nothing patches or disables that native refresh. Re-applying the LAST
      // text UpdateBindings computed - a couple of string assignments, not a recompute - is enough
      // to win that race, without re-running the expensive parts on every frame too. The previous
      // "no throttle at all" version fixed the flicker but paid for a full FindObjectsOfType scan
      // every frame just to keep two labels current; this keeps the win without the cost. Most
      // visible while paused, since vanilla's Update() keeps running under pause but a throttle
      // timer built on unscaledTime also keeps advancing, so the two were never in sync at any
      // fixed interval. _cachedStatusText is null (see the field comment) whenever _status is
      // hidden, so this is a no-op for the Status label in that state - it only still does real
      // work for _status while stopped/boarding, and always for _distance.
      if (this._status != null)
      {
        // Cheap (two byte compares) and belt-and-braces: nothing in vanilla writes this label's
        // colour today, so the assignment below normally never has anything to undo - but keeping
        // it here means the hide cannot regress into a race even if that ever changes.
        var wanted = this._statusTextHidden
          ? new Color32(this._statusVisibleTextColor.r, this._statusVisibleTextColor.g, this._statusVisibleTextColor.b, 0)
          : this._statusVisibleTextColor;
        this._status.textColor = wanted;

        if (this._cachedStatusText != null)
        {
          this._status.text = this._cachedStatusText;
        }
      }

      if (this._distance != null && this._cachedDistanceText != null)
      {
        this._distance.text = this._cachedDistanceText;
      }

      if (this._distanceTraveled != null)
      {
        // Only reapply progress for stopped vehicles (boarding/unbunching).
        // For moving vehicles, let vanilla handle the progress bar entirely.
        var manager = Singleton<VehicleManager>.instance;
        bool isStopped = false;
        bool isRouteProgress = false;
        if (manager != null && this._observedVehicleId != 0 && this._observedVehicleId < manager.m_vehicles.m_buffer.Length)
        {
          ref var vehicle = ref manager.m_vehicles.m_buffer[this._observedVehicleId];
          if (vehicle.Info != null)
          {
            isStopped = (vehicle.m_flags & Vehicle.Flags.Stopped) != 0;
            var subService = vehicle.Info.m_class?.m_subService ?? ItemClass.SubService.None;
            isRouteProgress = subService == ItemClass.SubService.PublicTransportShip
              || subService == ItemClass.SubService.PublicTransportPlane;
          }
        }

        if (isStopped)
        {
          if (this._cachedProgressValue.HasValue)
          {
            this._distanceTraveled.value = this._cachedProgressValue.Value;
          }

          if (this._cachedProgressColor.HasValue)
          {
            this._distanceTraveled.progressColor = this._cachedProgressColor.Value;
          }
        }
        else if (!isRouteProgress)
        {
          // Moving ordinary vehicle: hand the whole bar back to vanilla. Clear our cached colour
          // so it cannot stick green, and force the default (white) colour - the vanilla sprite
          // turns that into the normal blue bar.
          this._cachedProgressColor = null;
          this._cachedProgressValue = null;
          this._cachedProgressText = null;
          this._distanceTraveled.progressColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        }

        if (this._cachedProgressText != null)
        {
          this._distanceProgress.text = this._cachedProgressText;
        }
      }

    }

    private void ClearProgressOwnership()
    {
      this._cachedProgressValue = null;
      this._cachedProgressColor = null;
      this._cachedProgressText = null;
    }

    private void UpdateBoardingProgress(ushort vehicleId)
    {
      var manager = Singleton<VehicleManager>.instance;
      if (manager == null || vehicleId == 0 || vehicleId >= manager.m_vehicles.m_buffer.Length)
      {
        this.ClearProgressOwnership();
        return;
      }

      ref var vehicle = ref manager.m_vehicles.m_buffer[vehicleId];
      if (vehicle.Info == null || (vehicle.m_flags & Vehicle.Flags.Stopped) == 0)
      {
        this.ClearProgressOwnership();
        return;
      }

      var boardingTime = vehicle.Info.m_vehicleType == VehicleInfo.VehicleType.Plane
        ? CanLeaveStopPatch.AirplaneBoardingTime
        : CanLeaveStopPatch.BoardingTime;
      var progress = Mathf.Clamp01(vehicle.m_waitCounter / (float)boardingTime);
      this._distanceTraveled.progressColor = Color.green;
      this._distanceTraveled.value = progress;
      this._distanceProgress.text = LocaleFormatter.FormatPercentage(Mathf.RoundToInt(progress * 100f));
      this._cachedProgressValue = progress;
      this._cachedProgressColor = Color.green;
      this._cachedProgressText = this._distanceProgress.text;
    }

    private void ClearOwnedFields()
    {
      this._cachedStatusText = null;
      this._cachedDistanceText = null;
      this.ClearProgressOwnership();
      this._statusTextHidden = false;
      if (this._status != null)
      {
        this._status.textColor = this._statusVisibleTextColor;
      }
    }

    private bool TryObserveVehicle(out ushort vehicleId, out ushort lineId, out bool stopped, out bool routeProgress)
    {
      vehicleId = 0;
      lineId = 0;
      stopped = false;
      routeProgress = false;

      var current = WorldInfoPanel.GetCurrentInstanceID();
      if (current.Type != InstanceType.Vehicle || current.Vehicle == 0)
      {
        return false;
      }

      var manager = Singleton<VehicleManager>.instance;
      if (manager == null || current.Vehicle >= manager.m_vehicles.m_buffer.Length)
      {
        return false;
      }

      vehicleId = manager.m_vehicles.m_buffer[current.Vehicle].GetFirstVehicle(current.Vehicle);
      if (vehicleId == 0 || vehicleId >= manager.m_vehicles.m_buffer.Length)
      {
        return false;
      }

      ref var vehicle = ref manager.m_vehicles.m_buffer[vehicleId];
      if (vehicle.Info == null)
      {
        return false;
      }

      lineId = vehicle.m_transportLine;
      if (lineId == 0)
      {
        return true;
      }

      stopped = (vehicle.m_flags & Vehicle.Flags.Stopped) != 0;
      var subService = vehicle.Info.m_class?.m_subService ?? ItemClass.SubService.None;
      routeProgress = subService == ItemClass.SubService.PublicTransportShip
        || subService == ItemClass.SubService.PublicTransportPlane;
      return true;
    }

    private bool IsSnapshotCurrent()
    {
      return this._snapshotInitialized
        && this.TryObserveVehicle(out var vehicleId, out var lineId, out var stopped, out _)
        && vehicleId == this._observedVehicleId
        && lineId == this._observedLineId
        && stopped == this._observedStopped;
    }

    private void Init()
    {
      var panelObject = GameObject.Find("(Library) PublicTransportVehicleWorldInfoPanel");
      if (panelObject == null)
      {
        return;
      }

      this._publicTransportVehicleWorldInfoPanel = panelObject.GetComponent<PublicTransportVehicleWorldInfoPanel>();
      if (!((UnityEngine.Object) this._publicTransportVehicleWorldInfoPanel != (UnityEngine.Object) null))
        return;
      this._status = this._publicTransportVehicleWorldInfoPanel.Find<UILabel>("Status");
      if (this._status != null)
      {
        // Captured before anything of ours touches it, so restoring the visible state later can
        // never drift from whatever colour the active UI theme actually uses for this label.
        this._statusVisibleTextColor = this._status.textColor;
      }
      this._target = this._publicTransportVehicleWorldInfoPanel.Find<UIButton>("Target");
      // Remove any native click handlers so only ours fires
      ClearEventClickHandlers(this._target);
      this._target.eventClick += new MouseEventHandler(this.OnTargetClick);
      this._distance = this._publicTransportVehicleWorldInfoPanel.Find<UILabel>("Distance");
      this._distanceTraveled = Utils.GetPrivate<UIProgressBar>((object) this._publicTransportVehicleWorldInfoPanel, "m_DistanceTraveled");
      this._distanceProgress = Utils.GetPrivate<UILabel>((object) this._publicTransportVehicleWorldInfoPanel, "m_DistanceProgress");
      if (this._distanceTraveled == null || this._distanceProgress == null)
      {
        this.ClearOwnedFields();
        return;
      }
      this.AddPanelControls();
      this._initialized = true;
    }


    private void UpdateBindings()
    {
      VehicleManager vm = Singleton<VehicleManager>.instance;
      var vehicleID = this._observedVehicleId;
      var lineId = this._observedLineId;
      if (vm == null || vehicleID == 0 || vehicleID >= vm.m_vehicles.m_buffer.Length)
      {
        this.ClearOwnedFields();
        return;
      }

      if ((int) lineId == 0)
      {
        this._passengerPanel.Hide();
        this._statsPanel.Hide();
        this._buttonPanel.Hide();
        this._publicTransportVehicleWorldInfoPanel.component.height = 229f;
        // Not on a line: Status/Distance are vanilla's own concern here (no line/unbunching state
        // for us to describe), so nothing below sets them. Without clearing the cache, LateUpdate
        // would keep reapplying whatever the previous line-vehicle's text was onto this vehicle's
        // panel every frame.
        this.ClearOwnedFields();
      }
      else
      {
        this._publicTransportVehicleWorldInfoPanel.component.height = 377f;
        this._editType.isVisible = !ModSetting.Instance.HideVehicleEditor;
          var transportManager = Singleton<TransportManager>.instance;
          if (transportManager == null || lineId >= transportManager.m_lines.m_buffer.Length)
          {
            this.ClearOwnedFields();
            return;
          }
          var lineInfo = transportManager.m_lines.m_buffer[(int) lineId].Info;
          if (lineInfo == null)
          {
            this.ClearOwnedFields();
            return;
          }
          ItemClass itemClass = lineInfo.m_class;
          ItemClass.SubService subService = itemClass.m_subService;
          ItemClass.Service service = itemClass.m_service;
          ItemClass.Level level = itemClass.m_level;

        switch (subService)
        {
          case ItemClass.SubService.PublicTransportBus:
          case ItemClass.SubService.PublicTransportTours:
          case ItemClass.SubService.PublicTransportMetro:
          case ItemClass.SubService.PublicTransportTrain:
          case ItemClass.SubService.PublicTransportTram:
          case ItemClass.SubService.PublicTransportShip:
          case ItemClass.SubService.PublicTransportPlane:
          case ItemClass.SubService.PublicTransportMonorail:
          case ItemClass.SubService.PublicTransportCableCar:
          case ItemClass.SubService.PublicTransportTrolleybus:
            this._passengerPanel.Show();
            SetLastStopExchangeText(vehicleID);
            break;
         case ItemClass.SubService.None:
             if (service == ItemClass.Service.Disaster && level == ItemClass.Level.Level4)
             {
                 this._passengerPanel.Show();
                 SetLastStopExchangeText(vehicleID);
             }
             else
             {
                 this._passengerPanel.Hide();
             }
             break;
          default:
            this._passengerPanel.Hide();
            break;
        }
        this._distanceTraveled.parent.Show();
        this._distanceProgress.parent.Show();
        ref var vehicle = ref vm.m_vehicles.m_buffer[(int)vehicleID];
        if (vehicle.Info == null)
        {
          this.ClearOwnedFields();
          return;
        }
        if ((vehicle.m_flags & Vehicle.Flags.Stopped) != 0)
        {
          var vehicleCache = CachedVehicleData.m_cachedVehicleData;
          this._statusTextHidden = false;
          if (vehicleCache != null && vehicleID < vehicleCache.Length
              && vehicleCache[(int)vehicleID].IsUnbunchingInProgress)
            this._status.text = Localization.Get("VEHICLE_PANEL_STATUS_UNBUNCHING");
          this._distance.text = this._status.text;
          this._cachedStatusText = this._status.text;
          this._cachedDistanceText = this._distance.text;
          this.ApplyTargetStop(lineId, ref vehicle);
        }
        else
        {
          string text = Localization.Get("VEHICLE_PANEL_STATUS_NEXT_STOP");
          if (subService == ItemClass.SubService.PublicTransportPlane)
          {
            if ((vm.m_vehicles.m_buffer[(int) vehicleID].m_flags & Vehicle.Flags.Landing) != ~(Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive) || (vm.m_vehicles.m_buffer[(int) vehicleID].m_flags & Vehicle.Flags.TakingOff) != ~(Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive) || (vm.m_vehicles.m_buffer[(int) vehicleID].m_flags & Vehicle.Flags.Flying) == ~(Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive))
            {
              text = this._status.text;
            }
          }
          // Vanilla owns all three progress fields while ordinary vehicles are moving. Ship and
          // plane route progress is filled by the lightweight per-frame UpdateProgress call.
          this.ClearProgressOwnership();
          // "Próxima parada" is just a static label here (VEHICLE_PANEL_STATUS_NEXT_STOP never
          // changes while moving) - it carries no information, so rather than fight vanilla's own
          // competing per-frame write of it, we make it transparent (see the _statusTextHidden
          // field comment) and let vanilla write whatever it likes underneath.
          this._statusTextHidden = true;
          this._cachedStatusText = null;
          this.ApplyTargetStop(lineId, ref vehicle);
          this._distance.text = ColossalFramework.Globalization.Locale.Get(this._distance.localeID);
          // _status stays hidden (set above) - intentionally not re-caching its text here.
          this._cachedDistanceText = this._distance.text;
        }
        this._statsPanel.Show();
        var vCache = CachedVehicleData.m_cachedVehicleData;
        if (vCache == null || vehicleID == 0 || vehicleID >= vCache.Length)
        {
          return;
        }

        ref var vData = ref vCache[(int)vehicleID];
        this._passengersCurrentWeek.text = vData.PassengersThisWeek.ToString();
        this._passengersLastWeek.text = vData.PassengersLastWeek.ToString();
        this._passengersAverage.text = vData.PassengersAverage.ToString();
        PrefabData prefabData = VehiclePrefabs.instance.FindByIndex(vehicle.Info.m_prefabDataIndex);
        if (prefabData == null)
        {
          return;
        }
        int num1 = vData.IncomeThisWeek - prefabData.MaintenanceCost;
        UILabel earningsCurrentWeek = this._earningsCurrentWeek;
        float num2 = (float) num1 * 0.01f;
        string str1 = num2.ToString(ColossalFramework.Globalization.Locale.Get("MONEY_FORMAT"), (IFormatProvider) LocaleManager.cultureInfo);
        earningsCurrentWeek.text = str1;
        this._earningsCurrentWeek.textColor = (Color32) this.GetColor((float) num1);
        int incomeLastWeek = vData.IncomeLastWeek;
        UILabel earningsLastWeek = this._earningsLastWeek;
        num2 = (float) incomeLastWeek * 0.01f;
        string str2 = num2.ToString(ColossalFramework.Globalization.Locale.Get("MONEY_FORMAT"), (IFormatProvider) LocaleManager.cultureInfo);
        earningsLastWeek.text = str2;
        this._earningsLastWeek.textColor = (Color32) this.GetColor((float) incomeLastWeek);
        int incomeAverage = vData.IncomeAverage;
        UILabel earningsAverage = this._earningsAverage;
        num2 = (float) incomeAverage * 0.01f;
        string str3 = num2.ToString(ColossalFramework.Globalization.Locale.Get("MONEY_FORMAT"), (IFormatProvider) LocaleManager.cultureInfo);
        earningsAverage.text = str3;
        this._earningsAverage.textColor = (Color32) this.GetColor((float) incomeAverage);
        this._buttonPanel.Show();
      }
    }

    // Always own the Target button (the blue next-stop name) while the vehicle is on a line, in
    // every state. Previously we only wrote it while moving, and deliberately handed it back to
    // vanilla while boarding/stopped and during plane takeoff/landing - and handing it back is
    // exactly what made it flicker, because vanilla rewrites it every frame from its own Update,
    // so in those states there were two writers and no fixed winner. There is a correct value to
    // show in all of those states, so there is no reason to ever stop reapplying it.
    private void ApplyTargetStop(ushort lineId, ref Vehicle vehicle)
    {
      if (this._target == null)
      {
        return;
      }

      ushort targetBuilding = vehicle.m_targetBuilding;
      InstanceID id = new InstanceID();
      id.NetNode = targetBuilding;
      // objectUserData only - deliberately NOT .text. Vanilla's own Update writes this button's
      // caption every frame with the same stop name we would compute, so writing it too achieved
      // nothing except making us the second writer, and two writers with no fixed winner is
      // exactly what the flicker was. We need the instance ID (OnTargetClick reads it to open
      // IPT's stop panel), so that is all we set, and the caption is left to the game.
      this._target.objectUserData = (object) id;
      // Nothing else. Not .text, and deliberately not Enable()/Show() either: vanilla drives this
      // button's caption AND its visibility every frame, so forcing it visible on our 0.2s tick
      // meant that in any state where vanilla hides it, the button was being switched back on
      // several times a second - which reads as the caption flickering. The click handler only
      // needs objectUserData, so that is the only thing we still set.
    }

    private void SetLastStopExchangeText(ushort vehicleID)
    {
      var cache = CachedVehicleData.m_cachedVehicleData;
      if (cache == null || vehicleID == 0 || vehicleID >= cache.Length || this._lastStopExchange == null)
      {
        return;
      }

      this._lastStopExchange.text = string.Format(
          Localization.Get("VEHICLE_PANEL_LAST_STOP_EXCHANGE"),
          cache[vehicleID].LastStopGonePassengers,
          cache[vehicleID].LastStopNewPassengers);
    }

    private void OnDestroy()
    {
      if ((UnityEngine.Object) this._target != (UnityEngine.Object) null)
        this._target.eventClick -= new MouseEventHandler(this.OnTargetClick);
      if ((UnityEngine.Object) this._editType != (UnityEngine.Object) null)
        UnityEngine.Object.Destroy((UnityEngine.Object) this._editType.gameObject);
      if ((UnityEngine.Object) this._passengerPanel != (UnityEngine.Object) null)
        UnityEngine.Object.Destroy((UnityEngine.Object) this._passengerPanel.gameObject);
      if ((UnityEngine.Object) this._statsPanel != (UnityEngine.Object) null)
        UnityEngine.Object.Destroy((UnityEngine.Object) this._statsPanel.gameObject);
      if (!((UnityEngine.Object) this._buttonPanel != (UnityEngine.Object) null))
        return;
      UnityEngine.Object.Destroy((UnityEngine.Object) this._buttonPanel.gameObject);
    }

    private void AddPanelControls()
    {
      UILabel uiLabel1 = Utils.GetPrivate<UILabel>((object) this._publicTransportVehicleWorldInfoPanel, "m_Type");
      _publicTransportVehicleWorldInfoPanel.Find<UIButton>("LinesOverview").parent.height = 20f;
      int num1 = 132;
      uiLabel1.anchor = (UIAnchorStyle) num1;
      UIPanel parent = (UIPanel) uiLabel1.parent;
      RectOffset rectOffset = new RectOffset(0, 10, 0, 0);
      parent.autoLayoutPadding = rectOffset;
      double num2 = 25.0;
      parent.height = (float) num2;
      int num3 = 1;
      parent.useCenter = num3 != 0;
      UIButton button1 = UIUtils.CreateButton((UIComponent) parent);
      button1.name = "EditType";
      button1.autoSize = true;
      button1.anchor = UIAnchorStyle.Left | UIAnchorStyle.Right | UIAnchorStyle.CenterVertical;
      button1.textPadding = new RectOffset(10, 10, 4, 2);
      button1.text = Localization.Get("VEHICLE_PANEL_EDIT_TYPE");
      button1.tooltip = string.Format(Localization.Get("VEHICLE_PANEL_EDIT_TYPE_TOOLTIP"));
      button1.textScale = 0.75f;
      button1.eventClick += new MouseEventHandler(this.OnEditTypeClick);
      button1.isVisible = !ModSetting.Instance.HideVehicleEditor;
      this._editType = button1;
      UILabel uiLabel2 = Utils.GetPrivate<UILabel>((object) this._publicTransportVehicleWorldInfoPanel, "m_Passengers");
      UIPanel uiPanel1 = this._publicTransportVehicleWorldInfoPanel.component.Find<UIPanel>("Panel");
      UIPanel uiPanel2 = uiPanel1.AddUIComponent<UIPanel>();
      uiPanel2.autoLayout = true;
      uiPanel2.autoLayoutDirection = LayoutDirection.Horizontal;
      uiPanel2.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
      uiPanel2.height = uiLabel2.parent.height;
      uiPanel2.width = uiLabel2.parent.width;
      uiPanel2.zOrder = 4;
      this._passengerPanel = uiPanel2;
      UILabel uiLabel3 = uiPanel2.AddUIComponent<UILabel>();
      uiLabel3.name = "LastStopExchange";
      uiLabel3.font = uiLabel2.font;
      uiLabel3.textColor = uiLabel2.textColor;
      uiLabel3.textScale = uiLabel2.textScale;
      uiLabel3.processMarkup = true;
      this._lastStopExchange = uiLabel3;
      UIPanel uiPanel3 = uiPanel1.AddUIComponent<UIPanel>();
      uiPanel3.name = "PassengerStats";
      uiPanel3.anchor = UIAnchorStyle.Top | UIAnchorStyle.Left | UIAnchorStyle.Right;
      uiPanel3.autoLayout = true;
      uiPanel3.autoLayoutDirection = LayoutDirection.Vertical;
      uiPanel3.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
      uiPanel3.autoLayoutStart = LayoutStart.TopLeft;
      uiPanel3.size = new Vector2(349f, 60f);
      uiPanel3.zOrder = 5;
      this._statsPanel = uiPanel3;
      UILabel label1;
      UILabel label2;
      UILabel label3;
      UILabel label4;
      PublicTransportStopWorldInfoPanel.CreateStatisticRow((UIComponent) uiPanel3, out label1, out label2, out label3, out label4, true);
      label2.text = Localization.Get("CURRENT_WEEK");
      label3.text = Localization.Get("LAST_WEEK");
      label4.text = Localization.Get("AVERAGE");
      label4.tooltip = string.Format(Localization.Get("AVERAGE_TOOLTIP"), (object) ModSetting.Instance.StatisticWeeks);
      PublicTransportStopWorldInfoPanel.CreateStatisticRow((UIComponent) uiPanel3, out label1, out this._passengersCurrentWeek, out this._passengersLastWeek, out this._passengersAverage, false);
      label1.text = Localization.Get("VEHICLE_PANEL_PASSENGERS");
      PublicTransportStopWorldInfoPanel.CreateStatisticRow((UIComponent) uiPanel3, out label1, out this._earningsCurrentWeek, out this._earningsLastWeek, out this._earningsAverage, false);
      label1.text = Localization.Get("VEHICLE_PANEL_EARNINGS");
      label1.tooltip = Localization.Get("VEHICLE_PANEL_EARNINGS_TOOLTIP");
      UIPanel uiPanel4 = uiPanel1.AddUIComponent<UIPanel>();
      uiPanel4.name = "Buttons";
      uiPanel4.anchor = UIAnchorStyle.Top | UIAnchorStyle.Left | UIAnchorStyle.Right;
      uiPanel4.autoLayout = true;
      uiPanel4.autoLayoutDirection = LayoutDirection.Horizontal;
      uiPanel4.autoLayoutPadding = new RectOffset(0, 5, 0, 0);
      uiPanel4.autoLayoutStart = LayoutStart.TopLeft;
      uiPanel4.size = new Vector2(345f, 32f);
      this._buttonPanel = uiPanel4;
      UIButton button2 = UIUtils.CreateButton((UIComponent) uiPanel4);
      button2.name = "PreviousVehicle";
      button2.textPadding = new RectOffset(10, 10, 4, 0);
      button2.text = Localization.Get("VEHICLE_PANEL_PREVIOUS");
      button2.tooltip = Localization.Get("VEHICLE_PANEL_PREVIOUS_TOOLTIP");
      button2.textScale = 0.75f;
      button2.size = new Vector2(110f, 32f);
      button2.wordWrap = true;
      button2.eventClick += new MouseEventHandler(this.OnChangeVehicleClick);
      UIButton button3 = UIUtils.CreateButton((UIComponent) uiPanel4);
      button3.name = "RemoveVehicle";
      button3.textPadding = new RectOffset(10, 10, 4, 0);
      button3.text = Localization.Get("VEHICLE_PANEL_REMOVE_VEHICLE");
      button3.textScale = 0.75f;
      button3.size = new Vector2(100f, 32f);
      button3.wordWrap = true;
      button3.hoveredTextColor = (Color32) Color.red;
      button3.focusedTextColor = (Color32) Color.red;
      button3.pressedTextColor = (Color32) Color.red;
      button3.eventClick += new MouseEventHandler(this.OnRemoveVehicleClick);
      UIButton button4 = UIUtils.CreateButton((UIComponent) uiPanel4);
      button4.name = "NextVehicle";
      button4.textPadding = new RectOffset(10, 10, 4, 0);
      button4.text = Localization.Get("VEHICLE_PANEL_NEXT");
      button4.tooltip = Localization.Get("VEHICLE_PANEL_NEXT_TOOLTIP");
      button4.textScale = 0.75f;
      button4.size = new Vector2(110f, 32f);
      button4.wordWrap = true;
      button4.eventClick += new MouseEventHandler(this.OnChangeVehicleClick);
    }

    private void OnEditTypeClick(UIComponent component, UIMouseEventParameter eventParam)
    {
      if ((UnityEngine.Object) VehicleEditor.Instance == (UnityEngine.Object) null)
        return;
      InstanceID currentInstanceId = WorldInfoPanel.GetCurrentInstanceID();
      ushort firstVehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[(int) currentInstanceId.Vehicle].GetFirstVehicle(currentInstanceId.Vehicle);
      if ((int) firstVehicle == 0)
        return;
      VehicleInfo info = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[(int) firstVehicle].Info;
      Singleton<InfoManager>.instance.SetCurrentMode(InfoManager.InfoMode.Transport, InfoManager.SubInfoMode.Default);
      VehicleEditor.Instance.SetPrefab(info);
    }

    private static void ClearEventClickHandlers(UIComponent component)
    {
      // Walk up the type hierarchy to find and null the backing delegate field for eventClick
      Type type = component.GetType();
      while (type != null)
      {
        FieldInfo field = type.GetField("eventClick", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (field != null && typeof(Delegate).IsAssignableFrom(field.FieldType))
        {
          field.SetValue(component, null);
          return;
        }
        type = type.BaseType;
      }
    }

    private void OnTargetClick(UIComponent component, UIMouseEventParameter eventParam)
    {
      try
      {
        var lineId = WorldInfoCurrentLineIDQuery.Query(out var vehicleID);
        if (lineId == 0 || vehicleID == 0) return;

        ushort targetNode = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[(int) vehicleID].m_targetBuilding;
        if (targetNode == 0) return;

        InstanceID stopID = InstanceID.Empty;
        stopID.NetNode = targetNode;
        Vector3 position = Singleton<NetManager>.instance.m_nodes.m_buffer[(int) targetNode].m_position;

        ToolsModifierControl.cameraController.SetTarget(stopID, position, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        PublicTransportWorldInfoPanel.ResetScrollPosition();
        UIView.SetFocus(null);
        WorldInfoPanel.HideAllWorldInfoPanels();
        if (PublicTransportStopWorldInfoPanel.instance != null)
          PublicTransportStopWorldInfoPanel.instance.Show(position, stopID);
      }
      catch (Exception ex)
      {
        Debug.LogException(ex);
      }
    }

    private void OnChangeVehicleClick(UIComponent component, UIMouseEventParameter eventParam)
    {
      var lineId = WorldInfoCurrentLineIDQuery.Query(out var firstVehicle);
      if (lineId == 0)
        return;
      var num = component.name != "PreviousVehicle" ? TransportLineUtil.GetNextVehicle(lineId, firstVehicle) : TransportLineUtil.GetPreviousVehicle(lineId, firstVehicle);
      if (firstVehicle == (int) num)
        return;
      var instanceId = new InstanceID
      {
        Vehicle = num
      };
      WorldInfoPanel.ChangeInstanceID(WorldInfoPanel.GetCurrentInstanceID(), instanceId);
      ToolsModifierControl.cameraController.SetTarget(instanceId, ToolsModifierControl.cameraController.transform.position, Input.GetKey(KeyCode.LeftShift) | Input.GetKey(KeyCode.RightShift));
    }

    private void OnRemoveVehicleClick(UIComponent component, UIMouseEventParameter eventParam)
    {
        SimulationManager.instance.AddAction(() =>
        {
          var lineId = WorldInfoCurrentLineIDQuery.Query(out var firstVehicle);
            if (lineId == 0 || firstVehicle == 0)
                return;
            CachedTransportLineData.SetBudgetControlState(lineId, false);
            TransportLineUtil.RemoveVehicle(lineId, firstVehicle, true);
        });
    }

    private void UpdateProgress()
    {
      VehicleManager instance = Singleton<VehicleManager>.instance;
      ushort firstVehicle = this._observedVehicleId;
      if (instance == null || firstVehicle == 0 || firstVehicle >= instance.m_vehicles.m_buffer.Length)
      {
        this.ClearProgressOwnership();
        return;
      }

      float current;
      float max;
      if (!GetProgressStatus(firstVehicle, ref instance.m_vehicles.m_buffer[(int) firstVehicle], out current, out max)
          || float.IsNaN(current)
          || float.IsInfinity(current)
          || float.IsNaN(max)
          || float.IsInfinity(max)
          || max <= 0f)
      {
        this.ClearProgressOwnership();
        return;
      }
      this._distanceTraveled.parent.Show();
      this._distanceProgress.parent.Show();
      float num = Mathf.Clamp01(current / max);
      int p = Mathf.RoundToInt(num * 100f);
      this._distanceTraveled.value = num;
      this._distanceProgress.text = LocaleFormatter.FormatPercentage(p);
      this._cachedProgressValue = num;
      this._cachedProgressText = this._distanceProgress.text;
    }

    public static bool GetProgressStatus(ushort vehicleID, ref Vehicle data, out float current, out float max)
    {
        ushort transportLine = data.m_transportLine;
        ushort targetBuilding = data.m_targetBuilding;
        var transportManager = Singleton<TransportManager>.instance;
        var pathManager = Singleton<PathManager>.instance;
        if (transportManager != null
            && pathManager != null
            && transportLine != 0
            && transportLine < transportManager.m_lines.m_buffer.Length
            && targetBuilding != 0)
        {
            float min;
            float max1;
            float total;
            Singleton<TransportManager>.instance.m_lines.m_buffer[(int) transportLine]
                .GetStopProgress(targetBuilding, out min, out max1, out total);
            uint path = data.m_path;
            bool valid;
            if (path == 0
                || path >= pathManager.m_pathUnits.m_buffer.Length
                || (data.m_flags & Vehicle.Flags.WaitingPath) != 0)
            {
                current = min;
                valid = false;
            }
            else
                current = BusAI.GetPathProgress(path, (int) data.m_pathPositionIndex, min, max1, out valid);
            max = total;
            return valid;
        }
        current = 0.0f;
        max = 0.0f;
        return true;
    }

    private Color GetColor(float value)
    {
      if ((double) value >= 0.0)
        return Color.green;
      return Color.red;
    }
  }
}
