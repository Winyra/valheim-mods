# WorldCompareScreenshots

A client-side BepInEx mod for Valheim that lets players record multiple camera/player positions (waypoints) in one world, and then reproduce and capture exact matching screenshots across any number of other world saves.

## Default Keybinds

- **F6**: Add current position as waypoint
- **F7**: Capture selected waypoint
- **F8**: Run automated sequence capture across all waypoints
- **Page Up / Page Down**: Cycle previous / next waypoint
- **F10**: Toggle in-game management overlay

## Workflow

1. Load your finished world or main server save.
2. Navigate to your base or scenic locations and press **F6** to save waypoints.
3. Switch to another world save (fresh seed, earlier backup).
4. Press **F8** to automatically visit each waypoint, hide the HUD, and capture matched screenshots.
5. Find paired comparison screenshots in `BepInEx/screenshots/WorldCompare/`.
