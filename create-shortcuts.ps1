$wsh = New-Object -ComObject WScript.Shell
$targetExe = 'C:\Users\shiva\projects\BlackTube\BlackTube.exe'
$workDir = 'C:\Users\shiva\projects\BlackTube'
$iconPath = 'C:\Users\shiva\projects\BlackTube\icon.ico'

# 1. Desktop
$desktopPath = [Environment]::GetFolderPath('Desktop')
$desktopShortcut = Join-Path $desktopPath 'BlackTube.lnk'
$sc1 = $wsh.CreateShortcut($desktopShortcut)
$sc1.TargetPath = $targetExe
$sc1.WorkingDirectory = $workDir
$sc1.IconLocation = $iconPath
$sc1.Description = 'BlackTube - Ad-Free YouTube & YT Music'
$sc1.Save()

# 2. Start Menu Programs
$programsPath = [Environment]::GetFolderPath('Programs')
$startShortcut = Join-Path $programsPath 'BlackTube.lnk'
$sc2 = $wsh.CreateShortcut($startShortcut)
$sc2.TargetPath = $targetExe
$sc2.WorkingDirectory = $workDir
$sc2.IconLocation = $iconPath
$sc2.Description = 'BlackTube - Ad-Free YouTube & YT Music'
$sc2.Save()

Write-Host "Desktop shortcut created at: $desktopShortcut"
Write-Host "Start Menu shortcut created at: $startShortcut"
