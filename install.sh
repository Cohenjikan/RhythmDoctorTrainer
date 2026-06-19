#!/bin/bash
# Rhythm Doctor Trainer · macOS + Linux 一键安装（原生，无需 BepInEx / Rosetta）
#
# 懒人一键（在「终端」粘贴运行）：
#   curl -fsSL https://raw.githubusercontent.com/Cohenjikan/RhythmDoctorTrainer/refs/heads/main/install.sh | bash
#
# 脚本会自动：下载预编译产物（修改器 DLL + 织入器 + 字体）→ 备份游戏文件 → 织入加载器。
# 无需 .NET SDK、无需源码、无需编译；可直接 `curl | bash` 运行。
# 游戏更新或 Steam「验证文件完整性」后重跑本命令即可。
#
# 平台无关的托管代码（修改器逻辑 + Mono.Cecil 织入器）在 macOS 与 Linux 上完全一致；
# 本脚本只把「平台相关」的东西（游戏目录布局、Steam 位置、进程名、日志路径）
# 抽成变量，主逻辑两个平台共用。
set -euo pipefail

# 所有预编译产物均取自 Cohenjikan/RhythmDoctorTrainer 的 main 分支 / 最新 release。
RAW_BASE="https://raw.githubusercontent.com/Cohenjikan/RhythmDoctorTrainer/refs/heads/main"
REL_BASE="https://github.com/Cohenjikan/RhythmDoctorTrainer/releases/latest/download"

# --- 平台检测：把所有平台相关的取值集中在这里，下面主逻辑只用这些变量 ---
#   GAME_SUBPATH  : "Rhythm Doctor"/ 下到 Managed/ 的相对子路径（macOS 是 .app 包，Linux 是 *_Data）
#   GAME_PROC     : pgrep -f 用来判断游戏是否在跑的命令行特征
#   PLAYER_LOG    : Unity player 日志路径（验证补丁是否加载）
#   STEAM_ROOTS   : Steam 安装根目录候选（之后还会叠加 libraryfolders.vdf 里的自定义库）
#   FONT_DIR      : 用户级字体目录
OS="$(uname -s)"
case "$OS" in
  Darwin)
    GAME_SUBPATH="Rhythm Doctor.app/Contents/Resources/Data/Managed"
    GAME_PROC="Rhythm Doctor.app/Contents/MacOS/Rhythm Doctor"
    PLAYER_LOG="$HOME/Library/Logs/7th Beat Games/Rhythm Doctor/Player.log"
    STEAM_ROOTS=("$HOME/Library/Application Support/Steam")
    FONT_DIR="$HOME/Library/Fonts"   # 用户级字体目录，CoreText 立即可见，无需刷新缓存
    ;;
  Linux)
    GAME_SUBPATH="Rhythm Doctor_Data/Managed"
    GAME_PROC="Rhythm Doctor.x86_64"
    PLAYER_LOG="$HOME/.config/unity3d/7th Beat Games/Rhythm Doctor/Player.log"
    FONT_DIR="$HOME/.local/share/fonts"   # 用户级字体目录，装完用 fc-cache 刷新
    # 原生 Steam、~/.steam 软链、以及 Flatpak 版 Steam 的常见安装根
    STEAM_ROOTS=(
      "$HOME/.local/share/Steam"
      "$HOME/.steam/steam"
      "$HOME/.steam/root"
      "$HOME/.var/app/com.valvesoftware.Steam/.local/share/Steam"
    )
    ;;
  *)
    echo "ERROR: 不支持的平台：$OS（本脚本仅支持 macOS / Linux）。"
    exit 1
    ;;
esac

# --- 把 uname 映射到 .NET runtime id（RID），用于挑选对应的预编译织入器 ---
# 归一化 arch：arm64/aarch64 -> arm64 ; x86_64/amd64 -> x64。
ARCH="$(uname -m)"
case "$ARCH" in
  arm64|aarch64) ARCH="arm64" ;;
  x86_64|amd64)  ARCH="x64" ;;
esac
case "$OS/$ARCH" in
  Darwin/arm64) RID="osx-arm64" ;;
  Darwin/x64)   RID="osx-x64" ;;
  Linux/x64)    RID="linux-x64" ;;
  *)
    echo "ERROR: 不支持的系统/架构组合：$OS/$(uname -m)。"
    echo "       仅支持 macOS(arm64/x86_64) 与 Linux(x86_64)。"
    exit 1
    ;;
esac

# --- 需要 curl 才能下载预编译产物 ---
if ! command -v curl >/dev/null 2>&1; then
  echo "ERROR: 需要 curl 才能下载预编译产物。"
  echo "       macOS 自带 curl；Linux 用发行版包管理器安装，例如 sudo apt install curl。"
  exit 1
fi

# --- 工作目录（下载产物用），退出时自动清理 ---
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

