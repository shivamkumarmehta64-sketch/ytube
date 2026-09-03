@echo off
setlocal
title BlackTube Setup
echo [BlackTube] Setting up Desktop Shortcut...

set "TARGET_DIR=%~dp0"
set "TARGET_EXE=%TARGET_DIR%BlackTube.exe"
set "ICON_PATH=%TARGET_DIR%icon.ico"
set "SHORTCUT=%USERPROFILE%\Desktop\BlackTube.lnk"

powershell -NoProfile -Command "^ = New-Object -ComObject WScript.Shell; ^ = ^.CreateShortcut('%SHORTCUT%'); ^.TargetPath = '%TARGET_EXE%'; ^.WorkingDirectory = '%TARGET_DIR%'; ^.IconLocation = '%ICON_PATH%'; ^.Description = 'BlackTube - Ad-Free YouTube & YT Music'; ^.Save()"

echo [BlackTube] Shortcut created on Desktop!
pause
