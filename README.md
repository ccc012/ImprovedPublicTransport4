# Improved Public Transport 4 (IPT4)

Improved Public Transport 4 is a practical continuation of the IPT / IPT2 / IPT3 mod line for **Cities: Skylines**. It is aimed first at regular players who want better control over public transport without juggling several separate mods.

In everyday use, IPT4 helps you manage lines more clearly, reduce vehicle bunching, inspect stops and vehicles more easily, and adjust transport behavior through simpler in-game controls.

If you are a regular player, the short version is this: IPT4 is a transport management mod that tries to make public transit easier to run and easier to understand.

## Overview

IPT4 focuses on the parts of public transport that usually become painful in larger cities:

- line management
- vehicle assignment
- stop and station information
- spacing and bunching control
- fare-related adjustments where enabled
- consolidating useful quality-of-life features from earlier IPT releases

The mod is designed to be useful for casual play first. Advanced controls exist, but you do not need to use them to get value from the mod.

## What IPT4 does today

Current features center on practical transport management:

- clearer control over how many vehicles each line uses
- the ability to limit which vehicle models may run on a line
- tools to reduce buses, trams, and other vehicles arriving in clumps
- better information for lines, stops, and vehicles inside the game UI
- fare customization features where the relevant option is enabled
- quality-of-life features carried over from earlier related mods

## Planned and ongoing work

This fork follows the same general direction as the original IPT line: keep the mod useful for players, then improve it carefully without turning it into a pile of unrelated systems.

The safest way to track future work is through releases and repository issues. That is where compatibility updates, cleanup, and feature changes should appear first.

## Requirements

### Base game

- **Cities: Skylines** for PC
- This repository is for the original game, not the sequel

### Harmony

- **Harmony** is required for the patch-based parts of the mod
- If Harmony is missing, some mod functions will not work correctly

### DLCs

Some IPT4 behavior depends on DLC content, especially when the game exposes transport types or mechanics only through those expansions.

- **After Dark** is required for features tied to taxi-related behavior and cycling-related adjustments where applicable
- other DLCs are optional, but transport types and features tied to those DLCs only appear if you own them

### Practical note

If you are unsure whether a feature should appear, check whether the relevant transport type exists in your current game setup. IPT4 cannot surface game content that the base game or a missing DLC does not provide.

## Installation

There are two common ways to install IPT4.

### Manual installation

