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

## [4.8.8] Real-play bug sweep: vehicle pathing, CommuterDestination, budget control, crash guard

Not a feature release. Every entry below came from actually playing 4.8.7
and reporting what broke, one issue at a time, each investigated to a
confirmed root cause rather than a guessed fix - covers regressions
introduced in 4.8.7 itself, several bugs that had been there since much
earlier releases and were only now found, one real vanilla-game crash
(not ours, guarded against anyway), and a round of cleanup (dead options,
mod-branded labels, performance profile actually doing something).

### Fixed - CommuterDestination's panel and map icons never appeared at all

Root cause confirmed against Harmony's own documentation: "The first
prefix that returns false will skip all remaining prefixes ... and the
original method." `OpenStopDestinationPanelPatch` was a second,
independently-registered `[HarmonyPrefix]` on `PublicTransportStopButton.
OnMouseDown`, alongside IPT4's own prefix in
`HarmonyPatches/PublicTransportStopButtonPatches/OnMouseDownPatch.cs`,
which always returns `false` and has real side effects (it shows IPT4's
own stop panel). Whichever of the two prefixes Harmony happened to run
first, if it was ours, CommuterDestination's own prefix never ran - not
intermittently, never. Fixed by removing the second Harmony registration
entirely and calling `OpenStopDestinationPanelPatch.TryShowForStopClick`
directly from inside IPT4's own already-firing prefix, so it no longer
depends on execution order between two independently-registered patches.
`PatchController` no longer does any Harmony patching of its own as a
result - just panel creation and the `EnableCommuterDestination` gate.

Given how many real bugs this integration has hit this session (the
citizen-scan cap counting every pedestrian instead of just waiting
passengers, `PatchAll(assembly)` double-patching every other integration,
and now this), its labels and panel title are marked "(Experimental)" in
en/pt/pt-br so players know to report anything odd rather than assume
it's fully solid yet.

### Fixed - Train Display performance profile did nothing at its own slider default

`PerformanceProfile.TrainDisplayMinPollSeconds` was a floor
(`Mathf.Max(interval, floor)`) on top of the player's own Options
interval slider. Light's floor (0.40s) and Maximum's floor (0.15s) both
sit *below* the slider's own default (0.25s), so the floor never bound
anything at default settings - switching between Light/Normal/Maximum
changed nothing visible unless the player had also lowered the slider
below the floor first. Replaced with `TrainDisplayPollMultiplier`, applied
directly to the slider's value (Light `1.6x`, Normal `1x`, Maximum
`0.6x`) before the existing hard 0.1s hitching-safety clamp - always
changes the effective interval relative to whatever the slider says, in
either direction. At the slider's own default this reproduces the exact
0.40s/0.15s Light/Maximum used to hardcode, so a player who never touches
the slider sees no change; a player who does now sees the profile
actually do something.

### Fixed - buses driving on the pavement and sinking into bridge decks (BBSP regression)

`BetterBusStopPosition`'s `CalculateSegmentPosition` postfix skipped unless the
vehicle was `Leaving | Arriving`, and that gate was removed in an earlier pass
here on the reading that upstream only checks `Leaving`. That reading was wrong
in a way that is easy to repeat, so it is now spelled out in the code: upstream
is a *transpiler* injected inside the method's stop-handling branch, so it only
ever runs in stop context by construction and its `Leaving` test is an inner
detail (which `CalculateModifiedStopPosition` already replicates). This port is
an unconditional *postfix*, so without the outer gate it replaced the ordinary
driving position with a curb-pulled stop position on every call - buses hugged
the kerb permanently, and on bridges, where there is no kerb to pull toward,
dropped through the deck into the terrain. Gate restored for bus and trolleybus.
StopStacker's equivalent patch already carried the same gate and the same
reasoning, which is what confirmed the diagnosis.

### Removed - StopStacker forced boarding (premature arrivals, U-turns mid-route)

`Patch_ForceStackedBoarding` (added in the previous session, never confirmed
in-game) called `ArriveAtDestination` itself once a berthed vehicle looked
"settled". The berth target it compared against was re-recorded every frame from
the vehicle's *current* path offset, so the target tracked the moving bus; a bus
merely slowed in traffic could satisfy both the 2m and the 0.5 m/s test far from
any stop, arrive early, advance to the next stop and re-path - which is a U-turn
on the road. Removed along with `BerthRegistry`'s berth-target cache. Berth
stacking is back to repositioning only, as it was before. The gap it was meant
to close (berthed vehicles never boarding) remains open and undemonstrated; it
is a locked legacy feature, so this is not worth a speculative patch.

### Fixed - depot redirect drove one building's transfer through another's AI

`StartTransferPatch` redirects a spawn from whichever depot vanilla offered it
to, to the depot the line is actually assigned. It performed the redirect as
`__instance.StartTransfer(depot, ...)` - `__instance` being the AI of the
*original* building, applied to a *different* building. The two are frequently
different subclasses: `TransportStationAI` (bus/train stations, which act as
their own depot) derives from `DepotAI`, so a station and a plain depot can each
end up on either side of the redirect, and the call then mixes up
`m_transportInfo` / `m_vehicleAI` / capacity. That spawns vehicles which do not
match the line and are culled again almost immediately - matching a report of
buses appearing at a station, moving a little and vanishing without ever leaving.
Redirect now resolves and uses the target building's own `DepotAI`, and aborts
with a warning if the target is not a depot at all.

### Changed - vehicle panel "next stop" label hidden by transparency, not by a cover

Successive attempts to stop this label flickering fought vanilla for
`_status.text` / `.isVisible` and lost intermittently; the last one covered the
label with an opaque sibling panel, which failed too because the cover was sized
and positioned from `_status` itself and parented beside it, inheriting the exact
layout vanilla was churning. The label's *colour* is now made transparent
instead. Vanilla writes the label's text, never its colour, so it no longer
matters who writes the text last - invisible text is invisible either way, and
the race is sidestepped rather than fought. `ReapplyCachedFields` re-asserts the
colour anyway, so even a future game or mod that did touch `textColor` could not
bring it back.

### Changed - CommuterDestination restored to the original mod's behaviour

Icons had stopped appearing entirely. Rather than keep patching a rewrite that had drifted from
upstream, the integration is now a straight port of
`Jameskmonger/CSL-ShowCommuterDestination` and drives itself end to end, as the original does:

- Upstream's own destination panel is back (`StopDestinationInfoPanel`), with its title bar,
  Previous/Next stop navigation and close button.
- Its own stop-click patch is back (`OnMouseDown` on `PublicTransportStopButton`), so opening a
  stop opens the panel.
- Icons are gated on that panel being visible, which is upstream's own gate.
- The graph is grouped by alighting stop again, so an icon is drawn per (alighting stop,
  destination building) pair with upstream's `1 + popularity / 5` size formula, at its 50m height
  offset, using its notification (`TooLong | MajorProblem`).

Removed on the way: the button IPT4 injected into its own stop panel, the "suppress hide" frame
counter that button needed, the map-detail setting that never had a UI control, and the
performance caps applied to the citizen scan. That last one is the most likely reason no icons
appeared - the cap counted *every* pedestrian in the 64m window rather than only waiting
passengers, so on a busy street it was exhausted by people walking past before a single waiting
passenger was found. Upstream has no such cap and neither does this port.

No behavioural corrections are applied on top. Two safety departures that cannot change which
icons are produced are marked in the code where they occur:

- `GetDestinationStopId` walks the line with `while (true)`, terminating only when a stop has no
  next stop. A circular line has no such stop, so an unmatched citizen would spin forever and hang
  the simulation thread. Bounded to the game's own maximum stop count.
- `StopIsDestination` calls `PathManager.ReleaseFirstUnit(ref citizenData.m_path)` - correct in
  `TransportArriveAtTarget`, where consuming the path is the point, but here it runs inside a
  read-only query and frees a path unit the real citizen is still using. The port reads the same
  path position without freeing anything, reaching the same verdict.

The previous IPT4-flavoured implementation is kept verbatim, as `.txt`, under
`Docs/referencia-commuterdestination-nossa-versao/`.

### Added - Train Display: second panel layout

`Options > Train Display > Panel layout` chooses between `Classic` (the existing
corner panel, still the default, so no existing install changes) and `Detailed`:
a line-coloured header carrying up to four metrics, a next-stop strip, the line
name, a rail of stop dots and the upcoming stop list. Drawn with the same IMGUI
primitives and the same `OverlayData` as the classic panel, and honouring the
same per-field checkboxes, so neither layout costs more than the other.

### Removed - "Street car" express tram mode was a duplicate of "Express skip"

