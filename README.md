# Tasks & Notes Widget für Windows

Ein elegantes Desktop-Widget für Windows zum Verwalten von Tasks und Notes.

## Features

✅ **Task-Verwaltung**
- Tasks hinzufügen, abhaken und löschen
- **Unteraufgaben (Subtasks)** - Rechtsklick → "➕ Unteraufgabe hinzufügen"
- **Tasks bearbeiten** - Rechtsklick → "✏️ Bearbeiten"
- Hierarchische Darstellung mit Ein-/Ausklappen (▼/▶)
- Automatische Speicherung aller Tasks und Unteraufgaben
- Intelligenter Zähler für erledigte Tasks (inkl. Unteraufgaben)
- Smooth Animationen beim Hinzufügen/Löschen

📝 **Notizen**
- Freies Textfeld für Notizen
- Auto-Save Funktion (2 Sekunden Verzögerung)
- Persistent gespeichert

🎨 **Modernes UI Design**
- **Dark Mode & Light Mode** mit Toggle-Button (🌙/☀️)
- Optimierte Farbkontraste (Weiße Schrift im Dark Mode, Schwarze im Light Mode)
- Flüssige Slide-In/Out Animationen
- Moderne abgerundete Karten-Design
- Hover-Effekte und Transitions
- Glassmorphism-inspiriertes Design

⌨️ **Globaler Hotkey**
- **Rechts-Shift + Rechts-Strg** zum Ein-/Ausblenden des Widgets
- Widget erscheint mit Animation an der rechten Bildschirmseite
- Funktioniert system-weit, auch wenn Widget minimiert ist

