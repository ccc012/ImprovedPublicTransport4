from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
transport_path = root / "Integration" / "ExpressBusServices" / "TransportLineUtil.cs"
caller_path = root / "Integration" / "ExpressBusServices" / "Patch_PublicTransportExtraSkip.cs"


def failures(transport, caller):
    checks = {
        "currentStop validated before GetNextStop/index": re.search(r"if \(!TransportStopSafety\.IsInNodeBuffer\(currentStop\)\)[\s\S]*?return;[\s\S]*?TransportLine\.GetNextStop\(currentStop\)", transport),
        "nextStop validated before node index": re.search(r"if \(!TransportStopSafety\.IsInNodeBuffer\(nextStop\)\)[\s\S]*?return;[\s\S]*?m_buffer\[currentStop\][\s\S]*?m_buffer\[nextStop\]", transport),
        "instanceID validated before instance index": re.search(r"while \(instanceID != 0\)[\s\S]*?if \(instanceID >= .*?m_instances\.m_buffer\.Length\)[\s\S]*?break;[\s\S]*?m_instances\.m_buffer\[instanceID\]", transport),
        "citizenID validated before callback": re.search(r"citizenID = .*?m_citizen;[\s\S]*?citizenID != 0 && citizenID < .*?m_citizens\.m_buffer\.Length[\s\S]*?actionToDo\(citizenID\)", transport),
        "grid cycle bounded by instance buffer": "loopCounter > cm.m_instances.m_buffer.Length" in transport,
        "caller validates targetBuilding as node": re.search(r"currentApproachingStop = vehicleData\.m_targetBuilding;[\s\S]*?!TransportStopSafety\.IsLiveStopNode\(currentApproachingStop\)[\s\S]*?return true;", caller),
    }
    return [name for name, passed in checks.items() if not passed]


transport = transport_path.read_text(encoding="utf-8-sig")
caller = caller_path.read_text(encoding="utf-8-sig")
failed = failures(transport, caller)
if failed:
    raise SystemExit("Issue #21 verification failed: " + "; ".join(failed))

mutations = (
    (transport.replace("TransportStopSafety.IsInNodeBuffer(currentStop)", "true", 1), caller),
    (transport.replace("actionToDo(citizenID)", "actionToDo(instanceID)", 1), caller),
    (transport, caller.replace("TransportStopSafety.IsLiveStopNode(currentApproachingStop)", "true", 1)),
)
if any(not failures(mutated_transport, mutated_caller) for mutated_transport, mutated_caller in mutations):
    raise SystemExit("Issue #21 harness is not red-capable")

print("Issue #21 harness passed; guard/callback mutations fail.")
