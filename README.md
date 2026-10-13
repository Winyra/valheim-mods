# Valheim Mods

A collection of client-side and server mods for Valheim, built with C#, BepInEx, and Unity.

## Included Mods

| Mod | Description | Documentation |
|---|---|---|
| **[WorldCompareScreenshots](WorldCompareScreenshots/)** | Capture aligned screenshots from identical camera and player positions across multiple world saves (e.g., fresh seed vs. mid-playthrough backup vs. finished endgame base). Supports multi-waypoint saving, automated capture sequences, and automatic HUD hiding. | [Mod Readme](WorldCompareScreenshots/README.md) · [Thunderstore Package Readme](WorldCompareScreenshots/package/README.md) |
| **[TreeTransparency](TreeTransparency/)** | Visual toggle to make trees and foliage semi-transparent with a hotkey (default `F9`) and customizable opacity. Useful for planning, navigating, and placing building pieces through dense forest canopies. | [Mod Readme](TreeTransparency/README.md) · [Thunderstore Package Readme](TreeTransparency/package/README.md) |

---

## Repository Structure

- **`WorldCompareScreenshots/`**: Source code, project file, and documentation for the WorldCompareScreenshots mod.
  - `package/`: Thunderstore/Hexium package manifest, icon, and distribution readme.
- **`TreeTransparency/`**: Source code, project file, and documentation for the TreeTransparency mod.
  - `package/`: Thunderstore/Hexium package manifest, icon, and distribution readme.
- **`WorldCompareScreenshots.Tests/`**: Automated unit tests for waypoint math, JSON serialization/deserialization, and file persistence.
- **`valheim-mods.slnx`**: Solution file referencing all projects in the repository.

---

## Building

Prerequisites: [.NET SDK](https://dotnet.microsoft.com/download) (6.0 or newer).

Build all projects in the solution:
```bash
dotnet build valheim-mods.slnx
```

Or build an individual mod:
```bash
dotnet build WorldCompareScreenshots/WorldCompareScreenshots.csproj
dotnet build TreeTransparency/TreeTransparency.csproj
```

---

## Testing

Run unit tests:
```bash
dotnet test
```

### In-Game Client Testing
1. Copy the compiled mod `.dll` from the mod's `bin/Debug/` or `bin/Release/` directory into your `Valheim/BepInEx/plugins/` folder.
2. Launch Valheim with BepInEx 5.4.x installed.
3. For mod-specific usage, keybinds, and configuration, refer to the individual mod readmes linked above.

