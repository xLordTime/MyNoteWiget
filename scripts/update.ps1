# ============================================
# TaskBar Widget - Auto-Update Script
# ============================================
# Prüft auf neue Version und installiert automatisch
# Verwendung: .\update.ps1 [-Force]

param(
    [switch]$Force = $false
)

$ErrorActionPreference = "Stop"

# ============================================
# KONFIGURATION
# ============================================

$RepoOwner = "xLordTime"
$RepoName = "MyNoteWiget"
$InstallPath = "$env:LOCALAPPDATA\TaskBarWidget"
$ExeName = "TaskBarWidget.exe"
$VersionFile = "$InstallPath\version.txt"

# ============================================
# FUNKTIONEN
# ============================================

function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

function Get-InstalledVersion {
    if (Test-Path $VersionFile) {
        return Get-Content $VersionFile -Raw
    }
    return $null
}

function Get-LatestGitHubVersion {
    try {
        $url = "https://api.github.com/repos/$RepoOwner/$RepoName/commits/main"
        $response = Invoke-RestMethod -Uri $url -Method Get -Headers @{
            "User-Agent" = "TaskBarWidget-Updater"
        }
        return $response.sha.Substring(0, 7)  # Kurzer Commit-Hash
    }
    catch {
        Write-ColorOutput Red "❌ Fehler beim Abrufen der GitHub-Version: $_"
        return $null
    }
}

function Stop-TaskBarWidget {
    $process = Get-Process -Name "TaskBarWidget" -ErrorAction SilentlyContinue
    if ($process) {
        Write-ColorOutput Yellow "⏸️  Stoppe laufende TaskBarWidget Instanz..."
        Stop-Process -Name "TaskBarWidget" -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        return $true
    }
    return $false
}

