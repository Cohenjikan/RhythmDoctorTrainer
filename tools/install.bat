@echo off
setlocal
rem ===== Rhythm Doctor Trainer installer (BepInEx must already be installed) =====
set "GAME=D:\steam\steamapps\common\Rhythm Doctor"

echo Installing RDTrainer.dll into:
echo   "%GAME%\BepInEx\plugins"
echo.
if not exist "%GAME%\Rhythm Doctor.exe" (
  echo [ERROR] "Rhythm Doctor.exe" not found.
  echo Edit this .bat and set GAME= to your install folder.
  pause & exit /b 1
)
if not exist "%GAME%\BepInEx\plugins" (
  echo [ERROR] BepInEx is not installed yet ^(no BepInEx\plugins folder^).
  echo Install BepInEx 5 first, launch the game once, then re-run this. See README.
  pause & exit /b 1
)

copy /Y "%~dp0..\dist\RDTrainer.dll" "%GAME%\BepInEx\plugins\" >nul
if errorlevel 1 ( echo [ERROR] copy failed. & pause & exit /b 1 )

echo Done. Launch the game and press Insert in any level.
pause
