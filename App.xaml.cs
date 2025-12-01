using System;
using System.Threading;
using System.Windows;

namespace TaskBarWidget
{
    /// <summary>
    /// Anwendungs-Einstiegspunkt für TaskBar Widget WPF Application
    /// 
    /// DIN EN ISO 9241-110 Konformität:
    /// - Aufgabenangemessenheit: Minimaler Einstiegspunkt ohne Overhead
    /// - Erwartungskonformität: Standard WPF Application Pattern
    /// - Selbstbeschreibungsfähigkeit: XAML definiert StartupUri und Resources
    /// 
    /// Architektur:
    /// - Partial Class: Code-Behind zu App.xaml
    /// - App.xaml enthält:
    ///   * StartupUri="src/Views/MainWindow.xaml" (Hauptfenster)
    ///   * ResourceDictionary Merge für Themes.xaml
    /// - Erbt von System.Windows.Application (WPF Base Class)
    /// 
    /// Startup-Sequenz:
    /// 1. App() Konstruktor (implicit via InitializeComponent)
    /// 2. OnStartup() Override
    /// 3. MainWindow wird via StartupUri geladen
    /// 4. MainWindow Konstruktor läuft
    /// 5. Window_Loaded Event
    /// 
    /// Theme-System:
    /// - Themes.xaml wird in App.xaml als MergedDictionary geladen
    /// - Ermöglicht globale Resource-Zugriffe
    /// - MainWindow kann Application.Current.Resources manipulieren
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Mutex für Single-Instance Enforcement
        /// Verhindert dass mehrere Instanzen der Anwendung gleichzeitig laufen
        /// </summary>
        private static Mutex? _mutex = null;
        
        /// <summary>
        /// Override: Wird beim Anwendungsstart aufgerufen
        /// 
        /// DIN EN ISO 9241-110:
        /// - Aufgabenangemessenheit: Ermöglicht zentrale Initialisierung
        /// - Fehlertoleranz: Single-Instance Enforcement verhindert Mehrfach-Instanzen
        /// - Ressourcenschonung: Nur eine Instanz = weniger RAM/CPU-Verbrauch
        /// 
        /// Single-Instance Pattern:
        /// - Mutex mit eindeutiger GUID als Name
        /// - Prüft ob bereits eine Instanz läuft
        /// - Beendet sich selbst wenn Duplikat erkannt
        /// 
        /// Ressourcen-Optimierung:
        /// - ShutdownMode.OnExplicitShutdown: Keine automatische Beendigung
        /// - Dispatcher Priority optimiert für Background-Anwendungen
        /// 
        /// Parameter:
        /// - e.Args: Command-Line Argumente (string[])
        /// - e.PerformDefaultAction: Kann auf false gesetzt werden
        /// </summary>
        /// <param name="e">Startup Event Arguments mit Command-Line Args</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // ========================================
            // SINGLE-INSTANCE ENFORCEMENT
            // ========================================
            // Verhindert dass 100+ Instanzen parallel laufen
            // Mutex = Mutual Exclusion Object (systemweit)
            
            const string mutexName = "Global\\{8F6F0AC4-B9A1-45FD-A8E3-11E10C5C87E9}";
            
            _mutex = new Mutex(true, mutexName, out bool createdNew);
            
            if (!createdNew)
            {
                // Bereits eine Instanz läuft!
                MessageBox.Show(
                    "TaskBar Widget läuft bereits!\n\nVerwenden Sie Rechts-Shift + Rechts-Strg um das Widget zu öffnen.",
                    "Bereits gestartet",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                
                // Anwendung sofort beenden (keine zweite Instanz starten)
                Current.Shutdown();
                return;
            }
            
            // ========================================
            // RESSOURCEN-OPTIMIERUNG
            // ========================================
            
            // Process Priority: Niedrigere Prozess-Priorität für Hintergrund-Betrieb
            // Spart CPU wenn Anwendung im Hintergrund läuft
            try
            {
                using var process = System.Diagnostics.Process.GetCurrentProcess();
                process.PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            }
            catch
            {
                // Ignoriere Fehler bei Priority-Änderung (nicht kritisch)
            }
            
            base.OnStartup(e);
            
            // Shutdown-Modus: Explizit (nicht beim letzten Fenster schließen)
            // Widget läuft im Hintergrund weiter
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }
        
        /// <summary>
        /// Override: Aufräumen beim Beenden der Anwendung
        /// Mutex freigeben für sauberen Shutdown
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}
