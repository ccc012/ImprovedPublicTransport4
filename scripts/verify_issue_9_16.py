from pathlib import Path

root = Path(__file__).resolve().parents[1]
vehicle_editor = (root / "UI" / "VehicleEditor.cs").read_text(encoding="utf-8-sig")
waiting_query = (root / "Query" / "WaitingPassengerCountQuery.cs").read_text(encoding="utf-8-sig")
stop_panel = (root / "UI" / "PublicTransportStopWorldInfoPanel.cs").read_text(encoding="utf-8-sig")
line_panel = (root / "UI" / "PanelExtenders" / "PanelExtenderLine.cs").read_text(encoding="utf-8-sig")
copy_paste = (root / "UI" / "CopyPaste.cs").read_text(encoding="utf-8-sig")

assert "button1.eventClick += mouseEventHandler1" in vehicle_editor
assert "button2.eventClick += mouseEventHandler2" in vehicle_editor
assert "button1.eventMouseDown += mouseEventHandler1" not in vehicle_editor
assert "button2.eventMouseDown += mouseEventHandler2" not in vehicle_editor
apply_handler = vehicle_editor[vehicle_editor.index("private void OnApplyButtonClick"):vehicle_editor.index("private PrefabData[] GetPrefabs")]
normalization = apply_handler.index("this.OnCapacitySubmitted(uiTextField1, uiTextField1.text, prefab);")
assert normalization < apply_handler.index("int capacity = Utils.ToInt32(uiTextField1.text)")
assert normalization < apply_handler.index("int int32_1 = Utils.ToInt32(uiTextField2.text)")
assert apply_handler.count("GetPrefabs()") == 1
assert "this._selectedIndex >= prefabs.Length" in apply_handler
assert "if (prefab == null)" in apply_handler
capacity_handler = vehicle_editor[vehicle_editor.index("private void OnCapacitySubmitted"):vehicle_editor.index("private void OnApplyButtonClick")]
assert capacity_handler.count("GetPrefabs()") == 1
assert "this._selectedIndex >= prefabs.Length" in capacity_handler
assert "prefab == null" in capacity_handler
default_handler = vehicle_editor[vehicle_editor.index("private void OnDefaultButtonClick"):vehicle_editor.index("private void SetTransportType")]
assert default_handler.count("GetPrefabs()") == 1
assert "this._selectedIndex >= prefabs.Length" in default_handler
assert "if (prefab == null)" in default_handler
assert "prefab.SetDefaults();" in default_handler
bindings_handler = vehicle_editor[vehicle_editor.index("private void UpdateBindings"):vehicle_editor.index("private void UpdatePosition")]
assert bindings_handler.count("GetPrefabs()") == 1
assert "this._selectedIndex >= 0 && this._selectedIndex < prefabs.Length" in bindings_handler
assert "if (prefab != null)" in bindings_handler
assert 'uiTextField1.text = "0";' in bindings_handler
assert 'uiTextField2.text = "0";' in bindings_handler
assert 'uiTextField4.text = "0";' in bindings_handler
copy_handler = copy_paste[copy_paste.index("private void ApplyCopiedSettingsToLines"):copy_paste.index("private static bool SameTransportMode")]
assert copy_handler.index("new List<ushort>(lineIDs).ToArray()") < copy_handler.index("AddAction")
assert "foreach (ushort lineID in copiedLineIDs)" in copy_handler
assert "lineID >= transportManager.m_lines.m_size" in copy_handler
assert "TransportLine.Flags.Created" in copy_handler
assert "TransportLine.Flags.Deleted" in copy_handler
assert "foreach (ushort lineID in lineIDs)" not in copy_handler
assert "WaitingPassengerMaxInspect" not in waiting_query
assert "while (instanceID != 0)" in waiting_query
assert "TransportArriveAtSource(" in waiting_query
assert "private const int CacheLimit" in waiting_query
assert "Cache.Count >= CacheLimit" in waiting_query
cache_hit = waiting_query.index("if (Cache.TryGetValue(currentStop")
next_stop = waiting_query.index("nextStop = TransportLine.GetNextStop(currentStop)")
grid_scan = waiting_query.index("var instanceID = instance1.m_citizenGrid")
cache_store = waiting_query.index("Cache[currentStop] = new CacheEntry")
assert next_stop < cache_hit < grid_scan < cache_store
assert "cached.NextStop == nextStop" in waiting_query
assert "cached.Line == line" in waiting_query
assert "currentStop >= instance2.m_nodes.m_buffer.Length" in waiting_query
assert "nextStop >= instance2.m_nodes.m_buffer.Length" in waiting_query
assert "instanceID >= instance1.m_instances.m_buffer.Length" in waiting_query
assert "TransportArriveAtSource(" in waiting_query
assert "EnteringVehicle" not in waiting_query
assert "m_targetPos" in waiting_query
assert "_cachedPassengerCount" not in stop_panel
assert "Time.deltaTime" not in stop_panel[stop_panel.index("private void UpdateBindings"):]
assert "_cachedWaitingPassengers" not in line_panel

print("issue regressions: PASS")
