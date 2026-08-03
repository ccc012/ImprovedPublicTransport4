# Intercity Bus Control Integration Changelog

## Unreleased

### Documented - converted stations default OFF (native SH terminals default ON)

See root `CHANGELOG.md` under `[Unreleased]` for the full writeup. Summary:
`StationPatcher` puts every regular bus station it converts to intercity
service into `PrefabsDefaultReject`, so `IntercityAcceptanceState.DefaultAccepts`
returns `false` for those until the player opts in via the "Allow Intercity
Buses" checkbox; native Sunset Harbor intercity terminals default `true`
(matching vanilla). The upstream RegionalBuses/Intercity Bus Controller mod
enabled every converted stop by default - this integration does not, and no
documented crash/instability tied to that specific default was found in
project history to explain the divergence. Left as-is (not reverted, in case
the original off-by-default choice was guarding against something never
written down), but now called out in the checkbox tooltip itself
(`CITYSERVICE_ACCEPTINTERCITYBUSES_TOOLTIP`) so it's no longer a silent
difference from the original mod.

## v3.0.0 - IPT3 Integration Major Revision (2026-03-22)

### Overview
This is a comprehensive refactoring of the Intercity Bus Control integration from its original mod. The core functionality is preserved, but adapted for IPT3's architecture and enhanced to support all bus station types without hardcoding.

### Original RegionalBuses Design (Baseline)
RegionalBuses (https://github.com/bloodypenguin/Skylines-IntercityBusController) provided:
- **NetInfoPatches:** Hardcoded assignment of "Intercity Bus Line" to "Bus Station" and "Monorail Bus Hub"
- **BuildingInfoPatches:** Set up Intercity Bus class and vehicle capacity on compatible stations
- **CityServiceWorldInfoPanelPatches:** Intercity bus checkbox visible based on transport type detection
- **OnCreated() timing:** Patches applied before prefabs load (early in game init)

### IPT3 Adaptation Challenges
1. **Timing mismatch:** IPT3 calls `PatchAll()` in `OnLevelLoaded()` (after prefabs), so Harmony postfixes never fire for vanilla/workshop assets
2. **Hardcoded names:** Only "Bus Station" and "Monorail Bus Hub" were patched; ferry-bus, train-bus, harbor-bus variants missed
3. **Station discrimination:** Couldn't distinguish between a regular station we patched vs. a native intercity station (both have same ItemClass after patching)
4. **Coupling:** Had separate LoadingExtension; now integrated directly into IPT3's main mod flow

### Fixed (v3.0.0)
- **Prefab timing:** Added `StationPatcher.PatchStations()` called after `Patcher.PatchAll()` in `ImprovedPublicTransportMod.OnLevelLoaded()`. Retroactively patches all loaded BuildingInfo prefabs at game start.
- **Toggle not appearing on all stations:** Now shows on all bus-containing stations (Ferry-Bus, Train-Bus, Harbor-Bus, Monorail-Bus) and mods without hardcoding.
- **Hardcoded names removed:** All three patches now use semantic detection (transport type XOR check) instead of name lookups.
- **Station discrimination:** Introduced `StationPatcher.PatchedBuildingNames` HashSet to track buildings explicitly patched, allowing `UpdateBindingsPatch` to exclude native intercity stations.
- **Transport guard removal:** Deleted `!ships` and `!intercityTrains` guards. Mixed hubs (ferry+bus, train+bus) now correctly receive intercity bus support while vanilla intercity stations remain excluded.

### Added
- **`StationPatcher` class:** 
  - `PatchStations()` - Iterates all loaded prefabs, applies intercity bus to regular stations not already intercity
  - `PatchedBuildingNames` - Tracks which buildings this mod patched (excludes native intercity stations)
  - `Reset()` - Clears state on level unload
- **`StationPatcher.Reset()` call chain:** `BuildingInfoPatches.Reset()` now calls `StationPatcher.Reset()`

### Changed
- **Detection logic:** Unified across all three patches. A station qualifies if:
  - Exactly one transport slot is PublicTransportBus (XOR check)
  - Not already fully configured as intercity (guard: `m_transportLineInfo == null` OR `name == "Intercity Bus Line"`)
- **Discrimination logic:** Toggle visibility now depends solely on `PatchedBuildingNames` membership, not `m_transportLineInfo` name checking
- **`BuildingInfoPatches/InitializePrefabPatch`:**
  - Removed hardcoded name filters
  - Called `StationPatcher.PatchedBuildingNames.Add()` after successful patch
  - Changed gate from `!ships && !intercityTrains` to just `(intercityBus1 ^ intercityBus2)`
- **`NetInfoPatches/InitializePrefabPatch`:**
  - Removed hardcoded `PrefabCollection<BuildingInfo>.FindLoaded("Bus Station"/"Monorail Bus Hub")` calls
  - Replaced with loop over all loaded BuildingInfo using same XOR detection
- **`UpdateBindingsPatch`:**
  - Changed visibility check from `m_transportLineInfo?.name == IntercityBusLine` to `PatchedBuildingNames.Contains(info.name)`
  - Removed `!ships && !intercityTrains` gate (now only gated on PatchedBuildingNames)

### Technical Details
- **m_transportInfo vs m_transportLineInfo:**
  - `m_transportInfo` = TransportInfo (vehicle type, e.g., "Intercity Bus" prefab)
  - `m_transportLineInfo` = NetInfo (road/route network, e.g., "Intercity Bus Line" asset)
- **ItemClass patching:** Setting `info.m_class = intercityBusClass` (Level3 Bus) is required for game routing; this marks a station as intercity at the class level (needed for pathfinding)
- **Already-intercity detection:** A station is skipped from patching if `m_transportLineInfo.name == "Intercity Bus Line"` AND `m_maxVehicleCount > 0`
- **Native vs. patched distinction:** Only way to distinguish is via `PatchedBuildingNames` set; class and capacity fields are identical after patching

### Compatibility
- ✅ Supports all vanilla bus stations and multi-modal hubs (Ferry-Bus, Harbor-Bus, Monorail-Bus, etc.) except Bus-Train-Tram
- ✅ Automatically detects and patches modded station types (no name registration needed)
- ✅ Does not interfere with native intercity train toggles or vanilla train/ferry stations
- ✅ Correctly excludes native Intercity Bus Stations/Hubs (never adds toggle)
- ✅ Requires Sunset Harbor DLC for intercity bus item class and vehicles
