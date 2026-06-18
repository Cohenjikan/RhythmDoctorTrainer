# RD Trainer (节奏医生修改器) · Windows 一键卸载 (PowerShell)
#
# 懒人一键（在 PowerShell 粘贴运行）：
#   irm https://raw.githubusercontent.com/Cohenjikan/RhythmDoctorTrainer/refs/heads/main/win_uninstall.ps1 | iex
$ErrorActionPreference = 'Stop'
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch {}

Write-Host ""
Write-Host "==== 节奏医生修改器  Windows 卸载 ====" -ForegroundColor Cyan
Write-Host ""

function Find-Game {
    $cands = New-Object System.Collections.Generic.List[string]
    $steam = $null
    foreach ($k in 'HKCU:\Software\Valve\Steam','HKLM:\SOFTWARE\WOW6432Node\Valve\Steam','HKLM:\SOFTWARE\Valve\Steam') {
        try {
            $pp = Get-ItemProperty -Path $k -ErrorAction Stop
            if ($pp.SteamPath) { $steam = $pp.SteamPath } elseif ($pp.InstallPath) { $steam = $pp.InstallPath }
            if ($steam) { break }
        } catch {}
    }
    if ($steam) {
        $steam = $steam -replace '/','\'; $cands.Add($steam)
        $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
        if (Test-Path $vdf) { foreach ($line in Get-Content $vdf) { if ($line -match '"path"\s+"(.+?)"') { $cands.Add(($matches[1] -replace '\\\\','\')) } } }
    }
    'C:\Program Files (x86)\Steam','C:\Program Files\Steam','D:\steam','D:\SteamLibrary','D:\Steam','E:\steam','E:\SteamLibrary' | ForEach-Object { $cands.Add($_) }
    foreach ($lib in ($cands | Select-Object -Unique)) {
        $g = Join-Path $lib 'steamapps\common\Rhythm Doctor'
        if (Test-Path (Join-Path $g 'Rhythm Doctor.exe')) { return $g }
    }
    return $null
}

$game = Find-Game
if (-not $game) { $game = (Read-Host '没自动找到游戏，请粘贴《Rhythm Doctor》目录后回车').Trim().Trim('"') }
if (-not (Test-Path (Join-Path $game 'Rhythm Doctor.exe'))) { Write-Host "[错误] 目录无效。" -ForegroundColor Red; return }

$dll = Join-Path $game 'BepInEx\plugins\RDTrainer.dll'
if (Test-Path $dll) { Remove-Item $dll -Force; Write-Host "已删除修改器: $dll" -ForegroundColor Green }
else { Write-Host "未发现修改器 DLL（可能已删）。" -ForegroundColor Yellow }

# 移除安装时装进用户字体目录的中文字体 + 注册表项
try {
    $fontPath = Join-Path $env:LOCALAPPDATA 'Microsoft\Windows\Fonts\NotoSansSC-Regular.otf'
    if (Test-Path $fontPath) { Remove-Item $fontPath -Force -ErrorAction SilentlyContinue }
    Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows NT\CurrentVersion\Fonts' -Name 'Noto Sans SC (OpenType)' -ErrorAction SilentlyContinue
    Write-Host "已移除中文字体 Noto Sans SC。" -ForegroundColor Green
} catch {}

$ans = Read-Host "是否连 BepInEx 一起移除、彻底恢复原版？(y/N)"
if ($ans -match '^[Yy]') {
    foreach ($p in 'winhttp.dll','doorstop_config.ini','.doorstop_version','changelog.txt') {
        $f = Join-Path $game $p; if (Test-Path $f) { Remove-Item $f -Force -ErrorAction SilentlyContinue }
    }
    $bep = Join-Path $game 'BepInEx'; if (Test-Path $bep) { Remove-Item $bep -Recurse -Force -ErrorAction SilentlyContinue }
    Write-Host "已移除 BepInEx，游戏恢复原版。" -ForegroundColor Green
} else {
    Write-Host "保留 BepInEx（只移除了本修改器）。" -ForegroundColor Green
}
Write-Host ""