Added in the previous session as a supposedly missing upstream feature. An audit
found both values landing in the same branch of `DepartureChecker`
(`... == TRAM || ... == STREET_CAR`), so the two behaved identically while their
labels promised different things ("Express skip" vs "Street car (brief stop and
go)"). Removed rather than given invented behaviour.

### Changed - Safe and Vanilla gameplay profiles now actually differ

Both called `ApplyAllIntegrationsOff`, and Vanilla's only extra line
(`ShowLineInfo = false`) was already done inside that method - two dropdown
entries, identical result. Split along what their own labels promise: Safe
("everything optional OFF, best with many mods") keeps UI conveniences, Vanilla
("base game feel") turns them off too.

### Performance - profile now reaches beyond two integrations; hover query cached

The performance profile was consulted by only two integrations; everything else
used hardcoded numbers. Ticket Prices' full vehicle-buffer refresh
(`TicketPricesRefreshSeconds`) and the waiting-passenger grid scan
(`WaitingPassengerMaxInspect`) now derive their limits from it, with the Normal
profile keeping today's exact values.

Separately, `PanelExtenderLine`'s hover handler called the waiting-passenger
query once per stop, for every stop on the line, in a single frame with no
throttle - on a long line that is thousands of grid inspections plus a
`TransportArriveAtSource` call each, per hover. Result is now cached per line for
~1s.

### Removed - dead performance-profile helpers and unreachable map-detail setting

`CommuterDestinationMapDetails` had no UI control anywhere, so `FullMap` was
unreachable and every `fullMap` branch in `PerformanceProfile` was dead code -
including two previously "fixed" clamp bugs that could never have executed. The
enum, its property, and the helpers left without consumers by the
CommuterDestination rewrite (`IsLight`, `IsMaximum`, `CommuterPanelListRows`,
`CommuterRefreshFrames`, and both `*Effective` methods) are removed. The orphaned
translation keys are left in place rather than editing 34 language files.

### Fixed - pre-IPT4 save loads but never unpauses (root cause: corrupted vehicle assets)

Reported: loading a save created before IPT4 was installed shows the city, but
the game never auto-unpauses. Suspected `OnLevelLoaded` was silently returning
early due to a missing/undetectable condition.

Instrumentation added to diagnose the issue confirmed: `OnLevelLoaded` executes
normally with `mode=LoadGame` and Harmony properly installed, logging "Begin
init version" as expected. The real cause was pre-existing corrupted vehicle
assets in the save file: a vehicle slot deserialized with a malformed prefab
reference (UTF-16 encoding error in the asset name). When Loading Screen Mod
scanned the city for missing assets, it attempted to report this corrupted
asset name, hit an exception while processing the invalid UTF-16 bytes, and
crashed the loading process.

This was not an IPT4 bug — it resulted from assets corrupted before IPT4 was
installed. The temporary diagnostic logging (`OnLevelLoaded DIAG:`) has been
removed. The defensive repair routine `VehicleCorruptionRepair` remains active
in `OnLevelLoaded` to clear such corrupted slots before any other mod walks the
vehicle array, preventing this specific vector for "loads but never unpauses"
reports in the future.

### Fixed - StopStacker: berthed vehicles now actually board/alight passengers

