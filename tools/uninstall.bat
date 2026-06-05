@echo off
setlocal
set "GAME=D:\steam\steamapps\common\Rhythm Doctor"

del /q "%GAME%\BepInEx\plugins\RDTrainer.dll" 2>nul
echo Removed RDTrainer.dll.
echo (To fully disable BepInEx as well, delete "%GAME%\winhttp.dll".)
pause
