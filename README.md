# Improved Public Transport 4

**Version 4.8.0** (Stable channel) · Cities: Skylines 1 · targets 1.21.1-f9

IPT4 is a fork of [Improved Public Transport 3](https://github.com/TheMadisonian/ImprovedPublicTransport3)
that absorbs other public-transport mods into a **single assembly**. The goal is not
just convenience: mods that each manage the same per-line state end up fighting over
it, and that class of conflict is what motivated this fork (see
[Why this fork exists](#why-this-fork-exists)).

- **Workshop page:** https://steamcommunity.com/sharedfiles/filedetails/?id=3773802930
- **Player-facing release notes:** the Workshop changelog
- **Developer release notes:** [`CHANGELOG.md`](CHANGELOG.md) — root cause, affected class, why it was not obvious

> This is a personal fork maintained for one player's setup. It is public so the
> work is inspectable and reusable, not because it is a supported product.

---

## Why this fork exists

The original bug: public transport maintenance costs ran away to billions
(≈ -42,000,000,000 in one save). Root cause was **AutoLineBudget 21** writing
`TransportLine.m_budget` directly while IPT3 independently read and recalculated the
same field to decide how many vehicles to keep active. Two systems, one piece of
shared state, no coordination — the fleet and its upkeep inflated without bound.

IPT4 resolves that class of problem structurally. Absorbed integrations must go
through `Data/CachedTransportLineData`:

```csharp
CachedTransportLineData.SetTargetVehicleCount(lineID, target);
CachedTransportLineData.SetBudgetControlState(lineID, false);
```

Never `TransportLine.m_budget` directly. `Integration/AutoLineBudget/` is the
reference implementation — it also keeps a `HashSet<ushort>` of lines it took over,
so a line the player set to Manual is never hijacked (both cases otherwise look
identical, since both have `BudgetControl == false`).

The same failure family showed up again in 4.2.0, in absorbed taxi-fare code that
could overflow an `int` and drain the budget in a single simulation step.

---

## Performance note

Informal, single-machine observation, not a controlled benchmark: on the
maintainer's save, this mod consistently runs at **~50-60 FPS**, against
**~40-50 FPS** running the same feature set as separate standalone mods (or
under the predecessor IPT3). One assembly means one Harmony patch per hooked
method instead of several competing ones, and the 4.8.0 optimization pass
(caching several previously uncached reflection/UI lookups - see
`CHANGELOG.md`) narrowed that gap further. Your mileage will vary by city size
and mod list.

---

## Requirements

| Requirement | Required? | Notes |
|---|---|---|
| [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2040656402) | **Yes** | Every patch depends on it. `CitiesHarmony.API` bootstraps from this mod's install; we deliberately do not bundle `CitiesHarmony.Harmony`. |
| `Newtonsoft.Json.dll` | Bundled | Ships in the mod folder. Do **not** remove it — see [Dependency notes](#dependency-notes). |
| Ability to Read (`1145223801`) | No | Community in-joke listed as "required" by some mods. Nothing here uses it. |

**Unsubscribe the standalone versions** of anything under
[Absorbed mods](#absorbed-mods). Their code is compiled into this assembly;
running both patches the same methods twice. IPT3, Transport Lines Manager and
standalone AutoLineBudget are declared incompatible and will raise an in-game
warning.

---

## Building

```bash
dotnet build ImprovedPublicTransport4.csproj -c Debug
```

- **Target framework:** `net35` (Unity/Mono). Consequence: no `Enum.HasFlag`,
  no `Array.Empty<T>()`, no `System.ValueTuple` — several were hit in practice and
  are noted in `CHANGELOG.md`.
- Game assemblies resolve from the Steam install via `$(GamePath)`.
- `AlgernonCommons` is a git submodule; `CSLModsCommonShared*` are vendored
  source (`.projitems`) with local patches — see below.
- The build **auto-deploys** to
  `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\ImprovedPublicTransport4\`
  via the `DeployToModDirectory` target, so a build is an install.

```bash
git clone --recurse-submodules https://github.com/ccc012/ImprovedPublicTransport4
# or, in an existing clone:
git submodule update --init --recursive
```

### Dependency notes

Any `PackageReference` that produces its own DLL needs a matching `<Copy>` in
`DeployToModDirectory`. Otherwise it only works *by accident*, on machines where
another subscribed mod already loaded that assembly — exactly how the
`Newtonsoft.Json` bug in 4.3.2 stayed hidden.

Cities: Skylines loads every mod into one `AppDomain` with **no isolation**, so
assembly identity is first-come-first-served. `Real Time` ships a strong-named
Newtonsoft.Json 13.0.0.0 while CSLModsCommon needs the unsigned 9.0.0.0.
`JsonNetBootstrap` therefore installs an `AssemblyResolve` hook that always prefers
the copy next to our own DLL rather than whatever loaded first.

**Regression test worth repeating:** disable every other mod except Harmony and
confirm IPT4 still loads. That is what surfaced the bug.

---

## Architecture

```
ImprovedPublicTransport4/
├── Mod.cs                     # ICities.IUserMod  (CSLModsCommon entry point)
├── IptModManager.cs           # ModManagerBase: name, channel, changelog, incompatibilities
├── ImprovedPublicTransportMod.cs  # LoadingExtensionBase: patch apply/undo + integration wiring
├── ModSetting.cs              # single settings store (79 properties)
├── JsonNetBootstrap.cs        # deterministic Newtonsoft.Json binding
├── Data/CachedTransportLineData.cs   # per-line coordination point (see above)
├── Integration/<ModName>/     # one folder per absorbed mod
├── HarmonyPatches/            # patches owned by IPT itself
├── Translations/*.txt         # mod strings (19 files)
└── CSLModsCommonShared/       # vendored UI framework + its own locales
```

Two classes the game discovers **independently**:

- `Mod : ModEntry<IptModManager>` — the `IUserMod`; owns the Options UI.
- `ImprovedPublicTransportMod : LoadingExtensionBase` — owns per-level patch
  lifecycle and integration wiring.

They do not need to be the same class. `ModManagerBase` (not
`PatchModManagerBase`) is used deliberately so CSLModsCommon never touches Harmony
lifecycle — that keeps the existing per-`OnLevelLoaded` apply/undo untouched, and
avoids re-creating the "two systems, one piece of state" pattern this fork exists to
fix.

> **Do not locate the mod folder by looking for a specific `IUserMod` type.** That
> coupling broke three separate features across releases as the entry class moved.
> Use `PluginManager.FindPluginInfo(Assembly.GetExecutingAssembly())`.

### Adding an integration

1. Check the licence (`Projeto/IPT4/03_LICENCIAMENTO_MODS_FONTE.md`) — GPL-3.0
   sources may be adapted, some licences only permit clean-room reimplementation.
2. Create `Integration/<Name>/`, keeping the upstream `LICENSE` in that folder.
3. Route per-line vehicle/budget changes through `CachedTransportLineData`.
4. Wire it in `ImprovedPublicTransportMod.OnLevelLoaded` / `Deinit`, each in its own
   `try/catch` so one failure cannot abort the rest.
5. Add a toggle in `ModSetting` + `UI/CSLModsCommonOptionsPanel.cs`.
6. Add translation keys to **all** `Translations/*.txt`.

`Integration/*` compiles via a wildcard, so no `.csproj` edit is needed. Integrations
using their own `ThreadingExtensionBase` / `LoadingExtensionBase` (AutoLineColor,
ExpressBusServices) are discovered by the game directly — that is intentional, not
missing wiring.

---

## Absorbed mods

Compiled in; unsubscribe the standalone versions.

**Default configuration policy:** most absorbed integrations ship **off** by
default (Options > Integrations) - the mod installs with the minimum enabled,
so nothing changes behind your back on first launch. An integration is
switched on by default only once it's confirmed working well enough for
everyday use; as of 4.8.0 that applies to Intercity Bus Control and
Sub-Buildings Tabs. Commuter Destination and Train Display remain off by
default pending further work - see `CHANGELOG.md` for what's tracked.

Every author below is credited from the source itself — the Workshop item's
"Created by" field, or the copyright header in the licence file that shipped
with the code. Where a mod is a continuation of someone else's work, both are
named.

| Integration | Upstream mod | Author | Licence |
|---|---|---|---|
| `AdvancedStopSelection` | [Advanced Stop Selection Revisited](https://steamcommunity.com/sharedfiles/filedetails/?id=2862973068) | **macsergey**, continuing BloodyPenguin's original | GPL |
| `AutoLineBudget` | [Auto Line Budget 21](https://steamcommunity.com/sharedfiles/filedetails/?id=2349240408) · [source](https://github.com/jakeluba/AutoLineBudget21) | **snowie** | GPL-3.0 |
| `AutoLineColor` | [AutoLineColor Redux](https://steamcommunity.com/sharedfiles/filedetails/?id=1415090282) | **TaradinoC**, from Phil Scott's original AutoLineColor | MIT |
| `BetterBoarding` | [Better Train Boarding](https://steamcommunity.com/sharedfiles/filedetails/?id=2773460744) | **Vectorial1024** | MIT |
| `BetterBusStopPosition` | [Better Bus Stop Position](https://steamcommunity.com/sharedfiles/filedetails/?id=3491515535) | **llunak** | GPL |
| `CommuterDestination` | [Commuter Destination](https://steamcommunity.com/sharedfiles/filedetails/?id=2475986859) · [source](https://github.com/Jameskmonger/CSL-ShowCommuterDestination) | **jkm** (James Monger) | MIT |
| `ElevatedStopsEnabler` | [Elevated Stops Enabler Revisited](https://steamcommunity.com/sharedfiles/filedetails/?id=2862992091) | **macsergey** | GPL |
| `ExpressBusServices` | [Express Bus Services](https://steamcommunity.com/sharedfiles/filedetails/?id=2262054175) | **Vectorial1024** (Vincent Wong) | MIT |
| `FlightTracker` | [Flight Tracker](https://steamcommunity.com/sharedfiles/filedetails/?id=3033809468) | **Nyoko** | MIT |
| `IntercityBusControl` | [Intercity Bus Control](https://steamcommunity.com/sharedfiles/filedetails/?id=2499771767) · [source](https://github.com/bloodypenguin/Skylines-IntercityBusController) | **BloodyPenguin** | GPL |
| `MileageTaxiServices` | [Mileage Taxi Services](https://steamcommunity.com/sharedfiles/filedetails/?id=3492156582) | **Vectorial1024** | MIT |
| `PublicTransportUnstucker` | [Public Transport Unstucker](https://steamcommunity.com/sharedfiles/filedetails/?id=2774427140) | **Vectorial1024** | MIT |
| `RealisticWalkingSpeed` | [Realistic Walking Speed](https://steamcommunity.com/sharedfiles/filedetails/?id=1412844620) | **egi** (DaEgi01) | MIT |
| `SharedStopEnabler` | [Shared Stop Enabler](https://steamcommunity.com/sharedfiles/filedetails/?id=2096382380) · [source](https://github.com/CodeBardian/SharedStopEnabler) | **CodeBardian** | GPL-3.0 |
| `StopsAndStations` | [Stops & Stations](https://steamcommunity.com/sharedfiles/filedetails/?id=1776052533) | **dymanoid** | MIT |
| `SubBuildingsTabs` | [Sub-Buildings Tabs](https://steamcommunity.com/sharedfiles/filedetails/?id=608517757) · [source](https://github.com/bloodypenguin/Skylines-SubBuildingsTabBar) | **BloodyPenguin, AJ3D** | MIT |
| `TicketPriceCustomizer` | [Ticket Price Customizer](https://steamcommunity.com/sharedfiles/filedetails/?id=1393820309) | **BloodyPenguin** | GPL |
| `TrainDisplayUpdated` | [Train Display - Updated](https://steamcommunity.com/sharedfiles/filedetails/?id=3233229958) | **Will**, continuing [Asmape's Train Display Mod](https://steamcommunity.com/sharedfiles/filedetails/?id=2380878816) | GPL-3.0 |
| `HarmonyPatches/…/NormalizeFullwidthLineNamesPatch` | [Rescue Fullwidth Digits](https://steamcommunity.com/sharedfiles/filedetails/?id=1174585364) | **Gansaku** | — |

The licence column is the licence of the code **as absorbed**; the authoritative
copy lives in each `Integration/<Name>/LICENSE`. Three GPL-3.0 entries
(`AutoLineBudget`, `TrainDisplayUpdated`, `SharedStopEnabler`) are why IPT4 as a
whole is GPL-3.0.

`TaxiStandFix`, `SingleTrainTrackAI` and `StopStacker` are deliberately absent
from this table: unlike everything else above, none of the three are code
ports.

- `TaxiStandFix` is an original IPT4 implementation of the same idea as the
  standalone [Taxi Stand Fix](https://steamcommunity.com/sharedfiles/filedetails/?id=3712889232)
  mod - written fresh against IPT4's own Harmony conventions rather than
  adapted from its source - so there is no upstream licence to carry forward.
- `SingleTrainTrackAI` is a clean-room reimplementation of the concept behind
  [SingleTrainTrackAI](https://steamcommunity.com/sharedfiles/filedetails/?id=949504539)
  by **CoarzFlovv** (Workshop 949504539, no declared licence, no source read) -
  only the publicly documented idea (reserve a shared single-track segment for
  one direction at a time) was reimplemented, on this project's own
  Harmony-based patching instead of upstream's raw memory detour.
- `StopStacker` is a clean-room reimplementation of the concept behind
  [Stop Stacker](https://steamcommunity.com/sharedfiles/filedetails/?id=3751418194)
  by **ScratchyBald** (Workshop 3751418194, no declared licence, no source
  available) - written from the mod's public description only, reusing this
  project's own `BetterBusStopPosition` hook point. See
  `Integration/StopStacker/LICENSE.txt` for the documented scope reduction
  versus the original.

> **Attribution check, worth repeating if you absorb another mod:** several of
> these folders contain a copyright header for *AlgernonCommons*, the vendored
> UI framework — not for the mod's author. Flight Tracker's files carry
> algernon's copyright for that reason, while the mod is Nyoko's. Read the
> Workshop item's "Created by" field, not just the licence headers.

---

## Localization

24 mod translation files (`Translations/*.txt`, 447 keys each) plus 23 framework
locale files (`CSLModsCommonShared/Localization/Common/*.json`).

Two separate systems, which is easy to trip over:

- **Mod strings** — `Translations/<lang>.txt`, read by
  `TranslationFramework.LocalizationManager`.
- **Framework strings** (Version, Changelog, BETA, the language selector itself) —
  `Localization/Common/<locale>.json`, read by CSLModsCommon.

The **selector is populated from the framework's** locale list. A language with a
mod translation but no framework file is unreachable — that is why Bengali, Hindi,
Indonesian and Urdu were invisible until 4.3.3 despite being fully translated.

`FindLanguage` maps long ids to short filenames (`de-DE` → `de.txt`,
`zh-TW` → `zh-tw.txt`). **All 23** selector entries now have a full mod
translation - 447 keys each, same key set, verified programmatically.

`Localization/Common/TranslationStatus.json` feeds the percentage shown under the
language dropdown. It is a static file, not computed at runtime: if you add keys
without regenerating it, the panel will report a stale number while the
translations themselves are fine.

When adding a key, add it to **every** file. Use a literal `\n` for line breaks —
a real newline splits the entry and corrupts the parse, since the deserializer reads
one key per line.

---

## Steam Workshop release notes

The player-facing text for the Workshop item lives in [`Projeto-Steam/`](Projeto-Steam/):
the English description, the item metadata (title, tags, pinned discussion
topics, upload command) and the script that generates the upload `.vdf`.

English is canonical for everything player-facing on Steam. Two registers, same
release: the Workshop changenote states the symptom and what the player should
do about it; `CHANGELOG.md` states the root cause and the affected class.

## Repository layout note

Project documentation (phase plans, mod triage, dependency audit, current state) is
maintained in Portuguese under `Projeto/IPT4/` in the author's notes, outside this
repository. `06_ESTADO_ATUAL.md` is the living status document;
`07_AUDITORIA_DEPENDENCIAS.md` covers runtime dependencies.

---

## Core Features

### 🚌 Transport Line Panel

The public transport line info panel is extended with a new IPT control section:

- **Vehicle Count** — Manually add and remove vehicles on any line using the (+) or (-) buttons. The current vehicle count, as well as vehicles in the spawn queue, are displayed in real time.
- **Budget Control Mode** — Toggle between *Manual* (you control vehicle count directly) and *Budget* (vehicle count is governed by the line's budget slider, same as vanilla). Switching to Budget mode clears the spawn queue and applies the budget to existing lines immediately.
- **Unbunching per Line** — Enable or disable vehicle unbunching on individual lines independently of the global setting.
- **Vehicle Queue** — See how many vehicles are queued to spawn and clear them if needed.
- **Depot Selector** — Choose which depot serves a line from a drop-down; IPT automatically finds available depots for each transport type.
- **Line Length** — The total route length is displayed in the line panel.
- **Spawn Timer** — Shows the current vehicle spawn countdown for the line.
- **Hex Color Input** — Enter an exact hex color code for a line color, in addition to using the standard color picker.
- **Select Vehicle Types** — Opens the vehicle type selector for the line (see below).
- **Auto Show Line Info** — Optionally auto-opens the line info panel whenever a new line is created.

---

### 🎛️ Vehicle Type Selector

Control exactly which vehicle assets are allowed to run on each transit line:

- Browse all **available vehicles** for the line's transport type and DLC level.
- Move vehicles to the **selected list** to restrict the line to only those models.
- **Add All** / **Remove All** buttons for bulk changes.
- **Any Vehicle** mode restores vanilla behavior (any compatible vehicle may be used).
- Works with all transport types and custom vehicle assets from the workshop.

---

### 🔧 Vehicle Editor

Modify the stats of any public transport vehicle type directly in-game:

- **Passenger Capacity** — Increase or decrease the number of passengers the vehicle can carry.
- **Maintenance Cost** — Adjust the per-vehicle running cost.
- **Max Speed** — Change the top speed of the vehicle.
- **Engine on Both Ends** (trains) — Enable or disable bidirectional train engines to avoid needing to turn trains around at terminus stops.
- **Preview** — A rendered preview of the selected vehicle is shown while editing.
- The editor panel can be positioned at the **bottom** or **right** of the screen, or hidden entirely, from the Options panel.

---

### 🛑 Stop Info Panel

Clicking a stop node opens the IPT Stop Info Panel, which extends the vanilla stop window:

- **Stop Name** — Rename any stop; suggested names sourced from nearby streets and districts.
- **Passenger Statistics** — Current, last, and average boarding/alighting counts per stop visit.
- **Waiting Passengers** — Live count of citizens waiting at the stop.
- **Unbunching Toggle** — Enable or disable unbunching for this specific stop independently.
- **Sync Unbunching to Nearby Stops** — Apply the same unbunching state to all stops at the same station or interchange in one click.
- **Navigate Stops** — Previous / Next buttons jump the camera to adjacent stops along the line.
- **Delete Stop** — Remove a stop (hold Alt to enable; use with caution).

---

### 🔀 Unbunching Control

Fine-tune how aggressively the game tries to space out vehicles on a line:

- **Aggression Slider** (0–52) — 52 matches vanilla aggression; lower values reduce the effect; 0 disables it.
- **Per-Line Toggle** — Enable or disable unbunching on individual lines from the line panel or stop panel.
- **Spawn Interval** — Control the minimum time between vehicle spawns on a line.

---

### 🗑️ Lines Deletion Tool

Bulk-delete all lines of a given transport type from the Options panel:

- Select one or more transport categories (bus, trolleybus, tram, train, metro, monorail, ferry, helicopter, blimp, sightseeing bus).
- Confirm with a dialog before deletion to avoid accidents.
- Only available while a city is loaded.

---

## Integration details

Feature-level documentation for the absorbed mods, carried over from the IPT3 README.

### Advanced Stop Selection
Smarter tools for managing where vehicles can stop and pick up passengers at stations.

### Auto Line Color Redux
Automatically assigns colors and names to new transit lines based on route characteristics, keeping your transit map organized and visually appealing.

### Better Bus Stop Position (BBSP)
Controls how buses position themselves at stops, moving them forward instead of centered thus allowing a second bus to pull in behind.

### Better Train Boarding
- Passengers are assigned to the nearest available carriage/vehicle segment and boarding is buffered to avoid strange 'stuck passenger' behavior
- Improves consistency across transport modes and avoids passenger shuffling at busy stops
- Applied to:
  - BusAI (buses + sightseeing/intercity bus)
  - TrolleybusAI
  - TramAI
  - PassengerTrainAI (metro/trains/monorail)
  - PassengerHelicopterAI
  - PassengerBlimpAI
  - PassengerFerryAI

### Commuter Destination
Adds a "Destinations" button to the transit stop info panel showing where the
citizens currently waiting there are actually headed — useful for spotting a stop
that mostly serves one distant employer or venue versus a general-purpose one.
Read-only: it never writes simulation state, only reads existing citizen paths.

### Elevated Stops Enabler
Build transit stops on elevated roads, opening up new urban layouts.

### Express Bus Services
Buses and trams can depart early if there are very few passengers, keeping schedules tight
- **Minibus Mode**: Small-capacity buses can skip if load is very light, reducing unnecessary wait times
- **Self-Balancing**: The system automatically redeploys vehicles to busy stops and helps keep service balanced across the route
- **Middle-Stop Deployment**: Allows self-balancing to redeploy buses to busy intermediate stops along a route, not just terminus stops — useful for catching congestion mid-route
- **Express Tram Services**: Trams get smarter stopping decisions to reduce wait times

### Flight Tracker
Track planes with a dedicated panel attached to the plane stand building info window. Shows flight status and schedules at a glance.
- **Fix**: Panel is now correctly attached to the plane stand window instead of simply spawning there.
- **Fix**: Escape key now properly closes the Flight Tracker panel along with building info window.

### Intercity Bus Control
Fine-tune intercity bus behavior with a toggle on regular bus stations to allow Intercity Buses at them. (Sunset Harbor DLC).

- **Supported Hubs**: Adds intercity bus support to all multi-modal bus hubs:
  - Ferry-Bus Hub / Ferry and Bus Exchange Stop
  - Harbor-Bus-Monorail Hub / Harbor-Bus Hub
  - Monorail-Bus Hub
- **Note**: The Bus-Train-Tram Hub uses its native intercity trains toggle and is left unchanged to avoid transport mode conflicts (only one intercity toggle per building is supported by the game UI).
- **Per-terminal accept toggle**: each patched bus terminal has its own "accept intercity buses" checkbox, persisted per building rather than shared across the city.
- **Vehicle capacity mode** (Options > Integrations): `Disabled` keeps the effectively-uncapped terminal capacity this mod has always used, `Intermediate` applies a moderate fixed cap, `Realistic` leaves the terminal's own vanilla prefab capacity untouched. Existing saves default to `Disabled` so upgrading never silently shrinks a terminal out from under you.

### Mileage Taxi Service
Taxis now charge per mile/kilometer traveled (based on IPT 'Show speed in' setting) instead of straight line distance from start to finish points, making them a realistic urban transportation option (After Dark DLC).

### Realistic Walking Speed
Enables realistic pedestrian and cycling speeds in your city, controllable from the Options Panel:

**Available Modes:**
- **Standard**: Standard game walking and cycling speeds (default Cities: Skylines behavior)
- **Realistic**: Applies realistic slowed down walking speeds based on citizen age and gender, and reduces cycling speeds uniformly.

**What Changes with Realistic Mode:**
- **Walking Speed**: Citizens walk at realistic speeds (0.54–0.82 m/s) that vary by age and gender, replacing uniform vanilla speeds
- **Cycling Speed** (After Dark DLC only): All cyclists are slowed uniformly; cycling travel times become more significant regardless of cyclist profile
- **Animation Sync**: Walking and cycling animations adjust to match the new movement speeds for realism

**Gameplay Impact:**
- Realistic mode makes pedestrian and cycling connections between transit stops more time-consuming, emphasizing good transit coverage
- Cycling becomes a realistic alternative to transit for shorter distances, but longer trips favor public transport
- Citizens move more realistically overall, affecting passenger boarding times and transfer experiences

### Shared Stop Enabler
Allows more than one transit stop type to share the same road segment (for example
a bus stop and a tram stop on the same street), instead of vanilla's one-stop-per-
segment restriction. **Off by default** — see `Integration/SharedStopEnabler/LICENSE.txt`
for the reduced scope of what was ported.

### Single Train Track AI
Reserves a single-track rail segment for one direction of travel at a time, so
two trains sharing a bidirectional track no longer risk colliding. Trains
already standing on a shared segment keep their hold; a train approaching one
held by another train brakes to a stop instead of entering. Clean-room
reimplementation - see [Absorbed mods](#absorbed-mods).

### Stop Stacker
When more than one vehicle serving the same line converges on a stop, only the
lead vehicle uses the normal vanilla stop position - each vehicle behind it
gets its own berth further back along the same lane instead of queuing
single-file as ordinary blocked traffic, so following buses can load/unload
passengers without waiting. Bus and trolleybus only; falls back to vanilla
behavior if the lane is too short to fit another berth. Clean-room
reimplementation - see [Absorbed mods](#absorbed-mods).

### Stops and Stations
Adds a waiting passenger limiter to all transit stops in Options Panel:
- Controls maximum passenger overflow at busy stops
- Prevents unrealistic passenger accumulation that can cause performance issues
- Applies universally to each transport type

### Sub-Buildings Tabs
Adds a tab bar to any building's info panel that has sub-buildings — not just
airports, any multi-building service hub — so each sub-building's own panel is one
click away instead of requiring you to find and click it on the map directly.

### Taxi Stand Fix
Idle taxis (no passenger, no destination already assigned) head toward the
nearest taxi stand instead of wandering at random, using the same vanilla
dispatch path (`TaxiAI.SetTarget`) the game itself uses for normal fares. See the
note under [Absorbed mods](#absorbed-mods) — this one is an original
implementation, not a port.

### Ticket Price Customizer
Integrated directly into the Economy Panel with its own tab alongside Budget, Taxes, Loans, and Investments.

Set ticket prices **independently for each transport type**:
- **Buses**
- **Intercity Buses**
- **Sightseeing Buses**
- **Trolleybuses**
- **Trams**
- **Trains**
- **Metros**
- **Monorail**
- **Taxis** (charged per actual distance traveled via Mileage Taxi Services)
- **Cable Cars**
- **Ships**
- **Ferries**
- **Airplanes**
- **Blimps**
- **Helicopters**

**Key Features:**
- **Price Control**: Adjust individual transport fares from 0% (free) to 250% of base cost
- **Day/Night Support** : Set different prices for night hours — great for simulating night-shift premium fares
- **Smart Policy Integration**:
  - When **Free Public Transport policy** is active in a district, all transport becomes free regardless of your slider settings
  - When **High Ticket Prices policy** is active, fares automatically increase by 25% on top of your slider settings
  - **Note**: Taxis don't respond to policies — they always charge per distance traveled
- **Demand Balancing**: Higher prices naturally reduce passenger demand & lower prices induce demand, simulating realistic transit economics

**Free services (never charge):**
- Walking tours
- Hot Air Balloons
- Service vehicles (post vans, garbage trucks, etc.)

---

## How Prices Affect Your City

### Demand Impact
When you raise ticket prices, fewer people will use that route. This is realistic but can hurt revenue if prices get too high.

**Example:**
- Bus price at 100% (default): Good passenger count, steady revenue
- Bus price at 150%: Moderate passenger decrease, increased revenue per trip
- Bus price at 250% (maximum slider): Significant ridership drop; only works for premium routes

**Tip:** The slider maxes out at 250%. There's a sweet spot around 100%–150% for most routes. Premium routes (intercity, airports) can sustain 180%–250%.

### How Policies Work

**Free Public Transport** (available in the Policies menu):
- Overrides all your ticket price settings
- Makes all transport FREE in the affected district (except taxis)
- Great for promoting transit usage in struggling areas
- Revenue stops, but ridership skyrockets

**High Ticket Prices** (available in the Policies menu):
- Increases all fares by 25% on top of your slider settings (except taxis)
- Example: Your slider set to 150% + Policy active = 187.5% effective price
- Can exceed over the cap: Bus price at 250% slider + policy = 312.5% effective (250% × 1.25): Severely reduced ridership, high per-trip revenue

**Taxi Exception:** Taxis always charge per kilometer/mile and ignore both policies completely.

---

### Day/Night Prices

Different fares for day hours vs. night hours:
- **Day Mode**: Standard prices
- **Night Mode**: Can be cheaper or more expensive

This aligns with the game’s built-in day/night cycle and is used to automatically switch pricing when the time transition occurs.
**RealTime Mod Compatibility:** This feature works seamlessly with RealTime mods. Ticket prices will automatically transition at whatever times RealTime sets for day/night, including dynamic seasonal sunrise/sunset adjustments (if enabled). No additional configuration needed — they work together automatically.
---

## DLC Compatibility

Most features work with just the base game. Features that require DLC will be unavailable if you do not own it.

---

## Troubleshooting

### "Prices don't seem to be working"
- Check if a **Free Public Transport policy** is active — it overrides all prices
- Make sure you're adjusting the right transport type slider
- Save your game and reload to confirm changes are persisted

### "My buses/trams are still bunching up"
- Increase the **Unbunching Aggression** slider
- Make sure unbunching is enabled for the specific line (check line details panel)
- Try the **Express Bus Services** mode for buses or **Express Trams Services** for trams instead.

### "I can't see a particular transport type slider"
- You might not own the required DLC
- Some DLCs add new transport types with their own sliders

---

## Credits & licence

IPT4 is licensed under **GPL-3.0** ([`LICENSE`](LICENSE)). Two of the absorbed
mods are GPL-3.0 (`AutoLineBudget`, `TrainDisplayUpdated`), so the combined work
must be GPL-3.0 too - this is an obligation, not a preference.

### Lineage of this mod

| | Author |
|---|---|
| Improved Public Transport (2015-2016) | **DontCryJustDie** - Workshop item no longer listed |
| [Improved Public Transport 2](https://steamcommunity.com/sharedfiles/filedetails/?id=928128676) (2017-2023) | **BloodyPenguin** |
| [Improved Public Transport 3](https://steamcommunity.com/sharedfiles/filedetails/?id=3690061052) | **Madisonian** ([source](https://github.com/TheMadisonian/ImprovedPublicTransport3)) |
| IPT4 | this fork |

### Absorbed work

Per-mod credit, Workshop link and licence are in the
[Absorbed mods](#absorbed-mods) table above - that is the authoritative list.
The people whose work is compiled into this assembly:

**Asmape · BloodyPenguin · DaEgi01 (egi) · dymanoid · Gansaku · llunak ·
macsergey · Nyoko · Phil Scott · snowie · TaradinoC · Vectorial1024 · Will**

Each integration keeps its upstream `LICENSE` in its own folder under
`Integration/`. If you absorb another mod, keep doing that - the licence file is
the record of permission, and deleting it breaks the chain.

### Vendored frameworks

| Framework | Author | Licence | How it is included |
|---|---|---|---|
| [AlgernonCommons](https://github.com/algernon-A/AlgernonCommons) | **algernon** (K. Algernon A. Sheppard) | MIT | git submodule |
| [CSLModsCommon](https://github.com/Mbyron26/CSLModsCommon) | **Mbyron26** | MIT | vendored source, with local patches |
| [Json.NET for Unity](https://github.com/SaladLab/Json.Net.Unity3D) | Newtonsoft / SaladLab | MIT | NuGet, shipped in the mod folder |

Thank you to all of the above. IPT4 is mostly their work, rearranged so the
pieces stop stepping on each other.
