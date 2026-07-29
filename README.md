# Improved Public Transport 4 (IPT4)

Improved Public Transport 4 is a practical continuation of the IPT/IPT2/IPT3 mod line for **Cities: Skylines**. It is aimed first at regular players who want better control over public transport without juggling a pile of separate mods.

In short, IPT4 helps you run lines more clearly, reduce bad vehicle spacing, get better information about stops and vehicles, and tune transport behavior with simpler in-game controls.

## What IPT4 improves

For most players, IPT4 mainly improves these areas:

- clearer control over how many vehicles each line uses
- the ability to restrict which vehicle models may run on a line
- tools to reduce buses, trams, and other vehicles bunching together
- better stop, line, and vehicle information in the game UI
- fare customization features when enabled
- quality-of-life features consolidated from earlier related mods

## Requirements

- **Cities: Skylines (the original game)**
- **Harmony** is required for the patching-based features used by the mod
- **After Dark** is required for features tied to that DLC, such as taxi-related behavior and cycling-related adjustments where applicable
- other DLCs are optional, but transport types and features tied to those DLCs only appear if you own them

## Installation

### Manual installation

1. Close the game.
2. Download the release package for IPT4.
3. Extract the included `ImprovedPublicTransport4` folder into:
   - `C:\Users\<your-user>\AppData\Local\Colossal Order\Cities_Skylines\Addons\Mods\`
4. Start the game and enable the mod in Content Manager if needed.

### Updating an existing installation

1. Close the game.
2. Replace the old `ImprovedPublicTransport4` folder with the new one.
3. Start the game again.
4. Review the mod options before continuing an important save.

## Simple setup for regular players

If you just want a better public transport experience with less micromanagement, this is the recommended path.

### 1. Start with line-level controls

Open a transport line and use the IPT4 panel to:

- add or remove vehicles manually
- see active vehicles and queued vehicles
- change the depot when that option is available

### 2. Use budget control if you want less manual work

Budget mode lets the game manage vehicle counts through line budget behavior. It is useful if you do not want to tune every line by hand all the time.

### 3. Turn on vehicle unbunching if lines are clumping

If multiple vehicles from the same line keep arriving together, raise the unbunching setting. In many cities, that already improves service spacing a lot.

### 4. Check problematic stops

Clicking a stop gives you information that helps you find bottlenecks:

- waiting passengers
- boarding and alighting activity
- time until passengers give up waiting
- navigation to the previous and next stop

### 5. Only restrict vehicle types when you need to

If a line is using poor vehicle choices, open the vehicle selection UI and limit the line to the models you actually want there.

## Recommended starting configuration

For most players, a safe starting point is:

- budget control: enabled if you prefer less micromanagement
- unbunching: enabled
- vehicle editor: leave at default until you understand the impact
- ticket prices: leave at default for your first test
- stop and station passenger limits: only adjust if you are seeing overcrowding or odd behavior

## Main features

### Line control

Manage vehicle counts, spawn queues, allowed vehicle types, and line-level spacing behavior.

### Stop information

Get a faster read on where your network is failing and which stops are under pressure.

### Vehicle editor

Adjust capacity, cost, and speed for specific use cases. Most players can ignore this at first.

### Ticket price tools

When enabled, these let you adjust pricing by transport type to influence demand and revenue.

## Compatibility for players

- IPT4 tries to consolidate features that used to require separate mods.
- That reduces overlap, but it does not eliminate conflicts with other mods that patch the same transport systems, panels, or Harmony targets.
- If you already use older mods that do the same jobs, avoid keeping duplicate functionality enabled.

## Quick troubleshooting

### The mod is enabled, but something does not seem to change

- make sure the mod is actually enabled in Content Manager
- restart the game after replacing the mod folder
- test in a separate save before assuming a feature is broken

### Vehicles are still bunching together

- increase the unbunching aggression setting
- confirm the specific line has the relevant option enabled
- check whether your city has extreme bottlenecks forcing every vehicle to stop at the same point

### The wrong vehicles are being used on a line

- open vehicle type selection
- remove models that should not operate there
- confirm the chosen depot provides compatible vehicles

### Another mod appears to conflict with IPT4

- disable mods that alter the same transport features
- test IPT4 alone or with a minimal mod set
- re-enable other mods one by one if you need to isolate the conflict

## For advanced users

This section is intentionally smaller. IPT4 is still meant to be practical first.

### Detailed configuration

Advanced users can explore:

- deeper unbunching behavior tuning
- passenger limits by stop and station type
- fare customization by transport mode
- vehicle editor changes for capacity, cost, and speed
- integrated feature sets carried over from earlier IPT generations

### Compatibility notes

Take extra care with mods that:

- change line vehicle counts
- alter boarding or unloading logic
- modify transport pricing
- apply Harmony patches to the same game routines

### Advanced troubleshooting

If behavior still looks wrong:

- test with fewer active mods
- review logs and conflict messages
- compare behavior in a fresh save versus an older save
- confirm that required DLCs and Harmony are actually present in the environment you are using

## Credits

IPT4 builds on years of community modding work for Cities: Skylines. This repository preserves historical credit to earlier maintainers and to the source integrations that shaped the current codebase.

Historical authors, projects, and source code bases referenced by this repository include:

- [DontCryJustDie](https://steamcommunity.com/id/DontCryJustDie)
- [BloodyPenguin](https://github.com/BloodyPenguin)
- [Improved Public Transport 2](https://github.com/BloodyPenguin/ImprovedPublicTransport2)
- [Algernon](https://github.com/algernon-A)
- [AlgernonCommons](https://github.com/algernon-A/AlgernonCommons)
- [AutoLineBudget 21](https://github.com/jakeluba/AutoLineBudget21)
- [Phil Scott / Auto Line Color Redux](https://github.com/phillipscott)
- [Nyoko](https://github.com/NyokoDev)
- [egi](https://github.com/eg2dl)
- [llunak](https://github.com/llunak)
- [Vectorial1024](https://github.com/Vectorial1024)
- [macsergey](https://github.com/macsergey)
- [dymanoid](https://github.com/dymanoid)
- [TaradinoC](https://github.com/Taradino)

## License

This repository is distributed under the **GNU General Public License v3.0**, which matches the copyleft obligations already present in GPL-licensed source integrated into the project.

Important details:

- component-specific notices remain in their original directories
- MIT-licensed parts such as [AlgernonCommons](https://github.com/algernon-A/AlgernonCommons) and several integrated modules retain their own copyright and permission notices
- GPL-licensed integrated modules also keep their original license texts in place
- this top-level `LICENSE` file does **not** remove or replace any more specific notice already present in the repository

## Technical documentation

This README is not developer documentation. If technical material grows, architecture notes, build instructions, and deeper implementation documentation can live in a separate area of the repository in the future.