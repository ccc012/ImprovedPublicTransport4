# Changelog — Improved Public Transport 4

IPT4 is a local fork of [Improved Public Transport 3](https://github.com/TheMadisonian/ImprovedPublicTransport3)
that absorbs other public-transport mods into a single assembly, so they cannot
fight each other over shared game state.

Entries below are written for developers: they name the affected class, the root
cause, and why the failure was not obvious. The player-facing summary lives on the
[Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3773802930).

Versioning is `major.module.build`:
`major` = mod generation (4 = IPT4), `module` = incremented when a new mod or
integration is absorbed, `build` = build/test iteration within that module.

---

## [4.3.6] Public transport maintenance overflow

### Fixed - negative `-21.4 million` maintenance

`PrefabData.MaintenanceCost` divided the total vehicle capacity by the detected
capacity of the leading vehicle. Unsupported custom vehicle AIs returned zero,
so the calculation produced `NaN`; `Mathf.RoundToInt(NaN)` became an
`int.MinValue`-sized maintenance cost. `SimulationStepPatch.Postfix` then summed
all vehicle costs in another `int` and passed the negative result to
`EconomyManager.FetchResource`, recording a negative expense near the signed
32-bit limit.

Invalid prefab inputs now fall back to the vanilla weekly cost. Per-line totals
use `long`, negative per-vehicle values are rejected, and large valid totals are
split into bounded positive economy transactions.

### Fixed - already-corrupted saves

On level load, IPT now clears only negative public-transport income and expense
periods and adjusts their matching aggregate totals. Other services and all
non-negative history are left unchanged.

---

## [4.3.5] Train Display detection, ticket price display

### Fixed — Train Display overlay never rendered

`TrainDisplayIntegration.EnsureCached` probed the camera controller for
`target` / `m_target` properties and a `GetTarget()` method. None of those exist:
reflecting over `Assembly-CSharp.dll` shows the game stores the followed instance
in a **private** `CameraController.m_targetInstance` field. No probe ever matched,
so `TryGetFollowTarget` always failed and the overlay had no vehicle to describe.

Now resolves `m_targetInstance` by name, walking `Type.BaseType` so camera mods
that subclass `CameraController` still resolve. The old generic probes are kept as
a fallback.

### Changed — `TrainDisplayFirstPersonOnly` now defaults to `false`

`IsFirstPersonCameraActive` returns false for vanilla follow mode (it looks for
`"first"` in the controller type name or camera mode). With the setting defaulting
to `true`, the overlay was suppressed in exactly the mode most players use, which
read as "the feature is broken". The original Train Display mod showed its overlay
in any follow view.

### Fixed — Ticket Prices tab showed passenger count, not fare

`UpdateTotalLabel` called `CalculateCurrentPassengerLoad`. Since the sliders only
render a percentage, there was no way to see the resulting fare — and on a small
or paused city the column read a permanent `0`.

Added `PriceCustomization.TryGetCurrentPrice(type, out current, out base)`, which
reads `TransportInfo.m_ticketPrice` and the recorded base price. The label now
shows the fare, tinted amber when it differs from the type's original value, and
returns `-` when no prefab of that type is loaded (missing DLC). Passenger count
moved into the tooltip via `TICKET_PRICE_LABEL_TOOLTIP`.

### Fixed — corrupted translation key

`TICKET_PRICE_LABEL_TOOLTIP` was initially written with real newlines instead of
the literal `\n` the parser expects. `PlainTextLanguageDeserializer` reads one
key per line, so the value split across three lines and the two trailing
fragments were parsed as bogus keys — in all 19 files.

---

## [4.3.4] Startup ModException from AutoLineColor

### Fixed — `Failed to find ImprovedPublicTransportMod assembly!`

Thrown from `Util.Utils.PluginInfo` via `AutoLineColor.Naming.GenericNames.Initialize`
during `ColorMonitor.OnCreated`, before the map finished loading. The property
scanned plugins for an `IUserMod` instance of type `ImprovedPublicTransportMod` —
but that class stopped implementing `IUserMod` when `Mod` took over in the
CSLModsCommon migration (4.2.x). Automatic line naming/coloring was dead for the
whole session.

This was the **third** failure from the same coupling: it had already broken
translation loading (4.3.1) and the ticket-price icon atlases, each time after an
unrelated refactor. `Util/Utils.cs` and `TranslationFramework/Util.cs` now resolve
through `PluginManager.FindPluginInfo(Assembly.GetExecutingAssembly())`, which
cannot drift when the `IUserMod` class changes. The type scan remains as a
fallback and accepts either class.

---

## [4.3.3] Deterministic Json.NET binding, locale coverage, hot-path fix

### Fixed — Newtonsoft.Json version conflict with other mods

4.3.2 bundled `Newtonsoft.Json.dll`, but bundling alone was not enough.
**Real Time** (Workshop `3059406297`) ships a strong-named **13.0.0.0**, while
CSLModsCommon requires the unsigned **9.0.0.0** we ship. Cities: Skylines loads
every enabled mod into one `AppDomain` with no per-mod isolation, so load order
decided which copy our code bound to.

When the wrong build won, `CSLModsCommon.Utilities.JsonHelper` threw
`TypeInitializationException` on `JsonWriter` — and its `catch` swallowed the
exception and returned `new T()`. One silent failure produced three symptoms that
looked unrelated:

| Symptom | Mechanism |
|---|---|
| Settings never persisted | `SerializeToJsonFile` failed, logged, discarded |
| Framework UI untranslated | `DeserializeFromJsonFile` returned an empty dictionary; log showed `Added common locale source:` with no locales |
| Language selector showed raw ids (`de-DE`) | `GetLanguageOptions` fell back to the locale id because `Language_*` keys were missing from the empty sources |

`JsonNetBootstrap` (added 4.3.2) was hardened: `LoadBundledAssembly` no longer
returns the first already-loaded assembly matching the simple name. It resolves
the file next to our own DLL, reuses that exact instance if already loaded
(compared by `Assembly.Location`), and only falls back to a foreign copy if ours
is missing. Hooked from `Mod`, `IptModManager` and `OnCreateSettings`.

### Added — bn/hi/id/ur reachable in the language selector

Bengali, Hindi, Indonesian and Urdu were already fully translated in
`Translations/*.txt`, but unreachable: `GetLanguageOptions` enumerates
`LocaleSources`, which is built from the **framework's**
`Localization/Common/*.json`, and no file existed for them. Since the game itself
never reports those locale ids, nothing could select them.

Added `bn-BD.json`, `hi-IN.json`, `id-ID.json`, `ur-PK.json` (93 framework strings
each; `LoadAllSources` back-fills anything missing from `en-US`), registered their
display names across all 23 locale files, and added entries to
`TranslationStatus.json`. Selector entries backed by a full mod translation:
**14 → 18 of 23**.

`TranslationFramework.LocalizationManager.FindLanguage` maps long ids to our short
filenames (`bn-BD` → `bn.txt`) by trying the lowercased id, then the prefix before
`-`. The five remaining selector entries (cs, nl, sk, th, tr) resolve to English.

### Fixed — 49 keys missing outside en/pt

Train Display, integration toggles and AutoLineColor refresh keys existed only in
`en.txt`/`pt.txt`. UI strings were translated into the other 17 languages;
`CHANGELOG_*` keys were intentionally left in English.

### Performance — settings lookup hoisted out of a per-citizen loop

`StopsAndStations.PassengerCountLimiter.OnBeforeSimulationFrame` iterates
`StepSize` (`CitizenManager.MAX_INSTANCE_COUNT / 16` = 4096) instances per frame
and called `GetMaximumAllowedPassengers` per match, which read `Settings` →
`ModSetting.Instance` → two dictionary lookups (`Domain._managerLookup`,
`SettingManager._settings`) plus casts. Resolved once per frame and passed as a
parameter.

### Performance — empty catch in a per-frame path

`AutoLineColor.ColorMonitor.OnUpdate` wrapped `TicketPricesTab.OnUpdate` in
`try { } catch { }`, before its own throttle. A recurring failure would be hidden
forever while still paying exception cost ~60×/second. Now logs once and sets a
flag to stop calling it.

### Fixed — mod folder lookup in `TranslationFramework.Util`

Same `IUserMod` coupling described under 4.3.4; fixed here first, for the
translation path.

---

## [4.3.2] Newtonsoft.Json not deployed

### Fixed — mod failed to load with no other mods enabled

`Error: failed to load the mod's dll (or one of its dependencies)`.
`output_log.txt` showed
`Assembly resolution failure. No assembly named 'Newtonsoft.Json, Version=9.0.0.0'`
followed by `TypeLoadException` on `CSLModsCommon.Utilities.JsonHelper` and
`CSLModsCommon.Serialization.SerializationContext`. A `TypeLoadException` while
constructing the `IUserMod` instance takes down the entire mod, not just the
feature that needed it.

`DeployToModDirectory` never copied `Newtonsoft.Json.dll` into the mod folder,
even though CSLModsCommon's settings serialization requires it. It had only ever
worked because another subscribed mod happened to load that assembly first —
found by disabling every other mod for a clean test.

Added the copy step; also added `System.Runtime.Serialization.dll`. See
`Projeto/IPT4/07_AUDITORIA_DEPENDENCIAS.md` for the full dependency audit and the
rule for adding future `PackageReference`s.

---

## [4.3.1] Mod language selector had no effect on mod strings

### Fixed — selector only re-translated framework strings

`TranslationFramework.LocalizationManager.SetCurrentLanguage` matched
`LocaleManager.instance.language` — the **game's** language — and ignored
`ModSettingBase.LocaleId`, the value the Options panel selector writes. Picking a
language re-translated CSLModsCommon's own strings (Version, Changelog, BETA)
while every IPT4 string stayed in the game's language: about half the panel
appeared not to translate.

Resolution order is now mod selection → game language → English, with
`FindLanguage` handling both naming schemes. Subscribed to
`CSLModsCommon.Manager.LocalizationManager.ModActiveLocaleChanged` so the change
applies immediately; `OnLanguageSelectedChanged` fires that event before the panel
is rebuilt, so the order is correct.

### Fixed — vanilla `Locale.Get` "not found" marker treated as a translation

Colossal's `Locale.Get` returns `"{id}:0"` for unknown identifiers rather than
throwing. `Localization.Get` compared only against `translationId`, so `"KEY:0"`
looked like a hit and was rendered literally (`SETTINGS_SPEED:0`).

### Translation — filled 38 keys present only in en/pt

Whole Train Display section, Delete Lines description, three Express Bus
descriptions — every other language silently fell back to English.

### Translation — added 4 copy/paste tooltip keys missing from every file

`COPY_TIP`, `PASTE_TIP`, `COPY_BUILDING_TIP`, `COPY_DISTRICT_TIP` were absent even
from `en.txt`, so they would have rendered as raw keys. Currently unreachable —
those buttons are `Hide()`-ed with a `//TODO: restore`.

---

## [4.3.0] Train Display integration

### Added — `Integration/TrainDisplayUpdated/`

On-screen overlay showing line, destination and state while following a supported
vehicle. Configurable: screen position, scale, opacity, which fields are shown,
update interval, first-person-only restriction, and a Simple/Dark/Light theme.

### Fixed — build errors in the new integration

- Ambiguous `Utils` between `ImprovedPublicTransport.Util.Utils` and
  `ColossalFramework.Utils`; resolved with a `using` alias.
- `NetInfo.nameLocalized` and `ItemClass.Service.IntercityBus` do not exist on
  this game version.
- `Enum.HasFlag` does not exist on .NET Framework 3.5 (added in 4.0), the target
  framework here. Replaced with bitwise `(value & flag) != 0` in 6 places.

---

## [4.2.4] Options panel correctness pass

### Fixed — `Compatibility with Game Version: 0.0.0`

No `AddVersionModRule` override, so `VersionModRule` held a default
`GameVersionCompatibility`. Declared 1.21.1-f9 as the lower bound with a generous
upper bound.

### Fixed — AutoLineColor strategy dropdown showed raw enum names

The code built `"AUTOLINECOLOR_STRATEGY_" + e.ToString().ToUpperInvariant()`
(`..._RANDOMHUE`) while the translation files use `..._RANDOM_HUE`. Replaced the
string concatenation with an explicit key mapping.

### Fixed — checkbox descriptions never rendered

`SettingsCardBase.ArrangeRow` only positioned `DescriptionElement` inside the
`HeaderElement is not null` branch. Every checkbox in this mod passes
`header: null`, so descriptions were created, populated, and never laid out.
Header and description are now positioned independently.

### Fixed — Options tab captions clipped

`TabButton` had no word wrap and `TabHeight` was 30. Enabled `WordWrap`,
middle-aligned the text, raised `TabHeight` to 44 for two lines. Affected longer
captions generally, Portuguese most visibly.

### Fixed — Reset buttons appeared to do nothing

`OnResetButtonClick` / `ResetTicketPriceSettings` mutate `ModSetting` directly,
but controls already on screen hold their own copies. Added
`OptionsPanelManager.Refresh()` (extracted from `OnLocaleChanged`) and call it
after each reset.

### Fixed — Delete Lines selection persisted as a setting

The ten checkboxes were bound to `ModSetting` properties, so a one-shot
"pick types, press Delete" tool remembered its selection across sessions. Moved to
a transient static `DeleteLinesSelection`, cleared after the delete runs.

### Changed — tram unbunching mode names

`Light Rail` / `True Tram` → `Prudent` / `Realistic`, matching the bus modes.
Labels only; `ExpressTramServicesModes` values unchanged.

---

## [4.2.3] Translations loaded for no language

### Fixed — every mod string rendered as its raw key

`Localization.cs` passed `typeof(ImprovedPublicTransportMod)` to
`LocalizationManager`, which locates the mod folder by matching that type against
each plugin's `IUserMod` instance. After the CSLModsCommon migration the
`IUserMod` is `Mod`, so the match never succeeded, the folder was never found, and
**no** `Translations/*.txt` file loaded in any language.

### Removed — always-empty Key Bindings tab

`OptionsPanelBase.AddExtraPage` adds it unconditionally; this mod has no key
bindings. Stopped calling `base.AddExtraPage()`.

### Added — slider values visible again

The CSLModsCommon slider control does not render its value, unlike the old
attribute-driven panel. Added `AddSliderWithValue`, which appends the live value
to the card header (trimming a trailing `:` to avoid `Header:: 42`).

### Fixed — AutoLineColor sliders active for strategies that ignore them

`Disabled` and `CategorisedColor` route through `ColorSelector.LeastUsed`, which
never reads the difference threshold or attempt count. Those two sliders are now
disabled for those strategies.

---

## [4.2.0] Rescue Fullwidth Digits, Ticket Prices tab, taxi fare overflow

### Added — `HarmonyPatches/TransportManagerPatches/NormalizeFullwidthLineNamesPatch`

Normalizes U+FF10–U+FF19 fullwidth digits in custom line names to ASCII, via a
postfix on `TransportManager.UpdateData`. Ported from Rescue Fullwidth Digits
(Workshop `1174585364`).

### Fixed — that patch was applied once per session but undone per level

`Apply()` ran in `OnCreated` (once per game launch) while `Undo()` ran in
`Deinit()` (every level unload), so it silently stopped working for every save
after the first. Moved `Apply()` into `OnLevelLoaded` alongside the other patches.

### Fixed — taxi fare integer overflow draining the budget

`MileageTaxiServices.Patch_TaxiAI_SimulationStep.DetermineDelta` computes the
distance between two vehicle frames with no sanity bound. A taxi with corrupted or
uninitialized frame data (e.g. just after spawning) yields an implausibly large
distance; `(int)standardInstantFare` then overflows into a large negative value
and `EconomyManager.AddResource` drains the public transport budget.

This matches the unresolved
[upstream report](https://github.com/Vectorial1024/MileageTaxiServices/issues/1)
of runaway negative taxi income, and is the same failure family as the budget bug
that started this project. Frame distances above `MaxPlausibleFrameDistance`
(200 units) are now discarded, along with `NaN` and negatives.

---

## [4.1.x] CSLModsCommon Options UI migration

Fixes found while getting the migrated Options panel working. Full narrative in
`Projeto/IPT4/06_ESTADO_ATUAL.md`.

- **4.1.5** — Options panel rendered black. `LocalizationManager.LoadAllSources`
  bails out when `Localization/Common` is missing, leaving `CurrentLocaleSource`
  null; `GetTranslationProgress()` then threw inside `OptionsPanelBase.Awake`,
  and Unity swallowed the exception mid-construction. The `.csproj` never deployed
  that folder. Added the copy step and a null-guard.
- **4.1.4** — Two earlier causes of the black panel: `RootNamespace` was still
  `ImprovedPublicTransport3`, so all 34 embedded UI textures were compiled under
  names `AtlasLoader` never requested; and `OptionsPanelManager.SettingsUI` only
  created the panel on a visibility *change*, though the container is already
  visible when the game opens the mod's page.
- **4.1.3** — Three call sites wrote to `ModSetting.Instance` then called
  `OptionsWrapper<Settings.Settings>.SaveOptions()`, persisting the dead
  pre-migration object: What's New dismissal, the Options Reset button, and the
  per-hour ticket price editor. Removed the dead `OptionsFramework/` folder (14
  files), `Settings/Settings.cs` and `Settings/VehicleEditorPositions.cs`, plus
  the orphaned `using` in 23 files.

---

## [4.0.0] Fork from IPT3

- Renamed assembly to `ImprovedPublicTransport4`; declared IPT3, Transport Lines
  Manager and standalone AutoLineBudget as incompatible via
  `AddIncompatibleModRule`.
- Added `Integration/AutoLineBudget/`, porting AutoLineBudget 21 (GPL-3.0)
  demand-based fleet sizing. **Critically**, it drives
  `CachedTransportLineData.SetTargetVehicleCount` /
  `SetBudgetControlState` instead of writing `TransportLine.m_budget` directly —
  the race between that direct write and IPT's own vehicle-count logic is the
  runaway maintenance-cost bug that motivated this fork. A `HashSet<ushort>`
  tracks lines it took over, so lines the player set to Manual are never hijacked.
- Adopted CSLModsCommon for the Options UI (version badge, changelog,
  compatibility warnings, translation progress).

---

# Legacy: Improved Public Transport 3

Kept for provenance — these entries predate the fork.

## [3.0.1] Public Transport Unstucker integration

- Integrated Public Transport Unstucker support directly into IPT3.
- Added on/off toggle in the Unbunching settings tab.
- Added conflict detection logging for key economy/transport patches:
  - `EconomyPanel.Awake` patch checks for other Harmony owners before applying.
  - `TransportLine.SimulationStep` patch checks for existing prefix/postfix/transpilers before applying.
  - Logs warnings with other patch owners to help diagnose IPT3 budget/income anomalies in mod combinations.
- Added targeted debug logging to help track budget ordering issues:
  - `CanLeaveStopPatch` logs line, wait time, and chosen result.
  - `SimulationStepPatch` logs maintenance cost applied a line and active vehicle count.
  - `TicketPriceCustomizer` logs each ticket price adjustment and the number of modified lines.

## [3.0.0] UI, Performance, and Safety enhancements from previous IPT2 version

### UI Enhancements

- **Depot names now show user-assigned names first.** If a depot building has a custom name set in-game, that name is displayed. If not, the prefab name is used, qualified with the district name in parentheses when the depot is inside a district (e.g. `Bus Depot (Downtown)`). Falls back to the raw prefab `Info.name` if neither is available.
- **Label "Depot:" narrowed** from 97 px to 60 px and gap reduced from 6 px to 2 px so the dropdown has more room.
- **Dropdown widened** from 167 px to 241 px, making longer depot names fully visible.
- **District names in the dropdown now refresh live.** Added a lightweight hash of the district names of visible depots; the dropdown is repopulated whenever the hash changes (e.g. after renaming a district in-game), with no per-frame cost when nothing has changed.

- **Intercity Buses:** now selectable and editable in Vehicle Editor
- **Line Info Panel:** 'Number of vehicles:' shortened to 'Vehicle count:' so it doesn't overlap with 'Refresh Line Color/Name' button.


### Safety — Null-reference guards

- **`ActiveVehiclesQuery`**: Added `if (info == null) continue;` guard before accessing `VehicleInfo.m_class`, preventing a `NullReferenceException` if a vehicle slot holds a prefab that has since been unloaded.
- **`DepotAI.StartTransfer` redirection guard (IPT + More Vehicles compatibility)**: In `StartTransferPatch.StartTransferPre`, validate redirected depot before calling `StartTransfer` and abort with warning if depot is invalid. This prevents infinite redirection loops when using vehicle expansion mods such as More Vehicles Renewed.
- **`WaitingPassengerCountQuery`**: Added `citizenInstance.Info != null` guard before calling `TransportArriveAtSource`, preventing a crash when a citizen instance references an unloaded `CitizenInfo` prefab. Also cached `ref var citizenInstance` to eliminate five repeated buffer dereferences per loop iteration.
- **`PanelExtenderVehicle`**: Added null check on `TransportLine.Info` before accessing `Info.m_class` in `UpdateBindings`, preventing a crash when a line has no assigned prefab.
- **`PanelExtenderVehicle`**: Rewrote vehicle ID resolution in `UpdateBindings` to use `GetFirstVehicle()` so passenger exchange stats are always read from leading vehicle (fixes 0|0 passenger exchange on trailers). 
- **`PanelExtenderLine`**: Added null checks in `GetDepotDistrictNamesHash` (verifying depot array is not null before iteration) and `IDToName` (validating building IDs), fixing repeated NullReferenceException spam in error logs.

### Safety — BetterBoarding integration crash fixes

- Added bounds checks in BetterBoarding `LoadPassengers` prefixes for all vehicle types (bus, trolleybus, tram, train, ferry, helicopter, blimp): verify `CachedVehicleData.m_cachedVehicleData != null && vehicleID < ...Length` before `BoardPassengers`.
- Added node guard in BetterBoarding `LoadPassengers` prefixes: verify `CachedNodeData.m_cachedNodeData != null && currentStop < ...Length` before `PassengersIn +=`.
- **`BoardingUtility.ProcessRankedChoices` critical guards**: Added bounds checks for `chosenVehicleID` (verify `> 0 && < vehicleBuffer.Length`) and `citizenID` (verify `!= 0`) before buffer access. Added null checks for `citizenInfo` and `citizenInfo.m_citizenAI` before calling instance methods.


### Performance — Dictionary lookup (O(1)) replacing linear search (O(n))

- **`VehiclePrefabs`**: Added a `Dictionary<int, PrefabData> _prefabDataByIndex` field, populated in `RegisterPrefab`. New public `FindByIndex(int prefabDataIndex)` method does an O(1) lookup by `m_prefabDataIndex`.
- **`SimulationStepPatch`**: Replaced `Array.Find(prefabs, item => item.PrefabDataIndex == ...)` (O(n) linear scan executed once per vehicle per simulation tick) with `VehiclePrefabs.instance.FindByIndex(vInfo.m_prefabDataIndex)` (O(1) dictionary lookup). Also added a null guard on `vInfo` before the lookup.
- **`QueuedVehicleQuery`**: Replaced the O(n×m) nested loop (iterate all queued vehicles × iterate all known prefabs) with a single-pass approach: build a `Dictionary<string, PrefabData>` from the prefab list once, then do O(1) `TryGetValue` per queued vehicle. Also added an early-return when the queue is empty.

### Performance — Allocation reduction

- **`CachedTransportLineData.GetRandomPrefab`**: Removed a `HashSet.ToArray()` allocation that previously occurred on every vehicle spawn. The method now counts `prefabs.Count`, picks a random index, and iterates the `HashSet` with an index counter to retrieve the nth element — no intermediate array is created.
- **`VehiclePrefabs.GetPrefabsNoLogging` (all-levels overload)**: Replaced a four-stage `.Concat().Concat().Concat().ToArray()` LINQ chain (three intermediate arrays) with a single pre-allocated array filled via `CopyTo` — one array, zero intermediate allocations.
- **`VehiclePrefabs.FindByName`**: Changed from direct dictionary indexer (throws `KeyNotFoundException` on missing key) to `TryGetValue`, preventing a crash and eliminating the associated exception overhead.
- **`BoardingUtility`**: Replaced `freeVehiclesList.OrderBy(item => Vector3.Distance(...))` LINQ sort (allocates `IEnumerable` + enumerator per passenger) with a stack-allocated copy into a `VehicleOccupancyInfo[]` sort buffer followed by `System.Array.Sort` with a comparison delegate. Also changed `Vector3.Distance` (computes a square root) to `Vector3.SqrMagnitude` (no square root) since only relative ordering matters.

### Performance — Critical: Spatial grid replaces O(32,768) linear scan

- **`PublicTransportStopWorldInfoPanel.ProcessNodes`**: Replaced a full linear scan of all 32,768 net nodes with NetManager's built-in spatial node grid. The grid divides the world into 270×270 64-unit cells; the fix searches only the 3×3 cells surrounding the stop position, reducing the worst-case from 32,768 iterations to typically <50. This eliminates the main source of lag when renaming a stop or clicking "ungroup nearby stops". Also added a `?.` null-guard on `netNode1.Info` to prevent a rare NullReferenceException.

### Performance — Throttling: reduce per-frame citizen grid scan

- **`PublicTransportStopWorldInfoPanel.UpdateBindings`**: `WaitingPassengerCountQuery.Query()` was called every `LateUpdate` frame while the stop panel was open. The result is now cached and only re-queried at most every 0.5 seconds, eliminating redundant citizen grid scans during busy rush-hour periods.

### Performance — Per-frame Singleton and buffer caching

- **`PanelExtenderLine.UpdateBindings`**: Cached `Singleton<TransportManager>.instance` into a local `tm` variable, eliminating 4 repeated singleton lookups per frame while the line panel is open.
- **`PanelExtenderVehicle.UpdateBindings`**: Added `ref var vehicle = ref vm.m_vehicles.m_buffer[(int)vehicleID]` to cache the vehicle buffer slot by reference, eliminating 5+ repeated struct dereferences per frame while the vehicle panel is open. Also replaced `Array.Find(VehiclePrefabs.instance.GetPrefabs(...), lambda)` with `VehiclePrefabs.instance.FindByIndex(vehicle.Info.m_prefabDataIndex)` (O(1) dictionary lookup), and added a null guard when no prefab is found rather than crashing.

### Safety — Additional null guards

- **`SelectVehicleTypesCommand.Execute`**: Added `.Where(v => v.Info != null)` filter before `.Select(v => v.Info.name)` to prevent a NullReferenceException if a selected prefab item has a null Info reference.
- **`PanelExtenderVehicle.UpdateBindings`**: Added `if (vehicleID == 0) return;` and `if (vehicle.Info == null) return;` guards before accessing vehicle buffer data, preventing crashes when the panel is accessed with no vehicle selected or with an unloaded prefab.

### Performance — LINQ allocation removal in data classes

- **`PrefabData.TotalCapacity`**: Replaced `_trailerData.Select((t, index) => _trailerData[index].Capacity).Sum()` (allocates an `IEnumerable<int>` and enumerator per call) with a plain `for` loop — zero allocations.
- **`PrefabData.CarCount`**: Replaced `_trailerData.Count(t => t.Info.GetSubService() == ...)` (allocates a lambda closure per call) with a `for` loop that caches `Info.GetSubService()` into a local, ensuring the virtual call happens once instead of once per trailer. Zero allocations.
- **`PrefabData` carriage-aware costs**: `TotalCapacity`, `CarCount`, and maintenance cost are now based on `Info.m_trailers` each access, supporting runtime dynamic carriage add/remove mods (e.g., CarriageNumberSelector) and preventing stale maintenance cost after behavior-modifying train length changes.

### Performance — CityService panel: cache stop/vehicle data to eliminate per-frame allocations

- **`PanelExtenderCityService.Update` (bus/metro/train/ship/plane/monorail/trolleybus branch)**: `GetStationStops()` (which walks the building's net-node → segment → lane graph and allocates a `List<ushort>`) was called every frame while the city-service panel was open. The result is now cached in a `_cachedStopArray` field and only recomputed when the displayed building changes. The `Concat().ToArray()` LINQ chain used when a sub-building contributes additional stops is replaced with a pre-allocated array filled via two `CopyTo` calls — zero intermediate allocations.
- **`PanelExtenderCityService.Update` (taxi/cable-car branch)**: `GetDepotVehicles()` (which allocates a `List<ushort>`) was called unconditionally every frame. The fix first counts owned vehicles by walking the lightweight `m_ownVehicles` linked list (no allocation), then only calls `GetDepotVehicles()` and rebuilds the UI list when the count actually changes.

