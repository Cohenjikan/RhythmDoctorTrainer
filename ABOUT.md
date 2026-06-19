# About · 关于

## 这是什么 What

**节奏医生修改器 (Rhythm Doctor Trainer)** 是一个为单机节奏游戏《Rhythm Doctor》制作的游戏内修改器（trainer）。它在游戏里叠加一个图形菜单（按 `F3` 呼出），把游戏里原本隐藏的能力暴露成可一键开关的选项。全平台通用：**Windows** 以 BepInEx 插件形式加载；**macOS / Linux** 通过一个静态织入器把加载器织进游戏启动函数（无需 BepInEx）。

A small in-game trainer for the single-player rhythm game *Rhythm Doctor* that draws an IMGUI overlay (toggled with `F3`). On Windows it loads as a BepInEx plugin; on macOS / Linux a static weaver injects the loader into the game's startup (no BepInEx).

## 为什么 Why

最初的目标只是**录制完美通关视频**：游戏核心是「在第 7 拍按下」，手速对点很难做到全程满分。与其练手速或写键盘宏，不如直接借用游戏引擎**自带的 autoplay**——它按谱面的浮点拍点触发，没有输入延迟，天然帧级满分，画面和真人手打完全一致。后来顺手把扫描游戏文件时发现的其它隐藏能力（变速、解锁、调试、关卡直达等）也一并做进了菜单。

It started as a way to record flawless playthroughs without grinding timing skill, by reusing the engine's built-in autoplay instead of a keyboard macro. Other hidden capabilities found while reverse-engineering the game were then folded into the same menu.

## 架构 Architecture

整个修改器只有**一份**逻辑。曾经约 800 行的 IMGUI 菜单在 Windows 与 macOS 两份源码里重复，现已收敛为**单一共享内核 + 两个轻量宿主壳**：

- **`Shared/`** —— 唯一可信源，通过 `<Compile Include="../Shared/*.cs" />` 同时编进两个构建（`TrainerInfo.cs` 身份与水印、`Cheats.cs` 共享状态与中日韩字体、`Patches.cs` Harmony 补丁、`TrainerMenu.cs` IMGUI 菜单与逐帧应用、`IHost.cs` 平台接缝）。
- **`windows/`** —— `Plugin.cs`，实现 `IHost` 的 BepInEx `BaseUnityPlugin` 壳。
- **`mac/`** —— `TrainerHost.cs`，实现 `IHost` 的 `MonoBehaviour` 壳 + 被织入的 `Loader.cs`；一个 `netstandard2.1` 构建同时服务 macOS 与 Linux。
- **`patcher/`** —— `net8.0` 的 Mono.Cecil 织入器，把 `Loader.Init()` 注入游戏 `RDStartup.Setup`；自包含可执行文件按架构（osx-arm64 / osx-x64 / linux-x64）各发一份。

修改器的托管 DLL 是平台无关 CIL，全平台共用同一份；只有织入器（自带原生 .NET 运行时）需按架构区分。

One shared core (`Shared/`, compiled into both builds) plus two thin host shells (`windows/Plugin.cs` for BepInEx, `mac/TrainerHost.cs` for the Cecil-woven loader). The trainer DLLs are platform-neutral CIL shared by every OS; only the self-contained patcher is per-architecture.

## 工作原理 How it works

修改器**不做内存偏移扫描**，而是直接调用游戏自身已存在的开关和函数（通过 Harmony；Windows 经 BepInEx，macOS / Linux 经独立 0Harmony.dll）。例如：

- **Autoplay** = 把 `DebugSettings.instance.Auto` 设为 `true`——这正是游戏自带关卡编辑器里 autoplay 按钮调用的同一个原生标志（并刻意绕开会显示「autoplay on!」LED 字样的 `ToggleAutoplay`，保持画面干净）。
- **变速** = 设置 `scnGame.levelSpeed`，引擎在关卡加载时据此同时缩放 BPM 与音源音高。
- **关卡直达** = 调用 `scnBase.GoToLevelWithEnum(Level)` 直接载入任意关卡。
- **放宽判定 / 无敌 / 开发者模式** = Harmony 补丁 `GetHitMargin` / `FailLevel` / `isDev`。

因为只是「调用游戏自己的逻辑」，所以比内存修改稳定得多，游戏小更新通常也不易失效。

The trainer pokes the game's own native flags/methods via Harmony (no memory-offset/AOB scanning), which is far more stable across game updates than a traditional external trainer.

## 作者 Author

Cohenjikan · 以 MIT 许可证开源。与 7th Beat Games 无关联。
