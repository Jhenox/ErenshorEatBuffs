# Erenshor Eat Buffs

A **Lunaris** mod for Erenshor that uses selected food and drink items from your inventory to apply or refresh their buffs with one slash command.

**Current version:** 0.3.0

## Commands

| Command | Behavior |
| --- | --- |
| `/eatbuffs` | Apply missing buffs or refresh those with **25% or less** duration remaining. |
| `/eatbuffs 50` | Apply missing buffs or refresh those with **50% or less** remaining. |
| `/eatbuffs 0` | Apply missing buffs or refresh those with **0% or less** remaining. |
| `/eatbuffs 100` | Refresh all found buffs. |
| `/eatbuffs force` | Attempt to use each available configured item regardless of current buff duration. |

The threshold is a percentage from **0 to 100**.

## Supported consumables

The plugin checks for these items, in this order:

1. Bread
2. Brackfruit Juice
3. Water
4. Seafruit
5. Seaspice

Items must be present in inventory. The plugin waits for active casting to finish and processes the consumables sequentially. It reports used, skipped, and missing items in chat. The current item list is defined in `Plugin.cs` and is not configurable in-game.

**Note:** `force` can consume items even when their buffs are already active. Use it deliberately.

## Requirements

- Erenshor (Windows PC)
- Lunaris mod loader with Harmony patching available
- For building: Visual Studio with .NET Framework 4.8.1 development tools

## Installation

1. Install Lunaris according to its documentation.
2. Build this project in **Release**, or obtain the DLL from a release when available.
3. Place the compiled DLL in the Lunaris plugin directory used by your Erenshor installation.
4. Launch Erenshor and confirm the plugin loads in the Lunaris log.

This is a **Lunaris-only** mod. BepInEx and Thunderstore packaging are not supported.

## Building from source

1. Create or open a **Class Library (.NET Framework)** project targeting **.NET Framework 4.8.1**.
2. Add `Plugin.cs` to the project.
3. Add references to `Assembly-CSharp.dll`, `Lunaris.dll`, `0Harmony.dll`, and the Unity DLLs required by the source, from your local Erenshor/Lunaris installation.
4. Set game/loader DLL references to **Copy Local = False**.
5. Select **Release** and build the solution. The output DLL is typically under `bin/Release/`.

Game and loader assemblies are **not included** in this repository. Build references may need updating after Erenshor or Lunaris updates.

## Compatibility

Developed against the Erenshor build current in November 2026. Future game updates may change the methods this plugin patches. No compatibility guarantee is made for later builds.

## Disclaimer

Unofficial fan-made mod; not affiliated with or endorsed by Burgee Media or the Lunaris developers. Use at your own risk and back up your saves before trying new mods.
