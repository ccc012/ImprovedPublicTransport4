using System;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;
using ImprovedPublicTransport;
using ImprovedPublicTransport.UI;
using ImprovedPublicTransport.Util;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace CommuterDestination
{
    /// <summary>
    /// Clean-room replacement for the Commuter Destination feature. Written from the behaviour
    /// spec in Docs/2026-08-04-commuter-destinations-design.md using only vanilla game APIs; it
    /// does not reuse any code or structure from the ShowCommuterDestination-derived module that
    /// it replaces.
    ///
    /// Behaviour: when a stop is selected in IPT's stop panel, the citizens waiting at that stop
    /// are found through <see cref="CitizenManager.m_citizenGrid"/>, the ones actually waiting
    /// there are confirmed by <see cref="CitizenAI.TransportArriveAtSource"/> (the same check the
    /// game's pathfinding uses), and each passenger's final destination building is read from
    /// <see cref="CitizenInstance.m_targetBuilding"/> - the building CitizenAI pinned when the
    /// journey started, which stays valid all the way through a transit ride. Destinations are
    /// aggregated per building and published as an immutable snapshot.
    ///
    /// Rendering: the manager is registered with <see cref="SimulationManager.RegisterManager"/>
    /// as an <see cref="IRenderableManager"/> and draws in
    /// <see cref="BeginOverlayImpl"/>, projecting candidates with WorldToScreenPoint and culling
    /// behind-camera / off-viewport / too-far markers. Markers are world-space circles drawn
    /// through RenderManager.OverlayEffect.DrawCircle; counts above 10 are drawn with a small
    /// fixed pool of UILabels (never one component per marker).
    /// </summary>
    public class CommuterDestinationOverlay :
        SimulationManagerBase<CommuterDestinationOverlay, CommuterDestinationOverlay>
    {
        // CitizenManager's spatial grid: 8 m cells, 2160 cells per side, world offset 1080.
        private const int GridSize = 2160;
        private const float GridScale = 8f;
        private const float GridOffset = 1080f;

        // How far around the stop node we look for waiting citizens (matches the grid scan the
        // stop panel already uses for its waiting-passenger counter).
        private const float ScanRadius = 64f;
        private const float ScanRadiusSqr = ScanRadius * ScanRadius;

        // Bounded line-stop walk used as a fallback for stop->line attribution (spec limit).
        private const int MaxLineWalk = 256;

        // Hard ceilings for visible markers per profile. PerformanceProfile.CommuterMaxDestinations
        // (6/12/80) was the old panel-list cap and is far too small for map markers, so the
        // overlay defines its own profile caps per the module spec.
        private const int MaxMarkersLight = 500;
        private const int MaxMarkersNormal = 1000;
        private const int MaxMarkersMaximum = 2000;

        // Refresh cadence per profile; Light updates only on click.
        private const float RefreshNormal = 5f;
        private const float RefreshMaximum = 1f;

        // Fixed label pool for the counts above 10; the largest destinations win the pool.
        private const int LabelPoolSize = 64;

        // Markers farther than this from the camera are never projected/drawn.
        private const float MaxMarkerDistance = 2000f;
        private const float MaxMarkerDistanceSqr = MaxMarkerDistance * MaxMarkerDistance;

        // Overlay circle sizes in world units.
        private const float CircleSizeSmall = 6f;
        private const float CircleSizeLarge = 11f;

        // Light profile uses a fixed blue marker instead of the line colour.
        private static readonly Color LightProfileColor = new Color32(52, 120, 246, 255);

        // Throttle repeated scan/render errors so one bad frame cannot spam the log.
        private const float RateLimitLogInterval = 10f;

        private struct MarkerData
        {
            public Vector3 Position;
            public int Count;
        }

        private static CommuterDestinationOverlay _instance;
        private static bool _active;
        private static bool _registered;

        private ushort _stopId;
        private Color _markerColor = LightProfileColor;
        private float _lastScanTime = float.NegativeInfinity;
        private float _lastRateLimitLog;

        private readonly Dictionary<ushort, int> _counts = new Dictionary<ushort, int>(256);
        private readonly List<MarkerData> _markers = new List<MarkerData>(256);

        // Snapshot published by the scan (main thread) and read by the overlay pass (main thread).
        private MarkerData[] _snapshot;
        private int _snapshotCount;
        private bool _hasSnapshot;

        private UIView _uiView;
        private UILabel[] _labels;
        private int[] _labelCounts;

        public static bool IsActive => _active;

        /// <summary>Creates the manager component and registers it with the game.</summary>
        public static void Activate()
        {
            if (_active)
            {
                return;
            }

            try
            {
                var self = _instance;
                if (self == null)
                {
                    self = new GameObject("CommuterDestinationOverlay")
                        .AddComponent<CommuterDestinationOverlay>();
                    _instance = self;
                    // If the game destroyed the previous instance on level unload, the old
                    // registration is gone with it - re-register the new one.
                    _registered = false;
                }

                // The game exposes no UnregisterManager, so register the component once per
                // session and keep it alive across toggles instead of destroy/recreate - that
                // would leak dead entries in SimulationManager's overlay list and orphan the
                // label pool. Only the labels are torn down on Deactivate.
                if (!_registered)
                {
                    SimulationManager.RegisterManager(self);
                    _registered = true;
                }

                _active = true;
                self.EnsureLabels();
                Utils.LogWarning($"CommuterDestinationOverlay: integration active (stop {self._stopId}).");
            }
            catch (Exception ex)
            {
                _active = false;
                Utils.LogError($"CommuterDestinationOverlay: activate failed: {ex.Message}");
            }
        }

        /// <summary>Clears every marker and releases the label pool. The component stays
        /// registered (inert while inactive) so toggling off/on never leaks registrations.</summary>
        public static void Deactivate()
        {
            if (!_active)
            {
                return;
            }

            try
            {
                Clear();
                if (_instance != null)
                {
                    _instance.DestroyLabels();
                }

                _active = false;
            }
            catch (Exception ex)
            {
                Utils.LogError($"CommuterDestinationOverlay: deactivate failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Selects the stop shown in the IPT stop panel and immediately publishes its destination
        /// markers. Safe to call even while the feature is off (no-op).
        /// </summary>
        public static void SelectStop(ushort stopId)
        {
            var self = _instance;
            if (self == null)
            {
                Utils.LogWarning($"CommuterDestinationOverlay: SelectStop({stopId}) ignored - overlay not active.");
                return;
            }

            self._stopId = stopId;
            self._lastScanTime = float.NegativeInfinity;
            self.Scan();
        }

        /// <summary>Removes every marker without deactivating the manager.</summary>
        public static void Clear()
        {
            var self = _instance;
            if (self == null)
            {
                return;
            }

            self._stopId = 0;
            self._hasSnapshot = false;
            self._snapshotCount = 0;
            self.HideAllLabels();
        }

        /// <summary>Periodic rescans for the Normal/Maximum refresh cadence, plus stop-switch
        /// detection for panel-internal navigation (Prev/Next, stop list, vehicle-panel link).</summary>
        private void Update()
        {
            if (!_active || _stopId == 0 || !_hasSnapshot)
            {
                return;
            }

            // If the panel moved to a different stop through its own navigation (not our click),
            // follow it immediately instead of leaving the overlay on the previous stop.
            var panel = PublicTransportStopWorldInfoPanel.instance;
            if (panel != null && panel.isVisible)
            {
                var panelInstance = ImprovedPublicTransport.Util.Utils.GetPrivate<InstanceID>(panel, "m_InstanceID");
                if (panelInstance.Type == InstanceType.NetNode && panelInstance.NetNode != 0 && panelInstance.NetNode != _stopId)
                {
                    _stopId = panelInstance.NetNode;
                    _lastScanTime = float.NegativeInfinity;
                    Scan();
                    return;
                }
            }

            float interval = GetRefreshInterval();
            if (interval <= 0f || Time.realtimeSinceStartup - _lastScanTime < interval)
            {
                return;
            }

            _lastScanTime = Time.realtimeSinceStartup;
            Scan();
        }

        protected override void BeginOverlayImpl(RenderManager.CameraInfo cameraInfo)
        {
            try
            {
                HideAllLabels();

                if (!_active || !_hasSnapshot || _snapshotCount == 0 || _stopId == 0)
                {
                    return;
                }

                // Only draw while the IPT stop panel is actually showing the selected stop, so
                // closing the panel clears the overlay without extra hooks. If the panel is on a
                // different stop (switched via its own navigation), Update() already re-selected
                // it; never draw the previous stop's markers here.
                var panel = PublicTransportStopWorldInfoPanel.instance;
                if (panel == null || !panel.isVisible)
                {
                    Clear();
                    return;
                }

                var panelInstance = ImprovedPublicTransport.Util.Utils.GetPrivate<InstanceID>(panel, "m_InstanceID");
                if (panelInstance.Type != InstanceType.NetNode || panelInstance.NetNode != _stopId)
                {
                    return;
                }

                Camera cam = Camera.main;
                if (cam == null)
                {
                    return;
                }

                var overlay = RenderManager.instance.OverlayEffect;
                if (overlay == null)
                {
                    return;
                }

                Vector3 camPos = cam.transform.position;
                MarkerData[] snapshot = _snapshot;
                int labelsShown = 0;

                for (int i = 0; i < _snapshotCount && i < snapshot.Length; i++)
                {
                    Vector3 position = snapshot[i].Position;
                    if ((position - camPos).sqrMagnitude > MaxMarkerDistanceSqr)
                    {
                        continue;
                    }

                    Vector3 screen = cam.WorldToScreenPoint(position);
                    if (screen.z < 0f)
                    {
                        continue;
                    }

                    if (screen.x < -50f || screen.x > Screen.width + 50f ||
                        screen.y < -50f || screen.y > Screen.height + 50f)
                    {
                        continue;
                    }

                    int count = snapshot[i].Count;
                    overlay.DrawCircle(cameraInfo, _markerColor, position,
                        count > 10 ? CircleSizeLarge : CircleSizeSmall, 0f, 0f, false, true);

                    if (count > 10 && labelsShown < LabelPoolSize && _labels != null)
                    {
                        SetLabel(labelsShown++, screen, count);
                    }
                }
            }
            catch (Exception ex)
            {
                RateLimitedLog("CommuterDestinationOverlay: render failed: " + ex.Message);
            }
        }

        /// <summary>Recalculates the destination snapshot for the selected stop.</summary>
        private void Scan()
        {
            if (!_active)
            {
                return;
            }

            ushort stopId = _stopId;
            if (stopId == 0)
            {
                PublishEmpty();
                return;
            }

            try
            {
                RunScan(stopId);
                _lastScanTime = Time.realtimeSinceStartup;
            }
            catch (Exception ex)
            {
                // Fail-closed: keep the last valid snapshot instead of clearing or crashing.
                RateLimitedLog("CommuterDestinationOverlay: scan failed, keeping last snapshot: " + ex.Message);
            }
        }

        private void RunScan(ushort stopId)
        {
            NetManager netManager = Singleton<NetManager>.instance;
            if (netManager == null || stopId >= netManager.m_nodes.m_buffer.Length)
            {
                PublishEmpty();
                return;
            }

            NetNode node = netManager.m_nodes.m_buffer[stopId];
            if (node.m_flags == NetNode.Flags.None)
            {
                PublishEmpty();
                return;
            }

            Vector3 stopPos = node.m_position;

            ushort lineId = GetLineForStop(stopId);
            TransportManager transportManager = Singleton<TransportManager>.instance;
            if (lineId == 0 || lineId >= transportManager.m_lines.m_buffer.Length)
            {
                PublishEmpty();
                return;
            }

            _markerColor = GetMarkerColor((Color)transportManager.m_lines.m_buffer[lineId].m_color);

            // Next stop along the line, the direction the citizen is travelling towards; used by
            // TransportArriveAtSource the same way the stop panel's waiting-passenger scan uses it.
            ushort nextStop = TransportLine.GetNextStop(stopId);
            Vector3 nextStopPos = nextStop != 0 && nextStop < netManager.m_nodes.m_buffer.Length
                ? netManager.m_nodes.m_buffer[nextStop].m_position
                : stopPos;

            CitizenManager citizenManager = Singleton<CitizenManager>.instance;
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            _counts.Clear();

            int inspected = 0;
            int maxCitizens = PerformanceProfile.CommuterMaxCitizens;

            int minX = Mathf.Max((int)((stopPos.x - ScanRadius) / GridScale + GridOffset), 0);
            int maxX = Mathf.Min((int)((stopPos.x + ScanRadius) / GridScale + GridOffset), GridSize - 1);
            int minZ = Mathf.Max((int)((stopPos.z - ScanRadius) / GridScale + GridOffset), 0);
            int maxZ = Mathf.Min((int)((stopPos.z + ScanRadius) / GridScale + GridOffset), GridSize - 1);

            for (int gz = minZ; gz <= maxZ && inspected < maxCitizens; gz++)
            {
                for (int gx = minX; gx <= maxX && inspected < maxCitizens; gx++)
                {
                    ushort instanceId = citizenManager.m_citizenGrid[gz * GridSize + gx];
                    int guard = 0;
                    while (instanceId != 0 && inspected < maxCitizens)
                    {
                        if (instanceId >= citizenManager.m_instances.m_buffer.Length)
                        {
                            break;
                        }

                        CitizenInstance instance = citizenManager.m_instances.m_buffer[instanceId];
                        ushort next = instance.m_nextGridInstance;
                        if (++guard > citizenManager.m_instances.m_buffer.Length)
                        {
                            break;
                        }

                        if ((instance.m_flags & CitizenInstance.Flags.WaitingTransport) == 0)
                        {
                            instanceId = next;
                            continue;
                        }

                        inspected++;

                        CitizenInfo info = instance.Info;
                        if (info?.m_citizenAI == null ||
                            ((Vector3)instance.m_targetPos - stopPos).sqrMagnitude > ScanRadiusSqr ||
                            !info.m_citizenAI.TransportArriveAtSource(
                                instanceId, ref instance, stopPos, nextStopPos))
                        {
                            instanceId = next;
                            continue;
                        }

                        ushort building = instance.m_targetBuilding;
                        if (building != 0 && building < buildingManager.m_buildings.m_buffer.Length)
                        {
                            Building buildingData = buildingManager.m_buildings.m_buffer[building];
                            if ((buildingData.m_flags & Building.Flags.Created) != 0)
                            {
                                _counts.TryGetValue(building, out int current);
                                _counts[building] = current + 1;
                            }
                        }

                        instanceId = next;
                    }
                }
            }

            PublishMarkers(buildingManager);
        }

        /// <summary>Builds the per-building marker list, prioritised, and publishes the snapshot.</summary>
        private void PublishMarkers(BuildingManager buildingManager)
        {
            _markers.Clear();
            foreach (KeyValuePair<ushort, int> entry in _counts)
            {
                ushort building = entry.Key;
                if (building >= buildingManager.m_buildings.m_buffer.Length)
                {
                    continue;
                }

                _markers.Add(new MarkerData
                {
                    Position = buildingManager.m_buildings.m_buffer[building].m_position,
                    Count = entry.Value
                });
            }

            int maxMarkers = GetMaxMarkers();
            // Deterministic order: busiest first, so the label pool always favours the largest
            // counts, then drop the tail beyond the profile cap.
            _markers.Sort((a, b) => b.Count.CompareTo(a.Count));
            if (_markers.Count > maxMarkers)
            {
                ClusterMarkers(maxMarkers);
            }

            EnsureSnapshotCapacity(maxMarkers);
            for (int i = 0; i < _markers.Count; i++)
            {
                _snapshot[i] = _markers[i];
            }

            _snapshotCount = _markers.Count;
            _hasSnapshot = true;
            Utils.LogWarning($"CommuterDestinationOverlay: stop {_stopId} -> {_markers.Count} destination marker(s).");
        }

        /// <summary>
        /// Merges close destinations into a single marker once the profile cap is exceeded, so
        /// dense city centres collapse into one circle with a summed count instead of hundreds of
        /// overlapping points. Keeps the busiest marker's position and adds the others' counts.
        /// </summary>
        private void ClusterMarkers(int targetCount)
        {
            const float ClusterRadius = 160f;
            const float ClusterRadiusSqr = ClusterRadius * ClusterRadius;

            var clustered = new List<MarkerData>(_markers.Count);
            foreach (MarkerData marker in _markers)
            {
                bool merged = false;
                for (int i = 0; i < clustered.Count; i++)
                {
                    var existing = clustered[i];
                    if ((existing.Position - marker.Position).sqrMagnitude <= ClusterRadiusSqr)
                    {
                        existing.Count += marker.Count;
                        clustered[i] = existing;
                        merged = true;
                        break;
                    }
                }

                if (!merged)
                {
                    clustered.Add(marker);
                }
            }

            clustered.Sort((a, b) => b.Count.CompareTo(a.Count));
            if (clustered.Count > targetCount)
            {
                clustered.RemoveRange(targetCount, clustered.Count - targetCount);
            }

            _markers.Clear();
            _markers.AddRange(clustered);
        }

        private void PublishEmpty()
        {
            _hasSnapshot = false;
            _snapshotCount = 0;
            HideAllLabels();
        }

        private void EnsureSnapshotCapacity(int capacity)
        {
            if (_snapshot == null || _snapshot.Length < capacity)
            {
                _snapshot = new MarkerData[capacity];
            }
        }

        /// <summary>
        /// Resolves the transport line for a stop node. Uses the node's own line reference, and
        /// only when that is invalid walks candidate lines' stop chains (bounded by
        /// <see cref="MaxLineWalk"/>) to find the line that actually serves the stop.
        /// </summary>
        private static ushort GetLineForStop(ushort stopId)
        {
            NetManager netManager = Singleton<NetManager>.instance;
            TransportManager transportManager = Singleton<TransportManager>.instance;
            if (netManager == null || transportManager == null ||
                stopId >= netManager.m_nodes.m_buffer.Length)
            {
                return 0;
            }

            ushort line = netManager.m_nodes.m_buffer[stopId].m_transportLine;
            if (line != 0 && line < transportManager.m_lines.m_buffer.Length)
            {
                return line;
            }

            // Fallback: walk stop chains until the stop is found, then return that line.
            int budget = MaxLineWalk;
            int lineCount = transportManager.m_lines.m_buffer.Length;
            for (int i = 0; i < lineCount && budget > 0; i++)
            {
                ushort first = transportManager.m_lines.m_buffer[i].m_stops;
                if (first == 0)
                {
                    continue;
                }

                ushort current = first;
                for (int steps = 0; steps < MaxLineWalk && budget > 0; steps++, budget--)
                {
                    if (current == stopId)
                    {
                        return (ushort)i;
                    }

                    current = TransportLine.GetNextStop(current);
                    if (current == 0 || current == first)
                    {
                        break;
                    }
                }
            }

            return 0;
        }

        /// <summary>Marker colour: fixed blue on Light, otherwise the selected line's colour.</summary>
        private static Color GetMarkerColor(Color lineColor)
        {
            return PerformanceProfile.Current == ModSetting.PerformanceProfiles.Light
                ? LightProfileColor
                : lineColor;
        }

        /// <summary>Refresh interval in seconds; 0 means click-only updates (Light).</summary>
        private static float GetRefreshInterval()
        {
            switch (PerformanceProfile.Current)
            {
                case ModSetting.PerformanceProfiles.Light:
                    return 0f;
                case ModSetting.PerformanceProfiles.Maximum:
                    return RefreshMaximum;
                default:
                    return RefreshNormal;
            }
        }

        private static int GetMaxMarkers()
        {
            switch (PerformanceProfile.Current)
            {
                case ModSetting.PerformanceProfiles.Light:
                    return MaxMarkersLight;
                case ModSetting.PerformanceProfiles.Maximum:
                    return MaxMarkersMaximum;
                default:
                    return MaxMarkersNormal;
            }
        }

        /// <summary>Creates the fixed label pool once, parented to the UIView.</summary>
        private void EnsureLabels()
        {
            if (_labels != null || UIView.GetAView() == null)
            {
                return;
            }

            _uiView = UIView.GetAView();
            _labels = new UILabel[LabelPoolSize];
            _labelCounts = new int[LabelPoolSize];
            for (int i = 0; i < LabelPoolSize; i++)
            {
                UILabel label = (UILabel)_uiView.AddUIComponent(typeof(UILabel));
                label.font = ImprovedPublicTransport.Util.UIUtils.Font;
                label.textScale = 0.9f;
                label.pivot = UIPivotPoint.MiddleCenter;
                label.color = new Color32(255, 255, 255, 255);
                label.isVisible = false;
                _labels[i] = label;
                _labelCounts[i] = -1;
            }
        }

        private void HideAllLabels()
        {
            if (_labels == null)
            {
                return;
            }

            for (int i = 0; i < _labels.Length; i++)
            {
                UILabel label = _labels[i];
                if (label != null && label.isVisible)
                {
                    label.isVisible = false;
                }
            }
        }

        /// <summary>Destroys the label pool so toggling the feature off releases the UI objects.</summary>
        private void DestroyLabels()
        {
            if (_labels == null)
            {
                return;
            }

            for (int i = 0; i < _labels.Length; i++)
            {
                if (_labels[i] != null)
                {
                    UnityEngine.Object.Destroy(_labels[i].gameObject);
                }
            }

            _labels = null;
            _labelCounts = null;
        }

        private void SetLabel(int index, Vector3 screenPoint, int count)
        {
            UILabel label = _labels[index];
            if (_labelCounts[index] != count)
            {
                _labelCounts[index] = count;
                label.text = count.ToString();
            }

            Vector2 gui = _uiView.ScreenPointToGUI(new Vector2(screenPoint.x, screenPoint.y) / _uiView.inputScale);
            Vector3 pivotOffset = label.pivot.UpperLeftToTransform(label.size, label.arbitraryPivotOffset);
            label.relativePosition = new Vector3(gui.x + pivotOffset.x, gui.y + pivotOffset.y - 18f);
            label.isVisible = true;
        }

        private void RateLimitedLog(string message)
        {
            if (Time.realtimeSinceStartup - _lastRateLimitLog < RateLimitLogInterval)
            {
                return;
            }

            _lastRateLimitLog = Time.realtimeSinceStartup;
            Utils.LogError(message);
        }
    }
}
