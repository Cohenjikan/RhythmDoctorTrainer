<!-- Language switch -->
[简体中文](README.md) | **English**

# Rhythm Doctor Trainer · 节奏医生 修改器

An **in-game GUI trainer** for *Rhythm Doctor*, built on BepInEx 5. Press **Insert** to open the overlay — autoplay (frame-perfect auto-play), game speed, jump-to-any-level, unlock-all, and developer tools.

> Note: **Single-player only** (e.g. for recording flawless-clear videos). This project does not touch any online/versus logic — please don't use it for leaderboards. Not affiliated with 7th Beat Games.

<p align="center">
  <img src="docs/images/tab-normal.png" width="32%" alt="Normal"/>
  <img src="docs/images/tab-dev.png" width="32%" alt="Developer"/>
  <img src="docs/images/tab-advanced.png" width="32%" alt="Advanced"/>
</p>

## Features

**Normal**
- **Autoplay** — the engine plays every beat perfectly from the chart; on-screen it looks identical to a real run (no watermark), and the flawless / JCI results marker is preserved
- **Game speed 0.1x–3x** — slow-mo practice / speed-up (pitch included; applied on level start/restart — the engine cannot change speed mid-song)
- **Widen the hit window** — score Perfect even when playing by hand
- **No-fail** (never fail / get interrupted), **instant dialogue**, **skip menu transitions**, **uncap framerate**, **mute beat sounds**
- **Win level / skip level**, **unlock all levels**

**Level Jump**
- Lists every level; click one to **load it directly**, bypassing the hub's story/NPC gating — combine with Autoplay to record any level

**Developer**
- **Developer mode** (isDev), **Debug mode**
- **Mark game done / set all levels to S / reveal all story** (populates the hub with NPCs)
- **Unlock all achievements** (note: writes to your Steam account), **disable achievement granting** (avoid polluting your account while cheating)
- Open save folder, delete save (with confirm), misc debug flags

**Advanced**
- A/V calibration, infinite-mode record, Booth mode, dog mode, hidden song "Song of the Sea"

## Install

For the **Steam release** (Unity 6 / x64 / Mono). BepInEx 5 is required first.

### Step 1: install BepInEx 5 (x64, Mono)
1. Download **`BepInEx_win_x64_5.4.23.x.zip`** from [BepInEx Releases](https://github.com/BepInEx/BepInEx/releases).
2. **Extract its contents into the game root** (next to `Rhythm Doctor.exe`; you should then see `winhttp.dll`, `BepInEx/`, etc.).
   > Find the game folder: Steam → right-click *Rhythm Doctor* → Manage → Browse local files.
3. **Launch the game once, then quit**, so BepInEx generates `BepInEx/plugins`, `BepInEx/config`, etc.

### Step 2: install the trainer
- **Option A (manual, recommended):** download [`dist/RDTrainer.dll`](dist/RDTrainer.dll) and drop it into `<game>\BepInEx\plugins\`.
- **Option B (script):** clone this repo → edit the `GAME=` path in [`tools/install.bat`](tools/install.bat) → run it; it copies `dist\RDTrainer.dll` into `BepInEx\plugins`.

### Verify
After launching, open `<game>\BepInEx\LogOutput.log` and look for:
```
[Info : RD Trainer (节奏医生修改器)] RD Trainer ... loaded. Menu key = Insert.
```
Enter any level and press **Insert** to open the menu.

## Uninstall

- **Remove only the trainer:** delete `<game>\BepInEx\plugins\RDTrainer.dll` (or run [`tools/uninstall.bat`](tools/uninstall.bat)).
- **Remove BepInEx too / restore vanilla:** delete `winhttp.dll` from the game root (fastest way to disable BepInEx), or delete `winhttp.dll` + the `BepInEx/` folder + `doorstop_config.ini`.
- You can also use Steam's "Verify integrity of game files" to restore everything.

> The config file is at `<game>\BepInEx\config\com.cohen.rdtrainer.cfg` (you can rebind the menu key); delete it too when uninstalling.

## Usage

1. In any level, press **Insert** to open/close the menu.
2. To record a flawless run: enable **Autoplay** on the *Normal* tab → enter a level → capture with OBS, etc.
3. To record a story-locked level: use the **Level Jump** tab and click the level name to enter directly.
4. **Speed:** after moving the slider it applies on level **start/restart** (there's a "restart and apply" button in the menu).
5. To play by hand again: turn Autoplay off on the *Normal* tab.

## Build from source

Requires the .NET SDK (targets `netstandard2.1`) and a copy of the game with BepInEx installed (for the reference DLLs).

```bash
# Defaults to D:\steam\steamapps\common\Rhythm Doctor; override with -p:GameDir=...
dotnet build src/RDTrainer.csproj -c Release -p:GameDir="X:\path\to\Rhythm Doctor"
```
Output: `src/bin/Release/RDTrainer.dll`.

## Compatibility

| Item | Value |
|---|---|
| Game | Rhythm Doctor (Steam release) |
| Engine | Unity 6 (6000.3.x) / x64 / Mono |
| Loader | BepInEx 5.4.23.x |
| Target framework | netstandard2.1 |

> A major game update may require adjustments; if it fails to load, first confirm your BepInEx version matches this guide.

## Credits & Disclaimer

- *Rhythm Doctor* © [7th Beat Games](https://rhythmdr.com/) — this project is **not affiliated** with them and contains/distributes none of the game's assets or code.
- Mod frameworks: [BepInEx](https://github.com/BepInEx/BepInEx) / [HarmonyX](https://github.com/BepInEx/HarmonyX).
- The trainer only calls the game's **already-existing** flags and methods (such as the editor's built-in autoplay flag). It is a single-player toy — **use at your own risk**.
- "Unlock all achievements" writes to your Steam account; pair it with "disable achievement granting" if that matters to you.

Licensed under [MIT](LICENSE).
