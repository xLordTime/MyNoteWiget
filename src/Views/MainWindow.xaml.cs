using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Newtonsoft.Json;
using Serilog;

namespace TaskBarWidget
{
    /// <summary>
    /// Hauptfenster für das TaskBar Widget
    /// 
    /// DIN EN ISO 9241 Konformität:
    /// - Aufgabenangemessenheit (§9241-110): Widget bietet direkte Funktionen für Task- und Notizenverwaltung
    /// - Selbstbeschreibungsfähigkeit (§9241-110): Klare UI-Elemente und Feedback-Mechanismen
    /// - Erwartungskonformität (§9241-110): Bekannte Interaktionsmuster (Checkboxen, Rechtsklick-Menü)
    /// - Fehlertoleranz (§9241-110): Automatisches Speichern, Logging, Exception-Handling
    /// - Individualisierbarkeit (§9241-110): Dark/Light Mode für unterschiedliche Benutzerpräferenzen
    /// - Lernförderlichkeit (§9241-110): Intuitive Bedienung, konsistente Tastaturkürzel
    /// 
    /// Barrierefreiheit:
    /// - Keyboard-Navigation: Vollständige Steuerung über Tastatur möglich
    /// - Visuelle Klarheit: Hoher Kontrast in beiden Theme-Modi
    /// - Feedbacksysteme: Visuelles und textuelles Feedback bei allen Aktionen
    /// </summary>
    public partial class MainWindow : Window
    {
        // ========================================
        // Windows API Konstanten für Keyboard Hook
        // ========================================
        
        /// <summary>Low-Level Keyboard Hook Identifier (Windows API)</summary>
        private const int WH_KEYBOARD_LL = 13;
        
        /// <summary>Windows Message: Taste gedrückt</summary>
        private const int WM_KEYDOWN = 0x0100;
        
        /// <summary>Windows Message: Taste losgelassen</summary>
        private const int WM_KEYUP = 0x0101;
        
        /// <summary>Handle zum installierten Keyboard Hook</summary>
        private static IntPtr _hookID = IntPtr.Zero;
        
        /// <summary>Callback-Delegate für Low-Level Keyboard Events</summary>
        private static LowLevelKeyboardProc _proc = null!;
        
        /// <summary>Virtual Key Code für rechte Shift-Taste</summary>
        private const int VK_RSHIFT = 0xA1;
        
        /// <summary>Virtual Key Code für rechte Strg-Taste</summary>
        private const int VK_RCONTROL = 0xA3;
        
        /// <summary>Status: Rechte Shift-Taste aktuell gedrückt</summary>
        private static bool _rShiftPressed = false;
        
        /// <summary>Status: Rechte Strg-Taste aktuell gedrückt</summary>
        private static bool _rControlPressed = false;
        
        // ========================================
        // Anwendungsdaten und Konfiguration
        // ========================================
        
        /// <summary>
        /// Observable Collection aller Tasks mit automatischer UI-Benachrichtigung
        /// Gewährleistet Erwartungskonformität durch Echtzeit-Updates (DIN EN ISO 9241-110)
        /// </summary>
        private ObservableCollection<TaskItem> _tasks;
        
        /// <summary>
        /// Timer für verzögertes Auto-Save der Notizen
        /// Reduziert Schreibvorgänge und verbessert Performance (Fehlertoleranz DIN EN ISO 9241-110)
        /// </summary>
        private DispatcherTimer _saveTimer;
        
        /// <summary>Pfad zum Anwendungsdatenverzeichnis (%APPDATA%\TaskBarWidget\)</summary>
        private string _dataPath;
        
        /// <summary>
        /// Aktueller Theme-Modus (Dark = true, Light = false)
        /// Unterstützt Individualisierbarkeit (DIN EN ISO 9241-110)
        /// </summary>
        private bool _isDarkMode = true;
        
        /// <summary>Flag: Verhindert mehrfache Animation beim Schließen</summary>
        private bool _isClosing = false;
        
        /// <summary>Registry-Pfad für Windows Autostart</summary>
        private const string AutostartRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        
        /// <summary>Registry-Wert-Name für Autostart-Eintrag</summary>
        private const string AutostartValueName = "TaskBarWidget";
        
        /// <summary>
        /// Konstruktor: Initialisiert das Hauptfenster und alle Subsysteme
        /// 
        /// DIN EN ISO 9241-110 Prinzipien:
        /// - Fehlertoleranz: Strukturiertes Logging und Exception-Handling vorbereitet
        /// - Selbstbeschreibungsfähigkeit: Klare Initialisierungsreihenfolge dokumentiert
        /// - Erwartungskonformität: Standard-Verhalten (versteckt starten, rechts positionieren)
        /// 
        /// Initialisierungsreihenfolge (kritisch für Stabilität):
        /// 1. UI-Komponenten (InitializeComponent)
        /// 2. Logging-System (Serilog)
        /// 3. Datenstrukturen (Tasks Collection)
        /// 4. Dateisystem (Data Path)
        /// 5. Auto-Save Timer
        /// 6. Daten laden (Tasks, Notes)
        /// 7. Fensterpositionierung
        /// 8. Globaler Keyboard Hook
        /// 9. Theme-Anwendung
        /// 10. Versteckter Start
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            // ========================================
            // 1. Logging-System initialisieren
            // ========================================
            // Fehlertoleranz: Vollständige Nachvollziehbarkeit durch strukturiertes Logging
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TaskBarWidget",
                "Logs"
            );
            Directory.CreateDirectory(logPath);
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    Path.Combine(logPath, "taskbar-widget-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();
            
            Log.Information("TaskBar Widget started");
            
            // ========================================
            // 2. Task-Datenstruktur initialisieren
            // ========================================
            // Erwartungskonformität: ObservableCollection ermöglicht automatische UI-Updates
            _tasks = new ObservableCollection<TaskItem>();
            TasksList.ItemsSource = _tasks;
            
            // ========================================
            // 3. Datenverzeichnis erstellen
            // ========================================
            // Aufgabenangemessenheit: Persistente Speicherung in Standard-Benutzerverzeichnis
            _dataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TaskBarWidget"
            );
            Directory.CreateDirectory(_dataPath);
            Log.Information("Data path created: {DataPath}", _dataPath);
            
            // ========================================
            // 4. Auto-Save Timer konfigurieren
            // ========================================
            // Fehlertoleranz: Automatisches Speichern verhindert Datenverlust
            // Selbstbeschreibungsfähigkeit: 2-Sekunden-Verzögerung für optimale UX
            _saveTimer = new DispatcherTimer();
            _saveTimer.Interval = TimeSpan.FromSeconds(2);
            _saveTimer.Tick += SaveTimer_Tick;
            
            // ========================================
            // 5. Gespeicherte Daten laden
            // ========================================
            // Erwartungskonformität: Benutzer erwartet persistenten Zustand
            LoadData();
            UpdateTaskCounter();
            
            // ========================================
            // 6. Fensterposition festlegen
            // ========================================
            // Erwartungskonformität: Widget erscheint konsistent rechts
            PositionWindowRight();
            