`Integration/StopStacker` (legacy/locked feature, player-visible label "Bus stop
berth stacking") only repositioned the 2nd/3rd vehicle at a stop into its own
berth (`Patch_BusAI_CalculateSegmentPosition.cs`); it never made that vehicle
eligible to load/unload passengers, so only the lead vehicle in the queue ever
served riders — the follower(s) just sat visually parked in their berth for
their whole dwell time.

Root cause, confirmed by decompiling `Assembly-CSharp.dll` (BusAI/CarAI/VehicleAI):
vanilla only calls `LoadPassengers`/`UnloadPassengers` once, from inside
`ArriveAtDestination`, itself gated by a movement-code arrival check requiring
the vehicle's physical position to land within ~1m of its calculated stop
target while essentially stationary (`CarAI.SimulationStep`). A berthed vehicle
parked a fixed short distance behind the lead vehicle on the same lane can be
kept perpetually a hair short of that exact threshold by ordinary
collision-avoidance braking against the vehicle ahead — it visually settles in
its repositioned berth but never crosses vanilla's own strict arrival check.

- `Integration/StopStacker/BerthRegistry.cs`: added a small
  vehicle→target-position cache (`RecordBerthTarget`/`TryGetBerthTarget`/
  `ClearBerthTarget`) recording the exact berth position each non-lead vehicle
  was last repositioned to.
- `Integration/StopStacker/Patch_BusAI_CalculateSegmentPosition.cs`: records
  that berth target after computing it (only reached for berth index >= 1;
  lead vehicles are never cached).
- New `Integration/StopStacker/Patch_ForceStackedBoarding.cs`: postfixes
  `BusAI.SimulationStep`/`TrolleybusAI.SimulationStep` and, only for a cached
  (non-lead) berthed vehicle still flagged `Arriving`, checks its physical
  position/velocity against the recorded berth target with a looser-than-vanilla
  threshold (~2m / ~0.5 m/s). Once settled, it calls the same
  `VehicleAI.ArriveAtDestination` vanilla would have called — reusing vanilla's
  own unload/load/next-stop logic rather than reimplementing it — so berthed
  vehicles now board and alight passengers alongside the lead vehicle instead
  of skipping their turn. Idempotent: `ArriveAtDestination` clears the
  `Arriving` flag, so the check naturally stops firing once it succeeds.

This only affects players who already had the legacy StopStacker toggle
enabled before it was locked; new installs cannot turn it on. `BetterBoarding`
(a separate integration another agent maintains) was not touched.

### Changed - SingleTrainTrackAI: whole-section reservation instead of per-segment

The reservation model reserved one track SEGMENT at a time as a train advanced,
not the full single-track SECTION (the whole stretch between the two nearest
switches/junctions/stations/double-track resumption points). That left a gap:
two trains approaching a shared section from opposite ends could each claim a
different segment of the *same* section before either was blocking the other,
and only collide once already inside it.

- New `Integration/SingleTrainTrackAI/SectionClassifier.cs`: given any
  single-track segment, walks the `NetManager` node/segment graph outward in
  both directions (`ExtendFrom`/`FindSinglePassThroughContinuation`) and
  groups every contiguous single-track segment into one shared `Section`
  object, stopping at the first node that isn't a plain two-way pass-through
  (switch, junction, station platform, dead end, or where double-track
  resumes). Results are cached per segment ID (the network doesn't change at
  runtime) so the walk only happens once per section, not once per train per
  tick.
- `TrackReservation` (`Integration/SingleTrainTrackAI/TrackReservation.cs`)
  now keys reservations by `SectionClassifier.Section` instead of by
  `ushort segmentId`. `Patch_TrainAI_CalculateTargetSpeed.cs` now calls
  `SectionClassifier.GetSection(...)` before occupying/checking, so a train
  claims (or is blocked by) the entire section the moment it sets foot on any
  segment belonging to it, not just the one segment it's currently standing
  on.
- `PatchController.Deactivate` now also clears `SectionClassifier`'s cache.

**What this gets for free:** because only one train can hold a given
`Section` at a time, the "who enters first when two trains arrive at once"
case is already resolved without a separate queue structure — whichever
train's `CalculateTargetSpeed` runs first that tick and finds the section
free becomes its holder; every other train that finds it already held brakes
(existing soft-brake-to-8, not a hard stop) until the holder's current
segment moves out of the section and the reservation is released. The
600-frame stale-reservation safety net (a despawned/deleted holder's section
would otherwise stay blocked forever) carries over unchanged from the old
per-segment code.

**What is explicitly NOT included, and why:** a real fairness/ordering
priority queue (e.g. tracking how long each waiting train has been queued and
letting the longest-waiting one go next, or alternating direction on
contested sections) was not built — with only single-holder exclusivity,
ordering among simultaneously-waiting trains is whatever order the game
processes vehicles in that tick, not a deliberate fairness policy. Adding
real fairness would mean tracking per-section wait state across ticks (who
arrived first, how long they've waited) and is a reasonable follow-up if
starvation on busy single-track lines turns out to be a real problem in
practice, but it's a separate, smaller change from the section-reservation
gap this entry closes.

### Added - Train Display: prominent "next stop" indicator

Restored the flagship feature of the original Workshop 3233229958 mod that the
4.8.5 "single corner panel" redesign had dropped: a bold, LED-sign-style "next
stop" readout, shown as the topmost line of the panel, above the terminus
`Destination` line. Updates automatically as the vehicle advances from stop to
stop (each poll re-derives it from the vehicle's live state — no cached index
to go stale).

- `TrainDisplayIntegration.OverlayData.NextStop` (new field) is populated in
  `TryBuildOverlayData` (`Integration/TrainDisplayUpdated/TrainDisplayIntegration.cs`)
  by reusing the first entry of the already-computed route strip
  (`BuildRouteStrip`'s `RouteStopNames[0]`), which is resolved from
  `vehicle.m_targetBuilding` — the same field `DepartureChecker`/
  `TransportLineUtil` elsewhere in this codebase already treat as "the stop
  the vehicle is currently approaching". No second line-walk was needed; the
  route-strip resolver already had a correct next-stop name, it just was
  never surfaced prominently.
- Drawn in `DrawSingleCornerPanel` as a small caps "próxima parada" label
  followed by a large bold value in the line's colour, ahead of the
  Destination/Line rows.
- New `ModSetting.TrainDisplayFields.NextStop` flag (bit `64`), on by default,
  with its own Options checkbox ("Mostrar próxima parada" /
  `SETTINGS_TRAINDISPLAY_SHOW_NEXTSTOP`) next to the existing "Show
  destination" toggle in `UI/CSLModsCommonOptionsPanel.cs`, so it can be
  turned off independently of the terminus display.
- The previously-dead `ResolveNextStopName` method (unused since 4.8.5, see
  the entry below) is left in place but still unused — the route-strip-based
  approach above was simpler and reuses tested code; it can be deleted in a
  later cleanup pass if nothing else claims it.
- New localisation keys (`en`, `pt`, `pt-br`; other languages fall back to
  English until translated): `SETTINGS_TRAINDISPLAY_SHOW_NEXTSTOP(_TOOLTIP)`,
  `TRAINDISPLAY_LABEL_NEXTSTOP`.

**Not done in this pass (left for a future round):**
- **Loop/branch detection** — no logic yet distinguishes a circular line from
  an out-and-back line, and no warning is shown for lines with more than 2
  terminals (Y/branch lines). The next-stop indicator itself is agnostic to
  this (it just reads the live `m_targetBuilding`), but the route rail's
  "current index" and the terminus `Destination` label can both be
  misleading on such lines — worth a dedicated pass.
- **TTS (text-to-speech)** announcements from the original mod — intentionally
  skipped as out of scope per the requesting brief; no groundwork laid.

### Fixed - CommuterDestination LateUpdate patch, for real this time

The `[HarmonyPatch(typeof(PublicTransportStopWorldInfoPanel), "LateUpdate")]`
patch (decaying the suppress-hide counter) had already been removed once this
session, but a later `git reset --hard` to an earlier commit silently
undid that removal because it was still uncommitted — see the new
"Known engine/runtime gotchas" note in the README. Removed again. Functionality
is unaffected: `PatchController.IsSuppressHideActive()` already self-heals via
a 30-frame timeout with no dependency on this patch ever running.

### Fixed - CommuterDestination graph generation rejecting nearly all waiting citizens

`DestinationGraphGenerator.GenerateGraph` filtered candidate citizens by the
distance from `citizen.m_targetPos` (their current path-segment waypoint, which
has typically already advanced past the stop once `WaitingTransport` is set) to
the stop, instead of their actual position — silently rejecting almost every
legitimately-waiting citizen with no error ever logged. The citizen-grid
iteration bounds (already derived from `stopPosition ± StopRange`) already do
the real proximity filtering by grid cell; this was a redundant and incorrect
second check. Removed.

Also (temporary, for testing while confirming the fix works end to end):
`PerformanceProfile.CommuterMaxCitizens`/`CommuterMaxDestinations` bumped way
up across all three profiles, and `DestinationGraphGenerator.StopRange`
widened from 64f to 300f, so results were easy to spot rather than possibly
capped down to near-nothing. **Reverted** back to the original defaults (80/6
Light, 200/12 Normal, 2000/80 Maximum for citizens/destinations; StopRange
64f) - icons still aren't confirmed showing in-game as of this revert, so this
was reverted on principle (temporary test values shouldn't ship) rather than
because the underlying feature was confirmed working.

Icon set changed from a 3-tier Low/Mid/High colour ramp
(`NoCustomers`/`Noise`/`Death+MajorProblem`) to a single consistent
`MajorProblem` icon (vanilla's plain red circle-with-"!" problem badge) — easier
to visually confirm during testing, may revisit once confirmed working.

### Fixed - vehicle panel text flicker, root cause was LateUpdate ordering

`PanelExtenderVehicle`'s "reapply cached text every `LateUpdate`" pattern
(added to win the race against the vanilla vehicle panel's own `Update`)
still flickered — confirmed via user report that it alternated between real
text and **blank**, not between two different real values, ruling out a stale-
cache issue. Root cause: Unity does not guarantee `LateUpdate` order between
different `MonoBehaviour`s, so if vanilla's panel (or another mod) also has its
own `LateUpdate` touching the same labels, whoever runs later that frame wins,
non-deterministically — see the README gotcha. Fixed in two layers:
`[DefaultExecutionOrder(32000)]` on `PanelExtenderVehicle`/`PanelExtenderLine`
to run as late as possible, and a `WaitForEndOfFrame` coroutine
(`EndOfFrameReapplyLoop`) that reapplies the same cached fields again after
*every* `Update`/`LateUpdate` in the scene has run for the frame — later than
execution order alone can push a `LateUpdate`.

### Fixed - line panel button overlap, this time by removing the overflow instead of hiding it

Two earlier attempts (bumping `_iptContainer.height`, then repositioning the
vanilla sibling below it) both failed to fix the "Visão geral das linhas" /
"Excluir linha" row overlapping vanilla's own line-length/stop-count text
below it — `autoLayout` positions children by their own sizes regardless of a
parent's declared height (see README gotcha), so there was no height value
that fixed it. Root-fixed instead by removing the row entirely
(`CreateButtonPanel2` deleted) and moving both buttons into the existing
"Atualizar Nome/Cor · Copiar · Colar" row, stacked vertically next to Copy/
Paste (`CreateLineOverviewDeleteStack`) — that row has no vanilla sibling below
it to collide with.

### Changed - IntercityBusControl no longer auto-disables Stop Stacker

`IptModManager`'s incompatible-mods list still had a `Ban`-level entry for the
standalone Stop Stacker mod from before it became a locked/legacy feature (see
4.8.7 below) — meaning IPT4 was still silently force-disabling a mod whose
author had just asked us to stop interfering with it. Removed the entry.

### Optimised - one redundant dictionary lookup + one LINQ allocation

`TeleportRedeployInstructions.NotifyTransportLineAddFutureDeployment`/
`TransportLineReadFutureDeployment` did `ContainsKey` followed by the indexer
(two hashes); `TransportLineReadFutureDeployment` also used `.First()` to read
element 0 of a `List<ushort>`, allocating a LINQ enumerator for no reason.
Replaced with `TryGetValue` and `list[0]`.

### Documented - Intercity Bus Control: converted stations default OFF, unlike the original mod

First public documentation of a behaviour gap that has existed silently since
the 4.8.5 `IntercityAcceptanceState` rewrite: `StationPatcher.TryPatchStation`
adds every regular bus station it converts to intercity service to
`PrefabsDefaultReject`, so `IntercityAcceptanceState.DefaultAccepts` returns
`false` for those stations until the player explicitly flips the "Allow
Intercity Buses" checkbox. Native Sunset Harbor intercity terminals are not
added to that set and default `true`, matching vanilla. The original
[Intercity Bus Controller](https://github.com/bloodypenguin/Skylines-IntercityBusController)
mod this integration is based on enabled every converted stop by default -
IPT4 does not.

Audited `git log -p` on `IntercityAcceptanceState.cs`/`StationPatcher.cs` and
every CHANGELOG entry mentioning Intercity Bus Control (4.8.0, 4.8.5, 4.8.6)
looking for the reason this diverged: no crash, bug report, or instability
tied specifically to converted stations defaulting ON was found anywhere in
the project history. The only trace is the source comment "checkbox visible
but default OFF (player must opt in)" introduced alongside the 4.8.5 rewrite,
with no rationale recorded beyond that. Given the lack of a documented
failure mode, the default was left as-is rather than risk reintroducing
whatever prompted the original choice - but the divergence is now called out
explicitly in the checkbox's own tooltip (`CITYSERVICE_ACCEPTINTERCITYBUSES_TOOLTIP`,
`en`/`pt`/`pt-br`) so players coming from the original mod are not left
guessing why a freshly converted terminal isn't routing intercity buses.

---

## [4.8.7] Bus stop berth stacking: locked legacy toggle, naming cleanup

Out-of-cycle release, prompted by a Workshop comment from ScratchyBald
(author of [Stop Stacker](https://steamcommunity.com/sharedfiles/filedetails/?id=3751418194))
objecting to IPT4's Workshop page and in-game tooltip using his mod's name,
quoting his page wording, and recommending players unsubscribe from his mod.
The underlying feature (`Integration/StopStacker/`) is and always was a
clean-room reimplementation with no shared code - the objection was about
naming/wording, not the implementation, and is addressed as such.

### Changed - "Bus stop berth stacking" is now a locked legacy toggle

- Moved the checkbox from `Options → Rede/Paradas` to a new
  `Options → Advanced → Legacy` section, and disabled its control
  (`OptionsNestedTabs.SetEnabled(card.Control, false)`, same pattern as the
  "Future" spoiler cards) so it can no longer be toggled by anyone.
- Removed `settings.EnableStopStacker = true` from the Realistic profile
  cascade (`SettingsActions.OnGameplayProfileChanged`) - no gameplay profile
  turns this on anymore.
- No new persistence mechanism was needed for the grandfather behaviour:
  `ModSetting.EnableStopStacker` already defaults to `false` and is a normal
  JSON-backed property, so an install whose settings file already has it
  `true` keeps deserializing to `true` (and the load-time
  `StopStacker.PatchController.Activate()` call in
  `ImprovedPublicTransportMod.cs` is unchanged and still runs for it) - only
  fresh installs and the now-locked UI stop new adoption.
- Rewrote the tooltip to describe the mechanic in fully neutral terms, with
  no reference to the other mod's name or Workshop page.

### Changed - Workshop text

- Removed from the "Absorbed Standalone Mods (Unsubscribe Required)" list in
  the incompatible-mods discussion post.
- Removed the "Stop Stacker" name from the Workshop description's changelog
  blurb and credits line (English, Portuguese PT/BR, Spanish, LatAm) in
  favour of a plain description of the behaviour.

### Housekeeping

- Filled every translation file up to parity with `en.txt` (English
  fallback text for strings not yet hand-translated) - several files had
  been missing keys added across the last few releases
  (`SETTINGS_AUTONAMESTOPS_*`, `SETTINGS_RESCUEFULLWIDTHDIGITS_*`,
  `SETTINGS_HIDDENBEHAVIOUR_GROUP*`, `SETTINGS_FUTURE_BUSWAYPOINT*`,
  `SETTINGS_HOTKEY_ADVSTOPSELECT_ALT*`, three 4.8.6 changelog entries) as
  well as the new `SETTINGS_LEGACY_GROUP*` and `CHANGELOG_4_8_7_*` keys from
  this release. `pt.txt`/`pt-br.txt` got real translations for the new
  strings; every other locale got the English text as a stopgap.

---

## [4.8.6] Commuter Destination back, real disable-on-toggle audit

Quick follow-up, not a new module - existing bugs found and fixed, no new
absorbed integrations.

### Fixed - Commuter Destination re-enabled

4.8.5 force-disabled this integration everywhere (`OnCommuterDestinationChanged`
always set the flag back to `false` and unconditionally deactivated the
patch controller, ignoring its `enabled` parameter; the Options checkbox was
removed) as a stopgap while the underlying redesign was still pending.
That stopgap outlived its purpose - the toggle is restored to normal:
`OnCommuterDestinationChanged` now actually activates when `true` and
deactivates when `false`, the checkbox is back under `Options → Info`
(same call pattern as `SharedStopEnabler`/`SubBuildingsTabs`), and the
tooltip that told players the feature was "not available yet" is corrected
across every language that had it (33 of the 34 translation files carried
this text - `pt-br.fixed.txt` was skipped as its own known-incomplete
scratch file).

### Fixed - two integrations that did not actually revert when turned off

An audit of every optional integration's on/off path (prompted by "does
disabling this really disable it, or does it just look off") found two
that silently kept affecting the game after being toggled off:

- **`ElevatedStopsEnabler`** wrote directly into shared `NetInfo.Lane`
  (`m_stopType`, `m_stopOffset`) and `NetLaneProps.Prop.m_flagsForbidden`
  fields with no Harmony patch and no record of the prior values - the
  opposite of the Harmony-patch-based `Activate()`/`Deactivate()` pattern
  every other integration uses. Once applied, the mutation was permanent
  for the rest of the game session; turning the setting off did nothing,
  and any other mod reading those same prefabs (including a real "Elevated
  Stops Enabler Revisited" install) would keep seeing IPT4's changes
  regardless of the toggle state. Fixed by recording each lane's/prop's
  original value in a dictionary the first time it is touched
  (`ElevatedStops.RecordLane`/`_originalPropFlags`) and adding a real
  `Revert()` that restores every recorded value; wired into a new
  `SettingsActions.OnElevatedStopsChanged` live-toggle handler instead of
  the previous "takes effect on next level load" checkbox.
- **`AutoLineBudgetIntegration`** (a `MonoBehaviour`) was added to
  `IptGameObject` once at level load if enabled, with no code path that
  ever removed it - the Options dropdown just wrote the setting value with
  no live-toggle handler at all. Disabling it mid-session left the
  component running and still silently reassigning target vehicle counts
  on lines it had taken over, for the rest of that game session, with no
  "reload required" notice either. Fixed with a new
  `SettingsActions.OnAutoLineBudgetModeChanged` handler that adds/destroys
  the component live, plus an `OnDestroy()` on the component itself that
  hands every line it had taken over back to vanilla budget control
  (`CachedTransportLineData.SetBudgetControlState(lineID, true)`) instead
  of leaving it stuck on the last computed target count.

Seventeen other optional integrations were checked against the same
question and were already correct (`PatchController`/`Patcher` pairs with
`Harmony.UnpatchAll` scoped to a per-integration ID, called from a live
on/off handler): `SharedStopEnabler`, `SingleTrainTrackAI`, `StopStacker`,
`ExpressBusServices`, `IntercityBusControl`, `FlightTracker`,
`TaxiStandFix`, `OptimisedOutsideConnections`, `SubBuildingsTabs`,
`UnlimitedOutsideConnections`, `BetterBoarding`, `BetterBusStopPosition`,
`MileageTaxiServices`, `PublicTransportUnstucker`, `RealisticWalkingSpeed`,
`AdvancedStopSelection`, `TicketPriceCustomizer` (the last of these
explicitly resets prices to vanilla and clears stop-lane pathfinding costs
on disable, in addition to destroying its watcher component). A handful of
integrations (`BetterBusStopPosition`, `AdvancedStopSelection`, and now
`ElevatedStopsEnabler`'s live-toggle predecessor) intentionally only apply
on the next level load and say so via `NotifyReloadRequired` - that is a
documented limitation, not a fake-disable bug, since a reload with the
setting off genuinely never re-applies them.

### Added - two settings for behavior that always ran unconditionally

A follow-up sweep of `HarmonyPatches/` (the mod's own core patches, as
opposed to `Integration/`) for behavior with no matching `ModSetting`
found two real features running with no way to turn them off:

- **Auto-name unnamed stops** (`AutoNameStopPatch`) - silently renames any
  newly-selected stop with no name, using the same nearby-building
  suggestion the stop panel's dropdown already offers.
- **Rescue Fullwidth Digits** (`NormalizeFullwidthLineNamesPatch`) -
  credited on the Workshop page as an absorbed standalone mod (by
  Gansaku) alongside every other integration that does have a toggle, but
  this one never got one.

Both gained a `ModSetting` bool (`EnableAutoNameStops`,
`EnableRescueFullwidthDigits`), default `true` so existing installs see no
behavior change, with checkboxes under a new `Options → Advanced →
"Always-on behaviour"` section. The stale disabled "Coming later" spoiler
for Commuter Destination in that same Advanced page (left over from the
4.8.5 stopgap) was removed, since the feature has its own working checkbox
again.

Everything above ships in English and Portuguese (`pt`, `pt-br`) for this
release; the other 31 language files fall back to English for the new
strings until translated in a later pass.

---

## [4.8.5] Stability release, Train Display redesign, Commuter parked

Intermediate build for players who will not wait for a full 4.9. Started as a
stability/frame-time pass but grew a real Train Display redesign along the
way - documented in full below rather than left as an undisclosed side effect
of a "bugfix" release.

### Redesigned - Train Display: single corner panel, terminus destination, correct speed

The overlay's draw path was replaced outright (`DrawUnifiedOverlay` →
`DrawSingleCornerPanel`): a single corner LCD-style panel with rotated stop
labels and a new "extras" strip, plus new colour themes (`BlackSemi` and a
set of pastel hues) alongside the existing ones. Two behaviour changes ride
along with the visual rewrite:

- **Destination now shows the line's terminus, not the next stop** - the
  next-stop resolver (`ResolveNextStopName`) is no longer called from the
  main data-build path; `ResolveTerminalStopName` replaces it. Players who
  relied on "next stop" at a glance will see the route's end point instead.
- **Speed readout fixed**: the conversion was `* 3.6f * 8f`, showing
  230-300 km/h for perfectly normal vehicle speeds. The stray `* 8f` is
  removed; only the correct m/s-to-km/h `* 3.6f` conversion remains.

### Fixed - Train Display hitching after the "snappier panel" change

- Reverted immediate re-poll on vehicle change and sub-100ms Maximum floors that
  could freeze/hitch the game while the overlay was active.
- Hard 100ms floor on poll interval (code + Options slider); defaults raised to
  250ms. Performance profile floors are conservative again (Light 0.40s /
  Normal 0.20s / Maximum 0.15s).

### Deep sweep (same build stream)

- **PassengerCountLimiter**: no more full 65k citizen scan every sim tick;
  counts rebuild across 16 frames; lazy manager resolve.
- **EBS**: stop counting no longer zeros `m_maxWaitTime` (broke boredom timers).
- **BetterBoarding + IPT LoadPassengers**: double passenger/income accounting fixed.
- **CanLeave**: bounds-safe cache; unbunching uses live target stop.
- **Empty-before-depot**: forget pending on every vehicle release (not only EBS).
- **ServiceBalancer**: broken stop chains return empty analysis (no KeyNotFound).
- **StartTransfer / GetLineVehicle / SelectedVehicleTypesQuery**: cache-ready
  guards; null prefab falls back to vanilla spawn.
- **LineWatcher**: always scan buffer (delete+create same window).
- **EBS ExtraSkip**: cache MethodInfo (no AccessTools every skip).
- **Localization missing keys**: log once; RefreshVehicleButtons tooltips 0.5s;
  StopListBoxRow name/passenger throttle; MovingAverage without LINQ.

### Root-to-leaf sweep (docs + languages + more fixes)

- Version stamps: README / Steam `VERSION.txt` / Options date → **4.8.5**.
- Framework `pt-BR.json`: fixed wrong BETA/Fatal strings; full `Language_*` set
  filled across Common JSON packs from en-US.
- IPT packs: pt/pt-br Commuter map labels PT; en tooltip mojibake cleaned;
  deploy skips `*.fixed.txt`. **Correction (2026-08-01, independent audit
  before publishing):** the "578 keys complete all locales" claim originally
  written here was false - pt.txt/pt-br.txt are the only files at full
  parity (579/579 keys). Every other language is missing translations for
  113-179 of 578 keys (falls back to English at runtime, not broken, just
  untranslated) - newer keys added across the 4.8.0/4.8.5 cycle (gameplay
  profiles, performance profile, hotkeys, line panel copy/paste, quick tips)
  were never actually translated for most of the 34 language files. Tracked
  as a known gap, not blocking this release (English fallback works fine),
  fix planned for a follow-up pass. Separately, `SETTINGS_GAMEPLAY_PROFILE_DESC_BLOCK`
  had a real parsing bug in en.txt/pt.txt/pt-br.txt specifically (a literal
  multi-line value instead of `\n`-escaped, corrupting the line-based
  parser) - fixed in this same audit; the other 33 languages already had it
  right.
- Removed unused `TransportLineReverseDetour` Deploy; reverse-detour stubs no longer log.
- `VehiclePrefabs` multi-level merge cache; `BuildingExtension.GetDepots` without LINQ;
  vehicle/stop panels bounds-safe on IPT caches; WhatsNew logs IPT4 + Verbose-only.

### Optimized - Train Display route strip + stop auto-name

- At most one `StopAutoNamer.EnsureNamed` spatial scan per poll.
- Failed unnamed stops are remembered so FindBuilding is not re-run every interval.
- Route strip avoids intermediate `List` growth churn on the hot path.

### Updated - Commuter Destination parked; languages complete on deploy

- Commuter Destination forced off (profiles, load, live-apply) and Options toggle
  removed until a redesign; Advanced spoiler documents the park.
- Deploy always ships the full CSLModsCommon `Localization/Common` JSON set so
  the Options language dropdown is complete.
- Section description cards share the rounded group background with their rows.

### Optimized - quieter depot redirect logs

- Depot `StartTransfer` redirect messages are Verbose-only (was every redirect).

---

## [4.8.0] Two clean-room integrations, Intercity Bus Control root-caused, bug/perf sweep

Module bumps from 7 to 8 for two newly absorbed integrations (`SingleTrainTrackAI`,
`StopStacker`); everything else below is a `build`-level fix/optimization pass on
existing modules, not a new absorption. Reversible Tram AI and a Breakdown
Revisited port were both scoped this cycle and explicitly deferred to a future
version rather than shipped half-finished (no source / high risk for a half
port).

### Updated - Compatibility tab + deeper conflict detection

- Options → **Compatibility** tab: one-click scan, live status of conflicts/missing
  deps, short player guide (IPT1–3 / TLM / ITM / absorbed standalones / Harmony).
- Detector matches **assembly name + alternate names + Steam Workshop IDs** so
  renames still flag.
- Expanded ban list (IPT original, TLM alt listing, Train Display original,
  AutoLineColor originals, Taxi Stand Fix, Rescue Fullwidth Digits, Vehicle
  Unbuncher alts, Automatic Vehicle Numbers Adjuster, etc.).
- Declares **CitiesHarmony** as a required dependency.

### Updated - SharedStop full port + Options depth + pathfinding fares

- **Shared Stop Enabler** is no longer a reduced-only port: elevated/bridge stop
  enablement, RoadBridgeAI flag refresh, RoadAI.UpdateSegmentFlags transpiler,
  PathManager.FindPathPosition car-platform nudge, NetSegment bridge
  GetClosestLanePosition, and TransportTool.GetStopPosition multi-type placement
  (IL soft-fail safe). LICENSE/tooltips updated.
- **Options → Performance** tab for the global Light/Normal/Maximum profile.
- **Budget → Pathfinding fares**: optional write of `NetLane.m_ticketCost` on stop
  lanes from average ticket prices (experimental; off by default).

### Updated - 4.8 leftover UX / Options parity (post-playtest)

- **Line panel Copy/Paste** buttons restored on `PanelExtenderLine` (plus PrefabPanel
  icons); tooltip keys `COPY_TIP` / `PASTE_TIP` / `LINE_PANEL_COPY` / `LINE_PANEL_PASTE`.
- **Options → Key bindings** tab: Train Display toggle and Auto Line Color refresh
  (`IptHotkeys` + `KeyBindingManager`).
- **City Service depot side panel**: live fleet list for pure depots (not only
  station stops); robust prefab match via `FindByName` / `FindByIndex` so custom
  assets still appear.
- **Remove vehicle → garage**: `TransportLineUtil.RemoveVehicle` clears the line
  and ensures a return-to-depot (`GoingBack` + `SetTarget` fallback).
- **Commuter Destination**: opt-in, live-apply; secondary destinations list (no
  auto-open); map icons by colour band; older “forced off” docs removed.
- **Shared Stop Enabler honesty** in Options tooltips: reduced port (no elevated
  stop / PathManager IL) called out explicitly.
- **STTAI / Stop Stacker**: live `Activate`/`Deactivate` mid-session (idempotent
  controllers), including Realistic profile cascade.
- **Global Performance profile** (Light / Normal / Maximum) for Train Display
  poll floor and Commuter scan caps.

### Updated - Safe-by-default installs and Options completeness (pre-playtest)

- Fresh installs default to gameplay profile **Safe**: every optional integration
  and mode starts **off** so an undetected Workshop conflict cannot double-patch.
  Profiles: Safe / Vanilla / Recommended (IPT core) / Realistic / Custom.
- Options → Integrations now exposes **Advanced Stop Selection**, **Better
  Boarding**, **Mileage Taxi Services**, and **Elevated Stops** (were already
  gated by `ModSetting` + profiles but had no checkbox).
- Commuter Destination is **opt-in and live-applicable** (panel + map icons;
  Performance / Full map detail modes). Older docs that said “forced off” are
  obsolete.
- Player-facing changelog keys `CHANGELOG_4_8_0_8`…`_10` document Safe defaults,
  the four new toggles, and full Steam language pack coverage.


### Added - SingleTrainTrackAI, Stop Stacker

Two new `Integration/` folders, both **clean-room reimplementations** - neither
source mod (`SingleTrainTrackAI`, Workshop 949504539, CoarzFlovv;
`Stop Stacker`, Workshop 3751418194, ScratchyBald) has a declared licence or
available source, so only their publicly documented behaviour was
reimplemented, using this project's own established Harmony conventions. Full
scope notes and honesty disclosures are in each folder's `LICENSE.txt`.

- **SingleTrainTrackAI** - `TrackReservation` holds one reservation slot per
  single-track segment (`SegmentClassifier.IsSingleTrainTrack`, classifying by
  counting `NetInfo.m_lanes` entries with `VehicleType.Train` and
  `LaneType.Vehicle`): the first train to claim a segment holds it until it
  moves off, and a `TrainAI.CalculateTargetSpeed` postfix brakes any other
  train to a stop before it can enter a segment held by someone else. A
  `VehicleAI.ReleaseVehicle` postfix now also releases a train's hold
  immediately when it despawns mid-segment, rather than relying solely on
  `TrackReservation`'s 600-frame stale-reservation timeout as the only cleanup
  path.
- **Stop Stacker** - a Harmony postfix on
  `BusAI`/`TrolleybusAI.CalculateSegmentPosition` (the same hook point
  `BetterBusStopPosition` already uses) assigns any vehicle behind the lead
  vehicle on a stop lane its own berth further back, spaced by vehicle length
  plus margin, computed with the same vanilla `NetLane.CalculateStopPositionAndDirection`
  curb-offset math BBSP uses - so trailing buses can load/unload passengers
  instead of queuing single-file as ordinary blocked traffic. Falls back to
  vanilla positioning if the lane is too short for another berth. Bus and
  trolleybus only, matching the original's stated scope; trams and trains are
  intentionally excluded since `SingleTrainTrackAI` already owns train-track
  sharing logic.

### Fixed - Intercity Bus Control silently inert for players who own Sunset Harbor

`IntercityBusControl.Mod.IsSunsetHarborInstalled()` checked for the DLC via
reflection into `ItemClassCollection`'s private `m_classDict`, looking for an
`"Intercity Bus"` key - a fragile proxy that returned `false` for a player
confirmed to own and actively use the DLC (built and clicked multiple bus
terminals). Root-caused via `output_log.txt` (not this project's own
`ImprovedPublicTransport4.log`, a separate CSLModsCommon-only sink most
`Utils.Log`/`LogWarning`/`LogError` calls never reach - see the `Diagnostics`
note below) showing `"Intercity Bus Control - Sunset Harbor DLC not found,
skipping patches."` at load time, which meant `Patcher.PatchAll()` never ran
and the entire integration - not just the accept-toggle checkbox - was a
no-op regardless of the `EnableIntercityBusControl` setting. Replaced with
`SteamHelper.IsDLCOwned(SteamHelper.DLC.UrbanDLC)`, the exact same check
`TicketPriceCustomizer` already used successfully for the same DLC elsewhere
in this codebase.

### Fixed - accept-intercity-buses checkbox unresponsive, and other terminal bugs

- `UpdateBindingsPatch` now forces `_cachedCheckBox.isEnabled = true` whenever
  it relabels the checkbox for a bus terminal - vanilla's own `UpdateBindings`
  body can leave the control non-interactive under conditions this mod's
  reuse of it never accounted for, which showed as a visible, correctly-
  labelled checkbox that silently ignored every click.
- `StationPatcher.TryPatchStation`'s "already patched with the current mode's
  capacity" early-return bailed without adding the building to
  `PatchedBuildingNames`, so a terminal that was already correctly set up
  from an earlier sweep could lose its toggle's visibility on a later check.
  It now registers the name before bailing.
- Added `Diagnostics.VerboseRuntimeLogs`-gated logging to
  `StationPatcher.TryPatchStation`'s two silent bail branches (mismatched
  primary/secondary transport type, or an existing non-intercity line already
  assigned) so a future "toggle missing on this terminal" report is
  diagnosable from a single log grep instead of static analysis.

### Fixed - Sub-Buildings Tabs stale tab strip on a recycled building ID

`SubBuildingsTabstrip.UpdateInfoPanelTabs`'s cache check
(`_idList.Contains(buildingId)`) trusted a building ID match as proof the
player was still looking at the same building cluster as last time. Cities:
Skylines recycles building IDs after demolition, so a demolished sub-building
ID that later gets reassigned to an unrelated new building could pass this
check and inherit the previous cluster's stale tab strip - rare (needs a
demolish-and-rebuild to land on the exact freed slot), matching reports that
this only "sometimes" happened. The cache now re-walks the candidate
building's parent chain and only trusts the cache if it still resolves to the
same cached main-building ID.

### Fixed - fourteen-item bug sweep across existing integrations

A dedicated read-only review pass (not tied to any specific bug report) found:

- **ExpressBusServices** - `CachedVehicleProperties.GetFromCache`'s expiry
  check used `>` where it needed `<`, so a not-yet-expired entry was treated
  as expired (discarded immediately) while a genuinely expired entry was
  returned as still valid.
- **ExpressBusServices** - `Patch_TrolleyBusLoadsPassengers`'s postfix was
  missing the `BusStopSkippingLookupTable.ForgetBus(vehicleID)` call its
  `Patch_BusLoadsPassengers` sibling has, so a trolleybus once marked to skip
  a stop never got un-marked and kept skipping every subsequent stop
  indefinitely.
- **Unbunching** - `CanLeavePatch.TransportLineCanLeaveStopWrapper`'s "only
  one vehicle on this line" bypass compared a vehicle-ID field
  (`TransportLine.m_vehicles`) against `currentStop` (a node ID) instead of
  `currentVehicleID` - different ID spaces, so the bypass essentially never
  fired and a lone vehicle on a line could be incorrectly held for
  unbunching.
- **AutoLineColor** - `ColorMonitor`'s refresh loop and
  `NamingStrategyBase.GetExistingNames` both iterated `lines.Length - 1`,
  skipping the last slot of the line buffer on every pass (never
  auto-colored/considered for duplicate-name checks) and
  `ColorSelector.DifferenceThresholdSelector` used
  `Random.Range(0, colors.Count - 1)` (max-exclusive overload), so the last
  color in every palette could never be picked.
- **BetterBusStopPosition** - `BusAI_Patch`/`TrolleybusAI_Patch`'s postfixes
  dereferenced `info.m_lanes` without the `info?.` the sibling `StopStacker`
  patch already uses, risking an uncaught `NullReferenceException` in a
  Harmony postfix if `Info` is null for a segment mid-destruction.
- **TicketPriceCustomizer** - `PriceCustomization.TryGetTransportInfo` cached
  failed lookups permanently; a transport type queried before any line of
  that type existed (e.g. before an airport was built) stayed uncustomizable
  for the rest of the session even after a matching line was created. Only
  successful lookups are cached now.
- **UnlimitedOutsideConnections** - `BuildingUtil.FindServiceBuildings`'s
  early-return required `Service.PublicTransport`, making the method's own
  documented "intercity bus routes match to roads instead" branch
  unreachable dead code. The gate now allows both `PublicTransport` and
  `Road`.
- **ElevatedStopsEnabler** - `ElevatedStops.EnableStops` bounded its loop by
  `info.m_lanes.Length - 2` while indexing `info.m_sortedLanes[i]`,
  inconsistent with the sibling `AddElevatedStoptypes` (bounded by
  `m_sortedLanes.Length - 2`); a network variant where the two lengths differ
  would throw (silently caught) and skip elevated-stop enabling for it.
- **BetterBoarding** - `LoadPassengers_TrolleybusAI` was missing the
  `[HarmonyAfter(ExpressBusServicesHarmonyID)]` ordering attribute its
  `LoadPassengers_BusAI` sibling has, so a trolleybus that should skip
  boarding at an express-marked stop could still load passengers.
- **SingleTrainTrackAI** - see Added section above (`ReleaseVehicle` wiring).
- **Line panel UI** - `UpdateStopButtonsPatch.Prefix` cached a possibly-null
  `Find<UILabel>("PassengerCount")` result but dereferenced it unconditionally,
  risking an uncaught exception in this Harmony prefix; now guarded.
- **AutoLineColor** - `Console.Error` was gated behind the same debug flag as
  `Message`/`Warning`, silencing real failures for anyone who hadn't enabled
  debug logging.

### Optimized - reflection and per-frame lookup caching

- `RefreshVehicleButtonsPatch` (runs every frame a line panel is open, once
  per vehicle button) now caches each vehicle prefab's description instead of
  calling `VehiclePrefabs.instance.FindByName(...).GetDescription()` fresh
  every frame.
- `SegmentClassifier.IsSingleTrainTrack` now caches its result **by `NetInfo`
  prefab**, not by segment ID - the lane layout it inspects is entirely a
  prefab property, shared by every segment instance using that prefab, so
  this is both correct and immune to per-instance mutation, and avoids
  re-walking `m_lanes` on every `CalculateTargetSpeed` call (up to twice per
  train per tick: current segment and next).
- `AutoLineColor.UIExtender`'s `RefreshButtonStateUpdater.Update()` (runs
  every frame for the life of the loaded level) now only touches
  `Button.isEnabled`/`tooltip` when the enabled state actually changed since
  last frame, invalidated on a locale change via
  `LocalizationManager.ModActiveLocaleChanged` so a language switch still
  refreshes the tooltip text promptly.
- `IntercityBusControl.StationPatcher` and `InitializePrefabPatch` each did
  their own uncached reflection into `ItemClassCollection.m_classDict` -
  consolidated into one `static readonly FieldInfo` behind
  `Mod.GetItemClassDict()`.

### Changed - Safe-by-default installs (profiles)

Fresh installs use gameplay profile **Safe**: every optional integration and
mode starts **off** for maximum Workshop compatibility. **Recommended** turns
on IPT core only (budget fleet control, unbunching, Intercity Bus Control,
Sub-Buildings Tabs, unstucker, advanced stop selection, elevated stops).
**Realistic** enables most absorbed integrations; SharedStop / Commuter /
UnlimitedOutside stay off as higher risk. Custom never cascades.

Intercity Bus Control and Sub-Buildings Tabs remain available and are included
in Recommended after the fixes above. Remaining known issues (Sub-Buildings
Tabs' tab-strip vertical offset; an unconfirmed stop-name display report) are
minor and tracked for a future version.

---

## [4.7.0] Four new integrations, Options menu reorganized, bug/perf sweep

Jumps straight from 4.3.8 to 4.7.0 - the module counter absorbs everything below
as one release rather than a bump per item, since none of it shipped
individually. `major` stays 4: this project intentionally does not version past
IPT4's own generation, however many modules it grows.

### Added - Sub-Buildings Tabs, Shared Stop Enabler, Commuter Destination, Taxi Stand Fix

Four new `Integration/` folders, each with its own toggle under
Options > Integrations. Per-mod credit and licence detail is in
`README.md#absorbed-mods` and each folder's own `LICENSE.txt`; summary:

- **Sub-Buildings Tabs** (ported, MIT, BloodyPenguin/AJ3D) - the Harmony patch
  is on `CityServiceWorldInfoPanel.OnSetTarget`, which fires uniformly for every
  subclass that calls `base.OnSetTarget()`. It is not airport-specific; the
  "only works on the airport" symptom reported during testing is because only
  airports have vanilla sub-buildings to show tabs for, confirmed by reflection
  over the panel hierarchy and by asking whether any other hub type had actually
  been tested (it had not). No code change was needed for that part; see #15
  below for the one real bug found in this integration.
- **Shared Stop Enabler** (ported, GPL-3.0, CodeBardian) - a **reduced** port.
  The `RoadBridgeAI`/elevated-stop patch, the `PathManager.FindPathPosition`
  patch, and an IL-transpiler patch matching `TransportTool.GetStopPosition` by
  string content were deliberately left out - they either touch a very hot
  vanilla method or are fragile string-matched IL, out of proportion with what
  this integration adds. Only the core mechanism (more than one stop type per
  road segment) was ported. `SharedStopsTool`'s MonoBehaviour/Singleton was
  replaced with a static `SharedStopRegistry`, matching how IPT4 already tracks
  per-line/per-segment state elsewhere. **Off by default** - this is the one
  integration here that changes shared, global prefab data rather than being
  purely additive/per-instance.
- **Commuter Destination** (ported, MIT, jkm/Jameskmonger) - the destination-
  graph logic itself is a close, line-by-line-reviewed port; the *integration*
  point is not. Upstream hides its own panel and IL-transpiles a label into
  IPT2's stop panel when IPT2 is present - IPT4 doesn't need that trick, since
  this is built in, so it's a plain Harmony postfix adding a button to IPT4's
  own `UI/PublicTransportStopWorldInfoPanel.cs`. Upstream also auto-opens its
  panel on every stop click; IPT4 already opens its own stop panel on that same
  click, so stacking a second automatic popup would show two panels at once -
  this integration opens on-demand from a button instead. One real upstream bug
  was found and **not** ported: `Bridge.cs`'s `StopIsDestination` calls
  `PathManager.ReleaseFirstUnit`, which - per `ReleaseFirstUnit`'s own
  decompiled logic - frees a citizen's shared path unit back to the pool when
  `m_referenceCount <= 1`, the common case for an unshared citizen path, while
  that citizen may still be using it. Omitted with the accuracy trade-off
  documented in `Integration/CommuterDestination/LICENSE.txt`: citizens whose
  immediate path crosses a `PathUnit` boundary are treated as "not yet at this
  stop" instead of being read via the unsafe call.
- **Taxi Stand Fix** (original implementation, not a port) - sends a genuinely
  idle taxi (no passenger, no destination already assigned) toward the nearest
  stand via the same public, vanilla `TaxiAI.SetTarget` the game itself uses to
  dispatch taxis, so pathfinding, building registration and the "I am
  available" broadcast all behave exactly as a normal dispatch. Inspired by the
  standalone [Taxi Stand Fix](https://steamcommunity.com/sharedfiles/filedetails/?id=3712889232)
  mod's concept; written fresh rather than adapted from its code, so it carries
  no upstream licence and is not listed in the `README.md` absorbed-mods table.

### Added - Depot/terminal vehicle capacity mode

`ModSetting.DepotCapacityModes` (`Disabled` / `Intermediate` / `Realistic`),
exposed as `IntercityTerminalCapacityMode` under Options > Integrations. The
reported "extremely high garage/depot limits" traced specifically to
`IntercityBusControl.StationPatcher` and `InitializePrefabPatch`'s hardcoded
`m_maxVehicleCount = 100000` / `m_maxVehicleCount2 = 100000` - not a general
vanilla depot issue, so the fix is scoped to that one mechanism rather than
touching depot AI broadly. `Disabled` preserves the existing 100000 (no
behaviour change for saves already running this mod); `Intermediate` applies a
fixed 200; `Realistic` returns 40, close to the terminal's own vanilla prefab
capacity. Both call sites - the retroactive `PatchStations()` sweep and the
live `InitializePrefab` postfix - now read the same
`StationPatcher.GetCapacityForCurrentMode()` instead of two independent
hardcoded literals that could have drifted apart.

### Fixed - Intercity bus terminal accept-toggle silently touched `isEmptying`

Decompiling `CityServiceWorldInfoPanel.OnAcceptsIntercityChanged` showed
`acceptsInterCityTrains` is a pure alias for `isEmptying`
(`set { isEmptying = !value; }`). The existing "accept intercity buses"
checkbox on patched bus terminals just relabelled vanilla's train checkbox
without intercepting its click handler, meaning toggling it could put a bus
terminal into the game's building-shutdown/evacuate state. Fixed with a new
Harmony prefix (`Patch_OnAcceptsIntercityChanged`) that intercepts the change
for buildings in `StationPatcher.PatchedBuildingNames`, persists the choice
per-building via the new `IntercityAcceptanceState` (backed by
`SerializableDataExtension`, `DataID = "IPT4_IntercityBusAcceptance"`), and
returns `false` so `isEmptying` is never touched for those buildings; native
train stations are unaffected and keep using the vanilla property directly.
`UpdateBindingsPatch` now syncs the checkbox from this per-building state
instead of leaving it wired to nothing.

### Added - Train Display "Original" theme, and fixed wrong colors in the rest

Added `ModSetting.TrainDisplayColorThemes.Original`, built from screenshots of
the source [Train Display - Updated](https://steamcommunity.com/sharedfiles/filedetails/?id=3233229958)
mod's Workshop page (no live game access to compare against): a dark header
strip (name/status left, speed centre, next-stop/passengers right) plus a
route strip below it, coloured to the vehicle's actual `TransportLine.m_color`
and walking `TransportLine.m_stops`/`GetNextStop` to lay out passed/current/
upcoming station markers. Interface-only, as requested - no change to how or
when the overlay is triggered.

Separately, the three existing themes (Simple/Dark/Light) were rendering the
wrong colors: they tinted `GUI.Box(rect, GUIContent.none)` via
`GUI.backgroundColor`, which does not produce a flat, accurate color because
Unity's default skin box texture is not pure white - a tint multiplies against
whatever that texture actually is. Replaced with a cached solid white
`Texture2D` drawn via `GUI.DrawTexture` + `GUI.color`, which does tint
accurately.

### Fixed - Ticket Prices tab always used the fallback slider row

`CreateSliderRowFromTemplate` reflects into the vanilla `BudgetItem` prefab to
reuse its dual-handle slider visual. The lookup for the night-price slider
field had a typo (`m_NightSlidermalan` instead of `m_NightSlider`), so
`GetField` always returned `null`, the subsequent `GetValue` always threw, and
every row silently fell back to `CreateSliderRowFallback` - the vanilla-styled
row was never actually used, and every row paid for a wasted reflection/
exception/`GameObject.Destroy` cycle in the process.

### Optimized - cached per-frame UI lookups in two hot Harmony patches

- `IntercityBusControl/.../UpdateBindingsPatch` postfixes
  `CityServiceWorldInfoPanel.UpdateBindings`, which vanilla calls every frame
  while **any** city-service panel is open (fire, police, health - not just
  transport). It called `Find<UILabel>("Label")` and
  `Find<UICheckBox>("AcceptIntercityTrains")` every frame instead of once; both
  are now cached against the panel instance, which the game reuses across every
  building the player inspects. Also added a null-guard on `building.Info` -
  without it, inspecting a building that gets bulldozed while its panel is
  still open threw every frame until the panel closed, since Harmony does not
  swallow postfix exceptions.
- `PublicTransportWorldInfoPanelPatches/UpdateStopButtonsPatch` fully replaces
  `UpdateStopButtons` and called `Find<UILabel>("PassengerCount")` per stop
  button, per frame, while the line panel is open - 30+ redundant UI-tree
  searches per frame on a 30-stop line. The pooled `UIButton` instances are
  stable across frames, so the label is now cached in a
  `Dictionary<UIComponent, UILabel>` keyed by button instance instead.
- `XYZVehicleAIPatches/CanLeavePatch` marked its `currentVehicleID`/
  `currentStop` static fields `[ThreadStatic]`. They smuggle per-call context
  from `Prefix` into a transpiler-injected wrapper call within the same
  `CanLeave` invocation; vehicle AI simulation runs on a single thread today so
  this was not a live bug, but the attribute removes the risk at zero cost if
  that ever changes.

A broader sweep (hot-path allocations, Harmony patch safety, event-subscription
lifecycle across level loads) found nothing else actionable at this scope; see
the PR/commit history for the full list of what was checked and ruled out,
including a full-array-scan in `StopsAndStations/PassengerCountLimiter`'s
`OnBeforeSimulationTick` that was deliberately left alone - the array is a
fixed, small, compile-time bound (not city-size-dependent), the scan is
allocation-free, and simulation ticks run far less often than frames, so
bucketing it would trade real correctness risk (stale counts letting
over-capacity spikes through) for no measurable gain.

### Updated - Options menu reorganized around what each tab actually contains

The old "Auto Line" tab had grown to seven unrelated settings groups (line
info, budget, ticket prices, `AutoLineColor` under an un-localized hardcoded
section title, Express Bus, Express Tram, `PublicTransportUnstucker`, and a
catch-all "Integrations" group holding six unrelated third-party toggles).
Split into: **Fleet & Scheduling** (line info, Unbunching's aggression/vehicle-
count/spawn-interval controls, Express Bus/Tram - two approaches to the same
spacing problem, now together), **Budget & Prices**, **Line Colors** (given its
own tab and a real localization key instead of the hardcoded string), and
**Integrations** (every absorbed third-party mod's toggle in one place - the
landing tab for any future integration's switch). Stops, Delete Lines and
Train Display were already well-scoped and are unchanged. Also restored the
Shortcuts/Keybindings tab removed in an earlier version (currently an empty
scaffold - no keybindings are defined yet, but the tab is ready for an
integration that needs one), and added a GitHub project link under
Options > Advanced, matching what several of the absorbed mods already did on
their own settings pages.

---

## [4.3.8] Stable channel

### Updated - `CurrentBuildChannel` is now `BuildChannel.Stable`

Declared in `IptModManager`, not via a compile constant, so the channel is
explicit in code and independent of how the project is built. The framework
default is `Alpha`, which is what the Options header showed before this was set at
all.

Stable is justified now that the maintenance overflow the fork exists to fix is
resolved at its root (4.3.6) with a repair pass for saves it already damaged, and
all 23 languages are complete (4.3.7). Drop back to `Beta` when landing something
that needs field testing.

### Notes on publishing this release

Four things about the Steam upload path were wrong or unknown and are now settled.
They cost three failed uploads, so they are recorded in
`Projeto-Steam/workshop-metadata-en.md` and enforced in `write_vdf.py`:

- **The `.vdf` parser does not process escape sequences.** Literal `\n` stays
  literal - the 4.3.5 change note is still on the Workshop page with `\n` printed
  as visible text between its sentences. Use real newlines inside the quoted value.
- **An unescaped `"` is what actually breaks the parse**, not a newline. It
  terminates the value, and the remaining text is read as a key name. That is the
  real source of the `key name too long (1235 chars)` error from the first upload
  attempt, which had been misattributed to line breaks. The description file now
  uses typographic quotes and `write_vdf.py` asserts there are no straight ones.
- **A `description` key makes the commit fail** with
  `Failed to update workshop item (Invalid Parameter)`, regardless of length -
  tested at 10,723 and 7,968 characters, isolated by bisection. The description is
  published by hand; the file stays the source of truth.
- **`previewfile` must not point inside the deployed mod folder.** That folder is
  regenerated by `DeployToModDirectory`, so the cover image placed there by hand
  had vanished, and the upload failed with `File Not Found` without publishing
  anything. `write_vdf.py` now omits the key when the file is absent, which makes
  the Workshop keep the image already on the page.

---

## [4.3.7] All 23 languages complete; translation-progress indicator

### Translation - `cs`, `nl`, `sk`, `th`, `tr`

Five languages were listed in the mod's language selector but had no
`Translations/*.txt` file. `LocalizationManager.FindLanguage` found nothing and
they fell back to English with no indication anything was missing - the selector
is populated from the **framework's** locale list
(`CSLModsCommonShared/Localization/Common/*.json`), not from the presence of a mod
translation, so a language can be reachable in the UI and untranslated in fact.

Each now has a full file: 366 keys, same key set as `en.txt`, same order.

Verified programmatically across all 23 files rather than by inspection:

- key set identical to `en.txt`, no extra keys, no empty values
- `{0}`-style placeholder sets matching per key
- literal `\n` counts matching per key
- no line failing `^[A-Z0-9_]+ \S`

That last check is the one that matters most. A real newline inside a value
splits the entry across lines and corrupts the parse, because the reader takes one
key per line. It has bitten this project before.

**On key order:** the 18 pre-existing files have differed from `en.txt`'s order
since before this release (185 positions in `ar.txt` alone). Order is irrelevant
to the parser - entries go into a dictionary - so they were left as they are
rather than reordered, which would produce a large diff with no functional
effect. The new files follow `en.txt`.

### Translation - `CHANGELOG_4_3_6_1` / `_2`

The 4.3.6 entries existed only in `en.txt` and `pt.txt`. The other 21 languages
would have rendered the raw key in the changelog panel. Added everywhere,
inserted at `en.txt`'s position.

### Fixed - progress indicator reported 76% for complete languages

`Localization/Common/TranslationStatus.json` is a **static** file, read by
`EmbeddedLocalizationLoader` and consumed by
`LocaleEntry.RecalculateTranslationProgress`, which feeds the percentage shown
under the language dropdown in Options. It had never been regenerated: it claimed
112 total strings and 97 translated (76%) for sixteen locales, and had no `en-US`
entry at all.

All 23 framework locale files in fact contain all 116 keys with no empty values.
Regenerated from the real contents of both localization systems - framework json
(116 keys) and mod txt (366 keys), resolved through the same
long-id-to-short-filename mapping `FindLanguage` uses. All 23 now report 100% and
`en-US` is present.

Worth knowing before adding keys: this number is not computed at runtime. Add
strings without regenerating that file and the panel reports a stale figure while
the translations themselves are fine.

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
from `en.txt`, so they would have rendered as raw keys. Restored on the line panel
and PrefabPanel (see 4.8 leftover UX section above).

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

