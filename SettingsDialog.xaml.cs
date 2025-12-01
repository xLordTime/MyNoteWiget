using System.Windows;

namespace TaskBarWidget
{
    public partial class SettingsDialog : Window
    {
        public bool AutostartEnabled => AutostartCheckBox.IsChecked == true;
        
        public SettingsDialog(bool currentAutostartState)
        {
            InitializeComponent();
            AutostartCheckBox.IsChecked = currentAutostartState;
        }
        
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
        
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
