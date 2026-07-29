# ImprovedPublicTransport 3

**Version 3.0.1** | Last Updated: March 2026

## Overview

ImprovedPublicTransport 3 (IPT3) is the continuation of the classic IPT and IPT2 mods, rebuilt for the Race Day update and fully compatible with More Vehicles Renewed. It gives you granular control over every aspect of public transportation — vehicle counts, vehicle types, stop behavior, boarding, ticket prices, unbunching, and more — across all transport modes in Cities: Skylines.

IPT3 also encompasses a suite of formerly standalone mods that have been integrated directly, eliminating the need to manage them separately.

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

## What's New in Version 3.0

### ✨ What's New Dialog System
The mod now displays helpful notifications when major features are added or changed. The dialog appears once per version and can be dismissed. If you click "Don't Show Again," the reminder won't appear again until a new version is released.

## Mod Integrations

The following mods have been built directly into IPT3. You do not need — and should not use — the separate standalone versions alongside IPT3.

---

#### Advanced Stop Selection
Smarter tools for managing where vehicles can stop and pick up passengers at stations.

### 🎨 **Auto Line Color Redux
Automatically assigns colors and names to new transit lines based on route characteristics, keeping your transit map organized and visually appealing.

### 🎯 **Better Bus Stop Position (BBSP)
Controls how buses position themselves at stops, moving them forward instead of centered thus allowing a second bus to pull in behind.

#### Better Train Boarding
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

#### Elevated Stops Enabler
Build transit stops on elevated roads, opening up new urban layouts.

#### Express Bus Services
Buses and trams can depart early if there are very few passengers, keeping schedules tight
- **Minibus Mode**: Small-capacity buses can skip if load is very light, reducing unnecessary wait times
- **Self-Balancing**: The system automatically redeploys vehicles to busy stops and helps keep service balanced across the route
- **Middle-Stop Deployment**: Allows self-balancing to redeploy buses to busy intermediate stops along a route, not just terminus stops — useful for catching congestion mid-route
- **Express Tram Services**: Trams get smarter stopping decisions to reduce wait times

#### Flight Tracker
Track planes with a dedicated panel attached to the plane stand building info window. Shows flight status and schedules at a glance.
- **Fix**: Panel is now correctly attached to the plane stand window instead of simply spawning there.
- **Fix**: Escape key now properly closes the Flight Tracker panel along with building info window.

#### Intercity Bus Control
Fine-tune intercity bus behavior with a toggle on regular bus stations to allow Intercity Buses at them. (Sunset Harbor DLC).

- **Supported Hubs**: Adds intercity bus support to all multi-modal bus hubs:
  - Ferry-Bus Hub / Ferry and Bus Exchange Stop
  - Harbor-Bus-Monorail Hub / Harbor-Bus Hub
  - Monorail-Bus Hub
- **Note**: The Bus-Train-Tram Hub uses its native intercity trains toggle and is left unchanged to avoid transport mode conflicts (only one intercity toggle per building is supported by the game UI).

#### Mileage Taxi Service
Taxis now charge per mile/kilometer traveled (based on IPT 'Show speed in' setting) instead of straight line distance from start to finish points, making them a realistic urban transportation option (After Dark DLC).

#### Realistic Walking Speed
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

#### Stops and Stations
Adds a waiting passenger limiter to all transit stops in Options Panel:
- Controls maximum passenger overflow at busy stops
- Prevents unrealistic passenger accumulation that can cause performance issues
- Applies universally to each transport type

### 🎫 **Ticket Price Customizer** — Control How Much Transit Costs
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

## Version History

### 3.0.0 (March 2026) — Current
Complete rebuild of IPT2 for the Race Day update, with many standalone transport mods absorbed.
- ✨ What's New notification system
- ✅ Core transport line panel, vehicle type selector, vehicle editor, and stop info panel carried forward from IPT/IPT2
- ✅ All mod integrations verified against Cities: Skylines API source code
- ✅ All integrated settings properly save to XML and persist to save games
- ✅ Compatibility with More Vehicles Renewed for Race Day
- 🚍 Added Intercity Buses to the Vehicle Editor
- 🛠 Fixed Deinit early-return bug that left Harmony patches active across game sessions
- 🛠 Fixed Flight Tracker window positioning and Escape key handling
- 🛠 Switched from unsafe IL transpiler override to safe postfix mode with config toggle for BBSP
- 🛠 Fixed Intercity Bus Control to work on all applicable bus transport hubs except Bus-Train-Tram Hub; the game only supports one intercity setting per hub and train hubs have 'Allow Intercity Trains' by default.

### IPT2 (BloodyPenguin, 2017–2023)
Fixed and continued the original IPT after it was abandoned. Added Harmony, improved Vehicle Selection UI, and maintained compatibility through game updates up to 1.16.1-f2.

### IPT (DontCryJustDie, 2015–2016)
The original mod. Introduced vehicle count control, vehicle type selection, the Vehicle Editor, the Stop Info Panel, per-line unbunching configuration, and depot management to Cities: Skylines.

---

## Credits & License
Special thanks to all the authors who made code available under MIT or GNU license: Dontcryjustdie, BloodyPenguin, Nyoko, egi, llunak, Vectorial1024, macsergey, dymanoid, TaradinoC. 

Individual mod integratons include original LICENSE in their respective folders in Integration folder.

---

## Support
For issues, feedback, or feature requests, please visit the mod's community page on the Steam Workshop.



