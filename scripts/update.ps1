# ============================================
# TaskBar Widget - Auto-Update Script
# ============================================
# Prueft auf neue Version und installiert automatisch
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
        return $response.sha.Substring(0, 7)
    }
    catch {
        Write-Host "Fehler beim Abrufen der GitHub-Version: $_" -ForegroundColor Red
        return $null
    }
}

function Stop-TaskBarWidget {
    $process = Get-Process -Name "TaskBarWidget" -ErrorAction SilentlyContinue
    if ($process) {
        Write-Host "Stoppe laufende TaskBarWidget Instanz..." -ForegroundColor Yellow
        Stop-Process -Name "TaskBarWidget" -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        return $true
    }
    return $false
}

function Update-Application {
    Write-Host "Lade neueste Version herunter..." -ForegroundColor Cyan
    
    $tempDir = "$env:TEMP\TaskBarWidget_Update_$(Get-Date -Format 'yyyyMMddHHmmss')"
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    
    try {
        $zipUrl = "https://github.com/$RepoOwner/$RepoName/archive/refs/heads/main.zip"
        $zipPath = "$tempDir\repo.zip"
        
        Write-Host "  -> Download von GitHub..." -ForegroundColor White
        Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath -UseBasicParsing
        
        Write-Host "  -> Entpacke Dateien..." -ForegroundColor White
        Expand-Archive -Path $zipPath -DestinationPath $tempDir -Force
        
        $extractedFolder = "$tempDir\$RepoName-main"
        
        Write-Host "  -> Erstelle Build..." -ForegroundColor White
        Push-Location $extractedFolder
        
        $dotnetVersion = & dotnet --version 2>$null
        if (-not $dotnetVersion) {
            Write-Host ".NET SDK nicht gefunden!" -ForegroundColor Red
            Write-Host "Bitte installiere .NET 8.0 SDK von: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
            Pop-Location
            return $false
        }
        
        $publishOutput = & dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "$tempDir\publish" 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Build fehlgeschlagen!" -ForegroundColor Red
            Write-Host $publishOutput -ForegroundColor White
            Pop-Location
            return $false
        }
        
        Pop-Location
        
        $wasRunning = Stop-TaskBarWidget
        
        if (Test-Path $InstallPath) {
            Write-Host "  -> Sichere alte Version..." -ForegroundColor White
            $backupPath = "$InstallPath.backup_$(Get-Date -Format 'yyyyMMddHHmmss')"
            Copy-Item -Path $InstallPath -Destination $backupPath -Recurse -Force
        }
        
        Write-Host "  -> Installiere neue Version..." -ForegroundColor White
        
        if (-not (Test-Path $InstallPath)) {
            New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
        }
        
        Copy-Item -Path "$tempDir\publish\$ExeName" -Destination "$InstallPath\$ExeName" -Force
        
        $latestVersion = Get-LatestGitHubVersion
        Set-Content -Path $VersionFile -Value $latestVersion -NoNewline
        
        Write-Host "Update erfolgreich installiert!" -ForegroundColor Green
        Write-Host "   Version: $latestVersion" -ForegroundColor White
        
        if ($wasRunning) {
            Write-Host "Starte TaskBarWidget..." -ForegroundColor Cyan
            Start-Process "$InstallPath\$ExeName"
        }
        
        return $true
    }
    catch {
        Write-Host "Update fehlgeschlagen: $_" -ForegroundColor Red
        return $false
    }
    finally {
        if (Test-Path $tempDir) {
            Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

# ============================================
# HAUPTPROGRAMM
# ============================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TaskBar Widget - Auto-Updater" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path "$InstallPath\$ExeName")) {
    Write-Host "TaskBarWidget ist nicht installiert!" -ForegroundColor Red
    Write-Host "Fuehre zuerst install.ps1 aus." -ForegroundColor Yellow
    exit 1
}

$currentVersion = Get-InstalledVersion
if ($currentVersion) {
    Write-Host "Installierte Version: $currentVersion" -ForegroundColor White
} else {
    Write-Host "Keine Versionsinformation gefunden (alte Installation)" -ForegroundColor Yellow
    $currentVersion = "unknown"
}

Write-Host "Pruefe auf Updates..." -ForegroundColor White
$latestVersion = Get-LatestGitHubVersion

if (-not $latestVersion) {
    Write-Host "Konnte neueste Version nicht abrufen." -ForegroundColor Red
    exit 1
}

Write-Host "Neueste Version: $latestVersion" -ForegroundColor White
Write-Host ""

if ($currentVersion -eq $latestVersion -and -not $Force) {
    Write-Host "Du hast bereits die neueste Version!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Verwende -Force um trotzdem neu zu installieren:" -ForegroundColor White
    Write-Host "  .\scripts\update.ps1 -Force" -ForegroundColor Gray
    exit 0
}

if ($Force) {
    Write-Host "Force-Update wird durchgefuehrt..." -ForegroundColor Yellow
} else {
    Write-Host "Neue Version verfuegbar!" -ForegroundColor Cyan
}

Write-Host ""
$confirmation = Read-Host "Update jetzt installieren? (J/N)"

if ($confirmation -match "^[JjYy]") {
    Write-Host ""
    $success = Update-Application
    
    if ($success) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "  Update erfolgreich abgeschlossen!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Verwende Rechts-Shift + Rechts-Strg um das Widget zu oeffnen." -ForegroundColor White
    } else {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Red
        Write-Host "      Update fehlgeschlagen!" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Update abgebrochen." -ForegroundColor Yellow
    exit 0
}
