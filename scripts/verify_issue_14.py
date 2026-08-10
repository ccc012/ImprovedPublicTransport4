from pathlib import Path
import re

source = (Path(__file__).parents[1] / "UI" / "PanelExtenders" / "PanelExtenderLine.cs").read_text(encoding="utf-8-sig")
remove = source[source.index("private void OnRemoveVehicleClick"):source.index("private void OnDeleteLineClick")]
before_action, action = remove.split("SimulationManager.instance.AddAction", 1)

checks = {
    "UI selection snapshot before AddAction": "SelectedIndexes" in before_action and "SelectedVehicles" in before_action,
    "no Unity controls inside simulation action": "_vehiclesInQueueListBox" not in action and "_lineVehicleListBox" not in action,
    "transport line uses live size bound": "lineId >= transportManager.m_lines.m_size" in source,
    "vehicle uses live size bound": "vehicleID >= vehicleManager.m_vehicles.m_size" in remove,
    "vehicle must still belong to clicked line": "vehicle.m_transportLine != lineId" in remove,
    "simulation manager checked before queue": source.count("!Singleton<SimulationManager>.exists") >= 2
}

failures = [name for name, passed in checks.items() if not passed]
if failures:
    raise SystemExit("Issue #14 verification failed: " + "; ".join(failures))
print("Issue #14 source checks passed.")
