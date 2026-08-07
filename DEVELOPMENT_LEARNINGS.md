# IPT4 Development Learnings

> Non-persistent scratchpad — updated as discoveries are made, errors corrected, patterns recognized.

---

## Harmony & Patching Patterns

### 1. Prefix `bool` return `false` blocks original and side-effecting later prefixes
- **Rule**: First prefix returning `false` skips original and remaining prefixes that alter input/result. Harmony still runs side-effect-free prefixes (void, no `ref` arguments), postfixes, and finalizers.
- **Impact**: If we return `false` unconditionally, we silently break other mods' patches on the same method.
- **Fix**: Only return `false` when we have a specific condition (e.g., feature enabled AND line has filter). Otherwise return `true` to let chain continue.
- Official docs: https://harmony.pardeike.net/articles/patching-prefix.html

### 2. Postfix always runs, transpiler never skipped
- Postfixes execute regardless of prefix returns.
- Transpilers rewrite IL before runtime — they're never skipped by a runtime prefix result. Harmony reruns/chains transpilers whenever another transpiler is added.
- **Strategy**: Use postfix for cleanup/observation; transpiler for IL rewrites that must always apply.

### 3. `[HarmonyAfter]` / `[HarmonyBefore]` for deterministic ordering
- Use explicit ordering when two mods patch the same method.
- Example: `[HarmonyAfter("com.vectorial1024.cities.ebs")]` ensures our prefix runs AFTER EBS's prefix.
- Official docs: https://harmony.pardeike.net/articles/priorities.html

### 4. `HarmonyScope.PatchNamespace` is NOT idempotent
- Calling `Activate()` twice without `Deactivate()` duplicates patches.
- **Always** use `_isActive` guard:
```csharp
private static bool _isActive;
public static void Activate() { if (_isActive) return; ... _isActive = true; }
```

### 5. Patch all overrides, not just base class
- Virtual method overridden in subclasses without calling base → patching base only misses them.
- **Fix**: Patch each concrete AI type (BusAI, TramAI, TrolleybusAI, etc.) individually.

### 6. `HarmonyScope.PatchNamespace` vs explicit patching
- `PatchNamespace` applies ALL `[HarmonyPatch]` in namespace — convenient but can double-patch if called twice.
- Explicit `harmonyInstance.Patch(method, prefix, postfix, transpiler)` gives precise control.

### 7. Avoid `PatchAll(assembly)` with multiple Harmony IDs
- Reapplies ALL patches in assembly under new ID → duplicates every patch from other IDs.
- **Use**: `CreateClassProcessor(typeof(MyPatchClass)).Patch()` for single-class targeting.

### 8. Transpiler failure must return incoming IL, never an empty sequence
- `yield break` emits zero instructions and can replace the target with invalid/empty IL.
- Materialize `originalCodes`, edit a copy, and return `originalCodes` if matching is incomplete.
- Preserve labels and exception blocks. NOP instructions in uncertain ranges instead of removing them.
- Stack rule: conditional branches consume their tested value. Inject alternate checks after the original branch unless both stack paths are explicitly balanced.
- Official docs: https://harmony.pardeike.net/articles/patching-transpiler.html

### 9. Private fields via Harmony magic params
- Private fields on target class are accessible via `___fieldName` magic params (e.g. `___m_LineID`).
- Cannot access via direct C# (private field access).

### 10. Reflection Invoke boxes large structs on hot paths
- `MethodInfo.Invoke(obj, new object[]{ ... })` boxes every value-type arg (e.g. 96-byte Vehicle struct).
- Replace with `AccessTools.MethodDelegate<T>` typed delegates (created once statically). **BUT** — on CS1's Mono runtime, `AccessTools.MethodDelegate` fails with "method arguments are incompatible" when the target has `ref` value-type params (Vehicle by ref). Verified in production: EBS `Patch_PublicTransportExtraSkip` type-initializer crash. **Prefer reflection Invoke over MethodDelegate on CS1 Mono** unless the delegate signature is simple (no ref structs).

