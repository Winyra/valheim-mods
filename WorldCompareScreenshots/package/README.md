# WorldCompareScreenshots

A client-side BepInEx mod for Valheim that lets players record multiple camera and player positions (waypoints) in one world, and then reproduce and capture exact matching screenshots across any number of other world saves.

Perfect for comparing a completed endgame base against a fresh seed, or documenting progressive build stages across backup saves.

## Features

- **Multi-Waypoint Recording**: Save multiple locations and camera angles in your primary world (e.g. harbor, main hall, tower overlook, portal hub) before switching worlds.
- **Persistent Across Worlds**: Waypoints are saved to disk (`BepInEx/config/WorldCompareWaypoints.json`) in human-readable JSON so they persist across game restarts and world changes.
- **Automated Sequence Capture**: Press **F8** to automatically teleport to each waypoint, wait for terrain to settle, hide the HUD, and capture screenshots in sequence.
- **Clean Comparison Shots**: Automatically hides the Valheim HUD and mod menus during capture.
- **Safe Positioning**: Temporarily enables fly mode during positioning to prevent fall damage at high elevation.
- **Predictable File Naming**: Saved to `BepInEx/screenshots/WorldCompare/` as `{Index:D2}_{WaypointName}__{WorldName}__{Timestamp}.png` for easy alphabetical grouping and side-by-side comparison.
- **In-Game Overlay**: Press **F10** to manage, rename, teleport to, or delete waypoints.

## Default Keybinds

| Key | Action | Description |
|---|---|---|
| **F6** | **Add Waypoint** | Save current camera & player transform. |
| **F7** | **Capture Selected** | Teleport to current waypoint, hide HUD, and take screenshot. |
| **F8** | **Capture Sequence** | Automatically capture all saved waypoints in sequence. |
| **Page Up** | **Previous Waypoint** | Cycle to previous waypoint. |
| **Page Down** | **Next Waypoint** | Cycle to next waypoint. |
| **F10** | **Toggle UI Menu** | Open/close the in-game management overlay. |

*(All keybinds and delays can be reconfigured in `BepInEx/config/valheim.winyra.worldcomparescreenshots.cfg`)*

## Workflow

1. **Record Waypoints**: In your main or finished world, navigate to your favorite spots (walk or use `freefly`) and press **F6** at each location. Optionally press **F10** to rename them.
2. **Switch Worlds**: Log out and load your comparison world (fresh seed or backup).
3. **Capture**: Press **F8**. The mod will visit each waypoint in order, hide the HUD, capture each screenshot, and restore your position.
4. **Compare**: Open `<Valheim_Install_Dir>/BepInEx/screenshots/WorldCompare/` to see side-by-side matched screenshots.

## Installation

Install using r2modman / Thunderstore Mod Manager, or extract `WorldCompareScreenshots.dll` to your `Valheim/BepInEx/plugins/` directory. Requires BepInEx 5.4.x.

