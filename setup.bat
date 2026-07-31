@echo off
title ytube Setup
echo ========================================================
echo                 ytube - Setup Build
echo ========================================================
echo.

set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if not exist "%CSC%" (
    echo [ERROR] .NET Framework 4.8 compiler (csc.exe) not found.
    pause
    exit /b 1
)

echo [1/2] Compiling ytube.cs...
"%CSC%" /nologo /target:winexe /reference:Microsoft.Web.WebView2.Core.dll /reference:Microsoft.Web.WebView2.WinForms.dll /out:ytube.exe ytube.cs

if errorlevel 1 (
    echo [ERROR] Compilation failed. Check error messages above.
    pause
    exit /b 1
)
echo [OK] ytube.exe compiled successfully!
echo.

echo [2/2] Creating Desktop shortcut...
powershell -NoProfile -Command "^
    $desktop = [Environment]::GetFolderPath('Desktop'); ^
    $ws = New-Object -ComObject WScript.Shell; ^
    $sc = $ws.CreateShortcut(\"$desktop\YouTube Desktop.lnk\"); ^
    $sc.TargetPath = '%~dp0ytube.exe'; ^
    $sc.WorkingDirectory = '%~dp0'; ^
    $sc.IconLocation = '%~dp0icon.ico,0'; ^
    $sc.Description = 'YouTube Desktop - Ad-free Desktop Client'; ^
    $sc.Save();"

echo [OK] Desktop shortcut updated!
echo.
echo ========================================================
echo Setup complete! Launch YouTube Desktop from your Desktop.
echo ========================================================
echo.