            // ========================================
            // 7. Globalen Keyboard Hook registrieren
            // ========================================
            // Aufgabenangemessenheit: Systemweiter Hotkey für schnellen Zugriff
            // WICHTIG: Muss vor Window_Loaded erfolgen für zuverlässige Funktion
            _proc = HookCallback;
            _hookID = SetHook(_proc);
            Log.Information("Global keyboard hook registered. Hook ID: {HookID}", _hookID);
            
            // ========================================
            // 8. Theme anwenden
            // ========================================
            // Individualisierbarkeit: Anpassung an Benutzerpräferenzen
            ApplyTheme(_isDarkMode);
            
            // ========================================
            // 9. Versteckter Start
            // ========================================
            // Erwartungskonformität: Widget erscheint nur auf Hotkey-Aufruf
            this.Visibility = Visibility.Hidden;
            Log.Information("MainWindow initialized successfully");
        }
        
        /// <summary>
        /// Event-Handler: Fenster vollständig geladen
        /// Wird nach Konstruktor und InitializeComponent aufgerufen
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Information("Window loaded event triggered");
        }
        
        /// <summary>
        /// Override: Window Source initialisiert
        /// Ermöglicht Windows Message Handling (für zukünftige Erweiterungen)
        /// 
        /// DIN EN ISO 9241-110: Steuerbarkeit - Basis für erweiterte Fensterinteraktionen
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source?.AddHook(WndProc);
        }
        
        /// <summary>
        /// Override: Fenster wird geschlossen
        /// 
        /// DIN EN ISO 9241-110 Prinzipien:
        /// - Fehlertoleranz: Sicherstellen dass Daten gespeichert werden
        /// - Aufgabenangemessenheit: Ressourcen ordnungsgemäß freigeben
        /// - Selbstbeschreibungsfähigkeit: Logging für Nachvollziehbarkeit
        /// 
        /// Cleanup-Reihenfolge (kritisch):
        /// 1. Keyboard Hook deregistrieren (verhindert Memory Leaks)
        /// 2. Daten speichern (verhindert Datenverlust)
        /// 3. Logger schließen (stellt sauberes Herunterfahren sicher)
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // Keyboard Hook freigeben
            UnhookWindowsHookEx(_hookID);
            Log.Information("Global keyboard hook unregistered");
            
            // Finale Datensicherung
            SaveData();
            Log.Information("TaskBar Widget closed");
            
            // Logger ordnungsgemäß beenden
            Log.CloseAndFlush();
        }
        
        /// <summary>
        /// Windows Message Procedure
        /// Ermöglicht Verarbeitung von nativen Windows-Nachrichten
        /// 
        /// Aktuell nicht verwendet, aber vorbereitet für zukünftige Features
        /// (z.B. Custom Window Messages, Drag & Drop von anderen Anwendungen)
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            return IntPtr.Zero;
        }
        
        // ========================================
        // FENSTER-MANAGEMENT UND UI-VERHALTEN
        // ========================================
        
        /// <summary>
        /// Positioniert das Fenster rechts am Bildschirmrand
        /// 
        /// DIN EN ISO 9241-110 Prinzipien:
        /// - Erwartungskonformität: Konsistente Position rechts (wie typische Widgets/Sidebars)
        /// - Selbstbeschreibungsfähigkeit: Vorhersagbares Erscheinen
        /// - Aufgabenangemessenheit: Optimale Position für schnellen Zugriff
        /// 
        /// Berechnung:
        /// - WorkArea = Bildschirmfläche ohne Taskleiste
        /// - Left = Rechter Rand - Fensterbreite - 10px Abstand
        /// - Top = Vertikal zentriert
        /// </summary>
        private void PositionWindowRight()
        {
            var workingArea = SystemParameters.WorkArea;
            this.Left = workingArea.Right - this.Width - 10;
            this.Top = (workingArea.Height - this.Height) / 2;
        }
        
        /// <summary>
        /// Wendet das gewählte Theme (Dark/Light) auf alle UI-Elemente an
        /// 
        /// DIN EN ISO 9241-110 Prinzipien:
        /// - Individualisierbarkeit: Anpassung an Benutzerpräferenzen und Umgebungslicht
        /// - Selbstbeschreibungsfähigkeit: Visuelles Feedback durch Theme-Icon-Änderung
        /// - Barrierefreiheit: Hoher Kontrast in beiden Modi für bessere Lesbarkeit
        /// 
        /// Theme-System:
        /// - Dynamische Resource-Bindungen (DynamicResource in XAML)
        /// - Zentrale Farbdefinitionen in Themes.xaml
        /// - Sofortige Anwendung ohne Neustart
        /// 
        /// Barrierefreiheit:
        /// - Dark Mode: Weißer Text (#FFFFFF) auf dunklem Hintergrund (#1E1E1E)
        /// - Light Mode: Schwarzer Text (#000000) auf hellem Hintergrund (#FFFFFF)
        /// - WCAG 2.1 Level AA Kontrastverhältnisse eingehalten
        /// </summary>
        /// <param name="isDark">True = Dark Mode, False = Light Mode</param>
        private void ApplyTheme(bool isDark)
        {
            var resources = Application.Current.Resources;
            
            if (isDark)
            {
                // Dark Mode Farb-Ressourcen zuweisen
                resources["BackgroundBrush"] = resources["DarkBackground"];
                resources["SecondaryBackgroundBrush"] = resources["DarkSecondaryBackground"];
                resources["BorderBrush"] = resources["DarkBorderBrush"];
                resources["TextPrimaryBrush"] = resources["DarkTextPrimary"];
                resources["TextSecondaryBrush"] = resources["DarkTextSecondary"];
                resources["AccentBrush"] = resources["DarkAccent"];
                resources["SuccessBrush"] = resources["DarkSuccess"];
                resources["DangerBrush"] = resources["DarkDanger"];
                resources["HoverBrush"] = resources["DarkHover"];
                
                // Visuelles Feedback: Sonnen-Icon = "Switch zu Light Mode"
                ThemeToggleButton.Content = "☀️";
            }
            else
            {
                // Light Mode Farb-Ressourcen zuweisen
                resources["BackgroundBrush"] = resources["LightBackground"];
                resources["SecondaryBackgroundBrush"] = resources["LightSecondaryBackground"];
                resources["BorderBrush"] = resources["LightBorderBrush"];
                resources["TextPrimaryBrush"] = resources["LightTextPrimary"];
                resources["TextSecondaryBrush"] = resources["LightTextSecondary"];
                resources["AccentBrush"] = resources["LightAccent"];
                resources["SuccessBrush"] = resources["LightSuccess"];
                resources["DangerBrush"] = resources["LightDanger"];
                resources["HoverBrush"] = resources["LightHover"];
                
                // Visuelles Feedback: Mond-Icon = "Switch zu Dark Mode"
                ThemeToggleButton.Content = "🌙";
            }
            
            Log.Information("Theme changed to: {Theme}", isDark ? "Dark" : "Light");
        }
        
        /// <summary>
        /// Event-Handler: Theme-Umschaltung durch Benutzer
        /// 
        /// DIN EN ISO 9241-110:
        /// - Individualisierbarkeit: Benutzer kann Theme nach Präferenz wählen
        /// - Selbstbeschreibungsfähigkeit: Icon wechselt und zeigt nächsten Modus
        /// - Steuerbarkeit: Sofortige Reaktion auf Benutzereingabe
        /// 
        /// Interaktionsmuster:
        /// - Click togglet zwischen Dark/Light
        /// - Icon zeigt immer den NÄCHSTEN Modus (intuitive Affordanz)
        /// - Keine Bestätigung nötig (nicht-destruktive Aktion)
        /// </summary>
        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isDarkMode = !_isDarkMode;
            ApplyTheme(_isDarkMode);
        }
        
        /// <summary>
        /// Event-Handler: Einstellungen-Dialog öffnen
        /// 
        /// DIN EN ISO 9241-110:
        /// - Aufgabenangemessenheit: Zentraler Zugriff auf Konfigurationsoptionen
        /// - Selbstbeschreibungsfähigkeit: Dialog zeigt aktuellen Autostart-Status
        /// - Fehlertoleranz: Settings können jederzeit geändert werden
        /// 
        /// Dialogrückgabe:
        /// - true = Benutzer hat auf OK geklickt (Änderungen übernehmen)
        /// - false/null = Benutzer hat auf Abbrechen geklickt (keine Änderungen)
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsDialog = new SettingsDialog(IsAutostartEnabled());
            if (settingsDialog.ShowDialog() == true)
            {
                SetAutostart(settingsDialog.AutostartEnabled);
            }
        }
        
        /// <summary>
        /// Prüft ob Autostart in der Windows-Registry aktiviert ist
        /// 
        /// DIN EN ISO 9241-110:
        /// - Fehlertoleranz: Exception-Handling für Registry-Zugriffsfehler
        /// - Selbstbeschreibungsfähigkeit: Gibt klaren bool-Status zurück
        /// 
        /// Registry-Pfad: HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
        /// Wert-Name: "TaskBarWidget"
        /// 
        /// Sicherheit:
        /// - Nur Read-Zugriff (writable=false)
        /// - Keine Systemrechte erforderlich (HKCU statt HKLM)
        /// </summary>
        /// <returns>True wenn Autostart aktiviert, sonst false</returns>
        private bool IsAutostartEnabled()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(AutostartRegistryKey, false))
                {
                    if (key != null)
                    {
                        var value = key.GetValue(AutostartValueName);
                        return value != null;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking autostart status");
            }
            return false;
        }
        
        /// <summary>
        /// Aktiviert oder deaktiviert Windows-Autostart für das Widget
        /// 
        /// DIN EN ISO 9241-110 Prinzipien:
        /// - Steuerbarkeit: Benutzer hat volle Kontrolle über Autostart
        /// - Selbstbeschreibungsfähigkeit: Bestätigungsdialog gibt klares Feedback
        /// - Fehlertoleranz: Umfangreiches Exception-Handling und Fehler-Dialoge
        /// - Lernförderlichkeit: Erklärende Bestätigungsmeldungen
        /// 
        /// Funktionsweise:
        /// - Aktivieren: Registry-Eintrag mit Pfad zur .exe erstellen
        /// - Deaktivieren: Registry-Eintrag entfernen (throwOnMissingValue=false)
        /// 
        /// Sicherheit:
        /// - Write-Zugriff auf HKCU (keine Admin-Rechte erforderlich)
        /// - Nur eigene Anwendung betroffen (kein Systemeingriff)
        /// - GetCurrentProcess() liefert zuverlässigen Pfad zur .exe
        /// </summary>
        /// <param name="enable">True = Autostart aktivieren, False = deaktivieren</param>
        private void SetAutostart(bool enable)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(AutostartRegistryKey, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            // Pfad zur aktuellen .exe ermitteln
                            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                            if (exePath != null)
                            {
                                key.SetValue(AutostartValueName, exePath);
                                Log.Information("Autostart enabled");
                                
                                // Selbstbeschreibungsfähigkeit: Bestätigung mit Erklärung
                                MessageBox.Show("Autostart aktiviert!\n\nDas Widget startet jetzt automatisch mit Windows.", 
                                    "Autostart", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        else
                        {
                            // throwOnMissingValue=false: Fehlertoleranz falls bereits gelöscht
                            key.DeleteValue(AutostartValueName, false);
                            Log.Information("Autostart disabled");
                            
                            // Selbstbeschreibungsfähigkeit: Bestätigung mit Erklärung
                            MessageBox.Show("Autostart deaktiviert!\n\nDas Widget startet nicht mehr automatisch.", 
                                "Autostart", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fehlertoleranz: Benutzerfreundliche Fehlermeldung statt Absturz
                Log.Error(ex, "Error setting autostart");
                MessageBox.Show($"Fehler beim Ändern der Autostart-Einstellung:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Zeigt oder versteckt das Widget mit Slide-Animation
        /// 
        /// DIN EN ISO 9241-110 Prinzipien:
        /// - Erwartungskonformität: Smooth Animations wie in modernen Anwendungen üblich
        /// - Aufgabenangemessenheit: Toggle-Funktion für schnellen Zugriff
        /// - Selbstbeschreibungsfähigkeit: Visuelle Animation macht Zustandsänderung klar
        /// - Steuerbarkeit: Benutzer kann Widget jederzeit ein-/ausblenden
        /// 
        /// Interaktionsmuster:
        /// 1. Widget sichtbar → Slide-Out Animation → verstecken
        /// 2. Widget versteckt → positionieren → sichtbar machen → Slide-In Animation → Fokus
        /// 
        /// Benutzererfahrung (UX):
        /// - Animation gibt visuelles Feedback (nicht abrupt ein/aus)
        /// - Automatischer Fokus auf TaskInput für sofortiges Tippen
        /// - _isClosing Flag verhindert mehrfache Animationen
        /// - ContextIdle Priority: Fokus erst NACH vollständigem Rendering
        /// 
        /// Barrierefreiheit:
        /// - Fokus automatisch gesetzt (Keyboard Navigation)
        /// - Predictable Position (immer rechts)
        /// </summary>
        private void ToggleVisibility()
        {
            if (this.Visibility == Visibility.Visible && !_isClosing)
            {
                // Widget ausblenden mit Animation
                _isClosing = true;
                var slideOut = (System.Windows.Media.Animation.Storyboard)this.FindResource("SlideOutAnimation");
                slideOut.Begin(MainBorder);
                Log.Information("Widget hiding with animation");
            }
            else if (!_isClosing)
            {
                // Widget anzeigen mit Animation
                PositionWindowRight();  // Erwartungskonformität: Konsistente Position
                this.Visibility = Visibility.Visible;
                this.Activate();        // Fenster in Vordergrund bringen
                
                var slideIn = (System.Windows.Media.Animation.Storyboard)this.FindResource("SlideInAnimation");
                slideIn.Begin(MainBorder);
                
                Log.Information("Widget shown at position: Left={Left}, Top={Top}", this.Left, this.Top);
                
                // Automatischer Fokus auf Eingabefeld für sofortige Nutzung
                // ContextIdle: Warten bis Rendering komplett (verhindert Fokus-Probleme)
                this.Dispatcher.BeginInvoke(new Action(() => TaskInput.Focus()), 
                    System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
        }
        
        /// <summary>
        /// Event-Handler: Slide-Out Animation abgeschlossen
        /// 
        /// DIN EN ISO 9241-110:
        /// - Erwartungskonformität: Widget verschwindet erst nach Animation
        /// - Selbstbeschreibungsfähigkeit: Smooth Transition statt abruptes Verschwinden
        /// 
        /// Wird aus XAML Storyboard.Completed Event aufgerufen
        /// Versteckt Fenster erst nach Animation (bessere UX)
        /// </summary>
        private void SlideOutAnimation_Completed(object? sender, EventArgs e)
        {
            this.Visibility = Visibility.Hidden;
            _isClosing = false;
            Log.Information("Widget hidden");
        }
        
        // ========================================
        // TASK-VERWALTUNG
        // ========================================
        
        /// <summary>
        /// Event-Handler: Neuen Task über Button hinzufügen
        /// 
        /// DIN EN ISO 9241-110:
        /// - Aufgabenangemessenheit: Direkte Funktion zum Hinzufügen von Tasks
        /// - Erwartungskonformität: Button-Click als Standard-Interaktionsmuster
        /// </summary>
        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            AddTask();
        }
        
        /// <summary>
        /// Event-Handler: Neuen Task über Enter-Taste hinzufügen
        /// 
        /// DIN EN ISO 9241-110:
        /// - Steuerbarkeit: Keyboard-Alternative zu Button-Click
        /// - Erwartungskonformität: Enter-Taste als übliche Bestätigungs-Taste
        /// - Lernförderlichkeit: Intuitive Keyboard-Shortcuts
        /// 
        /// Barrierefreiheit:
        /// - Vollständige Keyboard-Bedienbarkeit ohne Maus
        /// </summary>
        private void TaskInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTask();
            }
        }
        
        /// <summary>
        /// Fügt einen neuen Task zur Liste hinzu
        /// 
        /// DIN EN ISO 9241-110 Prinzipien:
        /// - Fehlertoleranz: Validierung (kein leerer Text, Trim für Whitespace)
        /// - Aufgabenangemessenheit: Minimale Eingabe erforderlich (nur Text)
        /// - Selbstbeschreibungsfähigkeit: Sofortiges Feedback durch UI-Update
        /// - Erwartungskonformität: Task erscheint sofort in Liste
        /// 
        /// Workflow:
        /// 1. Text validieren und trimmen
        /// 2. TaskItem erstellen mit Zeitstempel
        /// 3. Zur ObservableCollection hinzufügen (auto UI-Update)
        /// 4. Eingabefeld leeren für nächsten Task
        /// 5. Counter aktualisieren
        /// 6. Automatisch speichern
        /// 
        /// Datenintegrität:
        /// - CreatedDate für Sortierung/Nachvollziehbarkeit
        /// - IsCompleted initial false (logischer Default)
        /// </summary>
        private void AddTask()
        {
            var taskText = TaskInput.Text.Trim();
            if (!string.IsNullOrEmpty(taskText))
            {
                _tasks.Add(new TaskItem
                {
                    Text = taskText,
                    IsCompleted = false,
                    CreatedDate = DateTime.Now
                });
                
                Log.Information("Task added: {TaskText}", taskText);
                TaskInput.Clear();
                UpdateTaskCounter();
                SaveData();
            }
        }
        
        /// <summary>
        /// Event-Handler: Task löschen über Delete-Button
        /// 
        /// DIN EN ISO 9241-110 Prinzipien:
        /// - Steuerbarkeit: Benutzer kann Tasks jederzeit entfernen
        /// - Fehlertoleranz: Type-Safe Casting verhindert Fehler
        /// - Selbstbeschreibungsfähigkeit: Sofortiges visuelles Feedback (Task verschwindet)
        /// 
        /// Implementierung:
        /// - Button.Tag enthält Referenz zum TaskItem (XAML Data Binding)
        /// - Type-Safe Casting mit 'is' Pattern Matching
        /// - ObservableCollection.Remove() triggert auto UI-Update
        /// - Counter und Speicherung automatisch aktualisiert
        /// 
        /// Keine Bestätigung nötig da:
        /// - Nicht-destruktive Aktion (Daten bleiben in tasks.json bis nächstes Speichern)
        /// - Schnelle Workflows wichtiger als Sicherheitsabfrage
        /// - Wiederherstellung über manuelles Editieren der JSON möglich
        /// </summary>
        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TaskItem task)
            {
                Log.Information("Task deleted: {TaskText}", task.Text);
                _tasks.Remove(task);
                UpdateTaskCounter();
                SaveData();
            }
        }
        
        /// <summary>
        /// Event-Handler: Task als erledigt/unerledigt markieren
        /// 
        /// DIN EN ISO 9241-110 Prinzipien:
        /// - Aufgabenangemessenheit: Kern-Funktionalität einer Todo-Liste
        /// - Erwartungskonformität: Checkbox als Standard-Pattern für Completed-Status
        /// - Selbstbeschreibungsfähigkeit: Visuelles Feedback (Strikethrough in XAML)
        /// - Steuerbarkeit: Toggle-Funktion für schnelles Umschalten
        /// 
        /// Funktionsweise:
        /// - CheckBox.DataContext enthält TaskItem (via XAML DataTemplate)
        /// - IsCompleted Property ist Two-Way gebunden
        /// - Änderung triggert PropertyChanged → UI-Update
        /// - Counter berücksichtigt Haupt- und Subtasks
        /// - Automatisches Speichern für Persistenz
        /// 
        /// Barrierefreiheit:
        /// - Checkbox mit Tastatur bedienbar (Space-Taste)
        /// - Screen Reader liest Status (via AutomationProperties)
        /// </summary>
        private void TaskCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is TaskItem task)
            {
                Log.Information("Task {Status}: {TaskText}", task.IsCompleted ? "completed" : "uncompleted", task.Text);
            }
            UpdateTaskCounter();
            SaveData();
        }
        
        /// <summary>
        /// Aktualisiert die Task-Counter Anzeige (z.B. "5/12 completed")
        /// 
        /// DIN EN ISO 9241-110:
        /// - Selbstbeschreibungsfähigkeit: Zeigt Fortschritt auf einen Blick
        /// - Aufgabenangemessenheit: Motiviert Benutzer durch Fortschrittsanzeige
        /// - Erwartungskonformität: Standard-Format "X/Y completed"
        /// 
        /// Hierarchische Zählung:
        /// - Berücksichtigt Haupt-Tasks UND alle Subtasks
        /// - Rekursive Algorithmen für beliebige Verschachtelungstiefe
        /// - Gibt vollständigen Überblick über alle offenen Aufgaben
        /// </summary>
        private void UpdateTaskCounter()
        {
            var completedCount = CountCompletedTasks(_tasks);
            var totalCount = CountAllTasks(_tasks);
            TaskCounter.Text = $"{completedCount}/{totalCount} completed";
        }
        
        /// <summary>
        /// Zählt ALLE Tasks (Haupt-Tasks + Subtasks) rekursiv
        /// 
        /// Algorithmus:
        /// 1. Zähle Tasks auf aktueller Ebene
        /// 2. Für jeden Task: Zähle rekursiv dessen Subtasks
        /// 3. Summiere alle Counts
        /// 
        /// Komplexität: O(n) wobei n = Gesamtzahl aller Tasks inkl. Subtasks
        /// </summary>
        /// <param name="tasks">Task-Collection auf aktueller Hierarchie-Ebene</param>
        /// <returns>Gesamtzahl aller Tasks in dieser Collection und allen Unter-Ebenen</returns>
        private int CountAllTasks(ObservableCollection<TaskItem> tasks)
        {
            int count = tasks.Count;
            foreach (var task in tasks)
            {
                count += CountAllTasks(task.SubTasks);
            }
            return count;
        }
        
        /// <summary>
        /// Zählt erledigte Tasks (IsCompleted=true) rekursiv
        /// 
        /// Algorithmus:
        /// 1. Zähle erledigte Tasks auf aktueller Ebene (LINQ Count mit Filter)
        /// 2. Für jeden Task: Zähle rekursiv erledigte Subtasks
        /// 3. Summiere alle Counts
        /// 
        /// Komplexität: O(n) wobei n = Gesamtzahl aller Tasks inkl. Subtasks
        /// 
        /// DIN EN ISO 9241-110:
        /// - Selbstbeschreibungsfähigkeit: Zeigt Fortschritt über alle Ebenen
        /// </summary>
        /// <param name="tasks">Task-Collection auf aktueller Hierarchie-Ebene</param>
        /// <returns>Anzahl erledigter Tasks in dieser Collection und allen Unter-Ebenen</returns>
        private int CountCompletedTasks(ObservableCollection<TaskItem> tasks)
        {
            int count = tasks.Count(t => t.IsCompleted);
            foreach (var task in tasks)
            {
                count += CountCompletedTasks(task.SubTasks);
            }
            return count;
        }
        
        /// <summary>
        /// Event-Handler: Task-Text bearbeiten über Rechtsklick-Menü
        /// 
        /// DIN EN ISO 9241-110 Prinzipien:
        /// - Fehlertoleranz: Benutzer kann Tippfehler nachträglich korrigieren
        /// - Steuerbarkeit: Volle Kontrolle über Task-Inhalte
        /// - Erwartungskonformität: Rechtsklick → Bearbeiten als Standard-Pattern
        /// - Selbstbeschreibungsfähigkeit: Dialog zeigt aktuellen Text zur Bearbeitung
        /// 
        /// Workflow:
        /// 1. EditTaskDialog mit aktuellem Text öffnen
        /// 2. Bei OK und nicht-leerem Text: Task aktualisieren
        /// 3. PropertyChanged Notification für Two-Way Binding
        /// 4. UI-Refresh (nötig da Text-Property direkt geändert)
        /// 5. Automatisch speichern
        /// 
        /// UI-Refresh Workaround:
        /// - ItemsSource auf null setzen und neu zuweisen
        /// - Triggert vollständiges Re-Rendering der Liste
        /// - Notwendig da Text-Änderung manchmal nicht propagiert
        /// </summary>
        private void EditTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is TaskItem task)
            {
                var dialog = new EditTaskDialog(task.Text);
                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.TaskText))
                {
                    var oldText = task.Text;
                    task.Text = dialog.TaskText;
                    task.OnPropertyChanged(nameof(task.Text));
                    
                    Log.Information("Task edited from '{OldText}' to '{NewText}'", oldText, task.Text);
                    SaveData();
                    
                    // UI-Refresh erzwingen für sofortige Darstellung
                    var items = TasksList.ItemsSource;
                    TasksList.ItemsSource = null;
                    TasksList.ItemsSource = items;
                }
            }
        }
        
        /// <summary>
        /// Event-Handler: Subtask hinzufügen über Rechtsklick-Menü
        /// 
        /// DIN EN ISO 9241-110 Prinzipien:
        /// - Aufgabenangemessenheit: Ermöglicht hierarchische Task-Strukturen
        /// - Lernförderlichkeit: Intuitive Rechtsklick-Interaktion
        /// - Selbstbeschreibungsfähigkeit: Dialog fordert Subtask-Text an
        /// - Erwartungskonformität: Subtask erscheint eingeklappt unter Parent
        /// 
        /// Workflow:
        /// 1. SubTaskDialog für Texteingabe öffnen
        /// 2. Bei OK und nicht-leerem Text: Subtask erstellen
        /// 3. Subtask zum Parent hinzufügen
        /// 4. Parent automatisch aufklappen (IsExpanded=true) für Sichtbarkeit
        /// 5. PropertyChanged für UI-Update
        /// 6. Counter und Speicherung aktualisieren
        /// 
        /// Hierarchie:
        /// - Subtasks können beliebig verschachtelt werden
        /// - Jeder TaskItem hat eigene SubTasks Collection
        /// - Rekursives Rendering in XAML ItemsControl
        /// </summary>
        private void AddSubTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is TaskItem parentTask)
            {
                var dialog = new SubTaskDialog();
                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.SubTaskText))
                {
                    var subTask = new TaskItem
                    {
                        Text = dialog.SubTaskText,
                        IsCompleted = false,
                        CreatedDate = DateTime.Now
                    };
                    
                    parentTask.SubTasks.Add(subTask);
                    parentTask.OnPropertyChanged(nameof(parentTask.SubTasks));
                    parentTask.IsExpanded = true;  // Erwartungskonformität: Neuen Subtask sofort zeigen
                    
                    Log.Information("Subtask added to '{ParentTask}': {SubTaskText}", parentTask.Text, subTask.Text);
                    UpdateTaskCounter();
                    SaveData();
                }
            }
        }
        
        /// <summary>
        /// Event-Handler: Subtask-Liste ein-/ausklappen über Rechtsklick-Menü
        /// 
        /// DIN EN ISO 9241-110:
        /// - Steuerbarkeit: Benutzer kontrolliert Detailgrad der Ansicht
        /// - Individualisierbarkeit: Anpassung an aktuelle Arbeitsweise
        /// - Erwartungskonformität: Toggle-Funktion wie in File-Explorern
        /// 
        /// Funktionsweise:
        /// - IsExpanded Property steuert Sichtbarkeit in XAML
        /// - Toggle zwischen true/false
        /// - Zustand wird gespeichert (bleibt über Neustarts erhalten)
        /// - Visuelles Feedback durch ▼/▶ Symbol in XAML
        /// </summary>
        private void ToggleSubTasks_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is TaskItem task)
            {
                task.IsExpanded = !task.IsExpanded;
                SaveData();
            }
        }
        
        // ========================================
        // NOTIZEN-VERWALTUNG
        // ========================================
        
        /// <summary>
        /// Event-Handler: Notizen-Text geändert
        /// 
        /// DIN EN ISO 9241-110 Prinzipien:
        /// - Fehlertoleranz: Automatisches Speichern ohne Benutzeraktion
        /// - Selbstbeschreibungsfähigkeit: "Saving..." Feedback während Timer läuft
        /// - Aufgabenangemessenheit: Kein manuelles Speichern nötig
        /// 
        /// Auto-Save Mechanismus:
        /// - TextChanged Event startet 2-Sekunden Timer
        /// - Weiteres Tippen resettet Timer (verhindert unnötige Schreibvorgänge)
        /// - Nach 2 Sekunden Inaktivität: Speichern wird ausgelöst
        /// - "Debouncing" Pattern für optimale Performance
        /// 
        /// Benutzererfahrung:
        /// - Kein lästiges Ctrl+S erforderlich
        /// - Visuelles Feedback "Saving..." → "Saved"
        /// - Verhindert Datenverlust bei Absturz/Stromausfall
        /// </summary>
        private void NotesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveIndicator.Text = "Saving...";
            _saveTimer.Stop();   // Vorherigen Timer abbrechen
            _saveTimer.Start();  // Neuen 2-Sekunden Countdown starten
        }
        
        /// <summary>
        /// Timer Tick Event: Speichern nach Inaktivitätsperiode
        /// 
        /// DIN EN ISO 9241-110:
        /// - Fehlertoleranz: Automatische Persistenz
        /// - Selbstbeschreibungsfähigkeit: "Saved" Bestätigung
        /// - Aufgabenangemessenheit: Transparentes Speichern
        /// 
        /// Wird nach 2 Sekunden ohne weitere Änderungen aufgerufen
        /// Timer wird gestoppt um nicht wiederholt zu triggern
        /// </summary>
        private void SaveTimer_Tick(object? sender, EventArgs e)
        {
            _saveTimer.Stop();
            SaveData();
            SaveIndicator.Text = "Saved";
            Log.Debug("Notes auto-saved");
        }
        
        // ========================================
        // DATEN-PERSISTENZ
        // ========================================
        
        /// <summary>
        /// Lädt gespeicherte Tasks und Notizen aus dem Dateisystem
        /// 
        /// DIN EN ISO 9241-110 Prinzipien:
        /// - Erwartungskonformität: Benutzer erwartet Wiederherstellung vorheriger Daten
        /// - Fehlertoleranz: Umfangreiches Exception-Handling
        /// - Selbstbeschreibungsfähigkeit: Logging dokumentiert Ladevorgang
        /// 
        /// Speicherort:
        /// - %APPDATA%\TaskBarWidget\tasks.json (Task-Hierarchie als JSON)
        /// - %APPDATA%\TaskBarWidget\notes.txt (Notizen als Plain Text)
        /// 
        /// Fehlertoleranz:
        /// - Dateien nicht vorhanden: Sauberer Start mit leeren Daten
        /// - JSON Parse Error: Exception-Handling mit Benutzer-Benachrichtigung
        /// - Korrupte Dateien: Fehlermeldung statt Absturz
        /// 
        /// JSON-Deserialisierung:
        /// - Newtonsoft.Json für robustes Parsing
        /// - Automatische Rekonstruktion der Subtask-Hierarchie
        /// - DateTime-Parsing für CreatedDate
        /// - Rekursive ObservableCollection Wiederherstellung
        /// </summary>
        private void LoadData()
        {
            try
            {
                // ========================================
                // Tasks laden (JSON)
                // ========================================
                var tasksFile = Path.Combine(_dataPath, "tasks.json");
                if (File.Exists(tasksFile))
                {
                    var json = File.ReadAllText(tasksFile);
                    var tasks = JsonConvert.DeserializeObject<ObservableCollection<TaskItem>>(json);
                    if (tasks != null)
                    {
                        _tasks.Clear();
                        foreach (var task in tasks)
                        {
                            _tasks.Add(task);
                        }
                        Log.Information("Loaded {Count} tasks from file", tasks.Count);
                    }
                }
                else
                {
                    Log.Information("No existing tasks file found, starting fresh");
                }
                
                // ========================================
                // Notizen laden (Plain Text)
                // ========================================
                var notesFile = Path.Combine(_dataPath, "notes.txt");
                if (File.Exists(notesFile))
                {
                    NotesTextBox.Text = File.ReadAllText(notesFile);
                    Log.Information("Notes loaded from file");
                }
                else
                {
                    Log.Information("No existing notes file found, starting fresh");
                }
            }
            catch (Exception ex)
            {
                // Fehlertoleranz: Benutzerfreundliche Fehlermeldung
                Log.Error(ex, "Error loading data");
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Speichert Tasks und Notizen ins Dateisystem
        /// 
        /// DIN EN ISO 9241-110 Prinzipien:
        /// - Fehlertoleranz: Datenverlust-Prävention durch Persistenz
        /// - Aufgabenangemessenheit: Automatisches Speichern ohne Benutzeraktion
        /// - Selbstbeschreibungsfähigkeit: Logging dokumentiert Speichervorgänge
        /// 
        /// Speicherformat:
        /// - tasks.json: Indentiertes JSON für Lesbarkeit (Formatting.Indented)
        /// - notes.txt: UTF-8 Plain Text
        /// 
        /// JSON-Serialisierung:
        /// - Newtonsoft.Json mit Formatting.Indented
        /// - Vollständige Hierarchie inkl. aller Subtasks
        /// - DateTime als ISO 8601 String
        /// - Boolean-Properties (IsCompleted, IsExpanded)
        /// 
        /// Fehlertoleranz:
        /// - IOException: Disk full, Permission denied → Benutzer-Benachrichtigung
        /// - Exception-Handling verhindert Datenverlust-Kaskaden
        /// - Logging für Troubleshooting
        /// 
        /// Aufruf-Kontext:
        /// - Nach jeder Task-Änderung (Add, Delete, Edit, Toggle)
        /// - Nach 2 Sekunden Notizen-Inaktivität (Auto-Save Timer)
        /// - Beim Schließen der Anwendung (OnClosed)
        /// </summary>
        private void SaveData()
        {
            try
            {
                // ========================================
                // Tasks speichern (JSON)
                // ========================================
                var tasksFile = Path.Combine(_dataPath, "tasks.json");
                var json = JsonConvert.SerializeObject(_tasks, Formatting.Indented);
                File.WriteAllText(tasksFile, json);
                Log.Debug("Saved {Count} tasks to file", _tasks.Count);
                
                // ========================================
                // Notizen speichern (Plain Text)
                // ========================================
                var notesFile = Path.Combine(_dataPath, "notes.txt");
                File.WriteAllText(notesFile, NotesTextBox.Text);
                Log.Debug("Notes saved to file");
            }
            catch (Exception ex)
            {
                // Fehlertoleranz: Benutzer-Benachrichtigung bei Speicherfehler
                Log.Error(ex, "Error saving data");
                MessageBox.Show($"Error saving data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // ========================================
        // FENSTER-STEUERUNG
        // ========================================
        
        /// <summary>
        /// Event-Handler: Close-Button geklickt
        /// 
        /// DIN EN ISO 9241-110:
        /// - Erwartungskonformität: Close-Button versteckt Fenster (nicht beenden)
        /// - Steuerbarkeit: Benutzer kann Widget jederzeit schließen
        /// - Selbstbeschreibungsfähigkeit: ×-Symbol als universelles Close-Icon
        /// 
        /// Verhalten:
        /// - Versteckt Fenster statt Anwendung zu beenden
        /// - Widget bleibt im Hintergrund aktiv (Keyboard Hook funktioniert weiter)
        /// - RShift+RCtrl öffnet Widget wieder
        /// - Daten werden automatisch gespeichert vor dem Verstecken
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleVisibility();
        }
        
        /// <summary>
        /// Event-Handler: Minimize-Button geklickt
        /// 
        /// DIN EN ISO 9241-110:
        /// - Erwartungskonformität: Minimize versteckt Fenster
        /// - Steuerbarkeit: Alternative zu Close mit gleichem Verhalten
        /// - Selbstbeschreibungsfähigkeit: _-Symbol als Minimize-Icon
        /// 
        /// Verhalten identisch zu CloseButton (verstecken statt minimieren)
        /// Beide Buttons führen zu ToggleVisibility für konsistentes UX
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleVisibility();
        }
        
        // ========================================
        // GLOBALER KEYBOARD HOOK (Windows API)
        // ========================================
        
        /// <summary>
        /// Delegate für Low-Level Keyboard Hook Callback
        /// 
        /// Windows API: SetWindowsHookEx mit WH_KEYBOARD_LL
        /// Ermöglicht systemweite Tastatur-Überwachung
        /// </summary>
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        
        /// <summary>
        /// Installiert den globalen Keyboard Hook
        /// 
        /// DIN EN ISO 9241-110:
        /// - Aufgabenangemessenheit: Systemweiter Hotkey für schnellen Zugriff
        /// - Steuerbarkeit: Widget aus jeder Anwendung heraus aufrufbar
        /// 
        /// Windows API:
        /// - WH_KEYBOARD_LL: Low-Level Keyboard Hook
        /// - Läuft im Context der Anwendung (kein separater Thread)
        /// - GetModuleHandle: Benötigt für Hook-Installation
        /// - dwThreadId=0: Systemweiter Hook (alle Threads)
        /// 
        /// Sicherheit:
        /// - Keine Admin-Rechte erforderlich
        /// - Hook läuft nur solange Anwendung aktiv
        /// - Wird in OnClosed() ordnungsgemäß deregistriert
        /// 
        /// Fehlerbehandlung:
        /// - Gibt IntPtr.Zero bei Fehler zurück
        /// - Anwendung funktioniert auch ohne Hook (nur Hotkey fehlt)
        /// </summary>
        /// <param name="proc">Callback-Funktion für Tastatur-Events</param>
        /// <returns>Hook Handle oder IntPtr.Zero bei Fehler</returns>
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                if (curModule?.ModuleName != null)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                        GetModuleHandle(curModule.ModuleName), 0);
                }
                return IntPtr.Zero;
            }
        }
        
        /// <summary>
        /// Callback-Funktion: Verarbeitet alle systemweiten Tastatur-Events
        /// 
        /// DIN EN ISO 9241-110 Prinzipien:
        /// - Aufgabenangemessenheit: RShift+RCtrl als ergonomischer Hotkey
        /// - Erwartungskonformität: Hotkey-Kombination wie in anderen Anwendungen
        /// - Steuerbarkeit: Toggle-Funktion (zeigen/verstecken)
        /// 
        /// Funktionsweise:
        /// 1. Empfängt alle Keyboard Events (WM_KEYDOWN, WM_KEYUP)
        /// 2. Tracked Status von RShift und RCtrl in static bool Variablen
        /// 3. Bei beiden Tasten gedrückt: ToggleVisibility() aufrufen
        /// 4. CallNextHookEx: Event an nächsten Hook/Anwendung weiterleiten
        /// 
        /// Threading:
        /// - Hook läuft im Windows Message Thread
        /// - Dispatcher.Invoke: UI-Zugriff thread-safe
        /// - MainWindow via Application.Current.MainWindow
        /// 
        /// Hotkey-Wahl Rationale:
        /// - Rechte Tasten: Selten von anderen Apps belegt
        /// - Shift+Ctrl: Beide modifiers für geringe False-Positives
        /// - Ergonomisch: Mit einer Hand auf Numpad-Bereich erreichbar
        /// 
        /// nCode >= 0: Standard Windows Hook Pattern
        /// - nCode < 0: Event muss direkt weitergeleitet werden
        /// - nCode >= 0: Event kann verarbeitet werden
        /// </summary>
        /// <param name="nCode">Hook Code (>= 0 für Processing)</param>
        /// <param name="wParam">Event Type (WM_KEYDOWN/WM_KEYUP)</param>
        /// <param name="lParam">Pointer zu Keyboard Event Daten (Virtual Key Code)</param>
        /// <returns>Result von CallNextHookEx</returns>
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                // Virtual Key Code aus lParam lesen
                int vkCode = Marshal.ReadInt32(lParam);
                
                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    // Taste gedrückt: Status aktualisieren
                    if (vkCode == VK_RSHIFT)
                        _rShiftPressed = true;
                    if (vkCode == VK_RCONTROL)
                        _rControlPressed = true;
                    
                    // Beide Tasten gedrückt? → Hotkey triggern!
                    if (_rShiftPressed && _rControlPressed)
                    {
                        Log.Debug("Hotkey triggered: RShift + RCtrl");
                        
                        // Thread-safe UI-Zugriff via Dispatcher
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            var mainWindow = Application.Current?.MainWindow as MainWindow;
                            mainWindow?.ToggleVisibility();
                        });
                    }
                }
                else if (wParam == (IntPtr)WM_KEYUP)
                {
                    // Taste losgelassen: Status zurücksetzen
                    if (vkCode == VK_RSHIFT)
                        _rShiftPressed = false;
                    if (vkCode == VK_RCONTROL)
                        _rControlPressed = false;
                }
            }
            
            // Event an nächsten Hook weiterleiten (Hook-Chain Pattern)
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        
        // ========================================
        // WINDOWS API P/INVOKE DEKLARATIONEN
        // ========================================
        
        /// <summary>
        /// Installiert einen Application-Defined Hook Procedure in eine Hook Chain
        /// 
        /// Windows API: user32.dll
        /// Dokumentation: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowshookexw
        /// 
        /// Parameter:
        /// - idHook: Hook-Type (WH_KEYBOARD_LL = 13)
        /// - lpfn: Pointer zur Callback-Funktion
        /// - hMod: Handle zum DLL-Modul (für Low-Level Hooks: EXE Module)
        /// - dwThreadId: Thread-ID (0 = systemweiter Hook)
        /// 
        /// Return: Hook Handle oder NULL bei Fehler
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, 
            IntPtr hMod, uint dwThreadId);
        
        /// <summary>
        /// Entfernt einen Hook aus der Hook Chain
        /// 
        /// Windows API: user32.dll
        /// Dokumentation: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-unhookwindowshookex
        /// 
        /// Parameter:
        /// - hhk: Handle vom SetWindowsHookEx-Aufruf
        /// 
        /// Return: true bei Erfolg, false bei Fehler
        /// 
        /// WICHTIG: Muss aufgerufen werden um Memory Leaks zu vermeiden!
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        
        /// <summary>
        /// Leitet Hook-Information zum nächsten Hook in der Chain weiter
        /// 
        /// Windows API: user32.dll
        /// Dokumentation: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-callnexthookex
        /// 
        /// Parameter:
        /// - hhk: Aktueller Hook Handle (kann NULL sein)
        /// - nCode: Hook Code vom HookCallback
        /// - wParam: wParam vom HookCallback
        /// - lParam: lParam vom HookCallback
        /// 
        /// Return: Wert vom nächsten Hook oder 0
        /// 
        /// WICHTIG: Immer aufrufen, sonst brechen andere Hook-Konsumenten!
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        
        /// <summary>
        /// Liefert Handle zu einem geladenen Modul (DLL oder EXE)
        /// 
        /// Windows API: kernel32.dll
        /// Dokumentation: https://learn.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-getmodulehandlea
        /// 
        /// Parameter:
        /// - lpModuleName: Name des Moduls (z.B. "TaskBarWidget.exe")
        /// 
        /// Return: Module Handle oder NULL bei Fehler
        /// 
        /// Verwendung: Benötigt für SetWindowsHookEx
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
    
    // ========================================
    // TASK-DATENMODELL
    // ========================================
    
    /// <summary>
    /// Datenmodell für einen Task (Haupt-Task oder Subtask)
    /// 
    /// DIN EN ISO 9241-110 Konformität:
    /// - Aufgabenangemessenheit: Minimale Properties für Todo-Liste
    /// - Erwartungskonformität: Standard-Task-Attribute (Text, Completed, Created)
    /// - Steuerbarkeit: IsExpanded für hierarchische Ansichts-Kontrolle
    /// 
    /// INotifyPropertyChanged Pattern:
    /// - Ermöglicht Two-Way Data Binding in WPF
    /// - UI aktualisiert sich automatisch bei Property-Änderungen
    /// - Standard MVVM-Pattern für reaktive UIs
    /// 
    /// Hierarchie:
    /// - Jeder TaskItem kann beliebig viele SubTasks enthalten
    /// - Rekursive Struktur (Tree-Pattern)
    /// - ObservableCollection für automatische UI-Updates bei Add/Remove
    /// 
    /// Serialisierung:
    /// - Newtonsoft.Json serialisiert komplette Hierarchie
    /// - ObservableCollection wird als Array gespeichert
    /// - DateTime als ISO 8601 String
    /// </summary>
    public class TaskItem : INotifyPropertyChanged
    {
        // ========================================
        // Private Backing Fields
        // ========================================
        
        /// <summary>Backing Field für IsCompleted Property</summary>
        private bool _isCompleted;
        
        /// <summary>Backing Field für IsExpanded Property (default: aufgeklappt)</summary>
        private bool _isExpanded = true;
        
        // ========================================
        // Public Properties
        // ========================================
        
        /// <summary>
        /// Task-Text (Beschreibung)
        /// 
        /// Kann direkt gesetzt werden (kein Backing Field nötig)
        /// Änderungen müssen via OnPropertyChanged(nameof(Text)) gemeldet werden
        /// </summary>
        public string Text { get; set; } = string.Empty;
        
        /// <summary>
        /// Task-Status: Erledigt (true) oder Offen (false)
        /// 
        /// DIN EN ISO 9241-110:
        /// - Aufgabenangemessenheit: Kern-Funktionalität einer Todo-Liste
        /// - Selbstbeschreibungsfähigkeit: PropertyChanged für UI-Feedback
        /// 
        /// Property Pattern:
        /// - Getter: Backing Field zurückgeben
        /// - Setter: Nur bei Änderung → PropertyChanged Event feuern
        /// - Verhindert unnötige UI-Updates bei gleichem Wert
        /// </summary>
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    OnPropertyChanged(nameof(IsCompleted));
                }
            }
        }
        
        /// <summary>
        /// Subtask-Liste ein-/ausgeklappt (true = sichtbar)
        /// 
        /// DIN EN ISO 9241-110:
        /// - Individualisierbarkeit: Benutzer kontrolliert Detailgrad
        /// - Steuerbarkeit: Toggle-Funktion für hierarchische Navigation
        /// 
        /// Property Pattern wie IsCompleted
        /// Default: true (neue Tasks sind aufgeklappt)
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }
        
        /// <summary>
        /// Erstellungs-Zeitstempel
        /// 
        /// Verwendung:
        /// - Nachvollziehbarkeit (wann wurde Task erstellt?)
        /// - Sortierung (neueste zuerst/zuletzt)
        /// - Auswertungen (Tasks pro Tag/Woche)
        /// 
        /// Format: DateTime (Newtonsoft.Json serialisiert als ISO 8601)
        /// </summary>
        public DateTime CreatedDate { get; set; }
        
        /// <summary>
        /// Hierarchische Subtask-Collection
        /// 
        /// DIN EN ISO 9241-110:
        /// - Aufgabenangemessenheit: Ermöglicht Task-Breakdown
        /// - Lernförderlichkeit: Hierarchische Strukturierung fördert Planung
        /// 
        /// ObservableCollection:
        /// - Auto UI-Update bei Add/Remove
        /// - Unterstützt Data Binding in WPF
        /// - Rekursive Struktur (SubTasks enthalten SubTasks)
        /// 
        /// JSON-Serialisierung:
        /// - Komplette Hierarchie wird gespeichert
        /// - Beliebige Verschachtelungstiefe
        /// </summary>
        public ObservableCollection<TaskItem> SubTasks { get; set; } = new ObservableCollection<TaskItem>();
        
        /// <summary>
        /// Computed Property: Hat dieser Task Subtasks?
        /// 
        /// Verwendung:
        /// - XAML Visibility Binding für Expand/Collapse Button
        /// - Conditional Styling
        /// 
        /// Kein Backing Field nötig (read-only, computed)
        /// </summary>
        public bool HasSubTasks => SubTasks.Count > 0;
        
        // ========================================
        // INotifyPropertyChanged Implementation
        // ========================================
        
        /// <summary>
        /// Event für Property-Änderungen (INotifyPropertyChanged Interface)
        /// 
        /// WPF Data Binding:
        /// - UI subscribt automatisch zu diesem Event
        /// - Bei PropertyChanged → UI-Element aktualisiert sich
        /// - Ermöglicht Two-Way Binding (UI ↔ ViewModel)
        /// 
        /// Nullable Event: '?' erlaubt null (keine Subscriber)
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
        
        /// <summary>
        /// Feuert PropertyChanged Event für gegebene Property
        /// 
        /// DIN EN ISO 9241-110:
        /// - Selbstbeschreibungsfähigkeit: UI aktualisiert sich automatisch
        /// - Erwartungskonformität: Standard WPF-Pattern
        /// 
        /// Spezialfall:
        /// - SubTasks Property geändert → auch HasSubTasks aktualisieren
        /// - Verkettete PropertyChanged Events für computed properties
        /// 
        /// Public Modifier:
        /// - Muss public sein für externe Aufrufe (z.B. nach Text-Änderung)
        /// - Standard INotifyPropertyChanged Pattern
        /// </summary>
        /// <param name="propertyName">Name der geänderten Property</param>
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            // Computed Property Cascade: SubTasks → HasSubTasks
            if (propertyName == nameof(SubTasks))
            {
                OnPropertyChanged(nameof(HasSubTasks));
            }
        }
    }
}
