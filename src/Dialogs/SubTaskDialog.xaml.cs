using System.Windows;
using System.Windows.Input;

namespace TaskBarWidget
{
    /// <summary>
    /// Dialog zum Hinzufügen von Subtasks zu einem bestehenden Task
    /// 
    /// DIN EN ISO 9241-110 Konformität:
    /// - Aufgabenangemessenheit: Minimaler Dialog für einfache Texteingabe
    /// - Selbstbeschreibungsfähigkeit: Klare OK/Abbrechen Buttons
    /// - Erwartungskonformität: Standard-Dialog-Pattern (Modal, OK/Cancel)
    /// - Steuerbarkeit: Keyboard-Shortcuts (Enter=OK, ESC=Cancel)
    /// - Lernförderlichkeit: Intuitive Bedienung ohne Dokumentation
    /// 
    /// Barrierefreiheit:
    /// - Automatischer Fokus auf Eingabefeld
    /// - Vollständige Keyboard-Bedienbarkeit
    /// - Tab-Navigation zwischen Controls
    /// 
    /// Modal-Dialog:
    /// - ShowDialog() blockiert Parent-Fenster
    /// - DialogResult signalisiert OK (true) oder Cancel (false/null)
    /// - Verhindert inkonsistente Zustände
    /// </summary>
    public partial class SubTaskDialog : Window
    {
        /// <summary>
        /// Read-Only Property: Gibt eingegebenen Subtask-Text zurück
        /// 
        /// DIN EN ISO 9241-110:
        /// - Aufgabenangemessenheit: Direkter Zugriff auf Ergebnis
        /// - Erwartungskonformität: Expression-Bodied Property (moderner C#-Stil)
        /// 
        /// Verwendung:
        /// var dialog = new SubTaskDialog();
        /// if (dialog.ShowDialog() == true)
        ///     string text = dialog.SubTaskText;
        /// </summary>
        public string SubTaskText => SubTaskTextBox.Text;
        
        /// <summary>
        /// Konstruktor: Initialisiert Dialog mit automatischem Fokus
        /// 
        /// DIN EN ISO 9241-110:
        /// - Lernförderlichkeit: Benutzer kann sofort tippen
        /// - Steuerbarkeit: Fokus automatisch auf Eingabefeld
        /// - Aufgabenangemessenheit: Minimale Klicks nötig
        /// 
        /// Fokus-Setzen:
        /// - SubTaskTextBox.Focus() direkt nach InitializeComponent()
        /// - Ermöglicht sofortiges Tippen ohne Maus-Click
        /// - Verbessert Keyboard-Workflow
        /// </summary>
        public SubTaskDialog()
        {
            InitializeComponent();
            SubTaskTextBox.Focus();
        }
        
        /// <summary>
        /// Event-Handler: OK-Button geklickt
        /// 
        /// DIN EN ISO 9241-110:
        /// - Erwartungskonformität: OK bestätigt Eingabe
        /// - Selbstbeschreibungsfähigkeit: DialogResult=true signalisiert Erfolg
        /// 
        /// Dialog-Pattern:
        /// - DialogResult = true → ShowDialog() gibt true zurück
        /// - Close() schließt Dialog-Fenster
        /// - Parent kann SubTaskText Property auslesen
        /// </summary>
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
        
        /// <summary>
        /// Event-Handler: Abbrechen-Button geklickt
        /// 
        /// DIN EN ISO 9241-110:
        /// - Fehlertoleranz: Benutzer kann Aktion abbrechen
        /// - Erwartungskonformität: Cancel verwirft Eingabe
        /// - Steuerbarkeit: Benutzer behält Kontrolle
        /// 
        /// Dialog-Pattern:
        /// - DialogResult = false → ShowDialog() gibt false zurück
        /// - Close() schließt Dialog ohne Änderungen
        /// - Parent ignoriert SubTaskText Property
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        /// <summary>
        /// Event-Handler: Tastatureingabe in TextBox
        /// 
        /// DIN EN ISO 9241-110:
        /// - Steuerbarkeit: Keyboard-Shortcuts für schnellere Interaktion
        /// - Erwartungskonformität: Enter=OK, ESC=Cancel als Standard-Pattern
        /// - Lernförderlichkeit: Universelle Tastenkürzel
        /// 
        /// Barrierefreiheit:
        /// - Vollständige Keyboard-Bedienbarkeit ohne Maus
        /// - Enter: Schnelle Bestätigung (häufigster Use-Case)
        /// - ESC: Schnelles Abbrechen (universelles Escape-Pattern)
        /// 
        /// Keyboard-Shortcuts:
        /// - Enter → Wie OK-Button (DialogResult=true)
        /// - ESC → Wie Cancel-Button (DialogResult=false)
        /// - Tab → Navigation zu Buttons (Standard WPF)
        /// </summary>
        private void SubTaskTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Enter = OK: Eingabe bestätigen
                DialogResult = true;
                Close();
            }
            else if (e.Key == Key.Escape)
            {
                // ESC = Cancel: Eingabe verwerfen
                DialogResult = false;
                Close();
            }
        }
    }
}
