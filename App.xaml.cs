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
        /// Override: Wird beim Anwendungsstart aufgerufen
        /// 
        /// DIN EN ISO 9241-110:
        /// - Aufgabenangemessenheit: Ermöglicht zentrale Initialisierung
        /// - Fehlertoleranz: Kann globales Exception-Handling einrichten
        /// 
        /// Aktuell minimal:
        /// - Nur base.OnStartup(e) Aufruf
        /// - Keine zusätzliche Logik nötig (MainWindow initialisiert sich selbst)
        /// 
        /// Mögliche Erweiterungen:
        /// - Globales Exception-Handling via DispatcherUnhandledException
        /// - Command-Line Argument Parsing (e.Args)
        /// - Single-Instance Enforcement (Mutex)
        /// - Splash Screen
        /// - Dependency Injection Container Setup
        /// 
        /// Parameter:
        /// - e.Args: Command-Line Argumente (string[])
        /// - e.PerformDefaultAction: Kann auf false gesetzt werden
        /// </summary>
        /// <param name="e">Startup Event Arguments mit Command-Line Args</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Minimaler Einstiegspunkt:
            // - base.OnStartup() triggert Window-Erstellung via StartupUri
            // - MainWindow Konstruktor übernimmt weitere Initialisierung
            // - Serilog wird in MainWindow Constructor eingerichtet
            // - Keyboard Hook wird in MainWindow Constructor installiert
        }
    }
}
