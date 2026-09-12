$wsh = New-Object -ComObject WScript.Shell
$targetExe = 'C:\Users\shiva\projects\BlackTube\BlackTube.exe'
$workDir = 'C:\Users\shiva\projects\BlackTube'
$iconPath = 'C:\Users\shiva\projects\BlackTube\icon.ico'

$desktopLocations = @(
    [Environment]::GetFolderPath('Desktop'),
    (Join-Path $env:USERPROFILE 'Desktop')
) | Select-Object -Unique

foreach ($loc in $desktopLocations) {
    if (Test-Path $loc) {
        $desktopShortcut = Join-Path $loc 'BlackTube.lnk'
        $sc = $wsh.CreateShortcut($desktopShortcut)
        $sc.TargetPath = $targetExe
        $sc.WorkingDirectory = $workDir
        $sc.IconLocation = "$iconPath,0"
        $sc.Description = 'BlackTube - YouTube Desktop'
        $sc.Save()
        Write-Host "Updated Desktop shortcut at: $desktopShortcut -> $targetExe"
    }
}

$startMenuLocations = @(
    [Environment]::GetFolderPath('Programs'),
    (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs')
) | Select-Object -Unique

foreach ($loc in $startMenuLocations) {
    if (Test-Path $loc) {
        $startShortcut = Join-Path $loc 'BlackTube.lnk'
        $sc = $wsh.CreateShortcut($startShortcut)
        $sc.TargetPath = $targetExe
        $sc.WorkingDirectory = $workDir
        $sc.IconLocation = "$iconPath,0"
        $sc.Description = 'BlackTube - YouTube Desktop'
        $sc.Save()
        Write-Host "Updated Start Menu shortcut at: $startShortcut -> $targetExe"
    }
}