### 11. GetMethod(name) is ambiguous for overloaded vanilla methods
- `PatchUtil.MethodDefinition(type, name)` without `argumentTypes` calls `type.GetMethod(name)` → throws `AmbiguousMatchException` on Mono if the vanilla method has multiple overloads.
- Confirmed in production: `NetManager.CreateSegment` has 2 overloads (with/without TreeInfo); the first `Apply()` in `NetworkChangePatch` crashed, silently disabling cache invalidation for all 4 net-change hooks.
- Fix: always pass `argumentTypes` when the vanilla method has overloads. Verified signatures: `CreateNode(out ushort, ref Randomizer, NetInfo, Vector3, uint)`, `CreateSegment(out ushort, ref Randomizer, NetInfo, [TreeInfo,] ushort, ushort, Vector3, Vector3, uint, uint, bool)`, `ReleaseNode(ushort)`, `ReleaseSegment(ushort, bool)`.

### 12. Trailer prefabs have zero passenger capacity by design
- `PrefabData.CalculateAutomaticMaintenanceCost` hit "invalid maintenance inputs" for every trailer prefab (ArticulatedBus_trailer, Tram Trailer02, etc.) because trailers legitimately carry no passengers — the lead vehicle owns capacity + maintenance.
- Logging a warning for them was noise (dozens of lines per load). Fix: only warn when `totalCapacity > 0` but other inputs are broken; trailers silently use the vanilla weekly fallback.

---

## CS1-Specific Patterns

### 1. UIComponents must use AddUIComponent, never AddComponent
- `new GameObject().AddComponent<UIPanel>()` does not attach to CS1's UI system.
- Use `view.AddUIComponent(typeof(PanelType))` (non-generic overload on UIView).
- `Start()` runs next frame; if panel is accessed immediately after creation, call setup manually.

### 2. Private fields on UIComponents
- Use Harmony's `___fieldName` magic param pattern to read private fields like `m_LineID`.

### 3. VehicleAI has no m_transportInfo
- `m_transportInfo` lives on concrete AI classes (BusAI, TaxiAI, etc.), NOT on base VehicleAI.
- No `TransportVehicleAI` type exists in the CS1 assembly.
- To match a vehicle to a depot slot, compare `vehicle.Info.m_class` (service + subService) to `slot.m_class`.

### 4. DepotAI.GetVehicleCount only counts PRIMARY slot
- Uses `CalculateOwnVehicles` which filters by `m_transportInfo.m_vehicleReason` (primary only).
- Never compare its result against the secondary cap (`m_maxVehicleCount2`).
- Must walk `m_ownVehicles` chain and filter by `vehicle.Info.m_class` to count per-slot.

### 5. CS1 net35 limitations
- No C# 7+ tuples `(uint, TransportType)` → use plain structs.
- No `Math.Clamp` → use `Math.Max(min, Math.Min(max, value))`.
- `Span<T>`, `stackalloc`, `default` expressions with type inference — limited availability.

