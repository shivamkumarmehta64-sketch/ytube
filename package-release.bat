@echo off
setlocal
title Packaging BlackTube Release
echo [BlackTube] Packaging portable distribution...

call build.bat || exit /b 1

set "DIST_DIR=dist"
if not exist "%DIST_DIR%" mkdir "%DIST_DIR%"

set "ZIP_NAME=BlackTube-v1.0.0-Windows-x64.zip"
if exist "%DIST_DIR%\%ZIP_NAME%" del "%DIST_DIR%\%ZIP_NAME%"

powershell -NoProfile -Command "Compress-Archive -Path 'BlackTube.exe', 'Microsoft.Web.WebView2.Core.dll', 'Microsoft.Web.WebView2.WinForms.dll', 'WebView2Loader.dll', 'icon.ico', 'setup.bat', 'README.md' -DestinationPath '%DIST_DIR%\%ZIP_NAME%' -Force"

echo [BlackTube] Release ZIP created at %DIST_DIR%\%ZIP_NAME%
