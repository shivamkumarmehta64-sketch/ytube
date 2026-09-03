@echo off
setlocal enabledelayedexpansion
title Building BlackTube
echo [BlackTube] Compiling high-performance native binary...

set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if not exist "%CSC%" (
    echo [ERROR] .NET 4.8 CSC compiler not found at %CSC%
    exit /b 1
)

echo [1/3] Generating Win32 resource with icon...
"%CSC%" /nologo /out:GenRes.exe GenRes.cs || exit /b 1
GenRes.exe icon.ico blacktube.res || exit /b 1

echo [2/3] Compiling BlackTube.exe...
"%CSC%" /nologo /target:winexe /win32res:blacktube.res /reference:Microsoft.Web.WebView2.Core.dll /reference:Microsoft.Web.WebView2.WinForms.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /out:BlackTube.exe src\*.cs || exit /b 1

echo [3/3] Build Successful: BlackTube.exe
exit /b 0
