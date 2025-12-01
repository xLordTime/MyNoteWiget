using System.Windows;
using System.Windows.Input;

namespace TaskBarWidget
{
    public partial class EditTaskDialog : Window
    {
        public string TaskText
        {
            get => TaskTextBox.Text;
            set => TaskTextBox.Text = value;
        }
        
        public EditTaskDialog()
        {
            InitializeComponent();
        }
        
        public EditTaskDialog(string currentText) : this()
        {
            TaskText = currentText;
            TaskTextBox.SelectAll();
            TaskTextBox.Focus();
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
        
        private void TaskTextBox_KeyDown(object sender, KeyEventArgs e)
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
