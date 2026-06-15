<!-- Language switch -->
[简体中文](README.md) | **English**

<div align="center">

<img src="docs/assets/hero.png" alt="Rhythm Doctor Trainer — an in-game GUI trainer opened with Insert" width="100%">

<sub> <a href="docs/assets/promo.mp4">Watch the 30-second promo</a></sub>

# Rhythm Doctor Trainer · all platforms

**Press F3 and you ARE Dr. Ian.**

An in-game GUI trainer for *Rhythm Doctor*, built for **single-player recording of flawless clears** — never for online play.

<br>

[![License: MIT](https://img.shields.io/github/license/Cohenjikan/RhythmDoctorTrainer?style=flat-square)](LICENSE)
[![Release](https://img.shields.io/github/v/release/Cohenjikan/RhythmDoctorTrainer?style=flat-square&label=release)](https://github.com/Cohenjikan/RhythmDoctorTrainer/releases)
[![Stars](https://img.shields.io/github/stars/Cohenjikan/RhythmDoctorTrainer?style=flat-square)](https://github.com/Cohenjikan/RhythmDoctorTrainer/stargazers)
[![Issues](https://img.shields.io/github/issues/Cohenjikan/RhythmDoctorTrainer?style=flat-square)](https://github.com/Cohenjikan/RhythmDoctorTrainer/issues)
[![BepInEx 5](https://img.shields.io/badge/BepInEx-5.4.23.x-blue?style=flat-square)](https://github.com/BepInEx/BepInEx)
[![Unity 6](https://img.shields.io/badge/Unity-6%20x64%20Mono-black?style=flat-square)](#compatibility)
[![Single-player only](https://img.shields.io/badge/scope-single--player%20offline-success?style=flat-square)](#disclaimer)

<br>

</div>

---

> [!IMPORTANT]
> **Single-player & recording only** (e.g. making flawless-clear videos). This project touches no online / versus / leaderboard logic — please don't use it anywhere it affects others' fairness. **Not affiliated with 7th Beat Games.**
>
> **Completely free and open-source (MIT) — reselling is forbidden.** Ships with a built-in integrity check.

## Install

### Windows

See the **[Releases](https://github.com/Cohenjikan/RhythmDoctorTrainer/releases)** in the right sidebar.

For full manual-install steps, see [Windows install (detailed)](#windows-install-bepinex-detailed) below.

### macOS / Linux (native, no BepInEx)

Paste one line into a terminal (auto-installs the .NET SDK → builds → weaves the loader into the game's startup):

```bash
curl -fsSL https://raw.githubusercontent.com/Cohenjikan/RhythmDoctorTrainer/refs/heads/main/install.sh | bash
```

Then launch the game via Steam and press **F3** in any level to toggle the menu. Uninstall (one line too):

```bash
curl -fsSL https://raw.githubusercontent.com/Cohenjikan/RhythmDoctorTrainer/refs/heads/main/uninstall.sh | bash
```
> After a game update or Steam "Verify integrity of game files," just re-run the install command.

---

## Showcase

![feature](docs/assets/feature.png)

## Core features

### Frame-perfect Autoplay

The engine auto-plays the chart perfectly — **no input lag, no "autoplay on!" LED text on screen** — identical to a real hand-played run. Pair it with Hide HUD to record clean footage.

- Keeps the **"Perfect / JCI" result marker** that the game normally hides during autoplay (on by default)
- Uses the engine's native `DebugSettings.Auto` flag — **not a keyboard macro / synthetic input**
- Want to play by hand? Just turn it off.

<br clear="right">

### Auto-restart on miss

For perfectionists chasing a flawless run, the trainer can automatically restart the level the moment you miss.

<br clear="right">

### Game speed 0.1×–3× (pitch included)

Slow down to nail details or speed up to grind; **BPM and audio pitch scale together**, so it sounds consistent.

> Takes effect at level **start / restart** — **you can't change speed mid-level** (the engine fixes BPM and pitch at load; changing mid-run desyncs). The menu has a "restart this level & apply" button.

### Widen judgement window / No-Fail

- **Widen judgement window ×1–×10** (default ×3) — score all-Perfect even by hand
- **No-Fail** — never fail or get interrupted mid-level
- **Instant dialogue / skip menu transitions / unlock framerate cap / mute beat-cue sounds**

### Level / hidden-level jump

Lists **every level** (with text filter); click one to **jump straight in**, bypassing the hub's story / NPC unlock gates — pair with Autoplay to record any level, including story-locked ones.

<br clear="right">

### Developer / Debug mode + achievement control

**Developer mode (isDev) / Debug mode (Debug)**, plus a set of low-level debug toggles (NoPro / ForceNoSteamworks / EmulateMobile / RunningOnSteamDeck / DebugAmbience / PaigeStays / PauseOnFocusLost).

<br clear="right">

### Save editing

Read & apply audio/visual calibration (visual / input / latency), edit the "Rhythm Weightlifter" infinite-mode best round, trigger Booth mode / Dog mode / the hidden track *Song of the Sea*.

- **Whether Paige stays**
- **Unlock all achievements** (writes to your Steam account) / **disable achievement granting** (avoid polluting your account while cheating)
- One-click S+
- One-click story progression

## How it works: Windows vs macOS / Linux

|              | Windows                              | macOS / Linux                                          |
| ------------ | ------------------------------------ | ------------------------------------------------------ |
| Injection    | BepInEx 5 + Doorstop (winhttp.dll)   | Mono.Cecil static weave into the game's startup method |
| Plugin form  | BaseUnityPlugin                      | plain MonoBehaviour (spawned by Loader)                |
| Patch lib    | HarmonyX bundled with BepInEx        | standalone 0Harmony.dll (Lib.Harmony 2.4.2)            |
| Runtime      | uses the prebuilt result             | builds + runs                                          |

## Windows install (BepInEx, detailed)

For the **Steam release** (Unity 6 / x64 / Mono). BepInEx 5 is required first.

### Step 1: install BepInEx 5 (x64, Mono)

1. Download **`BepInEx_win_x64_5.4.23.x.zip`** from [BepInEx Releases](https://github.com/BepInEx/BepInEx/releases).
2. **Extract it into the game's root folder** (next to `Rhythm Doctor.exe`; you'll get `winhttp.dll`, `BepInEx/`, etc.).
   > Find the game folder: Steam → right-click *Rhythm Doctor* → Manage → Browse local files.
3. **Launch the game once, then quit**, so BepInEx generates `BepInEx/plugins`, `BepInEx/config`, etc.

### Step 2: install this trainer

**Option A (manual, recommended)** — download [`dist/RDTrainer.dll`](dist/RDTrainer.dll) (~23 KB) into:

```text
<game>\BepInEx\plugins\RDTrainer.dll
```

**Option B (script)** — clone this repo → edit the `GAME=` path in [`tools/install.bat`](tools/install.bat) (defaults to a common Steam path) → double-click to run; it copies `dist\RDTrainer.dll` into `BepInEx\plugins`.

### Verify

After launching, open `<game>\BepInEx\LogOutput.log`; this line means success (the watermark prints your build's full project URL):

```log
[Info : RD Trainer (节奏医生修改器)] RD Trainer (节奏医生修改器) v2.41 loaded · 本工具免费开源，严禁倒卖 · FREE · github.com/Cohenjikan/RhythmDoctorTrainer · Menu key = Insert
```

Enter any level and press **Insert** to open the menu.

## Quickstart

1. Enter any level, press **Insert** to toggle the menu.
2. **Record a flawless clear**: on the "Normal" tab turn on **Autoplay** → enter a level → capture with OBS, etc.
3. **Record a story-locked level**: switch to the **Level jump** tab and click a level name to enter directly.
4. **Speed**: after moving the slider it applies at level **start / restart** (the menu has a "restart & apply" button).
5. **Play by hand**: just turn off Autoplay on the "Normal" tab.

## Good to know / caveats

- **Single-player / offline only** — touches no online, versus, or leaderboard logic; don't use it where it affects others' fairness.
- **Speed only applies at level start / restart**, not mid-level (the engine fixes BPM and pitch at load); use "restart & apply."
- **"Unlock all achievements" writes to your real Steam account** — if you care, pair it with "disable achievement granting."
- **Debug mode draws debug text on screen** — not ideal for clean recording.
- **Deleting saves / advancing story modifies your save file** — deletion is irreversible (with a confirm step).
- **Modding may violate the game's EULA / ToS**; use at your own risk (account penalties, save corruption are possible).
- **Unofficial fan tool**, not affiliated with or authorized by 7th Beat Games; ships no game source, DLLs, audio, or art.
- **Requires BepInEx 5 (x64, Mono)**; a major game update may need adapting before it loads again — **no guarantee** of permanent compatibility.
- **Removing or altering the watermark disables the whole trainer** (by design — integrity gate).

## Build from source

Needs the .NET SDK (targets `netstandard2.1`) and a copy of the game with BepInEx installed (for reference DLLs).

```bash
# defaults to D:\steam\steamapps\common\Rhythm Doctor; override with -p:GameDir=...
dotnet build src/RDTrainer.csproj -c Release -p:GameDir="your\Rhythm Doctor"
```

Output is at `src/bin/Release/RDTrainer.dll`. The repo references game DLLs with `Private=false` — it **does not redistribute** them.

## How it works (in brief)

The trainer does **no memory offset / AOB scanning** — it only calls the game's own switches and functions through BepInEx + HarmonyX:

| Feature | Implementation |
|---|---|
| Autoplay | sets `DebugSettings.instance.Auto` (deliberately bypassing `ToggleAutoplay`, which flashes the LED text) |
| Keep perfect marker | Harmony postfix on `LevelBase.isZeroOffset` (forces true only at zero offset and zero mistakes) |
| Speed | writes static `scnGame.levelSpeed`; the engine scales BPM and pitch at level Start |
| Widen judgement | postfix on `scnGame.GetHitMargin` multiplied by a factor |
| No-Fail | reflectively finds every single-arg `FailLevel` across the assembly and prefixes-skips the original |
| Level jump | calls `scnBase.GoToLevelWithEnum(Level)` |

It just "calls the game's own logic," so it's inherently more stable than memory editing and usually survives minor game updates. See [ABOUT.md](ABOUT.md).

## Uninstall

- **Remove just the trainer**: delete `<game>\BepInEx\plugins\RDTrainer.dll` (or run [`tools/uninstall.bat`](tools/uninstall.bat)).
- **Remove BepInEx too / restore vanilla**: delete `winhttp.dll` from the game root (fastest way to disable BepInEx), or delete `winhttp.dll` + the `BepInEx/` folder + `doorstop_config.ini`.
- You can also "Verify integrity of game files" in Steam to restore everything.

> The config file is at `<game>\BepInEx\config\com.cohen.rdtrainer.cfg` (you can rebind the menu hotkey); delete it too after uninstalling.

## Compatibility

| Item | Value |
|---|---|
| Game | Rhythm Doctor (Steam release) |
| Engine | Unity 6 (6000.3.x) / x64 / Mono |
| Loader | BepInEx 5.4.23.x |
| Target framework | netstandard2.1 |

> A major game update may need adapting; if it fails to load, first confirm your BepInEx version matches this doc. **No guarantee** of permanent compatibility.

## Disclaimer

- **Unofficial**: a fan-made, unofficial third-party tool, **not affiliated with** or endorsed by the developer [7th Beat Games](https://rhythmdr.com/). All rights to *Rhythm Doctor* and its name, trademarks, art, and music belong to 7th Beat Games.
- **No game content**: this repo **contains only the author's own plugin code** — no game source, DLLs, audio, images, or other assets; at runtime it only calls the game's **existing** public functions via BepInEx / HarmonyX, with no memory scanning.
- **Single-player only**: for **offline single-player** fun, practice, and recording. Do **not** use it online, on leaderboards, in versus, or anywhere it affects other players' fairness.
- **Respect the EULA**: modding may violate the game's EULA / ToS. Whether to use it is your call, and you bear all consequences (account penalties, save corruption, etc.).
- **No anti-cheat bypass**: this tool offers no "bypass anti-cheat / avoid bans" guarantee; "unlock all achievements" even writes to your real Steam account.
- **Completely free**: free and open-source ([MIT](LICENSE)), **reselling forbidden**; if you paid for it, get it free from this repo.
- **Rights-holder objections**: if a rights holder finds anything inappropriate here, reach out via a GitHub Issue and the author will cooperate to take it down or adjust.

## Credits

- Modding frameworks [BepInEx](https://github.com/BepInEx/BepInEx) / [HarmonyX](https://github.com/BepInEx/HarmonyX).
- Same approach and lineage as the sister project "ADOFAI Trainer / 冰与火之舞修改器" (also a 7th Beat Games title).

<div align="center">
<br>
Open-source under <a href="LICENSE">MIT</a> · free · reselling forbidden
<br>
If it helped you capture that flawless run, drop a Star · <a href="https://github.com/Cohenjikan/RhythmDoctorTrainer">github.com/Cohenjikan/RhythmDoctorTrainer</a>
</div>
