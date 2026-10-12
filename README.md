# Valheim Mods

Small client and server mods for Valheim.

## Mods in this Repository

- **WorldCompareScreenshots**: Capture aligned screenshots from identical camera and player positions across multiple world saves (e.g., fresh seed vs. mid-playthrough backup vs. finished endgame base).
- **TreeTransparency**: Fade tree canopies when close to the player.

---

# WorldCompareScreenshots

A client-side BepInEx mod for Valheim that lets players record multiple camera/player positions (waypoints) in one world, and then reproduce and capture exact matching screenshots across any number of other world saves.

## Engine Note (Unity vs. UE5)
> **Note regarding Unreal Engine 5 (UE5):** Valheim is built using the **Unity** game engine and uses the **BepInEx** modding framework in C# (.NET/Mono). Unreal Engine 5 (UE5) does not apply to Valheim or this mod repository. All setup, configuration, and testing occur directly inside the Valheim game client and the BepInEx runtime environment.

---

## Core Features

1. **Multi-Waypoint Recording**: Save multiple locations and camera angles in your primary world (e.g., harbor, main hall, tower overlook, portal hub) before switching worlds.
2. **Persistent Across Worlds and Sessions**: Waypoints are saved to disk (`BepInEx/config/WorldCompareWaypoints.json` by default) in human-readable JSON, so they survive game restarts and are immediately available when loading different world saves.
3. **Automated Sequence Capture**: One keypress cycles through all saved waypoints, teleports the player/camera, waits for terrain/assets to settle, hides the HUD, captures the screenshot, and advances to the next.
4. **Clean Comparison Shots**: Automatically hides the Valheim HUD and mod menus during capture for clean, unobstructed side-by-side comparison images.
5. **Predictable File Naming**: Screenshots are saved to `BepInEx/screenshots/WorldCompare/` formatted as:
   ```text
   {Index:D2}_{WaypointName}__{WorldName}__{Timestamp}.png
   ```
   Example:
   - `01_Base_Overlook__VikingRealm_Endgame__20261012_143000.png`
   - `01_Base_Overlook__Fresh_Seed__20261012_144500.png`
   Sorting alphabetically groups screenshots by waypoint across worlds for instant visual comparison.
6. **In-Game Menu**: Press `F10` to open an overlay to view, rename, teleport to, or delete waypoints.

---

## Default Keybinds

| Key | Action | Description |
|---|---|---|
| **F6** | **Add Waypoint** | Records current camera & player transform (position, rotation, FOV, free-fly state). |
| **F7** | **Capture Selected** | Teleports to the currently active waypoint, hides HUD, and captures a screenshot. |
| **F8** | **Capture Sequence** | Automatically visits and captures all saved waypoints in sequence. |
| **Page Up** | **Previous Waypoint** | Selects previous waypoint in the list. |
| **Page Down** | **Next Waypoint** | Selects next waypoint in the list. |
| **F10** | **Toggle UI Menu** | Opens/closes the in-game waypoint management window. |

*(All hotkeys and delays can be customized in `BepInEx/config/valheim.winyra.worldcomparescreenshots.cfg`)*

---

## Multi-World Comparison Workflow

### Step 1: Record waypoints in your main world
1. Launch Valheim with BepInEx installed.
2. Load into your world (e.g., your finished server base or favorite save).
3. Position your character or enter free-fly camera mode (open console with `F5`, type `devcommands`, then `freefly`).
4. Press **F6** to save the current position. A notification will appear (e.g., `"Added waypoint #1: Waypoint 1"`).
5. Move to other scenic spots (portal room, gatehouse, harbor) and press **F6** at each spot.
6. (Optional) Press **F10** to open the menu and rename waypoints to descriptive names (e.g., `Great_Hall`, `Harbor_Docks`).
7. (Optional) Press **F8** to capture your baseline screenshots for this world.

### Step 2: Switch to another world save
1. Log out to the main menu.
2. Load your comparison world save (e.g., a freshly created world with the same seed, or an earlier backup save).
3. Once in the world, press **F8**.
4. The mod will:
   - Temporarily enable fly mode to prevent falling damage.
   - Teleport your camera and player to Waypoint 1.
   - Wait for terrain and world chunks to load/settle (configurable delay, default 1.5s).
   - Hide the HUD.
   - Capture the screenshot.
   - Advance to Waypoint 2, Waypoint 3, etc.
   - Restore your original HUD and position when complete.

### Step 3: Compare results
Open `<Valheim_Install_Dir>/BepInEx/screenshots/WorldCompare/`.
You will see matched pairs:
- `01_Great_Hall__FinishedBase__20261012_150000.png`
- `01_Great_Hall__FreshSeed__20261012_151000.png`
- `02_Harbor_Docks__FinishedBase__20261012_150005.png`
- `02_Harbor_Docks__FreshSeed__20261012_151005.png`

---

## Configuration

Settings are saved in `BepInEx/config/valheim.winyra.worldcomparescreenshots.cfg`:

- `AddWaypointKey`: Key to add current camera/player position as waypoint (default: `F6`).
- `CaptureCurrentKey`: Key to capture the current waypoint (default: `F7`).
- `CaptureSequenceKey`: Key to run automated capture sequence (default: `F8`).
- `NextWaypointKey`: Key to select next waypoint (default: `PageDown`).
- `PrevWaypointKey`: Key to select previous waypoint (default: `PageUp`).
- `ToggleMenuKey`: Key to toggle the in-game UI menu (default: `F10`).
- `HideHudDuringCapture`: When enabled, automatically hides the HUD during capture (default: `true`).
- `TerrainSettleDelaySeconds`: Pause before capture in sequence mode to allow terrain/chunks to render (default: `1.5`s).
- `CustomOutputFolder`: Custom directory for screenshots. Defaults to `BepInEx/screenshots/WorldCompare/`.
- `CustomWaypointFile`: Custom path for `waypoints.json`. Defaults to `BepInEx/config/WorldCompareWaypoints.json`.

---

## Manual HUD Toggle (Fallback)
If `HideHudDuringCapture` is disabled or you prefer manual control:
- In Valheim, press **Ctrl + F3** to toggle the HUD on or off at any time.

---

## Testing & Development

### Building the Project
From the repository root:
```bash
dotnet build
```

### Running Unit Tests
```bash
dotnet test
```
The test suite validates:
- Coordinate and quaternion math.
- Lightweight zero-dependency JSON serialization and deserialization.
- SanitizeFileName and file naming conventions.
- Index wrapping and collection bounds.
- File persistence and atomic saving.
- Graceful degradation when running outside the Unity/Valheim runtime.

### In-Game Client Testing Steps
1. Copy `WorldCompareScreenshots.dll` to `<Valheim>/BepInEx/plugins/`.
2. Launch Valheim via Steam.
3. Open a world save, activate console (`F5`), type `devcommands`.
4. Press **F6** to create a waypoint.
5. Press **F10** to inspect the overlay GUI and confirm coordinates match player position.
6. Press **F7** to capture an individual shot, or **F8** to run sequence capture.
7. Verify that screenshots are created in `BepInEx/screenshots/WorldCompare/` with no HUD artifacts.
