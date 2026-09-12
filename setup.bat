@echo off
setlocal
title BlackTube Setup
echo [BlackTube] Creating Desktop and Start Menu Shortcuts...

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0create-shortcuts.ps1"

echo [BlackTube] Setup complete!
