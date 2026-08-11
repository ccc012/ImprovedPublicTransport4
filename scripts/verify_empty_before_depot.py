from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
empty = (root / "HarmonyPatches" / "VehicleAIPatches" / "EmptyBeforeDepotPatch.cs").read_text(encoding="utf-8-sig")
transport = (root / "Util" / "TransportLineUtil.cs").read_text(encoding="utf-8-sig")
depot = (root / "HarmonyPatches" / "DepotAIPatches" / "DepotCapacityEnforcePatch.cs").read_text(encoding="utf-8-sig")


def failures(empty_text, transport_text, depot_text):
    checks = {
        "pending return state is queryable only while enabled": "return ModSetting.Instance.EnableEmptyBeforeReturnToDepot && PendingReturn.Contains(vehicleID);" in empty_text,
        "RemoveVehicle preserves a blocked pending return": re.search(
            r"SetTransportLine\(vehicleID, ref vehicle, \(ushort\)0\);[\s\S]*?if \(EmptyBeforeDepotPatch\.IsReturnPending\(vehicleID\)\)[\s\S]*?return;[\s\S]*?vehicle\.m_flags \|= Vehicle\.Flags\.GoingBack",
            transport_text,
        ),
        "disabled option retains immediate fallback": "return ModSetting.Instance.EnableEmptyBeforeReturnToDepot && PendingReturn.Contains(vehicleID);" in empty_text and re.search(
            r"if \(EmptyBeforeDepotPatch\.IsReturnPending\(vehicleID\)\)[\s\S]*?return;[\s\S]*?if \(\(vehicle\.m_flags & Vehicle\.Flags\.GoingBack\) == 0[\s\S]*?vehicle\.m_flags \|= Vehicle\.Flags\.GoingBack",
            transport_text,
        ),
        "depot enforcer excludes deferred return from count": re.search(
            r"SetTransportLine\(vehicleID, ref vehicle, 0\);[\s\S]*?if \(!EmptyBeforeDepotPatch\.IsReturnPending\(vehicleID\)\)[\s\S]*?returned\+\+;",
            depot_text,
        ),
        "trailer manager and buffer validated": "vm == null || vm.m_vehicles.m_buffer == null" in empty_text,
        "trailer id bounded before indexing": re.search(
            r"trailing >= vm\.m_vehicles\.m_size \|\| trailing >= vm\.m_vehicles\.m_buffer\.Length[\s\S]*?break;[\s\S]*?ref var trail = ref vm\.m_vehicles\.m_buffer\[trailing\]",
            empty_text,
        ),
    }
    return [name for name, passed in checks.items() if not passed]


failed = failures(empty, transport, depot)
if failed:
    raise SystemExit("Empty Before Depot verification failed: " + "; ".join(failed))

mutations = (
    (
        empty.replace(
            "return ModSetting.Instance.EnableEmptyBeforeReturnToDepot && PendingReturn.Contains(vehicleID);",
            "return PendingReturn.Contains(vehicleID);",
            1,
        ),
        transport,
        depot,
    ),
    (empty, transport.replace("if (EmptyBeforeDepotPatch.IsReturnPending(vehicleID))", "if (false)", 1), depot),
    (empty, transport, depot.replace("if (!EmptyBeforeDepotPatch.IsReturnPending(vehicleID))", "if (true)", 1)),
    (empty.replace("vm == null || vm.m_vehicles.m_buffer == null", "false", 1), transport, depot),
    (empty.replace("trailing >= vm.m_vehicles.m_size || trailing >= vm.m_vehicles.m_buffer.Length", "false", 1), transport, depot),
)
if any(not failures(*mutation) for mutation in mutations):
    raise SystemExit("Empty Before Depot harness is not red-capable")

print("Empty Before Depot harness passed; pending/disabled/count mutations fail.")
