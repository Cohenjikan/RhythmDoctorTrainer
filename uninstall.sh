#!/bin/bash
# Rhythm Doctor Trainer · macOS + Linux 一键卸载 —— 还原游戏到原版。
# 与 install.sh 一样靠 uname 检测平台；游戏目录布局/进程名/Steam 位置抽成变量，主逻辑共用。
set -euo pipefail

# --- 平台检测（与 install.sh 保持一致；本脚本可被 curl 单独拉取，故内联而非 source） ---
OS="$(uname -s)"
case "$OS" in
  Darwin)
    GAME_SUBPATH="Rhythm Doctor.app/Contents/Resources/Data/Managed"
    GAME_PROC="Rhythm Doctor.app/Contents/MacOS/Rhythm Doctor"
    STEAM_ROOTS=("$HOME/Library/Application Support/Steam")
    ;;
  Linux)
    GAME_SUBPATH="Rhythm Doctor_Data/Managed"
    GAME_PROC="Rhythm Doctor.x86_64"
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

# --- locate the game's Managed folder (override: MANAGED=... ) ---
# 与 install.sh 相同的查找逻辑：遍历 Steam 根 + libraryfolders.vdf 里的自定义库。
CANDS=()
for root in "${STEAM_ROOTS[@]}"; do
  CANDS+=("$root/steamapps/common/Rhythm Doctor/$GAME_SUBPATH")
  vdf="$root/steamapps/libraryfolders.vdf"
  if [ -f "$vdf" ]; then
    while IFS= read -r libpath; do
      [ -n "$libpath" ] && CANDS+=("$libpath/steamapps/common/Rhythm Doctor/$GAME_SUBPATH")
    done <<< "$(awk -F'"' '/"path"/{print $4}' "$vdf" 2>/dev/null || true)"
  fi
done

if [ -z "${MANAGED:-}" ]; then
  for c in ${CANDS[@]+"${CANDS[@]}"}; do
    # 织入会留下 *.rdtrainer-backup，所以优先认带备份的那份；否则认带 Assembly-CSharp.dll 的那份。
    if [ -f "$c/Assembly-CSharp.dll.rdtrainer-backup" ] || [ -f "$c/Assembly-CSharp.dll" ]; then
      MANAGED="$c"; break
    fi
  done
fi
MANAGED="${MANAGED:-}"

if [ -z "$MANAGED" ]; then
  echo "ERROR: 找不到游戏的 Managed 目录。已尝试以下路径："
  for c in ${CANDS[@]+"${CANDS[@]}"}; do echo "  $c"; done
  echo "用 MANAGED=/path/.../Managed 指定游戏路径后重试。"
  exit 1
fi

BK="$MANAGED/Assembly-CSharp.dll.rdtrainer-backup"

if pgrep -f "$GAME_PROC" >/dev/null 2>&1; then
  echo "ERROR: Rhythm Doctor is running. Quit it first, then re-run."; exit 1
fi

if [ -f "$BK" ]; then
  cp -f "$BK" "$MANAGED/Assembly-CSharp.dll"
  rm -f "$BK"
  echo "restored Assembly-CSharp.dll from backup"
else
  echo "no backup found; if still patched, use Steam → Verify integrity of game files"
fi
rm -f "$MANAGED/RDTrainerMac.dll" "$MANAGED/0Harmony.dll"
echo "removed RDTrainerMac.dll + 0Harmony.dll"
echo "Done — 游戏已还原为原版。"
