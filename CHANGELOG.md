# Changelog

All notable changes to this project are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project follows [Semantic Versioning](https://semver.org/).

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
