# TreeTransparency

A client-side BepInEx mod for Valheim that lets you toggle tree transparency on and off with a configurable hotkey.

This mod is an on/off visual toggle (not proximity-based culling). When toggled on, it makes nearby and surrounding trees semi-transparent with a customizable opacity level so you can easily plan and place building pieces through dense forest canopies and foliage.

## Features

- **Hotkey Toggle**: Press **F9** (configurable) to instantly toggle tree transparency on or off.
- **Configurable Opacity**: Adjust transparency from nearly invisible (0.05) to opaque (1.0). Default is `0.30` (30% opacity).
- **Comprehensive Tree Coverage**: Supports all vanilla tree families and stumps:
  - Beech & Beech stubs
  - Birch (standard & autumn) & Birch stubs
  - Oak & Oak stubs
  - Pine trees & Pine stubs
  - Fir trees (small, big, dead) & Fir stubs
  - Swamp trees & Swamp stubs
  - Yggdrasil shoots & shoot stumps
  - Ashlands trees & Ashlands tree stumps
- **Dynamic Watcher**: While active, a background coroutine periodically detects newly loaded tree chunks and applies transparency automatically.
- **Clean State Restoration**: Toggling off or unloading the mod immediately restores original materials and shaders.

---

## Controls & Usage

- **F9** (default): Toggle tree transparency ON / OFF.
- While ON, newly spawned/rendered trees will automatically become transparent.
- Press **F9** again to restore original tree opacity.

---

## Configuration

Configuration is located at `BepInEx/config/winyra.treetransparency.cfg` (or editable in-game with BepInEx Configuration Manager):

| Section | Key | Default | Description |
|---|---|---|---|
| `Hotkeys` | `ToggleTreeTransparency` | `F9` | Hotkey used to toggle transparency on/off. |
| `General` | `Transparency` | `0.3` | Tree opacity level (`0.05` = nearly invisible, `1.0` = fully opaque). |

---

## Installation

1. Ensure BepInEx 5.4.x is installed for Valheim.
2. Place `TreeTransparency.dll` into your `Valheim/BepInEx/plugins/` folder.
3. Launch Valheim.

---

## Building

From the repository root or project folder:
```bash
dotnet build TreeTransparency/TreeTransparency.csproj
```
