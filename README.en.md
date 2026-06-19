<!-- Language switch -->
[简体中文](README.md) | **English**

<div align="center">

<img src="docs/assets/hero.png" alt="Rhythm Doctor Trainer — an in-game GUI trainer opened with F3" width="100%">

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

The menu hotkey is **F3** on every platform.

### Windows (BepInEx / Doorstop — no game files modified)

Open **PowerShell** and paste one line. It **auto-locates the Steam game**, **installs BepInEx if missing**, and downloads the plugin plus a CJK font (existing users get a smooth upgrade by running it too):

```powershell
irm https://raw.githubusercontent.com/Cohenjikan/RhythmDoctorTrainer/refs/heads/main/win_install.ps1 | iex
```

Press **F3** in a level. Uninstall the same way:

```powershell
irm https://raw.githubusercontent.com/Cohenjikan/RhythmDoctorTrainer/refs/heads/main/win_uninstall.ps1 | iex
```
> The old root URLs (`win_install.ps1` / `win_uninstall.ps1`) still work — they're forwarding shims pointing at the new paths above. You can also download manually from **[Releases](https://github.com/Cohenjikan/RhythmDoctorTrainer/releases)**; full steps in [Windows install (detailed)](#windows-install-bepinex-detailed) below.

### macOS / Linux (native — no BepInEx, no .NET SDK, no compiling)

One prebuilt DLL set serves **both** macOS and Linux. Paste one line into a terminal — it **detects your arch**, downloads the prebuilt DLLs + the matching patcher + a CJK font, and weaves the loader into the game's startup:

```bash
curl -fsSL https://raw.githubusercontent.com/Cohenjikan/RhythmDoctorTrainer/refs/heads/main/install.sh | bash
```

Then launch the game via Steam and press **F3** in any level to toggle the menu. Uninstall (restores the `Assembly-CSharp.dll` backup):

```bash
curl -fsSL https://raw.githubusercontent.com/Cohenjikan/RhythmDoctorTrainer/refs/heads/main/uninstall.sh | bash
```
> A game update or Steam "Verify integrity of game files" reverts the patch — just re-run the install command.

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

## Architecture: one shared core + two thin host shells

The trainer has exactly **one** copy of its logic. The ~800-line IMGUI menu used to be duplicated between the Windows and macOS sources; it's now collapsed into a single source of truth:

- **`Shared/`** — the single source of truth, compiled into **both** builds via `<Compile Include="../Shared/*.cs" />`:
  - `TrainerInfo.cs` — Guid / Name / Version / watermark (**bump the version here only**).
  - `Cheats.cs` — shared state + the CJK font loader (CjkFont).
  - `Patches.cs` — Harmony patches.
  - `TrainerMenu.cs` — the whole IMGUI menu + per-frame cheat application.
  - `IHost.cs` — the platform seam (logging + menu hotkey + persisted prefs).
- **`windows/`** — `Plugin.cs`, a thin BepInEx `BaseUnityPlugin` shell implementing `IHost`; `RDTrainer.csproj`.
- **`mac/`** — `TrainerHost.cs`, a thin `MonoBehaviour` shell implementing `IHost`; `Loader.cs` (the woven entry point); `Log.cs`; `RDTrainerMac.csproj`. This single `netstandard2.1` build serves **both macOS and Linux**.
- **`patcher/`** — `Program.cs` + `Patcher.csproj`: a `net8.0` Mono.Cecil static weaver that injects a `Loader.Init()` call into the game's `RDStartup.Setup`.

Both injection paths are unchanged in spirit: **Windows** uses BepInEx / Doorstop (no game files modified); **macOS / Linux** use the Cecil patcher to weave the loader into `Assembly-CSharp.dll` (with a `.rdtrainer-backup` for clean uninstall).

|              | Windows                              | macOS / Linux                                          |
| ------------ | ------------------------------------ | ------------------------------------------------------ |
| Injection    | BepInEx 5 + Doorstop (winhttp.dll)   | Mono.Cecil static weave into the game's startup method |
| Host shell   | `windows/Plugin.cs` (BaseUnityPlugin) | `mac/TrainerHost.cs` (plain MonoBehaviour, spawned by Loader) |
| Patch lib    | HarmonyX bundled with BepInEx        | standalone 0Harmony.dll (Lib.Harmony 2.4.2)            |
| Loading      | BepInEx chainloader                  | Cecil weaves a `Loader.Init()` call into `RDStartup.Setup` |
| Menu logic   | the same `Shared/` core              | the same `Shared/` core                                |

### Distribution (`dist/`)

- `dist/windows/RDTrainer.dll` — the BepInEx plugin.
- `dist/mac/RDTrainerMac.dll` and `dist/mac/0Harmony.dll` — **one copy each**, used by **both macOS and Linux**.

The patcher's self-contained binaries (`patcher-osx-arm64`, `patcher-osx-x64`, `patcher-linux-x64`) are **not in the repo** — they ship via GitHub Releases, and `install.sh` downloads only the one matching your arch.

### Why three patchers but one DLL set?

The trainer's managed DLLs (`RDTrainerMac.dll`, `0Harmony.dll`) are **platform-neutral CIL** and run unchanged on every OS/arch, so there is exactly **one** copy. The patcher, however, is a **self-contained executable** that bundles a native .NET runtime (so users need nothing installed), which is inherently **per-architecture** — hence three builds (`osx-arm64` / `osx-x64` / `linux-x64`). Windows doesn't use the patcher at all — it uses BepInEx.

