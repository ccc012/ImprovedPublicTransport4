from pathlib import Path

root = Path(__file__).resolve().parents[1]
vehicle_editor = (root / "UI" / "VehicleEditor.cs").read_text(encoding="utf-8-sig")
waiting_query = (root / "Query" / "WaitingPassengerCountQuery.cs").read_text(encoding="utf-8-sig")

assert "button1.eventClick += mouseEventHandler1" in vehicle_editor
assert "button2.eventClick += mouseEventHandler2" in vehicle_editor
assert "button1.eventMouseDown += mouseEventHandler1" not in vehicle_editor
assert "button2.eventMouseDown += mouseEventHandler2" not in vehicle_editor
apply_handler = vehicle_editor[vehicle_editor.index("private void OnApplyButtonClick"):vehicle_editor.index("private PrefabData[] GetPrefabs")]
normalization = apply_handler.index("this.OnCapacitySubmitted(uiTextField1, uiTextField1.text);")
assert normalization < apply_handler.index("int capacity = Utils.ToInt32(uiTextField1.text)")
assert normalization < apply_handler.index("int int32_1 = Utils.ToInt32(uiTextField2.text)")
assert "WaitingPassengerMaxInspect" not in waiting_query
assert "while (instanceID != 0)" in waiting_query
assert "TransportArriveAtSource(" in waiting_query
assert "private const int CacheLimit" in waiting_query
assert "Cache.Count >= CacheLimit" in waiting_query
cache_hit = waiting_query.index("if (Cache.TryGetValue(currentStop")
next_stop = waiting_query.index("nextStop = TransportLine.GetNextStop(currentStop)")
grid_scan = waiting_query.index("var instanceID = instance1.m_citizenGrid")
cache_store = waiting_query.index("Cache[currentStop] = new CacheEntry")
assert cache_hit < next_stop < grid_scan < cache_store
assert waiting_query.index("return cached.Count;") < next_stop

print("issue regressions: PASS")
