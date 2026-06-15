# Rhythm Doctor Trainer — macOS 原生版（无需 BepInEx）

节奏医生修改器的 **macOS 原生移植**。功能与 Windows 版一致（Autoplay、变速、放宽判定、无敌、关卡直达、开发者工具等），
但**不使用 BepInEx / Doorstop**——因为 Unity 6 的 macOS 播放器用 `dlopen`/`dlsym` 动态加载 Mono，
Doorstop 在 macOS 上无法挂钩（详见根因说明）。

## 原理

这游戏是 **Mono** 后端，`Assembly-CSharp.dll` 是可改写的 IL。本方案用 **Mono.Cecil 静态织入**：
在游戏自身的启动函数 `RDStartup.Setup` 开头插入一句 `RDTrainerMac.Loader.Init()`。
游戏一启动就自动把修改器带起来——零注入器、原生运行（Apple Silicon arm64 直接可用，无需 Rosetta）。

- 修改器逻辑：`RDTrainerMac.dll`（由 `src/` 的源码移植，去掉 BepInEx 外壳，改为普通 MonoBehaviour）
- 运行时补丁库：`0Harmony.dll`（pardeike Lib.Harmony 2.4.2，自包含；2.4 起原生支持 Apple Silicon arm64）
- 织入器：`Patcher/`（Mono.Cecil 控制台程序，幂等：总是从纯净备份重新打补丁，绝不重复注入）

## 安装

需要 .NET SDK（已装在 `~/.dotnet`）。在**仓库根目录**直接运行：

```bash
./install.sh
```

脚本会：编译修改器 → 备份 `Assembly-CSharp.dll`（`*.rdtrainer-backup`）→ 把 `RDTrainerMac.dll` + `0Harmony.dll`
拷进游戏的 `Managed/` → 织入启动钩子。

然后**正常通过 Steam 启动游戏**，进任意关卡按 **Insert** 开/关菜单。

> 自定义游戏路径：`MANAGED="/path/to/Rhythm Doctor.app/Contents/Resources/Data/Managed" mac/install.sh`

## 验证是否加载

```bash
grep RDTrainerMac "$HOME/Library/Logs/7th Beat Games/Rhythm Doctor/Player.log"
# 期望看到： ... loaded · ... · patches ok=4 fail=0
```

## 卸载 / 还原

```bash
./uninstall.sh
```

从备份还原 `Assembly-CSharp.dll`，删除两个 DLL。或用 Steam「验证游戏文件完整性」一键还原。

## 注意

- **游戏更新或 Steam「验证文件完整性」会还原 `Assembly-CSharp.dll`，补丁随之失效**——重新跑一次 `install.sh` 即可。
- 仅单机；写存档/成就的功能与 Windows 版一致，谨慎使用。
- 保留了原作者的免费/防倒卖水印（删改水印会触发自校验、修改器自动失效）。