1. Close the game.
2. Download the release ZIP from the GitHub Releases page.
3. Extract the contents into:
   - `C:\Users\<your-user>\AppData\Local\Colossal Order\Cities_Skylines\Addons\Mods\ImprovedPublicTransport4\`
4. Start the game.
5. Enable the mod in Content Manager if it is not already enabled.

### Install from a release

If you are updating an existing copy, the release ZIP is the safest path because it contains only the files meant to be installed.

1. Close the game.
2. Download the latest release asset for the version you want.
3. Replace the existing `ImprovedPublicTransport4` folder contents with the files from the archive.
4. Start the game again.
5. Open the mod options and confirm your settings before loading an important save.

### Updating from an older version

1. Close the game.
2. Back up your current mod folder if you want a fallback copy.
3. Replace the old files with the new release files.
4. Launch the game and verify the mod appears normally.
5. Test on a separate save if the update changes transport behavior in a city you care about.

## Quick start

If you just want a cleaner transport experience with less micromanagement, start here.

### 1. Open a transport line

Use the IPT4 panel to review a line and:

- add or remove vehicles manually
- inspect active vehicles and queued vehicles
- change the depot when that option is available

### 2. Use budget control if you want less hand tuning

Budget mode lets the game manage vehicle counts through line-budget behavior. It is useful when you do not want to adjust every line by hand all the time.

### 3. Turn on unbunching if vehicles keep arriving together

If vehicles from the same line keep bunching up, increase the unbunching setting. In many cities this is the first setting worth touching.

### 4. Check stops that seem overloaded

Click a stop to see information that helps you diagnose bottlenecks:

- waiting passengers
- boarding and alighting activity
- time until passengers give up waiting
- the previous and next stop in the line

### 5. Restrict vehicle types only when needed

If a line is using poor vehicle choices, open the vehicle selection UI and limit the line to the models you actually want there.

## Basic configuration

For most players, a sensible starting configuration is:

- budget control: enabled if you prefer less micromanagement
- unbunching: enabled
- vehicle editor: leave at default until you understand the impact
- ticket prices: leave at default for the first test
- stop and station passenger limits: adjust only if you see overcrowding or odd behavior

This gives you the main benefits of IPT4 without changing too many variables at once.

## Advanced configuration

IPT4 also exposes more detailed controls for players who want to fine-tune a transport network.

### What you can tune

- deeper unbunching behavior
- passenger limits by stop and station type
- fare customization by transport mode
- vehicle editor values such as capacity, cost, and speed
- line-level vehicle selection and related operating rules

### Good practice for advanced users

- change one or two settings at a time
- test on a save that you can reload safely
- keep notes about what you changed if you are tuning a large city
- if a setting looks unstable, revert it before changing anything else

## Compatibility

IPT4 consolidates features that used to require separate mods. That reduces overlap, but it does not remove all conflict risk.

### Likely compatible use

IPT4 is usually easiest to use when it is the mod handling transport management for your city.

### Potential conflict areas

Be careful with mods that:

- change line vehicle counts
- alter boarding or unloading behavior
- modify transport pricing
- patch the same game routines with Harmony
- provide overlapping transport UI panels or vehicle control tools

### Practical advice

- avoid enabling duplicate mods that do the same job
- if something looks wrong, test IPT4 with a smaller mod set
- compare behavior in a fresh save before blaming a long-running city

## Incompatibilities and limits

No mod can safely promise compatibility with every other transport-related project.

Things that commonly cause trouble:

- older mods that still patch the same transport systems
- heavy transport overhauls
- out-of-date Harmony-dependent mods
- saves that already contain strange line or vehicle states

If behavior looks inconsistent, isolate the cause by testing with fewer active mods and, if needed, a clean save.

## Troubleshooting

### The mod is enabled, but nothing seems to change

- confirm the mod is enabled in Content Manager
- restart the game after updating the files
- test in a new or separate save before assuming the feature is broken

### Vehicles still bunch together

- increase the unbunching setting
- confirm the line has the relevant option enabled
- check whether the city has a bottleneck that forces vehicles to stop in the same place

### The wrong vehicle types are being used

- open the vehicle selection UI
- remove models that should not operate on that line
- confirm the depot provides compatible vehicles

### The game behaves strangely after an update

- replace the entire mod folder with the new release files
- clear out old mixed files if you manually merged versions
- test the mod on a separate save before continuing a critical city

### A different mod seems to conflict with IPT4

- disable mods that alter the same transport features
- test IPT4 on its own or with a minimal mod set
- re-enable other mods one by one to identify the conflict source

### The release zip does not match what I expected

- make sure you downloaded the tagged release asset, not the source archive
- use the packaged ZIP from the Releases page
- do not mix files from different versions in the same mod folder

## Versions and releases

Release tags are used for public, installable builds. Each release should contain only the runtime files needed to install the mod.

For this repository:

- the release tag name matches the published version when possible
- release notes should stay short and practical
- pre-releases are used when the build is still in beta or development

If you want the most stable packaged version, use the latest non-development release.

## Credits

IPT4 builds on years of community work for Cities: Skylines. The repository keeps historical credit to the earlier maintainers and the source projects that shaped the current codebase.

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

This repository is distributed under the **GNU General Public License v3.0**.

That means you can redistribute and modify the code under the terms of the GPLv3, with the usual requirement to keep the same license and preserve the relevant notices.

A few practical points:

- component-specific notices remain in their original directories
- MIT-licensed parts such as [AlgernonCommons](https://github.com/algernon-A/AlgernonCommons) keep their own copyright and permission notices
- GPL-licensed integrated modules keep their original license texts in place
- this top-level `LICENSE` file does not remove or replace any more specific notice already present in the repository

## Need help?

If something looks wrong, check the latest release notes first. They are the most reliable place to see what changed in a given build.

For installation problems, the most useful checks are:

- whether the correct release ZIP was installed
- whether the files were extracted into the right mod folder
- whether Harmony and the relevant DLCs are present
- whether another transport mod is patching the same game systems
