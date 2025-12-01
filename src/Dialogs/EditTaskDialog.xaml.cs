using System.Windows;
using System.Windows.Input;

namespace TaskBarWidget
{
    /// <summary>
    /// Dialog zum Bearbeiten eines bestehenden Task-Textes
    /// 
    /// DIN EN ISO 9241-110 Konformität:
    /// - Fehlertoleranz: Ermöglicht Korrektur von Tippfehlern
    /// - Selbstbeschreibungsfähigkeit: Dialog zeigt aktuellen Text
    /// - Erwartungskonformität: Standard Edit-Dialog mit OK/Cancel
    /// - Steuerbarkeit: Keyboard-Shortcuts und Textauswahl
    /// - Lernförderlichkeit: Konsistent mit SubTaskDialog
    /// 
    /// Barrierefreiheit:
    /// - Automatische Textauswahl für schnelles Überschreiben
    /// - Automatischer Fokus auf Eingabefeld
    /// - Vollständige Keyboard-Steuerung
    /// 
    /// UX-Optimierung:
    /// - SelectAll() ermöglicht sofortiges Überschreiben ODER Editieren
    /// - Focus() verhindert zusätzlichen Klick
    /// - Enter/ESC Shortcuts für Power-User
    /// </summary>
    public partial class EditTaskDialog : Window
    {
        /// <summary>
        /// Property: Task-Text mit Get/Set Zugriff
        /// 
        /// DIN EN ISO 9241-110:
        /// - Aufgabenangemessenheit: Direkter Zugriff auf TextBox-Inhalt
        /// - Erwartungskonformität: Standard Property-Pattern
        /// 
        /// Verwendung:
        /// var dialog = new EditTaskDialog("Alter Text");
        /// if (dialog.ShowDialog() == true)
        ///     string newText = dialog.TaskText;
        /// 
        /// Expression-Bodied Members (C# 7.0):
        /// - Get => TaskTextBox.Text
        /// - Set => TaskTextBox.Text = value
        /// - Kompakter Syntax für einfache Properties
        /// </summary>
        public string TaskText
        {
            get => TaskTextBox.Text;
            set => TaskTextBox.Text = value;
        }
        
        /// <summary>
        /// Default-Konstruktor: Initialisiert leeren Dialog
        /// 
        /// Wird intern von parametrisiertem Konstruktor aufgerufen (:this())
        /// Sollte normalerweise nicht direkt verwendet werden
        /// </summary>
        public EditTaskDialog()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// Parametrisierter Konstruktor: Dialog mit vorhandenem Text initialisieren
        /// 
        /// DIN EN ISO 9241-110:
        /// - Selbstbeschreibungsfähigkeit: Zeigt aktuellen Text zur Bearbeitung
        /// - Fehlertoleranz: Text kann geändert oder beibehalten werden
        /// - Steuerbarkeit: SelectAll() für schnelle Komplett-Änderung
        /// - Lernförderlichkeit: Text ist vorausgewählt (intuitive UX)
        /// 
        /// Constructor Chaining:
        /// - :this() ruft Default-Konstruktor auf
        /// - Danach: Spezialisierte Initialisierung
        /// - Best Practice für DRY (Don't Repeat Yourself)
        /// 
        /// UX-Optimierung:
        /// 1. TaskText = currentText → TextBox mit vorhandenem Text füllen
        /// 2. SelectAll() → Gesamten Text markieren
        /// 3. Focus() → Cursor in TextBox setzen
        /// 
        /// Benutzer-Workflows:
        /// - Sofort tippen → Überschreibt kompletten Text
        /// - Pfeiltasten → Selektion aufheben, Text editieren
        /// - Ende/Home → Zum Textende/-anfang springen
        /// </summary>
        /// <param name="currentText">Aktueller Task-Text zum Bearbeiten</param>
        public EditTaskDialog(string currentText) : this()
        {
            TaskText = currentText;
            TaskTextBox.SelectAll();  // Erwartungskonformität: Vorauswahl für schnelles Editieren
            TaskTextBox.Focus();      // Steuerbarkeit: Sofort mit Tippen beginnen
        }
        
        /// <summary>
        /// Event-Handler: OK-Button geklickt
        /// 
        /// DIN EN ISO 9241-110:
        /// - Erwartungskonformität: OK übernimmt Änderungen
        /// - Selbstbeschreibungsfähigkeit: DialogResult=true signalisiert Erfolg
        /// - Steuerbarkeit: Explizite Bestätigung erforderlich
        /// 
        /// Dialog-Pattern:
        /// - DialogResult = true → ShowDialog() gibt true zurück
        /// - Parent kann TaskText Property auslesen
        /// - Close() schließt Modal-Dialog
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
        /// - Fehlertoleranz: Benutzer kann Änderungen verwerfen
        /// - Steuerbarkeit: Volle Kontrolle über Commit/Rollback
        /// - Erwartungskonformität: Cancel verwirft alle Änderungen
        /// 
        /// Dialog-Pattern:
        /// - DialogResult = false → ShowDialog() gibt false zurück
        /// - Parent ignoriert TaskText (verwendet Original-Text)
        /// - Close() schließt Dialog ohne zu speichern
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
        /// - Steuerbarkeit: Keyboard-Shortcuts beschleunigen Workflow
        /// - Erwartungskonformität: Enter=OK, ESC=Cancel (universell)
        /// - Lernförderlichkeit: Keine Dokumentation nötig
        /// - Aufgabenangemessenheit: Power-User können Maus vermeiden
        /// 
        /// Barrierefreiheit:
        /// - Vollständige Keyboard-Bedienbarkeit
        /// - Enter: Häufigste Aktion (Bestätigen)
        /// - ESC: Universelles "Zurück" Pattern
        /// - Screen Reader kompatibel
        /// 
        /// Keyboard-Shortcuts:
        /// - Enter → Änderungen übernehmen (DialogResult=true)
        /// - ESC → Änderungen verwerfen (DialogResult=false)
        /// - Tab → Navigation zu Buttons (Standard WPF)
        /// - Ctrl+A → Alles auswählen (Standard TextBox)
        /// </summary>
        private void TaskTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Enter = OK: Änderungen übernehmen
                DialogResult = true;
                Close();
            }
            else if (e.Key == Key.Escape)
            {
                // ESC = Cancel: Änderungen verwerfen
                DialogResult = false;
                Close();
            }
        }
    }
}