# --- locate the game's Managed folder (override: MANAGED=... ) ---
# 依次尝试每个 Steam 根目录，并解析其 libraryfolders.vdf 里登记的自定义库路径。
# 不写死单一路径：默认库 = Steam 根自身，其余库 = vdf 里的 "path" 项。
CANDS=()
for root in "${STEAM_ROOTS[@]}"; do
  CANDS+=("$root/steamapps/common/Rhythm Doctor/$GAME_SUBPATH")
  vdf="$root/steamapps/libraryfolders.vdf"
  if [ -f "$vdf" ]; then
    # vdf 里每个库一行：  "path"\t\t"/some/Steam Library" —— 按引号切分取第 4 段即路径。
    # 只读解析，awk 在 macOS(BSD) 与 Linux(GNU/mawk) 行为一致；|| true 防止 set -e 误杀。
    while IFS= read -r libpath; do
      [ -n "$libpath" ] && CANDS+=("$libpath/steamapps/common/Rhythm Doctor/$GAME_SUBPATH")
    done <<< "$(awk -F'"' '/"path"/{print $4}' "$vdf" 2>/dev/null || true)"
  fi
done

if [ -z "${MANAGED:-}" ]; then
  for c in ${CANDS[@]+"${CANDS[@]}"}; do
    if [ -f "$c/Assembly-CSharp.dll" ]; then MANAGED="$c"; break; fi
  done
fi
MANAGED="${MANAGED:-}"

if [ -z "$MANAGED" ] || [ ! -f "$MANAGED/Assembly-CSharp.dll" ]; then
  echo "ERROR: 找不到 Assembly-CSharp.dll。已尝试以下路径："
  for c in ${CANDS[@]+"${CANDS[@]}"}; do echo "  $c"; done
  echo "用 MANAGED=/path/.../Managed 指定游戏路径后重试。"
  exit 1
fi

# --- refuse to patch while the game is running (the DLL is mapped in memory) ---
if pgrep -f "$GAME_PROC" >/dev/null 2>&1; then
  echo "ERROR: 游戏正在运行，请先退出再安装。"; exit 1
fi

# --- 下载预编译产物（curl -fL 跟随重定向）---
# 修改器 DLL 与 0Harmony 取自 main 分支的 dist/mac；织入器取自最新 release 的 patcher-<RID>。
echo "==> 下载预编译产物（RID=$RID）"
curl -fL "$RAW_BASE/dist/mac/RDTrainerMac.dll"     -o "$WORK/RDTrainerMac.dll"
curl -fL "$RAW_BASE/dist/mac/0Harmony.dll"         -o "$WORK/0Harmony.dll"
curl -fL "$REL_BASE/patcher-$RID"                  -o "$WORK/patcher"
curl -fL "$RAW_BASE/fonts/NotoSansSC-Regular.otf"  -o "$WORK/NotoSansSC-Regular.otf"
chmod +x "$WORK/patcher"

# --- 织入 Assembly-CSharp.dll ---
# 织入器会：创建 Assembly-CSharp.dll.rdtrainer-backup（一次性原版备份）、把两个 DLL 拷进 Managed/、
# 把 Loader.Init 织进 RDStartup.Setup。其 CLI 形如：patcher <ManagedDir> <RDTrainerMac.dll> <0Harmony.dll>
echo "==> 织入 Assembly-CSharp.dll（把 Loader.Init 织进 RDStartup.Setup）"
"$WORK/patcher" "$MANAGED" "$WORK/RDTrainerMac.dll" "$WORK/0Harmony.dll"

# --- 安装菜单用的中文字体（三端统一按家族名 "Noto Sans SC" 加载）---
# Unity 的 IMGUI 只能用「操作系统已注册」的字体，没有运行时直接读取 .ttf 的接口，所以把
# 随仓库附带的 Noto Sans SC 装进当前用户的字体目录（无需 sudo）。Linux 上若有 fc-cache 顺手刷新缓存。
# 缺字体文件只警告、不致命（运行时会回退到系统已有的中文字体）。
FONT_SRC="$WORK/NotoSansSC-Regular.otf"
if [ -f "$FONT_SRC" ]; then
  mkdir -p "$FONT_DIR"
  cp -f "$FONT_SRC" "$FONT_DIR/NotoSansSC-Regular.otf"
  command -v fc-cache >/dev/null 2>&1 && fc-cache -f "$FONT_DIR" >/dev/null 2>&1 || true
  echo "==> 已安装中文字体 Noto Sans SC -> $FONT_DIR"
else
  echo "WARN: 未下载到随附字体，跳过；菜单中文将回退到系统已有的中文字体。"
fi

cat <<EOF

==> 完成！正常通过 Steam 启动游戏，进任意关卡按 F3 开/关菜单。
    验证：  grep RDTrainerMac "$PLAYER_LOG"
    卸载：  curl -fsSL https://raw.githubusercontent.com/Cohenjikan/RhythmDoctorTrainer/refs/heads/main/uninstall.sh | bash
    NOTE: 游戏更新或 Steam「验证文件完整性」会还原补丁 —— 重跑安装命令即可。
EOF
