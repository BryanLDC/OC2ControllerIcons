# OC2 Controller Icons

Universal and per-player controller button prompts for **Overcooked! 2** (Steam, PC).

On PC, Overcooked! 2 always shows Xbox buttons, whatever controller you use. This
[BepInEx](https://github.com/BepInEx/BepInEx) plugin makes the prompts work for
Xbox, PlayStation and Nintendo controllers — including couch co-op sessions where
every player uses a different one.

## Features

| Area | What changes |
|---|---|
| **Menus** | Every button prompt becomes a layout-agnostic icon: four circles (top / left / right / bottom) with the button to press filled in. Bumpers, triggers and sticks follow the same idea. When every joined player uses the same layout, menus show that layout's real buttons instead. |
| **Controls screens** | Controller diagrams show an unbranded controller (no logo, empty face buttons), generated at runtime from the game's own images. With a shared layout they show the game's Xbox or PlayStation diagram, or Nintendo button icons. |
| **In-game prompts** | Tutorial and kitchen prompts (*pick up*, *chop*, …) use each player's layout: **Xbox**, **PlayStation**, **Nintendo** (position-correct: bottom = B) or **Generic**. Shared prompts follow the closest chef. |
| **Player select** | Each player card shows that player's controller (Xbox, DualShock or Pro Controller silhouette). |
| **Settings** | A native *CONTROLLER LAYOUTS* section at the bottom of *Settings › Game*, with the game's look, navigation and Save/Discard dialog. Shows the number of players detected and only their rows. Localized in the game's 12 languages. |
| **New players** | A controller joining later gets the most common layout of the other players (tie: Player 1's). |

The mod is **disabled by default**, never modifies game files or save data, and each
patch is applied independently so a future game update can only disable the part it breaks.

## Supported platforms

| Platform | Package | Status |
|---|---|---|
| Windows | `Windows_Linux` | Tested |
| Linux / Steam Deck (Proton) | `Windows_Linux` + launch option | Expected to work |
| macOS (native build) | `macOS` | Expected to work |
| Existing BepInEx 5 setup | `PluginOnly` | — |

## Installation

Download the package for your platform from the releases page and follow
[`docs/README_EN.txt`](docs/README_EN.txt) — it covers Windows, Linux / Steam Deck, macOS,
configuration and uninstallation step by step.

Quick start (Windows): extract `OC2ControllerIcons_v3.1.0-Windows_Linux.zip` into the
game folder (next to `Overcooked2.exe`), start the game and enable the mod in
**Settings › Game › CONTROLLER LAYOUTS**.

Overcooked! 2 has no native Linux build: on Linux and Steam Deck the Windows version runs
through Proton, which needs this launch option:

```
WINEDLLOVERRIDES="winhttp=n,b" %command%
```

## Configuration

In-game: **Settings › Game**, bottom of the list.

| Option | Values | Default |
|---|---|---|
| Universal controller icons | Off / On | Off |
| Player 1–4 controller (joined players only) | Xbox / PlayStation / Nintendo / Generic | Xbox |

Values are stored in `BepInEx/config/com.oc2mods.controllericons.cfg`.

## Building from source

Requirements:

- Windows with .NET Framework 4.x (its `csc.exe` is used — no SDK needed).
- Overcooked! 2 installed (its managed DLLs are referenced, never redistributed).
- Internet access on the first build (BepInEx is downloaded into `libs/`).

```powershell
.\build.ps1              # build + create release\v<version>\
.\build.ps1 -Install     # also install the Windows package into the game folder
.\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Overcooked! 2"
```

The game folder is auto-detected from the Steam registry key and library folders.

Optional asset generation (Python 3 + Pillow):

```bash
python tools/make_icons.py          # generic button icons  -> res/
python tools/make_procontroller.py  # Pro Controller masks  -> res/
```

> The game runs on Unity 2017.4 with the Mono 2.0 runtime (.NET 3.5 profile), so the
> code targets **C# 5** and is compiled against the game's own `mscorlib`.

## Project structure

```
src/
  Plugin.cs                 BepInEx entry point; applies each patch independently
  ModSettings.cs            Live vs. committed settings (BepInEx config file)
  ModStrings.cs             UI strings in the game's 12 languages
  IconLibrary.cs            Generic sprites + per-layout sprites from the game
  DiagramGenericizer.cs     Runtime "unbranding" of controller diagrams
  Patches/
    MenuIconPatch.cs        Generic icons for every UI prompt
    GameplayIconPatch.cs    Per-player in-world prompts (closest chef)
    LobbyIconPatch.cs       Player-select controller silhouettes
    DiagramSpritePatch.cs   Swaps diagram sprites on UI Images
    SettingsMenuPatch.cs    Native rows in Settings > Game
    SettingsHeader.cs       "CONTROLLER LAYOUTS" section header
res/                        Original PNG art embedded in the DLL
tools/                      Python generators for res/
docs/                       End-user guide and Nexus Mods page text
build.ps1                   Build and packaging script
```

## Compatibility notes

- Keyboard players keep keyboard prompts.
- Online: only the local screen is affected.
- Other BepInEx plugins can coexist.

## License

Released under the [MIT License](LICENSE).

Bundled in the release packages (not in this repository):
[BepInEx](https://github.com/BepInEx/BepInEx) (LGPL-2.1) and
[HarmonyX](https://github.com/BepInEx/HarmonyX) (MIT).

## Disclaimer

This is an unofficial fan project, not affiliated with or endorsed by Team17 or
Ghost Town Games. *Overcooked!* is a trademark of its respective owners. Xbox,
PlayStation and Nintendo Switch are trademarks of Microsoft, Sony Interactive
Entertainment and Nintendo. No game assets are included in this repository.
