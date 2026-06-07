<!-- Language switch -->
[简体中文](README.md) | **English**

<div align="center">

<img src="docs/assets/hero.png" alt="Rhythm Doctor Trainer — an in-game GUI trainer opened with the Insert key" width="100%">

# 🎵 Rhythm Doctor Trainer · 节奏医生 修改器

**Press Insert. Record a flawless clear. No timing grind, no keyboard macro.**

Frame-perfect autoplay · keeps the Perfect / JCI marker · 0.1×–3× game speed · jump to any level · dev unlocks
A BepInEx in-game GUI trainer for *Rhythm Doctor*, built for **recording flawless single-player clears** — never for online play.

<br>

[![License: MIT](https://img.shields.io/github/license/Cohenjikan/RhythmDoctorTrainer?style=flat-square)](LICENSE)
[![Release](https://img.shields.io/github/v/release/Cohenjikan/RhythmDoctorTrainer?style=flat-square&label=release)](https://github.com/Cohenjikan/RhythmDoctorTrainer/releases)
[![Stars](https://img.shields.io/github/stars/Cohenjikan/RhythmDoctorTrainer?style=flat-square)](https://github.com/Cohenjikan/RhythmDoctorTrainer/stargazers)
[![Issues](https://img.shields.io/github/issues/Cohenjikan/RhythmDoctorTrainer?style=flat-square)](https://github.com/Cohenjikan/RhythmDoctorTrainer/issues)
[![BepInEx 5](https://img.shields.io/badge/BepInEx-5.4.23.x-blue?style=flat-square)](https://github.com/BepInEx/BepInEx)
[![Unity 6](https://img.shields.io/badge/Unity-6%20x64%20Mono-black?style=flat-square)](#%EF%B8%8F-compatibility)
[![Single-player only](https://img.shields.io/badge/scope-offline%20single--player-success?style=flat-square)](#-disclaimer)

<br>

</div>

---

> [!IMPORTANT]
> **Single-player only** (e.g. for recording flawless-clear videos). This project does not touch any online / versus / leaderboard logic — please don't use it anywhere it affects other players' fairness. **Not affiliated with 7th Beat Games.**
>
> 💚 **Completely free and open-source (MIT) — reselling is forbidden.** It ships with an integrity check: the menu title and load log show the project URL, and removing or altering that watermark **disables the trainer**. **If you paid for it, you were scammed** — get it free from this repository.

## ✨ What it does

The core of *Rhythm Doctor* is "press exactly on the 7th beat." Playing every beat perfectly by hand is brutal. This trainer reuses the engine's **built-in autoplay** (it flips the engine's native `DebugSettings.Auto` flag and lets the engine play the chart) — so it has **no input lag, is genuinely frame-perfect, and looks identical to a real hand-played run**. It also deliberately avoids the entry that flashes the "autoplay on!" LED text, so your recordings stay clean.

It does **no memory scanning** — it only calls the game's **own existing** flags and methods via BepInEx + HarmonyX, which is far more stable across game updates than a traditional trainer.

## 🚀 Features

> One in-game IMGUI overlay, four tabs: **Normal / Developer / Advanced / Level Jump**.

### 🎬 Frame-perfect autoplay (clean, no LED watermark)

<img src="docs/assets/feature-1.png" alt="Normal tab: Autoplay toggle, speed slider, widen-hit-window, no-fail and other recording options" align="right" width="45%">

The engine plays every beat perfectly — **no input lag, no on-screen "autoplay on!" LED text** — indistinguishable from a real run. Combine it with a hidden HUD for spotless footage.

- ✅ Restores the **Perfect / JCI results marker** the game normally suppresses during autoplay (on by default)
- ✅ Uses the engine's native `DebugSettings.Auto` flag — **not a keyboard macro / input simulator**
- ✅ Want to play by hand? Just toggle it off

<br clear="right">

### ⏱️ Game speed 0.1×–3× (pitch included)

Slow-mo to nail the details, or speed up for fast runs; **BPM and song pitch scale together** consistently.

> ⚠️ Applies on level **start / restart** — there is **no mid-song change** (the engine fixes BPM and pitch at load, so a live change would desync). A "restart and apply" button is provided in the menu.

### 🎯 Widened judgment window · 🛡️ No-fail · QoL

<img src="docs/assets/feature-2.png" alt="Advanced tab: audio/visual calibration, an endless-mode score slot, and show / hidden-track (Song of the Sea) easter eggs" align="right" width="45%">

- **Widen hit window ×1–×10** (default ×3) — score Perfect by hand even when slightly off
- **No-fail** — never fail or get interrupted mid-level
- **Instant dialogue / skip menu transitions / uncap framerate / mute beat sounds**

### 🗺️ Jump to any level (bypass story / NPC gating)

Lists **every level** (with a text filter); click one to **load it directly**, bypassing the hub's story/NPC gating — combine with Autoplay to record any level, including ones locked behind progression.

<br clear="right">

### 🔓 Unlock & progress tools

- **Unlock all levels** (writes to save) · **set all levels to S** · **mark game done** · **reveal all story** (populates the hub with NPCs)

### 🛠️ Developer / debug mode + achievement controls

<img src="docs/assets/feature-3.png" alt="Developer tab: developer mode, debug mode, progress/rank, achievement controls, save tools and misc debug flags" align="right" width="45%">

- **Developer mode (isDev) / Debug mode**, plus a set of low-level debug switches (NoPro / ForceNoSteamworks / EmulateMobile / RunningOnSteamDeck / DebugAmbience / PaigeStays / PauseOnFocusLost)
- **Unlock all achievements** (⚠️ writes to your Steam account) / **disable achievement granting** (avoid polluting your account while cheating)
- Open save folder, delete save (with confirm)

> ⚠️ Debug mode draws debug text on screen, so it is **unsuitable for clean recording**.

<br clear="right">

### 🧪 Advanced: calibration / infinite-mode record / easter eggs

Read and apply A/V calibration values (visual / input / latency), edit the *Rhythm Weightlifter* infinite-mode best-round record, and trigger Booth mode / dog mode / the hidden song *Song of the Sea*.

### 🔒 Anti-resale watermark + integrity gate

To keep the tool free forever: the trainer SHA256-checks its watermark on startup, and **removing or altering the project-URL watermark = the entire trainer refuses to run** (no Harmony patches, no features, no menu).

## 📦 Install

For the **Steam release** (Unity 6 / x64 / Mono). BepInEx 5 is required first.

### Step 1: install BepInEx 5 (x64, Mono)

1. Download **`BepInEx_win_x64_5.4.23.x.zip`** from [BepInEx Releases](https://github.com/BepInEx/BepInEx/releases).
2. **Extract its contents into the game root** (next to `Rhythm Doctor.exe`; you should then see `winhttp.dll`, `BepInEx/`, etc.).
   > 🔍 Find the game folder: Steam → right-click *Rhythm Doctor* → Manage → Browse local files.
3. **Launch the game once, then quit**, so BepInEx generates `BepInEx/plugins`, `BepInEx/config`, etc.

### Step 2: install the trainer

**Option A (manual, recommended)** — download [`dist/RDTrainer.dll`](dist/RDTrainer.dll) (~23 KB) and drop it into:

```text
<game>\BepInEx\plugins\RDTrainer.dll
```

**Option B (script)** — clone this repo → edit the `GAME=` path in [`tools/install.bat`](tools/install.bat) (defaults to a common Steam path) → run it; it copies `dist\RDTrainer.dll` into `BepInEx\plugins`.

### ✅ Verify

After launching, open `<game>\BepInEx\LogOutput.log` and look for (the watermark part prints your build's full project URL):

```log
[Info : RD Trainer (节奏医生修改器)] RD Trainer (节奏医生修改器) v2.2.0 loaded · 本工具免费开源，严禁倒卖 · FREE · github.com/Cohenjikan/RhythmDoctorTrainer · Menu key = Insert
```

Enter any level and press **Insert** to open the menu.

## 🎮 Quickstart

1. In any level, press **Insert** to open / close the menu.
2. **Record a flawless run:** enable **Autoplay** on the *Normal* tab → enter a level → capture with OBS, etc.
3. **Record a story-locked level:** use the **Level Jump** tab and click the level name to enter directly.
4. **Speed:** after moving the slider it applies on level **start / restart** (there's a "restart and apply" button in the menu).
5. **Play by hand again:** turn Autoplay off on the *Normal* tab.

## ⚠️ Good to know / caveats

- **Single-player / offline only** — touches no online, versus, or leaderboard logic; don't use it where it affects others' fairness.
- **Speed changes apply only on level start / restart**, never mid-song (the engine fixes BPM and pitch at load); use "restart and apply."
- **"Unlock all achievements" writes to your real Steam account** — pair it with "disable achievement granting" if that matters.
- **Debug mode draws debug text on screen**, so it is unsuitable for clean recording.
- **Delete-save and reveal-story actions modify your save file** — deleting the save is irreversible (there's a confirm step).
- **Modding may violate the game's EULA / ToS** — use at your own risk (account penalties, save corruption possible).
- **Unofficial fan tool**, not affiliated with or authorized by 7th Beat Games; ships no game source, DLLs, audio, or art.
- **Requires BepInEx 5 (x64, Mono)**; a major game update may require updates to keep it loading, and compatibility is **not** guaranteed forever.
- **Removing or altering the watermark disables the entire trainer** by design (integrity gate).

## 🔧 Build from source

Requires the .NET SDK (targets `netstandard2.1`) and a copy of the game with BepInEx installed (for the reference DLLs).

```bash
# Defaults to D:\steam\steamapps\common\Rhythm Doctor; override with -p:GameDir=...
dotnet build src/RDTrainer.csproj -c Release -p:GameDir="X:\path\to\Rhythm Doctor"
```

Output: `src/bin/Release/RDTrainer.dll`. The repo references game DLLs with `Private=false` — it does **not** redistribute them.

## ⚙️ How it works (in brief)

The trainer does **no memory-offset / AOB scanning** — it only calls the game's own existing flags and methods via BepInEx + HarmonyX:

| Feature | Implementation |
|---|---|
| Autoplay | sets `DebugSettings.instance.Auto` (deliberately bypasses `ToggleAutoplay`, which flashes the LED text) |
| Keep flawless marker | Harmony postfix on `LevelBase.isZeroOffset` (forces true only when zero offset and zero mistakes) |
| Game speed | writes static `scnGame.levelSpeed`; the engine scales BPM and pitch from it at level Start |
| Widen judgment | postfix on `scnGame.GetHitMargin` multiplies the result |
| No-fail | reflection targets every 1-arg `FailLevel` in the assembly; a prefix skips the original |
| Level jump | calls `scnBase.GoToLevelWithEnum(Level)` |

Because it just "calls the game's own logic," it tends to survive minor game updates far better than a memory trainer. See [ABOUT.md](ABOUT.md) for more.

## 🧹 Uninstall

- **Remove only the trainer:** delete `<game>\BepInEx\plugins\RDTrainer.dll` (or run [`tools/uninstall.bat`](tools/uninstall.bat)).
- **Remove BepInEx too / restore vanilla:** delete `winhttp.dll` from the game root (fastest way to disable BepInEx), or delete `winhttp.dll` + the `BepInEx/` folder + `doorstop_config.ini`.
- You can also use Steam's "Verify integrity of game files" to restore everything.

> The config file is at `<game>\BepInEx\config\com.cohen.rdtrainer.cfg` (you can rebind the menu key); delete it too when uninstalling.

## 🖥️ Compatibility

| Item | Value |
|---|---|
| Game | Rhythm Doctor (Steam release) |
| Engine | Unity 6 (6000.3.x) / x64 / Mono |
| Loader | BepInEx 5.4.23.x |
| Target framework | netstandard2.1 |

> A major game update may require adjustments; if it fails to load, first confirm your BepInEx version matches this guide. Compatibility is **not** guaranteed forever.

## 📜 Disclaimer

- **Unofficial.** This is an unofficial, fan-made third-party tool, **not affiliated with, authorized, or endorsed by** the game's developer [7th Beat Games](https://rhythmdr.com/). *Rhythm Doctor* and all related names, trademarks, art, and music are the property of 7th Beat Games.
- **No game content.** This repository contains **only the author's own plugin code** — it includes and distributes no game source, DLLs, audio, images, or other assets. At runtime it only calls the game's **own existing** public functions via BepInEx / HarmonyX; no memory scanning.
- **Single-player only.** For **offline single-player** fun, practice, and recording only. Do **not** use it online, for leaderboards, in competition, or in any way that affects fairness for other players.
- **Respect the EULA.** Modding the game may violate its End-User License Agreement / Terms of Service. Use is entirely at your own discretion, and any consequences (account penalties, save corruption, etc.) are your own.
- **No anti-cheat evasion.** This tool makes no guarantee of bypassing anti-cheat or avoiding bans; "unlock all achievements" even writes to your real Steam account.
- **Free.** Free and open-source ([MIT](LICENSE)); **reselling is forbidden.** If you paid for it, you were scammed — get it free from this repository.
- **Rights holders.** If a rights holder considers anything here improper, please reach out via a GitHub Issue and the author will comply with takedown or changes.

## 🙏 Credits

- Mod frameworks: [BepInEx](https://github.com/BepInEx/BepInEx) / [HarmonyX](https://github.com/BepInEx/HarmonyX).
- Shares its approach with the sibling project *ADOFAI Trainer* (also a 7th Beat Games title).

<div align="center">
<br>
Licensed under <a href="LICENSE">MIT</a> · Free · Reselling forbidden
<br>
⭐ If it got you a flawless clear on the first take, drop a Star · <a href="https://github.com/Cohenjikan/RhythmDoctorTrainer">github.com/Cohenjikan/RhythmDoctorTrainer</a>
</div>
