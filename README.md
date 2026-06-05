<!-- Language switch -->
**简体中文** | [English](README.en.md)

# 节奏医生 修改器 · Rhythm Doctor Trainer

一个《节奏医生》(Rhythm Doctor) 的**游戏内图形修改器**，基于 BepInEx。按 **Insert** 呼出菜单，集成 Autoplay（全程满分自动演奏）、游戏变速、关卡直达、一键解锁、开发者工具等。

> 注意：**仅供单机自娱与录制使用**（例如制作完美通关视频）。本项目不修改任何在线/对战逻辑，也请勿用于排行榜等场景。与 7th Beat Games 无任何关联。

<p align="center">
  <img src="docs/images/tab-normal.png" width="32%" alt="普通玩家"/>
  <img src="docs/images/tab-dev.png" width="32%" alt="开发者"/>
  <img src="docs/images/tab-advanced.png" width="32%" alt="高级"/>
</p>

## 功能

**普通 / Normal**
- **Autoplay** —— 引擎按谱面帧级满分自动演奏，画面与真人手打无异（无水印），并保留「完美 / JCI」结算标记
- **游戏变速 0.1×–3×** —— 慢放练习 / 加速（含音高；在关卡开始/重开时生效，引擎不支持中途变速）
- **放宽判定窗口** —— 手打也能轻松全 Perfect
- **无敌**（不会失败/被打断）、**瞬间对白**、**跳过菜单转场**、**解锁帧率上限**、**关闭节拍提示音**
- **秒通本关 / 跳过本关**、**解锁全部关卡**

**关卡直达 / Level Jump**
- 列出全部关卡，点一下**直接进入**，绕过 hub 的剧情/NPC 揭示限制 —— 配合 Autoplay 即可录制任意关卡

**开发者 / Developer**
- **开发者模式**（isDev）、**调试模式**（Debug）
- **标记游戏通关 / 全部关卡刷 S / 一键推进全部剧情**（铺满 hub 角色）
- **解锁全部成就**（注意：写入 Steam 账号）、**关闭成就发放**（作弊时防污染账号）
- 打开存档目录、删除存档（二次确认）、杂项调试标志

**高级 / Advanced**
- 音画校准、无限模式记录、展会(Booth)模式、狗狗模式、隐藏曲 Song of the Sea

## 安装

适用于 **Steam 正式版**（Unity 6 / x64 / Mono）。需要先安装 BepInEx 5。

### 第一步：装 BepInEx 5（x64, Mono）
1. 到 [BepInEx Releases](https://github.com/BepInEx/BepInEx/releases) 下载 **`BepInEx_win_x64_5.4.23.x.zip`**。
2. 把压缩包内容**解压到游戏根目录**（即与 `Rhythm Doctor.exe` 同一层；解压后该目录会多出 `winhttp.dll`、`BepInEx/` 等）。
   > 游戏目录怎么找：Steam → 右键《Rhythm Doctor》→ 管理 → 浏览本地文件。
3. **启动一次游戏再退出**，让 BepInEx 生成 `BepInEx/plugins`、`BepInEx/config` 等文件夹。

### 第二步：装本修改器
- **方法 A（手动，推荐）**：下载 [`dist/RDTrainer.dll`](dist/RDTrainer.dll)，放进 `游戏目录\BepInEx\plugins\`。
- **方法 B（脚本）**：下载本仓库 → 编辑 [`tools/install.bat`](tools/install.bat) 里的 `GAME=` 路径（默认是常见 Steam 路径）→ 双击运行，它会把 `dist\RDTrainer.dll` 拷到 `BepInEx\plugins`。

### 验证
启动游戏后，打开 `游戏目录\BepInEx\LogOutput.log`，看到这行即成功：
```
[Info : RD Trainer (节奏医生修改器)] RD Trainer ... loaded. Menu key = Insert.
```
进入任意关卡，按 **Insert** 即可呼出菜单。

## 卸载

- **只移除修改器**：删除 `游戏目录\BepInEx\plugins\RDTrainer.dll`（或运行 [`tools/uninstall.bat`](tools/uninstall.bat)）。
- **连 BepInEx 一起移除 / 恢复原版**：删除游戏根目录的 `winhttp.dll`（最快的"禁用 BepInEx"方式），或删除 `winhttp.dll` + `BepInEx/` 文件夹 + `doorstop_config.ini`。
- 也可以在 Steam 里「验证游戏文件完整性」一键还原。

> 配置文件在 `游戏目录\BepInEx\config\com.cohen.rdtrainer.cfg`（可改菜单热键），卸载后可一并删除。

## 使用

1. 进入任意关卡，按 **Insert** 开/关菜单。
2. 录制完美通关：在「普通」页打开 **Autoplay** → 进关卡 → 用 OBS 等录屏。
3. 想录被剧情锁住的关卡：用「**关卡直达**」页点关卡名直接进入。
4. **变速**：拖动滑块后需在关卡**开始/重开**时生效（菜单内有「重开本关并应用」按钮）。
5. 想自己手打：把「普通」页的 Autoplay 关掉即可。

## 从源码构建

需要 .NET SDK（构建 `netstandard2.1`）、已安装 BepInEx 的游戏副本（用于引用 DLL）。

```bash
# 默认读取 D:\steam\steamapps\common\Rhythm Doctor；其它路径用 -p:GameDir=... 覆盖
dotnet build src/RDTrainer.csproj -c Release -p:GameDir="你的\Rhythm Doctor"
```
产物在 `src/bin/Release/RDTrainer.dll`。

## 兼容性

| 项 | 值 |
|---|---|
| 游戏 | Rhythm Doctor（Steam 正式版） |
| 引擎 | Unity 6（6000.3.x）/ x64 / Mono |
| 加载器 | BepInEx 5.4.23.x |
| 目标框架 | netstandard2.1 |

> 游戏大版本更新后可能需要适配；若加载失败，先确认 BepInEx 版本与本说明一致。

## 致谢与免责

- 游戏《Rhythm Doctor》© [7th Beat Games](https://rhythmdr.com/) —— 本项目与其**无任何关联**，不包含、不分发游戏的任何资源或代码。
- 模组框架 [BepInEx](https://github.com/BepInEx/BepInEx) / [HarmonyX](https://github.com/BepInEx/HarmonyX)。
- 本修改器只调用游戏**自身已存在**的开关与函数（如编辑器自带的 autoplay 标志），属单机娱乐工具，**使用风险自负**。
- 「解锁全部成就」会写入你的 Steam 账号，介意者请配合「关闭成就发放」使用。

本项目代码以 [MIT](LICENSE) 许可证开源。
