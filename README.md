# Improved Public Transport 4

**Version 4.3.5** (BETA channel) · Cities: Skylines 1 · targets 1.21.1-f9

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

| Integration | Upstream |
|---|---|
| `AdvancedStopSelection` | Advanced Stop Selection Revisited |
| `AutoLineBudget` | AutoLineBudget 21 (GPL-3.0) |
| `AutoLineColor` | Auto Line Color Redux |
| `BetterBoarding` | Better Train Boarding |
| `BetterBusStopPosition` | Better Bus Stop Position |
| `ElevatedStopsEnabler` | Elevated Stops Enabler Revisited |
| `ExpressBusServices` | Express Bus Services |
| `FlightTracker` | Flight Tracker |
| `IntercityBusControl` | Intercity Bus Control |
| `MileageTaxiServices` | Mileage Taxi Services |
| `PublicTransportUnstucker` | Public Transport Unstucker |
| `RealisticWalkingSpeed` | Realistic Walking Speed |
| `StopsAndStations` | Stops and Stations |
| `TicketPriceCustomizer` | Ticket Price Customizer |
| `TrainDisplayUpdated` | Train Display - Updated (GPL-3.0) |
| `HarmonyPatches/…/NormalizeFullwidthLineNamesPatch` | Rescue Fullwidth Digits |

---

## Localization

19 mod translation files (`Translations/*.txt`, ~310 keys each) plus 23 framework
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
`zh-TW` → `zh-tw.txt`). **18 of 23** selector entries have a full mod translation;
`cs`, `nl`, `sk`, `th`, `tr` fall back to English.

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

### Stops and Stations
Adds a waiting passenger limiter to all transit stops in Options Panel:
- Controls maximum passenger overflow at busy stops
- Prevents unrealistic passenger accumulation that can cause performance issues
- Applies universally to each transport type

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

IPT4 is licensed under **GPL-3.0** ([`LICENSE`](LICENSE)); it absorbs GPL-3.0 code and
so must remain GPL-3.0.

Lineage: **IPT** (DontCryJustDie, 2015-2016) -> **IPT2** (BloodyPenguin, 2017-2023)
-> **IPT3** (TheMadisonian) -> **IPT4** (this fork).

Thanks to the authors whose work is absorbed here, released under MIT or GNU
licences: Dontcryjustdie, BloodyPenguin, Nyoko, egi, llunak, Vectorial1024,
macsergey, dymanoid, TaradinoC, algernon, Mbyron26.

Each integration keeps its upstream `LICENSE` in its own folder under
`Integration/`. Vendored frameworks: [AlgernonCommons](https://github.com/algernon-A/AlgernonCommons)
(submodule) and [CSLModsCommon](https://github.com/Mbyron26/CSLModsCommon) (MIT,
vendored source with local patches).
