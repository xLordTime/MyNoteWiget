# TaskBar Widget - Installer
# Installiert das Widget in Programme und erstellt Desktop-Verknüpfung

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TaskBar Widget - Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Überprüfe Administrator-Rechte
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "NOTE: Running without administrator rights." -ForegroundColor Yellow
    Write-Host "Autostart feature will be available but not automatically enabled." -ForegroundColor Yellow
    Write-Host ""
}

# Überprüfe ob exe existiert
$exePath = "bin\Release\net8.0-windows\win-x64\publish\TaskBarWidget.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: TaskBarWidget.exe not found!" -ForegroundColor Red
    Write-Host "Please run build.ps1 first to create the executable." -ForegroundColor Red
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}

# Installationsverzeichnis
$installDir = "$env:LOCALAPPDATA\TaskBarWidget"
Write-Host "Installation directory: $installDir" -ForegroundColor Yellow

# Erstelle Installationsverzeichnis
if (-not (Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
    Write-Host "Created installation directory" -ForegroundColor Green
}

# Kopiere Dateien
Write-Host "Copying files..." -ForegroundColor Yellow
Copy-Item $exePath -Destination $installDir -Force
Write-Host "Files copied successfully" -ForegroundColor Green

# Versionsnummer speichern (Git Commit Hash)
Write-Host "Saving version information..." -ForegroundColor Yellow
try {
    $gitHash = git rev-parse --short HEAD 2>$null
    if ($gitHash) {
        Set-Content -Path "$installDir\version.txt" -Value $gitHash -NoNewline
        Write-Host "Version: $gitHash" -ForegroundColor Green
    }
}
catch {
    Write-Host "Could not retrieve version information (Git not available)" -ForegroundColor Yellow
}
Write-Host ""

# Erstelle Desktop-Verknüpfung
$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcutPath = "$desktopPath\TaskBar Widget.lnk"

Write-Host "Creating desktop shortcut..." -ForegroundColor Yellow
$WScriptShell = New-Object -ComObject WScript.Shell
$Shortcut = $WScriptShell.CreateShortcut($shortcutPath)
$Shortcut.TargetPath = "$installDir\TaskBarWidget.exe"
$Shortcut.WorkingDirectory = $installDir
$Shortcut.Description = "Tasks & Notes Widget"
$Shortcut.Save()
Write-Host "Desktop shortcut created" -ForegroundColor Green
Write-Host ""

# Erstelle Startmenü-Verknüpfung
$startMenuPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs"
$startMenuShortcut = "$startMenuPath\TaskBar Widget.lnk"

Write-Host "Creating start menu shortcut..." -ForegroundColor Yellow
$Shortcut = $WScriptShell.CreateShortcut($startMenuShortcut)
$Shortcut.TargetPath = "$installDir\TaskBarWidget.exe"
$Shortcut.WorkingDirectory = $installDir
$Shortcut.Description = "Tasks & Notes Widget"
$Shortcut.Save()
Write-Host "Start menu shortcut created" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "  Installation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Widget installed to: $installDir" -ForegroundColor White
Write-Host ""
Write-Host "How to use:" -ForegroundColor Yellow
Write-Host "  1. Launch from desktop or start menu" -ForegroundColor White
Write-Host "  2. Press Right-Shift + Right-Ctrl to open the widget" -ForegroundColor White
Write-Host "  3. Use the Settings menu to enable autostart (optional)" -ForegroundColor White
Write-Host ""
Write-Host "Starting the widget now..." -ForegroundColor Yellow
Start-Process "$installDir\TaskBarWidget.exe"

Write-Host ""
Read-Host "Press Enter to exit installer"
