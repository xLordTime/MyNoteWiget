using System.Windows;
using System.Windows.Input;

namespace TaskBarWidget
{
    public partial class SubTaskDialog : Window
    {
        public string SubTaskText => SubTaskTextBox.Text;
        
        public SubTaskDialog()
        {
            InitializeComponent();
            SubTaskTextBox.Focus();
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
        
        private void SubTaskTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DialogResult = true;
                Close();
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}
