#!/bin/bash
# Rhythm Doctor Trainer · macOS + Linux 一键安装（原生，无需 BepInEx / Rosetta）
#
# 懒人一键（在「终端」粘贴运行）：
#   curl -fsSL https://raw.githubusercontent.com/MrTangLuyao/RhythmDoctorTrainerMac/refs/heads/main/install.sh | bash
#
# 脚本会自动：装 .NET SDK（若无）→ 拉取源码 → 编译 → 备份游戏文件 → 织入加载器。
# 游戏更新或 Steam「验证文件完整性」后重跑本命令即可。
#
# 平台无关的托管代码（修改器逻辑 + Mono.Cecil 织入器）在 macOS 与 Linux 上完全一致；
# 本脚本只把「平台相关」的东西（游戏目录布局、Steam 位置、进程名、日志路径、缺包提示）
# 抽成变量，主逻辑两个平台共用。
set -euo pipefail

REPO_GIT="https://github.com/MrTangLuyao/RhythmDoctorTrainerMac.git"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"

# --- 平台检测：把所有平台相关的取值集中在这里，下面主逻辑只用这些变量 ---
#   GAME_SUBPATH  : "Rhythm Doctor"/ 下到 Managed/ 的相对子路径（macOS 是 .app 包，Linux 是 *_Data）
#   GAME_PROC     : pgrep -f 用来判断游戏是否在跑的命令行特征
#   PLAYER_LOG    : Unity player 日志路径（验证补丁是否加载）
#   STEAM_ROOTS   : Steam 安装根目录候选（之后还会叠加 libraryfolders.vdf 里的自定义库）
#   GIT_HINT / CURL_HINT : 缺少 git / curl 时给当前平台的安装建议
OS="$(uname -s)"
case "$OS" in
  Darwin)
    GAME_SUBPATH="Rhythm Doctor.app/Contents/Resources/Data/Managed"
    GAME_PROC="Rhythm Doctor.app/Contents/MacOS/Rhythm Doctor"
    PLAYER_LOG="$HOME/Library/Logs/7th Beat Games/Rhythm Doctor/Player.log"
    STEAM_ROOTS=("$HOME/Library/Application Support/Steam")
    GIT_HINT="先运行：xcode-select --install"
    CURL_HINT="curl 为 macOS 自带；若缺失可用 Homebrew：brew install curl"
    ;;
  Linux)
    GAME_SUBPATH="Rhythm Doctor_Data/Managed"
    GAME_PROC="Rhythm Doctor.x86_64"
    PLAYER_LOG="$HOME/.config/unity3d/7th Beat Games/Rhythm Doctor/Player.log"
    # 原生 Steam、~/.steam 软链、以及 Flatpak 版 Steam 的常见安装根
    STEAM_ROOTS=(
      "$HOME/.local/share/Steam"
      "$HOME/.steam/steam"
      "$HOME/.steam/root"
      "$HOME/.var/app/com.valvesoftware.Steam/.local/share/Steam"
    )
    GIT_HINT="用发行版包管理器安装，例如：sudo apt install git / sudo dnf install git / sudo pacman -S git"
    CURL_HINT="用发行版包管理器安装，例如：sudo apt install curl / sudo dnf install curl / sudo pacman -S curl"
    ;;
  *)
    echo "ERROR: 不支持的平台：$OS（本脚本仅支持 macOS / Linux）。"
    exit 1
    ;;
esac

# --- self-bootstrap: when piped via `curl | bash` there's no source tree, so clone it & re-exec ---
SELF="${BASH_SOURCE[0]:-$0}"
if [ -f "$SELF" ]; then HERE="$(cd "$(dirname "$SELF")" && pwd)"; else HERE=""; fi
if [ -z "$HERE" ] || [ ! -f "$HERE/mac/RDTrainerMac/RDTrainerMac.csproj" ]; then
  if ! command -v git >/dev/null 2>&1; then
    echo "ERROR: 需要 git。$GIT_HINT"; exit 1
  fi
  SRC="${RDT_SRC:-$HOME/.rdtrainer-mac/src}"
  echo "==> 获取源码 -> $SRC"
  if [ -d "$SRC/.git" ]; then git -C "$SRC" pull --ff-only || true
  else mkdir -p "$(dirname "$SRC")"; git clone --depth 1 "$REPO_GIT" "$SRC"; fi
  exec bash "$SRC/install.sh" "$@"
fi

# --- ensure .NET SDK (installs locally to ~/.dotnet, no sudo, fully removable) ---
# dotnet-install.sh 是跨平台的官方 POSIX 脚本（macOS + Linux 通用），装到 ~/.dotnet。
# 只要 `dotnet` 已在 PATH（无论本地安装还是发行版包），就跳过；不掺任何 macOS 假设。
if ! command -v dotnet >/dev/null 2>&1; then
  if ! command -v curl >/dev/null 2>&1; then
    echo "ERROR: 需要 curl 才能下载 .NET SDK 安装脚本。$CURL_HINT"; exit 1
  fi
  echo "==> 未发现 .NET SDK，自动安装到 ~/.dotnet（约 200MB，一次性）…"
  curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0 --install-dir "$HOME/.dotnet" --no-path
  export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$HOME/.dotnet:$PATH"
fi

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

echo "==> 编译修改器 DLL"
dotnet build -c Release "$HERE/mac/RDTrainerMac/RDTrainerMac.csproj" -p:Managed="$MANAGED" | tail -3

# glob 直接喂给 sort -V 取最新版本；不用 ls（避免 SC2012，也更稳）。glob 不匹配时保持字面量，下面 -f 判定会兜住。
HARMONY="$(printf '%s\n' "$HOME"/.nuget/packages/lib.harmony/*/lib/net472/0Harmony.dll | sort -V | tail -1 || true)"
if [ -z "${HARMONY:-}" ] || [ ! -f "$HARMONY" ]; then
  echo "ERROR: NuGet 缓存里找不到 net472 0Harmony.dll。"; exit 1
fi

echo "==> 织入 Assembly-CSharp.dll（把 Loader.Init 织进 RDStartup.Setup）"
dotnet run -c Release --project "$HERE/mac/Patcher/Patcher.csproj" -- \
  "$MANAGED" \
  "$HERE/mac/RDTrainerMac/bin/Release/RDTrainerMac.dll" \
  "$HARMONY"

cat <<EOF

==> 完成！正常通过 Steam 启动游戏，进任意关卡按 F3 开/关菜单。
    验证：  grep RDTrainerMac "$PLAYER_LOG"
    卸载：  curl -fsSL https://raw.githubusercontent.com/MrTangLuyao/RhythmDoctorTrainerMac/refs/heads/main/uninstall.sh | bash
    NOTE: 游戏更新或 Steam「验证文件完整性」会还原补丁 —— 重跑安装命令即可。
EOF