## Windows install (BepInEx, detailed)

For the **Steam release** (Unity 6 / x64 / Mono). BepInEx 5 is required first.

### Step 1: install BepInEx 5 (x64, Mono)

1. Download **`BepInEx_win_x64_5.4.23.x.zip`** from [BepInEx Releases](https://github.com/BepInEx/BepInEx/releases).
2. **Extract it into the game's root folder** (next to `Rhythm Doctor.exe`; you'll get `winhttp.dll`, `BepInEx/`, etc.).
   > Find the game folder: Steam → right-click *Rhythm Doctor* → Manage → Browse local files.
3. **Launch the game once, then quit**, so BepInEx generates `BepInEx/plugins`, `BepInEx/config`, etc.

### Step 2: install this trainer

**Option A (one-line script, recommended)** — just run the PowerShell one-liner from [Install](#install) above; it auto-locates the game, installs BepInEx if missing, and downloads the plugin + font.

**Option B (manual)** — download [`dist/windows/RDTrainer.dll`](dist/windows/RDTrainer.dll) into:

```text
<game>\BepInEx\plugins\RDTrainer.dll
```

### Verify

After launching, open `<game>\BepInEx\LogOutput.log`; this line means success (the watermark prints your build's full project URL):

```log
[Info : RD Trainer (节奏医生修改器)] RD Trainer (节奏医生修改器) v2.50 loaded · 本工具免费开源，严禁倒卖 · FREE · github.com/Cohenjikan/RhythmDoctorTrainer · Menu key = F3
```

Enter any level and press **F3** to open the menu.

## Quickstart

1. Enter any level, press **F3** to toggle the menu.
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
- **Windows requires BepInEx 5 (x64, Mono); macOS / Linux have no such dependency** (Cecil weave); a major game update may need adapting before it loads again — **no guarantee** of permanent compatibility.
- **macOS / Linux: a game update or Steam "Verify integrity of game files" reverts the patch** — just re-run the install command.
- **Removing or altering the watermark disables the whole trainer** (by design — integrity gate).

## Build from source

> Most users **don't** need to build — just use the one-line scripts above. The repo ships prebuilt artifacts in `dist/`.

Needs the .NET SDK and a copy of the game for the matching platform (for reference DLLs). All three projects compile the same `Shared/` core via `<Compile Include="../Shared/*.cs" />`, and reference game DLLs with `Private=false` — they **do not redistribute** them.

```bash
# Windows plugin (netstandard2.1; needs a copy of the game with BepInEx installed)
# defaults to D:\steam\steamapps\common\Rhythm Doctor; override with -p:GameDir=...
dotnet build windows/RDTrainer.csproj -c Release -p:GameDir="your\Rhythm Doctor"

# macOS / Linux trainer DLL (netstandard2.1; one build serves both)
dotnet build mac/RDTrainerMac.csproj -c Release -p:Managed="<game>/.../Data/Managed"

# self-contained patcher, published per arch (osx-arm64 / osx-x64 / linux-x64)
dotnet publish patcher/Patcher.csproj -c Release -r osx-arm64
```

The Windows plugin output is at `windows/bin/Release/RDTrainer.dll`.

## How it works (in brief)

The trainer does **no memory offset / AOB scanning** — it only calls the game's own switches and functions through Harmony (these patches all live in the shared `Shared/Patches.cs`, used by both platforms):

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

- **Windows (remove just the trainer)**: run the `win_uninstall.ps1` one-liner from [Install](#install), or manually delete `<game>\BepInEx\plugins\RDTrainer.dll`.
- **Windows (remove BepInEx too / restore vanilla)**: delete `winhttp.dll` from the game root (fastest way to disable BepInEx), or delete `winhttp.dll` + the `BepInEx/` folder + `doorstop_config.ini`.
- **macOS / Linux**: run the `uninstall.sh` one-liner; it restores `Assembly-CSharp.dll` from the `.rdtrainer-backup`.
- On any platform you can also "Verify integrity of game files" in Steam to restore everything.

> The config file is at `<game>\BepInEx\config\com.cohen.rdtrainer.cfg` (you can rebind the menu hotkey); delete it too after uninstalling.

## Compatibility

| Item | Value |
|---|---|
| Game | Rhythm Doctor (Steam release) |
| Engine | Unity 6 (6000.3.x) / x64 / Mono |
| Loader (Windows) | BepInEx 5.4.23.x |
| Loader (macOS / Linux) | Mono.Cecil weave + standalone 0Harmony.dll (Lib.Harmony 2.4.2) |
| Trainer DLL target framework | netstandard2.1 (platform-neutral, shared by all platforms) |
| Patcher target framework | net8.0 (self-contained, one build per arch: osx-arm64 / osx-x64 / linux-x64) |

> A major game update may need adapting; on Windows, if it fails to load, first confirm your BepInEx version matches this doc; on macOS / Linux, just re-run the install script. **No guarantee** of permanent compatibility.

## Disclaimer

- **Unofficial**: a fan-made, unofficial third-party tool, **not affiliated with** or endorsed by the developer [7th Beat Games](https://rhythmdr.com/). All rights to *Rhythm Doctor* and its name, trademarks, art, and music belong to 7th Beat Games.
- **No game content**: this repo **contains only the author's own plugin code** — no game source, DLLs, audio, images, or other assets; at runtime it only calls the game's **existing** public functions via Harmony (through BepInEx on Windows, through a standalone 0Harmony.dll on macOS / Linux), with no memory scanning.
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