function Update-Application {
    Write-ColorOutput Cyan "📥 Lade neueste Version herunter..."
    
    # Temporäres Verzeichnis erstellen
    $tempDir = "$env:TEMP\TaskBarWidget_Update_$(Get-Date -Format 'yyyyMMddHHmmss')"
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    
    try {
        # Repository als ZIP herunterladen
        $zipUrl = "https://github.com/$RepoOwner/$RepoName/archive/refs/heads/main.zip"
        $zipPath = "$tempDir\repo.zip"
        
        Write-ColorOutput White "  → Download von GitHub..."
        Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath -UseBasicParsing
        
        # ZIP entpacken
        Write-ColorOutput White "  → Entpacke Dateien..."
        Expand-Archive -Path $zipPath -DestinationPath $tempDir -Force
        
        $extractedFolder = "$tempDir\$RepoName-main"
        
        # Build erstellen
        Write-ColorOutput White "  → Erstelle Build..."
        Push-Location $extractedFolder
        
        # Prüfe ob .NET SDK installiert ist
        $dotnetVersion = & dotnet --version 2>$null
        if (-not $dotnetVersion) {
            Write-ColorOutput Red "❌ .NET SDK nicht gefunden!"
            Write-ColorOutput Yellow "Bitte installiere .NET 8.0 SDK von: https://dotnet.microsoft.com/download"
            Pop-Location
            return $false
        }
        
        # Build mit dotnet publish
        $publishOutput = & dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "$tempDir\publish" 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            Write-ColorOutput Red "❌ Build fehlgeschlagen!"
            Write-ColorOutput White $publishOutput
            Pop-Location
            return $false
        }
        
        Pop-Location
        
        # Installierte Version stoppen
        $wasRunning = Stop-TaskBarWidget
        
        # Alte Dateien sichern
        if (Test-Path $InstallPath) {
            Write-ColorOutput White "  → Sichere alte Version..."
            $backupPath = "$InstallPath.backup_$(Get-Date -Format 'yyyyMMddHHmmss')"
            Copy-Item -Path $InstallPath -Destination $backupPath -Recurse -Force
        }
        
        # Neue Version installieren
        Write-ColorOutput White "  → Installiere neue Version..."
        
        if (-not (Test-Path $InstallPath)) {
            New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
        }
        
        # Kopiere nur die .exe (User-Daten bleiben in %APPDATA%)
        Copy-Item -Path "$tempDir\publish\$ExeName" -Destination "$InstallPath\$ExeName" -Force
        
        # Version speichern
        $latestVersion = Get-LatestGitHubVersion
        Set-Content -Path $VersionFile -Value $latestVersion -NoNewline
        
        Write-ColorOutput Green "✅ Update erfolgreich installiert!"
        Write-ColorOutput White "   Version: $latestVersion"
        
        # Widget wieder starten wenn es vorher lief
        if ($wasRunning) {
            Write-ColorOutput Cyan "🚀 Starte TaskBarWidget..."
            Start-Process "$InstallPath\$ExeName"
        }
        
        return $true
    }
    catch {
        Write-ColorOutput Red "❌ Update fehlgeschlagen: $_"
        return $false
    }
    finally {
        # Temporäre Dateien aufräumen
        if (Test-Path $tempDir) {
            Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

# ============================================
# HAUPTPROGRAMM
# ============================================

Write-ColorOutput Cyan "========================================"
Write-ColorOutput Cyan "  TaskBar Widget - Auto-Updater        "
Write-ColorOutput Cyan "========================================"
Write-Output ""

# Prüfe ob installiert
if (-not (Test-Path "$InstallPath\$ExeName")) {
    Write-ColorOutput Red "❌ TaskBarWidget ist nicht installiert!"
    Write-ColorOutput Yellow "Führe zuerst install.ps1 aus."
    exit 1
}

# Aktuelle Version
$currentVersion = Get-InstalledVersion
if ($currentVersion) {
    Write-ColorOutput White "📦 Installierte Version: $currentVersion"
} else {
    Write-ColorOutput Yellow "⚠️  Keine Versionsinformation gefunden (alte Installation)"
    $currentVersion = "unknown"
}

# Neueste Version von GitHub
Write-ColorOutput White "🔍 Prüfe auf Updates..."
$latestVersion = Get-LatestGitHubVersion

if (-not $latestVersion) {
    Write-ColorOutput Red "❌ Konnte neueste Version nicht abrufen."
    exit 1
}

Write-ColorOutput White "📦 Neueste Version: $latestVersion"
Write-Output ""

# Vergleiche Versionen
if ($currentVersion -eq $latestVersion -and -not $Force) {
    Write-ColorOutput Green "✅ Du hast bereits die neueste Version!"
    Write-Output ""
    Write-ColorOutput White "Verwende -Force um trotzdem neu zu installieren:"
    Write-ColorOutput Gray "  .\update.ps1 -Force"
    exit 0
}

# Update durchführen
if ($Force) {
    Write-ColorOutput Yellow "⚠️  Force-Update wird durchgeführt..."
} else {
    Write-ColorOutput Cyan "🔄 Neue Version verfügbar!"
}

Write-Output ""
$confirmation = Read-Host "Update jetzt installieren? (J/N)"

if ($confirmation -match '^[JjYy]') {
    Write-Output ""
    $success = Update-Application
    
    if ($success) {
        Write-Output ""
        Write-ColorOutput Green "========================================"
        Write-ColorOutput Green "  Update erfolgreich abgeschlossen!    "
        Write-ColorOutput Green "========================================"
        Write-Output ""
        Write-ColorOutput White "Verwende Rechts-Shift + Rechts-Strg um das Widget zu oeffnen."
    } else {
        Write-Output ""
        Write-ColorOutput Red "========================================"
        Write-ColorOutput Red "      Update fehlgeschlagen!           "
        Write-ColorOutput Red "========================================"
        exit 1
    }
} else {
    Write-ColorOutput Yellow "Update abgebrochen."
    exit 0
}