### 6. Game auto-scan for LoadingExtensionBase
- CS1 auto-discovers classes extending `LoadingExtensionBase` in the plugin assembly.
- Order of loading is NOT guaranteed across multiple LoadingExtensionBase classes.
- Features depending on settings must either: (a) read settings lazily at first use, or (b) be explicitly activated from a single lifecycle manager (IPT's ImprovedPublicTransportMod).

---

## Performance & Profiling

### 1. Profile-aware coding
The mod has 3 performance profiles (Light/Normal/Maximum). Code should respect them:
```csharp
var profile = ModSetting.Instance.PerformanceProfile;
if (profile == ModSetting.PerformanceProfiles.Light) {
    // Minimal work, skip optional scans
} else if (profile == ModSetting.PerformanceProfiles.Maximum) {
    // Full detail, frequent updates
}
```

### 2. Throttling hot paths
- UI updates: 0.2–0.5s throttles (already in VehicleEditor, PanelExtenders)
- Per-frame allocations: Avoid `new List<>()` in hot paths; reuse arrays/pools
- LINQ in hot paths: Replace `.Any()`/`.FirstOrDefault()` with manual loops or Dictionary lookups

### 3. Cache invalidation on network edits
- SectionClassifier cache in SingleTrainTrackAI not invalidated on segment/node changes
- Fix: Hook `NetManager.ReleaseNode`/`CreateSegment` to clear caches

### 4. Double-buffered passenger counts
- `PassengerCountLimiter` uses front/back buffers over 8/16/32 frames by profile.
- Clear both buffers on load, unload, toggle, and profile mask change.
- After clear/mask change, wait for bucket zero before rebuilding; otherwise next swap publishes a partial count.

### 5. Per-frame cache patterns (EBS VehicleLineProgress)
- For progress analysis that's called per-vehicle per-frame, cache per simulation frame (not wall time).
- Use a plain struct entry with `uint FrameIndex` (no tuples on net35).
- Invalidate explicitly when line vehicles change (AddVehicle/ReleaseLine patches).

---

## Prefab Mutation & Revert Pattern

### Safe prefab mutation
```csharp
// Snapshot original values BEFORE mutating
public static void TrySnapshotPrefab(BuildingInfo info, TransportStationAI ai)
{
    if (info == null || ai == null || _snapshots.ContainsKey(info.name))
        return;
    _snapshots[info.name] = new PrefabSnapshot {
        m_class = info.m_class,
        m_transportInfo = ai.m_transportInfo,
        m_secondaryTransportInfo = ai.m_secondaryTransportInfo,
        m_transportLineInfo = ai.m_transportLineInfo,
        m_maxVehicleCount = ai.m_maxVehicleCount,
        m_maxVehicleCount2 = ai.m_maxVehicleCount2,
    };
}
```

### Revert on unload/deactivate
```csharp
public static void RevertAll()
{
    foreach (var kvp in _snapshots)
    {
        var info = PrefabCollection<BuildingInfo>.FindLoaded(kvp.Key);
        if (info?.m_buildingAI is TransportStationAI ai)
        {
            var s = kvp.Value;
            info.m_class = s.m_class;
            ai.m_transportInfo = s.m_transportInfo;
            // ... restore all fields
        }
    }
    _snapshots.Clear();
}
```
Call in `Deinit()` / `OnLevelUnloading()`.
Key by **name** not index — index lookup fails during early InitializePrefab when prefab isn't in LoadedCount yet.

---

## Translation & Encoding

### Mojibake detection
- **False positive**: Counting raw `C3 83` bytes — legitimate capitals (`Öffnet`, `Économie`, `À droite`) have `C3 83`.
- **Real detection**: `Ã` (U+00C3) **followed by** U+0080-U+00BF continuation byte — only happens in double-encoded text.

### Repair strategy
- Walk line char-by-char; collapse provable re-encoded pairs (latin-1/cp1252 → UTF-8)
- Leave unprovable chars untouched (don't skip entire line on one bad char)
- Multiple passes for triple-encoded lines

---

## Mod Compatibility Strategy

### 1. Detect and coordinate
- Check if other mod's Harmony instance already active before patching
- Example: `VehicleSelector` detection → use postfix instead of transpiler if SSE already patched

### 2. Absorb, don't conflict
- If standalone mod's functionality is fully absorbed → ban via `IptModManager`
- If partially overlapping → coordinate patches (priority, postfix, coordination)

### 3. Ban by assembly name, not just Workshop ID
- `IPT Essentials` = IPT2 revival with assembly `ImprovedPublicTransport2` → banned by assembly name

### 4. Runtime compatibility guard for feature-level disabling
- For conflicts with external mods that are NOT replaced (e.g. TMCE):
  - Detect via assembly name (`PluginHelper.IsPluginEnabled`)
  - Confirm active conflict via Harmony patch inspection (`Harmony.GetPatchInfo`)
  - Disable ONLY the conflicting IPT feature (not the whole mod)
  - Log clear warning with instructions

---

## Common Pitfalls (Do Not Repeat)

| Pitfall | Symptom | Fix |
|---------|---------|-----|
| Unconditional `return false` in prefix | Original and later side-effecting prefixes skipped | Gate `return false` behind feature flag |
| `PatchAll(assembly)` with multiple Harmony IDs | Double-patches everywhere | Use `CreateClassProcessor(typeof(MyClass)).Patch()` |
| Mutating prefabs without snapshot | Changes persist after toggle off / level unload | Snapshot before mutate; `RevertAll()` on unload |
| Cache by ID without recycle check | Stale data after demolition/despawn | Invalidate on `ReleaseNode`/`ReleaseSegment` or add TTL |
| `catch { }` empty | Silent failures | At minimum log: `Utils.LogError($"Context: {ex}")` |
| `LoadedArray` / `LoadedCount` mismatch | CS0117 / wrong API | Use `LoadedCount()` + `GetLoaded(i)` loop |
| Tuple return in public API on .NET 3.5 | CS8137 (TupleElementNamesAttribute missing) | Use `struct`/`class` instead |
| `Math.Clamp` in net35 target | CS0117 | Use `Math.Max(min, Math.Min(max, value))` |
| `SetDefaults()` used as post-JSON validation | Reset keeps current values instead of defaults | Add separate `ValidateLoadedValues()` and call after deserialize |
| `new GameObject().AddComponent<UIPanel>()` in CS1 | UI component not in hierarchy | Use `UIView.GetAView().AddUIComponent(typeof(T))` |
| `Method.Invoke` boxing large structs on hot path | Per-call heap allocation | Use `AccessTools.MethodDelegate<T>` with typed ref delegate |
| `VehicleAI.m_transportInfo` | CS0246 — field on derived classes only | Check via `vehicle.Info.m_class` for slot matching |
| Public line selector always hidden | Mods extending selector broken | Gate hide behind `CachedTransportLineData.GetBudgetControlState(lineId)` |
| C# 7 tuples in net35 assembly | CS8137 | Use plain private struct |
| `Math.Clamp` in net35 assembly | CS0117 | Use `Math.Max(min, Math.Min(max, value))` |
| Duplicate field declarations from overlapping edits | CS0102 | Check file state before editing |

---

## Performance Profile Implementation Notes

| Profile | Behavior |
|---------|----------|
| **Light** | Minimal scans, longest throttles, skip optional UI, disable heavy overlays |
| **Normal** | Balanced — default for most players |
| **Maximum** | All features, shortest throttles, all overlays active, frequent updates |

**Implementation**: Check `ModSetting.Instance.PerformanceProfile` at start of hot paths; early-return or reduce work for Light.

---

## Debugging & Logging

| Level | Use For |
|-------|---------|
| `Utils.LogError` | Exceptions, critical failures, data corruption |
| `Utils.LogWarning` | Recoverable issues, unexpected but handled |
| `Utils.Log` (VerboseRuntimeLogs) | Per-frame debugging, patch application, cache stats |
| `Diagnostics.VerboseTranspileLogs` | Harmony patch application, transpiler matches |
| `CompatibilityGuard.LogWarning` | Runtime mod conflict detection + auto-disable |

**Rule**: Never log per-frame in release without `VerboseRuntimeLogs` gate.

---

## CommuterDestination Lifecycle (Verified Correct)

1. Feature starts disabled by default (`ModSetting.cs:198`)
2. On level load, `PatchController.Activate()` marks active + registers overlay manager
3. Panel is NOT created upfront — created lazily via `PatchController.EnsurePanelReady()` on first stop click
4. Panel creation uses `UIView.GetAView().AddUIComponent(typeof(StopDestinationInfoPanel))` — NOT `new GameObject()`
5. `Show()` calls `SetupPanel()` if labels are null (lazy init for first frame before `Start()` ran)
6. Overlay checks `panel.isVisible` (panel must be open for icons to render)
7. Graph generator uses `PerformanceProfile.CommuterMaxCitizens/MaxDestinations` caps
8. `ResetRegistration()` called on `Deactivate()` — tracks instance, not bool, so 2nd city re-registers
9. Diagnostic logging (verbose): click stage, graph stage, overlay render stage

---

## Performance Profile Knobs Summary

| Knob | Light | Normal | Maximum | Used By |
|------|-------|--------|---------|---------|
| `TrainDisplayPollMultiplier` | 1.6× | 1× | 0.6× | TrainDisplayWatcher |
| `TicketPricesRefreshSeconds` | 12s | 5s | 3s | TicketPricesTab |
| `CommuterMaxCitizens` | 80 | 200 | 2000 | DestinationGraphGenerator |
| `CommuterMaxDestinations` | 6 | 12 | 80 | DestinationGraphGenerator, Overlay |
| `CommuterRefreshFrames` | 120 | 50 | 25 | StopDestinationInfoPanel |
| `WaitingPassengerMaxInspect` | 80 | 150 | 400 | WaitingPassengerCountQuery, PassengerWaitingInfo |
| `StopsAndStationsStepMask` | 0x1F (32) | 0xF (16) | 0x7 (8) | PassengerCountLimiter |
| `GetUnstuckerWaitCounterModulo(base)` | base×4 | base | max(1,base/2) | PublicTransportUnstucker |
| `LineWatcherScanSeconds` | 2s | 0.5s | 0.25s | LineWatcher |
| `ColorMonitorSeconds` | 30s | 15s | 10s | ColorMonitor |
| `TaxiStandRefreshFrames` | 2048 | 1024 | 512 | TaxiStandRegistry |
| `WaitingPassengerUiRefreshSeconds` | 3s | 1.5s | 0.5s | StopListBoxRow, PanelExtenderLine, StopInfoPanel |

---

## Harmony Owners (Complete List)

| Owner ID | Integration | Purpose |
|----------|------------|---------|
| `"com.IPT"` | IPT4 core | AlgernonCommons PatcherManager |
| `"com.IPT.FlightTracker"` | FlightTracker | |
| `"github.com/bloodypenguin/Skylines-IntercityBusController"` | IntercityBusControl | |
| `"ipt3.advancedstopselection.mod"` | AdvancedStopSelection | |
| `"IPT3.BetterBoarding"` | BetterBoarding | |
| `"com.vectorial1024.cities.ebs"` | ExpressBusServices | |
| `"com.vectorial1024.cities.ptu"` | PublicTransportUnstucker | |
| `"com.ipt3.mileagetaxi"` | MileageTaxiServices | |
| `"llunak.BetterBusStopPosition"` | BetterBusStopPosition | |
| `"egi.citiesskylinesmods.realisticwalkingspeed"` | RealisticWalkingSpeed | |
| `"IPT4.OptimisedOutsideConnections"` | OptimisedOutsideConnections | |
| `"IPT4.SharedStopEnabler"` | SharedStopEnabler | |
| `"IPT4.SingleTrainTrackAI"` | SingleTrainTrackAI | |
| `"IPT4.StopStacker"` | StopStacker | |
| `"IPT4.SubBuildingsTabs"` | SubBuildingsTabs | |
| `"IPT4.TaxiStandFix"` | TaxiStandFix | |
| `"IPT4.UnlimitedOutsideConnections"` | UnlimitedOutsideConnections | |

## Harmony Ordering Constraints

| Method | Constraint | Why |
|--------|-----------|-----|
| `BusAI.LoadPassengers` (7 AIs) | BetterBoarding `[HarmonyAfter]` EBS | BB must see EBS skip-flag first |
| `BusAI.CalculateSegmentPosition` (2 AIs) | StopStacker `[HarmonyAfter("BetterBusStopPosition")]` | StopStacker reads BBSP result |
| `TransportTool.GetStopPosition` | AdvancedStopSelection `[after]` SharedStopEnabler | Advanced reads SSE's patched IL |

---

## Things to Investigate Next

- [ ] `PrefabData` hardcoded Workshop asset names (London 1992 Stock / Solaris Urbino) in constructor
- [ ] `StopStacker` locked off in UI but code still maintained
- [ ] `TrainDisplayUpdated/TrainDisplayIntegration.cs:1067` dead method `ResolveNextStopName`
- [ ] `mp10_Commuter Train Loco` / `mp10_High Speed Train Engine`: `unitCapacity=0` despite `totalCapacity=400/300` — GetCapacity returns 0 for train locomotives with trailers; currently falls back to vanilla maintenance (correct-ish, but investigate if capacity scale is being under-applied)
- [ ] AutoLineColor: `ColorSetProvider` only covers partial types, TODO for more
- [ ] AutoLineColor `RoadNamingStrategy` TODOs for unique names
- [ ] `Settings/SettingsActions.cs:1017` TODO for outside connection line deletion safety
- [ ] `VehicleUtil.cs:35` TODO for fish boats
- [ ] BetterBoarding passenger counting: `PassengerChoice[maxRank, paxCount]` matrix — bounded but allocates per boarding
- [x] TMCE compat guard verified — added `[HarmonyPriority(Priority.First)]` to IntercityBusControl CreateVehicle prefixes (4.9)
- [ ] Test CommuterDestination: verify panel opens on stop click, icons appear, overlay resets on 2nd city

---

## 4.9 Compatibility Toggles

Pattern for giving every IPT4 feature an off-switch so standalone mods can coexist:

1. Add `Enable<Feature>` bool to ModSetting (default **true** = current behaviour).
2. Gate the Harmony prefix/transpiler body with `if (!ModSetting.Instance.Enable<Feature>) return true;` — returns to vanilla, other mods' patches still run.
3. Add UI checkbox under Options → Advanced → "Compatibility toggles" with `NotifyReloadRequired`.
4. Add EN/PT translations + English placeholder in all 37 language files.

Implemented in 4.9:
- `EnableBusDepotLevelCrossCompat` → gates `ClassMatchesPatch` + `GetDepotLevelsPatch`
- `EnableVehicleSelectionOverride` → gates `GetLineVehiclePatch`
- `EnableDepotStatsDisplay` → gates `DepotStatsDisplayPatch`
- `EnableUnbunching` → gates `CanLeaveStopPatch` (global master; existing per-node `Unbunching` is separate save state)
- TMCE/aeroporto fix: `[HarmonyPriority(Priority.First)]` on `Patch_CreateIncomingVehicle`/`Patch_CreateOutgoingVehicle`

Key insight: **banning is not the same as conflict**. All 17 banned mods are replaced-wholesale by IPT4 (absorbed feature) or collide with unconditional core patches (`SimulationStep`, `StartTransfer`, `LoadPassengers`) that can't be toggled without breaking the mod. The `CompatibilityGuard` disables a specific feature (downgrade), never un-bans. Don't promise un-banning for absorbed mods.

---

## Commands Reference

```powershell
# Build with deploy
dotnet build -p:AutoDeploy=true -c Release --no-incremental

# Decompile for inspection
ilspycmd -p -o <out_dir> <assembly.dll>

# Find Harmony patches in codebase
Select-String -Path <cs_files> -Pattern "HarmonyPatch|PatchUtil\.Patch"

# Find all decompiled patches
Select-String -Path <decompile_dir> -Pattern "HarmonyPatch|Patch\(|AddPrefix|AddPostfix|AddTranspiler"

# Check deployed DLL timestamp
Get-Item "$env:LOCALAPPDATA\Colossal Order\Cities_Skylines\Addons\Mods\ImprovedPublicTransport4\ImprovedPublicTransport4.dll" | Select-Object Length,LastWriteTime
```
