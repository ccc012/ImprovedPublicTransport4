from pathlib import Path

root = Path(__file__).resolve().parents[1]
line_watcher = (root / "LineWatcher.cs").read_text(encoding="utf-8-sig")
depot_util = (root / "Util" / "DepotUtil.cs").read_text(encoding="utf-8-sig")
buildings = (root / "BuildingExtension.cs").read_text(encoding="utf-8-sig")
panel = (root / "UI" / "PanelExtenders" / "PanelExtenderLine.cs").read_text(encoding="utf-8-sig")
copy_paste = (root / "UI" / "CopyPaste.cs").read_text(encoding="utf-8-sig")
vehicle_patch = (root / "HarmonyPatches" / "TransportLinePatches" / "GetLineVehiclePatch.cs").read_text(encoding="utf-8-sig")

assert "GetStop(0) == 0" in line_watcher and "_knownLines.Remove(lineID)" in line_watcher
assert "distance == bestFallbackDistance && depotId < bestFallbackDepot" in depot_util
assert "distance == bestAvailableDistance && depotId < bestAvailableDepot" in depot_util
assert "foreach (var source in _depotMap.Values)" in buildings
assert "result.Sort();" in buildings
assert panel.count("BuildingExtension.GetDepots(info)") == 1
assert "lock (_updateDepots)" not in panel
assert panel.count("lock (_updateDepotsLock)") >= 3
handler = panel[panel.index("private void OnSelectedDepotChanged"):panel.index("private void OnDepotMarkerClicked")]
assert "DepotUtil.IsValidDepot(depotId, info)" in handler
assert "SameTransportMode(sourceInfo, info)" in copy_paste
assert "VehiclePrefabCatalog.IsCompatible(prefab, info)" in copy_paste
assert "VehiclePrefabCatalog.IsCompatible(__result, info)" in vehicle_patch

print("depot and vehicle mode regressions: PASS")
