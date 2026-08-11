from pathlib import Path
import re
import time

source = (Path(__file__).resolve().parents[1] / "Integration" / "TaxiStandFix" / "TaxiStandRegistry.cs").read_text(encoding="utf-8-sig")


def select(position, stands, limit=5):
    best = None
    best_distance = 2000 * 2000
    for stand in reversed(stands):
        if not stand["live"] or stand["taxi"] is False or stand["committed"] >= limit:
            continue
        distance = (stand["x"] - position) ** 2
        if distance < best_distance:
            best_distance = distance
            best = stand
    return best


def dispatch(taxis, stands):
    assigned = []
    for position in taxis:
        stand = select(position, stands)
        if stand is not None:
            stand["committed"] += 1
            assigned.append(stand["id"])
    return assigned


def stand(identifier, x, committed=0, live=True, taxi=True):
    return {"id": identifier, "x": x, "committed": committed, "live": live, "taxi": taxi}


required = {
    "uses vanilla guest count": "standAI.CalculateGuestVehicles(" in source,
    "uses prefab capacity": "count >= standAI.m_maxVehicleCount" in source,
    "removes stale IDs": "StandBuildingIds.RemoveAt(i)" in source,
    "validates taxi stand slot": re.search(r"standAI == null \|\| building\.m_flags == Building\.Flags\.None", source) is not None,
}
failed = [name for name, passed in required.items() if not passed]
if failed:
    raise SystemExit("TaxiStandFix verification failed: " + "; ".join(failed))

full = [stand(1, 0, 5)]
assert dispatch([0], full) == [], "capacity 5 must reject sixth taxi"

multiple = [stand(1, 0), stand(2, 100)]
assigned = dispatch([0] * 6, multiple)
assert assigned == [1, 1, 1, 1, 1, 2], "multiple stands must distribute overflow"

approaching = [stand(1, 0, 5), stand(2, 100)]
assert dispatch([0], approaching) == [2], "taxis en route must count as committed"

stale = [stand(1, 0, live=False), stand(2, 100)]
assert dispatch([0], stale) == [2], "stale stand IDs must be ignored"

stands = [stand(i, i * 20) for i in range(1, 101)]
start = time.perf_counter()
for _ in range(10000):
    select(500, stands)
elapsed = time.perf_counter() - start
assert elapsed < 2.0, "10,000 selections across 100 stands exceeded 2 seconds: %.3fs" % elapsed

mutations = (
    source.replace("count >= standAI.m_maxVehicleCount", "false", 1),
    source.replace("StandBuildingIds.RemoveAt(i)", "StandBuildingIds.Clear()", 1),
)
if all("count >= standAI.m_maxVehicleCount" in mutation and "StandBuildingIds.RemoveAt(i)" in mutation for mutation in mutations):
    raise SystemExit("TaxiStandFix harness is not red-capable")

print("TaxiStandFix harness: PASS (%.3fs performance loop)" % elapsed)
