# About · 关于

## 这是什么 What

**节奏医生修改器 (Rhythm Doctor Trainer)** 是一个为单机节奏游戏《Rhythm Doctor》制作的游戏内修改器（trainer）。它以一个 BepInEx 插件的形式运行，在游戏里叠加一个图形菜单（按 `Insert` 呼出），把游戏里原本隐藏的能力暴露成可一键开关的选项。

A small in-game trainer for the single-player rhythm game *Rhythm Doctor*, shipped as a BepInEx plugin that draws an IMGUI overlay (toggled with `Insert`).

## 为什么 Why

最初的目标只是**录制完美通关视频**：游戏核心是「在第 7 拍按下」，手速对点很难做到全程满分。与其练手速或写键盘宏，不如直接借用游戏引擎**自带的 autoplay**——它按谱面的浮点拍点触发，没有输入延迟，天然帧级满分，画面和真人手打完全一致。后来顺手把扫描游戏文件时发现的其它隐藏能力（变速、解锁、调试、关卡直达等）也一并做进了菜单。

It started as a way to record flawless playthroughs without grinding timing skill, by reusing the engine's built-in autoplay instead of a keyboard macro. Other hidden capabilities found while reverse-engineering the game were then folded into the same menu.

## 工作原理 How it works

修改器**不做内存偏移扫描**，而是直接调用游戏自身已存在的开关和函数（通过 BepInEx + HarmonyX）。例如：

- **Autoplay** = 把 `DebugSettings.instance.Auto` 设为 `true`——这正是游戏自带关卡编辑器里 autoplay 按钮调用的同一个原生标志（并刻意绕开会显示「autoplay on!」LED 字样的 `ToggleAutoplay`，保持画面干净）。
- **变速** = 设置 `scnGame.levelSpeed`，引擎在关卡加载时据此同时缩放 BPM 与音源音高。
- **关卡直达** = 调用 `scnBase.GoToLevelWithEnum(Level)` 直接载入任意关卡。
- **放宽判定 / 无敌 / 开发者模式** = Harmony 补丁 `GetHitMargin` / `FailLevel` / `isDev`。

因为只是「调用游戏自己的逻辑」，所以比内存修改稳定得多，游戏小更新通常也不易失效。

The trainer pokes the game's own native flags/methods via BepInEx + HarmonyX (no memory-offset/AOB scanning), which is far more stable across game updates than a traditional external trainer.

## 作者 Author

Cohenjikan · 以 MIT 许可证开源。与 7th Beat Games 无关联。
