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
    public partial class MainWindow : Window
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private static IntPtr _hookID = IntPtr.Zero;
        private static LowLevelKeyboardProc _proc = null!;
        
        private const int VK_RSHIFT = 0xA1;
        private const int VK_RCONTROL = 0xA3;
        
        private static bool _rShiftPressed = false;
        private static bool _rControlPressed = false;
        
        private ObservableCollection<TaskItem> _tasks;
        private DispatcherTimer _saveTimer;
        private string _dataPath;
        private bool _isDarkMode = true;
        private bool _isClosing = false;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize Logger
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
            
            _tasks = new ObservableCollection<TaskItem>();
            TasksList.ItemsSource = _tasks;
            
            _dataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TaskBarWidget"
            );
            Directory.CreateDirectory(_dataPath);
            Log.Information("Data path created: {DataPath}", _dataPath);
            
            _saveTimer = new DispatcherTimer();
            _saveTimer.Interval = TimeSpan.FromSeconds(2);
            _saveTimer.Tick += SaveTimer_Tick;
            
            LoadData();
            UpdateTaskCounter();
            
            // Position window on the right side of the screen
            PositionWindowRight();
            
            // Set up global keyboard hook immediately in constructor
            _proc = HookCallback;
            _hookID = SetHook(_proc);
            Log.Information("Global keyboard hook registered. Hook ID: {HookID}", _hookID);
            
            // Initialize theme
            ApplyTheme(_isDarkMode);
            
            // Start hidden
            this.Visibility = Visibility.Hidden;
            Log.Information("MainWindow initialized successfully");
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Information("Window loaded event triggered");
        }
        
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source?.AddHook(WndProc);
        }
        
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            UnhookWindowsHookEx(_hookID);
            Log.Information("Global keyboard hook unregistered");
            SaveData();
            Log.Information("TaskBar Widget closed");
            Log.CloseAndFlush();
        }
        
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            return IntPtr.Zero;
        }
        
        private void PositionWindowRight()
        {
            var workingArea = SystemParameters.WorkArea;
            this.Left = workingArea.Right - this.Width - 10;
            this.Top = (workingArea.Height - this.Height) / 2;
        }
        
        private void ApplyTheme(bool isDark)
        {
            var resources = Application.Current.Resources;
            
            if (isDark)
            {
                resources["BackgroundBrush"] = resources["DarkBackground"];
                resources["SecondaryBackgroundBrush"] = resources["DarkSecondaryBackground"];
                resources["BorderBrush"] = resources["DarkBorderBrush"];
                resources["TextPrimaryBrush"] = resources["DarkTextPrimary"];
                resources["TextSecondaryBrush"] = resources["DarkTextSecondary"];
                resources["AccentBrush"] = resources["DarkAccent"];
                resources["SuccessBrush"] = resources["DarkSuccess"];
                resources["DangerBrush"] = resources["DarkDanger"];
                resources["HoverBrush"] = resources["DarkHover"];
                ThemeToggleButton.Content = "☀️";
            }
            else
            {
                resources["BackgroundBrush"] = resources["LightBackground"];
                resources["SecondaryBackgroundBrush"] = resources["LightSecondaryBackground"];
                resources["BorderBrush"] = resources["LightBorderBrush"];
                resources["TextPrimaryBrush"] = resources["LightTextPrimary"];
                resources["TextSecondaryBrush"] = resources["LightTextSecondary"];
                resources["AccentBrush"] = resources["LightAccent"];
                resources["SuccessBrush"] = resources["LightSuccess"];
                resources["DangerBrush"] = resources["LightDanger"];
                resources["HoverBrush"] = resources["LightHover"];
                ThemeToggleButton.Content = "🌙";
            }
            
            Log.Information("Theme changed to: {Theme}", isDark ? "Dark" : "Light");
        }
        
        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isDarkMode = !_isDarkMode;
            ApplyTheme(_isDarkMode);
        }
        
        private void ToggleVisibility()
        {
            if (this.Visibility == Visibility.Visible && !_isClosing)
            {
                _isClosing = true;
                var slideOut = (System.Windows.Media.Animation.Storyboard)this.FindResource("SlideOutAnimation");
                slideOut.Begin(MainBorder);
                Log.Information("Widget hiding with animation");
            }
            else if (!_isClosing)
            {
                PositionWindowRight();
                this.Visibility = Visibility.Visible;
                this.Activate();
                
                var slideIn = (System.Windows.Media.Animation.Storyboard)this.FindResource("SlideInAnimation");
                slideIn.Begin(MainBorder);
                
                Log.Information("Widget shown at position: Left={Left}, Top={Top}", this.Left, this.Top);
                
                // Focus on task input with slight delay
                this.Dispatcher.BeginInvoke(new Action(() => TaskInput.Focus()), 
                    System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
        }
        
        private void SlideOutAnimation_Completed(object? sender, EventArgs e)
        {
            this.Visibility = Visibility.Hidden;
            _isClosing = false;
            Log.Information("Widget hidden");
        }
        
        // Task Management
        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            AddTask();
        }
        
        private void TaskInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTask();
            }
        }
        
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
        
        private void TaskCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is TaskItem task)
            {
                Log.Information("Task {Status}: {TaskText}", task.IsCompleted ? "completed" : "uncompleted", task.Text);
            }
            UpdateTaskCounter();
            SaveData();
        }
        
        private void UpdateTaskCounter()
        {
            var completedCount = CountCompletedTasks(_tasks);
            var totalCount = CountAllTasks(_tasks);
            TaskCounter.Text = $"{completedCount}/{totalCount} completed";
        }
        
        private int CountAllTasks(ObservableCollection<TaskItem> tasks)
        {
            int count = tasks.Count;
            foreach (var task in tasks)
            {
                count += CountAllTasks(task.SubTasks);
            }
            return count;
        }
        
        private int CountCompletedTasks(ObservableCollection<TaskItem> tasks)
        {
            int count = tasks.Count(t => t.IsCompleted);
            foreach (var task in tasks)
            {
                count += CountCompletedTasks(task.SubTasks);
            }
            return count;
        }
        
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
                    
                    // Refresh UI
                    var items = TasksList.ItemsSource;
                    TasksList.ItemsSource = null;
                    TasksList.ItemsSource = items;
                }
            }
        }
        
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
                    parentTask.IsExpanded = true;
                    
                    Log.Information("Subtask added to '{ParentTask}': {SubTaskText}", parentTask.Text, subTask.Text);
                    UpdateTaskCounter();
                    SaveData();
                }
            }
        }
        
        private void ToggleSubTasks_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is TaskItem task)
            {
                task.IsExpanded = !task.IsExpanded;
                SaveData();
            }
        }
        
        // Notes Management
        private void NotesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveIndicator.Text = "Saving...";
            _saveTimer.Stop();
            _saveTimer.Start();
        }
        
        private void SaveTimer_Tick(object? sender, EventArgs e)
        {
            _saveTimer.Stop();
            SaveData();
            SaveIndicator.Text = "Saved";
            Log.Debug("Notes auto-saved");
        }
        
        // Data Persistence
        private void LoadData()
        {
            try
            {
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
                Log.Error(ex, "Error loading data");
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void SaveData()
        {
            try
            {
                var tasksFile = Path.Combine(_dataPath, "tasks.json");
                var json = JsonConvert.SerializeObject(_tasks, Formatting.Indented);
                File.WriteAllText(tasksFile, json);
                Log.Debug("Saved {Count} tasks to file", _tasks.Count);
                
                var notesFile = Path.Combine(_dataPath, "notes.txt");
                File.WriteAllText(notesFile, NotesTextBox.Text);
                Log.Debug("Notes saved to file");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving data");
                MessageBox.Show($"Error saving data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Window Controls
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleVisibility();
        }
        
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleVisibility();
        }
        
        // Global Keyboard Hook
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        
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
        
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                
                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    if (vkCode == VK_RSHIFT)
                        _rShiftPressed = true;
                    if (vkCode == VK_RCONTROL)
                        _rControlPressed = true;
                    
                    // Check if both keys are pressed
                    if (_rShiftPressed && _rControlPressed)
                    {
                        Log.Debug("Hotkey triggered: RShift + RCtrl");
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            var mainWindow = Application.Current?.MainWindow as MainWindow;
                            mainWindow?.ToggleVisibility();
                        });
                    }
                }
                else if (wParam == (IntPtr)WM_KEYUP)
                {
                    if (vkCode == VK_RSHIFT)
                        _rShiftPressed = false;
                    if (vkCode == VK_RCONTROL)
                        _rControlPressed = false;
                }
            }
            
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, 
            IntPtr hMod, uint dwThreadId);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
    
    public class TaskItem : INotifyPropertyChanged
    {
        private bool _isCompleted;
        private bool _isExpanded = true;
        
        public string Text { get; set; } = string.Empty;
        
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
        
        public DateTime CreatedDate { get; set; }
        
        public ObservableCollection<TaskItem> SubTasks { get; set; } = new ObservableCollection<TaskItem>();
        
        public bool HasSubTasks => SubTasks.Count > 0;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            if (propertyName == nameof(SubTasks))
            {
                OnPropertyChanged(nameof(HasSubTasks));
            }
        }
    }
}