💾 **Datenpersistenz**
- Alle Daten werden automatisch gespeichert
- Gespeichert in: `%APPDATA%\TaskBarWidget\`

📊 **Logging-System**
- Vollständiges Event-Logging mit Serilog
- Log-Dateien: `%APPDATA%\TaskBarWidget\Logs\`
- Automatische tägliche Rotation (7 Tage aufbewahrt)
- Log-Level: Debug, Information, Warning, Error

## Installation & Ausführung

### 🚀 Schnellinstallation (Empfohlen)

1. **Build erstellen:**
   ```powershell
   .\build.ps1
   ```

2. **Installieren:**
   ```powershell
   .\install.ps1
   ```

Das Installationsskript:
- Erstellt eine ausführbare Datei
- Installiert das Widget nach `%LOCALAPPDATA%\TaskBarWidget`
- Erstellt Desktop- und Startmenü-Verknüpfungen
- Startet das Widget automatisch

### ⚙️ Autostart einrichten

Nach der Installation:
1. Widget öffnen (**Rechts-Shift + Rechts-Strg**)
2. Klicken Sie auf **⚙️ Settings** (oben rechts)
3. Aktivieren Sie **"Autostart mit Windows"**
4. Klicken Sie auf **Speichern**

Das Widget startet nun automatisch beim Windows-Start!

### 🔄 Update auf neue Version

Wenn eine neue Version verfügbar ist:

```powershell
.\scripts\update.ps1
```

Das Update-Script:
- ✅ Prüft automatisch ob eine neue Version auf GitHub verfügbar ist
- ✅ Zeigt installierte vs. neueste Version
- ✅ Lädt neue Version herunter und baut sie
- ✅ Stoppt laufendes Widget automatisch
- ✅ Sichert alte Version als Backup
- ✅ Installiert neue Version
- ✅ Startet Widget automatisch neu
- ✅ Behält alle deine Tasks und Notizen (bleiben in %APPDATA%)

**Force-Update** (neu installieren auch wenn Version gleich):
```powershell
.\scripts\update.ps1 -Force
```

### 🗑️ Deinstallation

```powershell
.\scripts\uninstall.ps1
```

---

### 👨‍💻 Für Entwickler

**Voraussetzungen:**
- .NET 8.0 SDK oder höher
- Windows 10/11

**Projekt bauen und ausführen:**

1. Projekt wiederherstellen:
   ```powershell
   dotnet restore
   ```

2. Projekt ausführen:
   ```powershell
   dotnet run
   ```

**Manuelle Build-Erstellung:**
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Die exe finden Sie in: `bin\Release\net8.0-windows\win-x64\publish\`

## Verwendung

### Grundfunktionen
1. **Starten**: Führen Sie die Anwendung aus (Widget startet versteckt)
2. **Widget öffnen/schließen**: **Rechts-Shift + Rechts-Strg**
3. **Theme wechseln**: Klicken Sie auf 🌙/☀️ Button (oben rechts)

### Tasks verwalten
1. **Task erstellen**: Text eingeben → Enter oder "➕" klicken
2. **Task abhaken**: Checkbox vor dem Task anklicken
3. **Task bearbeiten**: **Rechtsklick** auf Task → "✏️ Bearbeiten"
4. **Task löschen**: 🗑 Symbol klicken

### Unteraufgaben
1. **Unteraufgabe hinzufügen**: **Rechtsklick** auf Task → "➕ Unteraufgabe hinzufügen"
2. **Ein-/Ausklappen**: Klicken Sie auf ▼/▶ Button oder Rechtsklick → "📁 Unteraufgaben ein-/ausklappen"
3. **Unteraufgabe bearbeiten**: **Rechtsklick** auf Unteraufgabe → "✏️ Bearbeiten"

### Notizen
1. **Notizen schreiben**: Wechseln Sie zum "📄 Notes" Tab
2. **Auto-Save**: Änderungen werden automatisch nach 2 Sekunden gespeichert

## Dateispeicherung

Alle Daten werden automatisch gespeichert in:
- Tasks: `%APPDATA%\TaskBarWidget\tasks.json`
- Notes: `%APPDATA%\TaskBarWidget\notes.txt`
- Logs: `%APPDATA%\TaskBarWidget\Logs\taskbar-widget-YYYY-MM-DD.log`

## Logging

Das Widget verwendet Serilog für umfangreiches Logging:
- **Speicherort**: `%APPDATA%\TaskBarWidget\Logs\`
- **Rotation**: Täglich neue Log-Datei
- **Aufbewahrung**: 7 Tage
- **Geloggte Events**:
  - Start/Stop der Anwendung
  - Hotkey-Aktivierung (RShift + RCtrl)
  - Task-Aktionen (Hinzufügen, Löschen, Status-Änderung)
  - Datei-Operationen (Laden/Speichern)
  - Fehler und Ausnahmen

## Technische Details

- **Framework**: .NET 8.0 WPF
- **UI**: XAML mit modernem Design
- **Datenspeicherung**: JSON (Tasks) und TXT (Notes)
- **Hotkey**: Windows Low-Level Keyboard Hook API
- **Logging**: Serilog mit File Sink
- **Pakete**: 
  - Newtonsoft.Json 13.0.3
  - Serilog 3.1.1
  - Serilog.Sinks.File 5.0.0

### 📁 Projekt-Struktur

```
TaskBar - Addon/
├── src/
│   ├── Views/          # Hauptfenster und UI-Komponenten
│   │   └── MainWindow.xaml(.cs)
│   ├── Dialogs/        # Dialog-Fenster
│   │   ├── EditTaskDialog.xaml(.cs)
│   │   └── SubTaskDialog.xaml(.cs)
│   └── Resources/      # Themes und Styles
│       └── Themes.xaml
├── scripts/            # PowerShell Installations-Skripte
│   ├── install.ps1
│   └── uninstall.ps1
├── App.xaml(.cs)       # Anwendungs-Einstiegspunkt
├── TaskBarWidget.csproj
└── README.md
```

### 📖 Code-Dokumentation

Der gesamte Code ist **vollständig dokumentiert** nach **DIN EN ISO 9241** Standard:

- ✅ **Aufgabenangemessenheit** - Jede Funktion ist klar auf ihren Zweck fokussiert
- ✅ **Selbstbeschreibungsfähigkeit** - Umfassende XML-Kommentare für alle Klassen und Methoden
- ✅ **Erwartungskonformität** - Standard-Patterns und bekannte Interaktionsmuster
- ✅ **Fehlertoleranz** - Exception-Handling und Auto-Save dokumentiert
- ✅ **Steuerbarkeit** - Keyboard-Navigation und Hotkey-System erklärt
- ✅ **Individualisierbarkeit** - Theme-System und Anpassungsmöglichkeiten
- ✅ **Lernförderlichkeit** - Intuitive Bedienung mit Erklärungen

Alle Methoden enthalten:
- Zweck und Funktionsweise
- DIN EN ISO 9241 Bezüge
- Barrierefreiheit-Aspekte
- Threading-Hinweise
- Windows API Dokumentation (P/Invoke)

## Anpassungen

Sie können das Widget anpassen, indem Sie folgende Dateien bearbeiten:
- `src/Views/MainWindow.xaml(.cs)` - UI-Design und Hauptlogik
- `src/Dialogs/` - Dialog-Fenster für Subtasks und Bearbeitung
- `src/Resources/Themes.xaml` - Farben und Styles (Dark/Light Mode)
- `App.xaml(.cs)` - Anwendungsstart und globale Ressourcen
- `TaskBarWidget.csproj` - Projektkonfiguration

## Lizenz

Dieses Projekt ist für den persönlichen Gebrauch erstellt.
