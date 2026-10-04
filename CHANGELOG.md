# Changelog

All notable changes to this project are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project follows [Semantic Versioning](https://semver.org/).

## [3.1.0] - 2026-10-04

### Added
- When every joined controller player uses the same layout (Xbox, PlayStation or
  Nintendo), menu and UI prompts show that layout's real buttons instead of the generic
  icons. The generic icons are used when layouts differ or all are set to Generic.
- Controls screens follow the shared layout too: the game's own Xbox or PlayStation
  diagrams, or an unbranded diagram with Nintendo button icons (the game has no
  Nintendo diagram). Mixed layouts keep the unbranded diagram.
- *CONTROLLER LAYOUTS* shows a "Players detected" line and only the rows of the players
  who have joined.
- A player joining in a slot that was not part of the saved setup gets the most common
  layout among the other players (tie: Player 1's layout).

### Changed
- Keyboard players do not affect the "same layout" check.
- The chalkboard "Player N press [button] / Space to join locally" prompt always uses
  the generic icon (whoever joins may hold any controller).

### Fixed
- The "press [button] / Space to rejoin" popup no longer forces the Xbox A icon; it
  follows the same rule as every other prompt.
- When a layout lacks an icon in one style, the same layout's other style (or the
  generic icon) is used instead of falling back to Xbox.

## [3.0.0] - 2026-10-04

First public release.

### Added
- Universal "four circles" button prompts in every menu and UI screen.
- Unbranded controller diagrams on the Controls screens, generated at runtime.
- Per-player layouts for in-game prompts: Xbox, PlayStation, Nintendo and Generic.
- Player-select cards show each player's controller; Nintendo uses a Pro Controller silhouette.
- Native *CONTROLLER LAYOUTS* section in Settings › Game with Save/Discard support.
- UI strings in the game's 12 languages.
- Release packages for Windows / Linux (Proton, incl. Steam Deck), macOS and plugin-only installs.
- Build script with automatic game detection and BepInEx download.

### Changed
- Settings are stored in the BepInEx config file instead of a global layout switch.
- Windows package now bundles BepInEx x86 (the game executable is 32-bit).
