==============================================================================
 OC2 Controller Icons  v3.0.0  -  Overcooked! 2 (Steam, PC / Steam Deck / Mac)
==============================================================================

Universal controller icons for Overcooked! 2. Play with Xbox, PlayStation or
Nintendo controllers (or a mix of them) and see the right buttons:

  * Menus: every button prompt becomes a layout-agnostic icon - four circles
    (top / left / right / bottom) with the button you must press filled in.
  * Controls screens: the controller diagrams show an unbranded controller
    (no logo, empty face buttons).
  * In-game prompts (tutorial, "pick up", "chop"...): each player sees the
    buttons of THEIR controller (Xbox / PlayStation / Nintendo / Generic).
  * Player select: each player's card shows their controller type.

The mod is installed DISABLED. Turn it on from the in-game Settings menu.


------------------------------------------------------------------------------
 WHICH FILE DO I NEED?
------------------------------------------------------------------------------
  OC2ControllerIcons_v3.0.0-Windows_SteamDeck.zip
      Windows PC and Steam Deck. Includes BepInEx 5 (x86 - the game is 32-bit).

  OC2ControllerIcons_v3.0.0-macOS.zip
      Mac (native Mac version of the game). Includes BepInEx 5 for macOS.

  OC2ControllerIcons_v3.0.0-PluginOnly.zip
      Only the mod. For people who ALREADY have BepInEx 5 installed.


------------------------------------------------------------------------------
 FIND THE GAME FOLDER
------------------------------------------------------------------------------
  Steam > Library > right-click "Overcooked! 2" > Manage > Browse local files.
  It is the folder that contains "Overcooked2.exe" (Windows / Steam Deck) or
  "Overcooked2.app" (Mac).


------------------------------------------------------------------------------
 INSTALL - WINDOWS
------------------------------------------------------------------------------
  1. Close the game.
  2. Extract the Windows_SteamDeck zip INTO the game folder (next to
     Overcooked2.exe). Say "yes" if asked to merge folders.
  3. Start the game normally from Steam.


------------------------------------------------------------------------------
 INSTALL - STEAM DECK
------------------------------------------------------------------------------
  1. Switch to Desktop Mode.
  2. Extract the Windows_SteamDeck zip INTO the game folder (next to
     Overcooked2.exe). The "Ark" app that comes with the Deck can do it.
  3. In Steam: right-click "Overcooked! 2" > Properties > General >
     Launch Options, and paste exactly:

         WINEDLLOVERRIDES="winhttp=n,b" %command%

     (Without this line Proton ignores BepInEx and the mod will not load.)
  4. Go back to Gaming Mode and play.


------------------------------------------------------------------------------
 INSTALL - MAC
------------------------------------------------------------------------------
  1. Close the game.
  2. Extract the macOS zip INTO the game folder (next to Overcooked2.app).
     Usually: ~/Library/Application Support/Steam/steamapps/common/Overcooked! 2
  3. Open Terminal, go to that folder and make the launcher executable:

         cd ~/Library/Application\ Support/Steam/steamapps/common/Overcooked\!\ 2
         chmod +x run_bepinex.sh

  4. In Steam: right-click "Overcooked! 2" > Properties > General >
     Launch Options, and paste (keep the quotes):

         "$HOME/Library/Application Support/Steam/steamapps/common/Overcooked! 2/run_bepinex.sh" %command%

  5. Start the game from Steam.
     If macOS blocks "libdoorstop.dylib", allow it in
     System Settings > Privacy & Security, or run in the game folder:

         xattr -dr com.apple.quarantine .

  Note: on Apple Silicon Macs the game runs through Rosetta. The mod itself is
  the same as on Windows; the Mac loader is the official BepInEx macOS build.


------------------------------------------------------------------------------
 INSTALL - PLUGIN ONLY (you already use BepInEx 5)
------------------------------------------------------------------------------
  Extract the zip into the game folder. It only adds:
      BepInEx/plugins/OC2ControllerIcons/OC2ControllerIcons.dll


------------------------------------------------------------------------------
 HOW TO TURN IT ON / CONFIGURE
------------------------------------------------------------------------------
  1. Start the game. On the main menu go to  Settings > Game  (Ajustes > Juego).
  2. Scroll to the bottom: section "CONTROLLER LAYOUTS".
  3. "Universal controller icons"  ->  On.
  4. "Player 1..4 controller"  ->  Xbox / PlayStation / Nintendo / Generic
        (default: Xbox for everyone)
  5. Press SAVE.

  Changes are visible immediately. "Cancel / Discard" restores the previous
  values, exactly like the game's own options.
  The options follow the game language (12 languages supported).

  Settings are stored in:  BepInEx/config/com.oc2mods.controllericons.cfg
  (you can also edit that file with the game closed).

  Good to know:
  - "Player N" is the slot in the player-select screen.
  - Prompts that float over kitchen objects are shared by everyone, so they
    use the layout of the chef standing closest to that object.
  - Keyboard players keep seeing keyboard keys.
  - Online: it only changes what YOU see on your screen.


------------------------------------------------------------------------------
 UNINSTALL
------------------------------------------------------------------------------
  Only disable it:
    Settings > Game > "Universal controller icons" -> Off.

  Remove only this mod (keep BepInEx for other mods):
    Delete the folder  BepInEx/plugins/OC2ControllerIcons
    (optional) delete  BepInEx/config/com.oc2mods.controllericons.cfg

  Remove everything - Windows / Steam Deck (game folder):
    BepInEx/            (folder)
    winhttp.dll
    doorstop_config.ini
    .doorstop_version
    changelog.txt
    OC2ControllerIcons_README.txt
    Steam Deck: also clear the Launch Options line.

  Remove everything - Mac (game folder):
    BepInEx/            (folder)
    libdoorstop.dylib
    run_bepinex.sh
    .doorstop_version
    changelog.txt
    OC2ControllerIcons_README.txt
    and clear the Launch Options line in Steam.

  The mod never modifies game files or your save files, so after removing it
  the game is exactly as before.


------------------------------------------------------------------------------
 TROUBLESHOOTING
------------------------------------------------------------------------------
  * Nothing changes: make sure the option is On and saved. Check that
    BepInEx/LogOutput.log exists (if not, BepInEx is not loading - on Steam
    Deck/Mac re-check the Launch Options).
  * In LogOutput.log every part of the mod reports "Patch applied: ...".
    If one says "Patch FAILED", please report it together with the log.


------------------------------------------------------------------------------
 CREDITS
------------------------------------------------------------------------------
  BepInEx  - https://github.com/BepInEx/BepInEx  (LGPL-2.1)
  HarmonyX - https://github.com/BepInEx/HarmonyX (MIT)
  Generic icons and the Pro Controller silhouette are original art made for
  this mod. Controller diagrams are generated at runtime from the game's own
  images - no game assets are redistributed.
