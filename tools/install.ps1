# RD Trainer (节奏医生修改器) — one-click installer
# 自动定位游戏 → 没装 BepInEx 就自动下载安装 → 放入插件。无需手动操作。
$ErrorActionPreference = 'Stop'
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch {}

$BepVer = '5.4.23.5'
$BepUrl = "https://github.com/BepInEx/BepInEx/releases/download/v$BepVer/BepInEx_win_x64_$BepVer.zip"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# 找到随脚本附带的 RDTrainer.dll（同目录 / dist / 上级 dist）
$Plugin = @(
    (Join-Path $ScriptDir 'RDTrainer.dll'),
    (Join-Path $ScriptDir 'dist\RDTrainer.dll'),
    (Join-Path $ScriptDir '..\dist\RDTrainer.dll')
) | Where-Object { Test-Path $_ } | Select-Object -First 1

Write-Host ""
Write-Host "==== 节奏医生修改器  一键安装 ====" -ForegroundColor Cyan
Write-Host ""

if (-not $Plugin) {
    Write-Host "[错误] 没找到 RDTrainer.dll，请确保它和本脚本在同一文件夹。" -ForegroundColor Red
    exit 1
}

function Find-Game {
    $cands = New-Object System.Collections.Generic.List[string]
    # 1) 从注册表拿 Steam 路径
    $steam = $null
    foreach ($k in 'HKCU:\Software\Valve\Steam','HKLM:\SOFTWARE\WOW6432Node\Valve\Steam','HKLM:\SOFTWARE\Valve\Steam') {
        try {
            $pp = Get-ItemProperty -Path $k -ErrorAction Stop
            if ($pp.SteamPath)   { $steam = $pp.SteamPath }
            elseif ($pp.InstallPath) { $steam = $pp.InstallPath }
            if ($steam) { break }
        } catch {}
    }
    # 2) 解析 libraryfolders.vdf 拿到所有 Steam 库
    if ($steam) {
        $steam = $steam -replace '/','\'
        $cands.Add($steam)
        $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
        if (Test-Path $vdf) {
            foreach ($line in Get-Content $vdf) {
                if ($line -match '"path"\s+"(.+?)"') { $cands.Add(($matches[1] -replace '\\\\','\')) }
            }
        }
    }
    # 3) 常见兜底路径
    'C:\Program Files (x86)\Steam','C:\Program Files\Steam','D:\steam','D:\SteamLibrary','D:\Steam','E:\steam','E:\SteamLibrary' | ForEach-Object { $cands.Add($_) }

    foreach ($lib in ($cands | Select-Object -Unique)) {
        $g = Join-Path $lib 'steamapps\common\Rhythm Doctor'
        if (Test-Path (Join-Path $g 'Rhythm Doctor.exe')) { return $g }
    }
    return $null
}

$game = Find-Game
if (-not $game) {
    Write-Host "没能自动找到《Rhythm Doctor》。" -ForegroundColor Yellow
    Write-Host "请打开 Steam → 右键游戏 → 管理 → 浏览本地文件，复制地址栏路径，粘贴到这里后回车："
    $game = (Read-Host '游戏目录').Trim().Trim('"')
}
if (-not (Test-Path (Join-Path $game 'Rhythm Doctor.exe'))) {
    Write-Host "[错误] 该目录下没有 Rhythm Doctor.exe：`n  $game" -ForegroundColor Red
    exit 1
}
Write-Host "游戏目录: $game" -ForegroundColor Green

# 安装 BepInEx（若未安装）
$hasBep = (Test-Path (Join-Path $game 'winhttp.dll')) -and (Test-Path (Join-Path $game 'BepInEx\core'))
if (-not $hasBep) {
    Write-Host "未检测到 BepInEx，正在下载 BepInEx $BepVer (x64) ..." -ForegroundColor Cyan
    $zip = Join-Path $env:TEMP "BepInEx_RD_$BepVer.zip"
    try { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 } catch {}
    Invoke-WebRequest -Uri $BepUrl -OutFile $zip -UseBasicParsing
    Write-Host "解压到游戏目录 ..." -ForegroundColor Cyan
    Expand-Archive -Path $zip -DestinationPath $game -Force
    Remove-Item $zip -ErrorAction SilentlyContinue
    Write-Host "BepInEx 安装完成。" -ForegroundColor Green
} else {
    Write-Host "BepInEx 已安装，跳过。" -ForegroundColor Green
}

# 放入插件
$plugins = Join-Path $game 'BepInEx\plugins'
New-Item -ItemType Directory -Force -Path $plugins | Out-Null
Copy-Item -Path $Plugin -Destination (Join-Path $plugins 'RDTrainer.dll') -Force

Write-Host ""
Write-Host "==== 安装成功 ====" -ForegroundColor Green
Write-Host "已安装: $plugins\RDTrainer.dll"
Write-Host ""
Write-Host "现在启动游戏，进入任意关卡，按 [F3] 呼出修改器。" -ForegroundColor Cyan
Write-Host "免费开源 · 严禁倒卖 · github.com/Cohenjikan/RhythmDoctorTrainer"
