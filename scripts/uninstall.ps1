# TaskBar Widget - Uninstaller

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TaskBar Widget - Uninstaller" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Installationsverzeichnis
$installDir = "$env:LOCALAPPDATA\TaskBarWidget"
$desktopShortcut = "$([Environment]::GetFolderPath("Desktop"))\TaskBar Widget.lnk"
$startMenuShortcut = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\TaskBar Widget.lnk"
$autostartShortcut = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\TaskBar Widget.lnk"

Write-Host "This will remove TaskBar Widget from your system." -ForegroundColor Yellow
$confirm = Read-Host "Continue? (Y/N)"

if ($confirm -ne "Y" -and $confirm -ne "y") {
    Write-Host "Uninstall cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""

# Beende laufende Prozesse
Write-Host "Stopping running instances..." -ForegroundColor Yellow
$processes = Get-Process | Where-Object { $_.ProcessName -eq "TaskBarWidget" }
if ($processes) {
    foreach ($proc in $processes) {
        try {
            $proc.Kill()
            $proc.WaitForExit(5000)  # Warte max 5 Sekunden
            Write-Host "  Stopped process (PID: $($proc.Id))" -ForegroundColor Gray
        } catch {
            Write-Host "  Failed to stop process (PID: $($proc.Id))" -ForegroundColor Red
        }
    }
    Start-Sleep -Seconds 2  # Zusätzliche Wartezeit für Dateisystem
    Write-Host "Processes stopped" -ForegroundColor Green
} else {
    Write-Host "No running instances found" -ForegroundColor Gray
}

# Entferne Autostart
if (Test-Path $autostartShortcut) {
    Remove-Item $autostartShortcut -Force
    Write-Host "Removed from autostart" -ForegroundColor Green
}

# Entferne Verknüpfungen
if (Test-Path $desktopShortcut) {
    Remove-Item $desktopShortcut -Force
    Write-Host "Removed desktop shortcut" -ForegroundColor Green
}

if (Test-Path $startMenuShortcut) {
    Remove-Item $startMenuShortcut -Force
    Write-Host "Removed start menu shortcut" -ForegroundColor Green
}

# Entferne Installationsverzeichnis
if (Test-Path $installDir) {
    try {
        # Versuche mehrmals, falls Dateien noch gesperrt sind
        $retries = 3
        $success = $false
        
        for ($i = 1; $i -le $retries; $i++) {
            try {
                Remove-Item $installDir -Recurse -Force -ErrorAction Stop
                $success = $true
                break
            } catch {
                if ($i -lt $retries) {
                    Write-Host "  Retry $i/$retries..." -ForegroundColor Gray
                    Start-Sleep -Seconds 2
                }
            }
        }
        
        if ($success) {
            Write-Host "Removed installation directory" -ForegroundColor Green
        } else {
            Write-Host "Warning: Could not remove all files. Try running as administrator or restart your computer." -ForegroundColor Yellow
        }
    } catch {
        Write-Host "Error removing installation directory: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Frage ob Daten gelöscht werden sollen
$dataDir = "$env:APPDATA\TaskBarWidget"
if (Test-Path $dataDir) {
    Write-Host ""
    Write-Host "Found user data (tasks, notes, logs) at:" -ForegroundColor Yellow
    Write-Host "  $dataDir" -ForegroundColor White
    $deleteData = Read-Host "Delete user data? (Y/N)"
    
    if ($deleteData -eq "Y" -or $deleteData -eq "y") {
        Remove-Item $dataDir -Recurse -Force
        Write-Host "User data removed" -ForegroundColor Green
    } else {
        Write-Host "User data kept" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Uninstall Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

Read-Host "Press Enter to exit"
