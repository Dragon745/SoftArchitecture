using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SoftArchitecture.Models;
using SoftArchitecture.Services;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using IOPath = System.IO.Path;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace SoftArchitecture
{
    public class TaskWrapper : INotifyPropertyChanged
    {
        private SoftArchitecture.Models.Task _task;
        private string _taskName;
        private string _goal;
        private bool _isCompleted;
        private bool _isSelected;

        public SoftArchitecture.Models.Task Task
        {
            get => _task;
            set => SetProperty(ref _task, value);
        }

        public string TaskName
        {
            get => _taskName;
            set => SetProperty(ref _taskName, value);
        }

        public string Goal
        {
            get => _goal;
            set => SetProperty(ref _goal, value);
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public TaskWrapper(SoftArchitecture.Models.Task task, string taskName)
        {
            _task = task;
            _taskName = taskName;
            _goal = task.Goal;
            _isCompleted = task.IsCompleted;
            _isSelected = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ProjectData? _projectData;
        private string? _currentProjectPath;
        private int _selectedPhaseIndex = -1;
        private int _selectedTaskIndex = -1; // Added for task selection
        private const string SETTINGS_FILE = "settings.json";

        public MainWindow()
        {
            InitializeComponent();
            LoadLastProjectOnStartup();
        }

        private async void LoadLastProjectOnStartup()
        {
            try
            {
                string? lastProjectPath = LoadLastProjectPath();
                if (!string.IsNullOrEmpty(lastProjectPath) && Directory.Exists(lastProjectPath))
                {
                    _currentProjectPath = lastProjectPath;
                    await LoadProjectData();
                }
            }
            catch (Exception ex)
            {
                // Silently handle startup errors - don't show message box on startup
                System.Diagnostics.Debug.WriteLine($"Error loading last project on startup: {ex.Message}");
            }
        }

        private string? LoadLastProjectPath()
        {
            try
            {
                string settingsPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, SETTINGS_FILE);
                if (File.Exists(settingsPath))
                {
                    string jsonContent = File.ReadAllText(settingsPath);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                    if (settings != null && settings.ContainsKey("LastProjectPath"))
                    {
                        return settings["LastProjectPath"];
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
            return null;
        }

        private void SaveLastProjectPath(string projectPath)
        {
            try
            {
                var settings = new Dictionary<string, string>
                {
                    { "LastProjectPath", projectPath }
                };

                string settingsPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, SETTINGS_FILE);
                string jsonContent = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, jsonContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select a project folder containing ToDo.json",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _currentProjectPath = dialog.SelectedPath;
                SaveLastProjectPath(_currentProjectPath);
                await LoadProjectData();
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void RefreshMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentProjectPath))
            {
                await LoadProjectData();
            }
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                "Project Manager v1.0\n\nA professional project management tool for software developers.\n\nLoad ToDo.json files to manage project phases and tasks.",
                "About Project Manager",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async System.Threading.Tasks.Task LoadProjectData()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                // Look for ToDo.json in the selected folder
                string jsonPath = IOPath.Combine(_currentProjectPath!, "ToDo.json");

                if (!File.Exists(jsonPath))
                {
                    System.Windows.MessageBox.Show("ToDo.json file not found in the selected folder. Please ensure the file exists in the project root.",
                                  "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _projectData = ProjectDataService.LoadProjectData(jsonPath);

                if (_projectData != null)
                {
                    await System.Threading.Tasks.Task.Delay(300); // Small delay for loading effect
                    PopulateUI();
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to load project data. Please check the JSON file format.",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading project data: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void PopulateUI()
        {
            if (_projectData == null) return;

            // Set project header information
            ProjectNameText.Text = _projectData.ProjectName;
            ProjectDescriptionText.Text = _projectData.ProjectDescription;

            // Calculate and display project statistics
            UpdateProjectStatistics();

            // Clear existing phases and populate the list
            PhasesList.Items.Clear();

            // Add phases to the list
            for (int phaseIndex = 0; phaseIndex < _projectData.Phases.Count; phaseIndex++)
            {
                var phase = _projectData.Phases[phaseIndex];
                // Set the display name with numbering (don't modify original PhaseName)
                phase.DisplayPhaseName = $"Phase {phaseIndex}: {phase.PhaseName}";
                phase.IsSelected = false; // Reset selection
                PhasesList.Items.Add(phase);
            }

            // Clear other panels
            ClearOtherPanels();
        }

        private void UpdateProjectStatistics()
        {
            if (_projectData == null) return;

            int totalPhases = _projectData.Phases.Count;
            int completedPhases = _projectData.Phases.Count(p => p.IsCompleted);
            int totalTasks = _projectData.Phases.Sum(p => p.Tasks.Count);
            int completedTasks = _projectData.Phases.Sum(p => p.Tasks.Count(t => t.IsCompleted));

            double phaseProgress = totalPhases > 0 ? (double)completedPhases / totalPhases * 100 : 0;
            double taskProgress = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0;

            TotalPhasesText.Text = totalPhases.ToString();
            TotalTasksText.Text = totalTasks.ToString();

            ProjectStatsText.Text = $"Phases: {completedPhases}/{totalPhases} ({phaseProgress:F1}%)\n" +
                                   $"Tasks: {completedTasks}/{totalTasks} ({taskProgress:F1}%)\n\n" +
                                   $"Current Path:\n{_currentProjectPath}";
        }

        private void ClearOtherPanels()
        {
            // Clear tasks list
            TasksList.Items.Clear();
            TasksHeaderText.Text = "Tasks";

            // Clear details panel
            DetailsHeaderText.Text = "Task Details";
            DetailsContentPanel.Children.Clear();

            var placeholder = new TextBlock
            {
                Text = "Select a phase to view its tasks",
                FontSize = 13,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 102, 102)),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(0, 50, 0, 0)
            };
            DetailsContentPanel.Children.Add(placeholder);
        }

        private void OnPhaseBorderClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Phase phase)
            {
                // Clear previous selection
                if (_selectedPhaseIndex >= 0 && _selectedPhaseIndex < _projectData!.Phases.Count)
                {
                    _projectData.Phases[_selectedPhaseIndex].IsSelected = false;
                }

                // Set new selection
                int phaseIndex = _projectData!.Phases.IndexOf(phase);
                if (phaseIndex >= 0)
                {
                    _selectedPhaseIndex = phaseIndex;
                    phase.IsSelected = true;

                    // Populate tasks for the selected phase
                    PopulateTasksForPhase(phase);
                }
            }
        }

        private void PopulateTasksForPhase(Phase phase)
        {
            // Clear existing tasks
            TasksList.Items.Clear();

            // Update tasks header
            TasksHeaderText.Text = $"Tasks - {phase.DisplayPhaseName}";

            // Reset task selection
            _selectedTaskIndex = -1;

            // Add tasks to the list with numbering
            for (int taskIndex = 0; taskIndex < phase.Tasks.Count; taskIndex++)
            {
                var task = phase.Tasks[taskIndex];
                var taskWrapper = new TaskWrapper(task, $"Task {taskIndex}: {task.TaskName}");
                TasksList.Items.Add(taskWrapper);
            }

            // Clear details panel
            DetailsHeaderText.Text = "Task Details";
            DetailsContentPanel.Children.Clear();

            var placeholder = new TextBlock
            {
                Text = $"Tasks for {phase.PhaseName} are displayed in Panel 2",
                FontSize = 13,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 102, 102)),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(0, 50, 0, 0)
            };
            DetailsContentPanel.Children.Add(placeholder);
        }

        private void OnPhaseCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.DataContext is Phase phase)
            {
                // Update the phase completion status
                phase.IsCompleted = checkBox.IsChecked ?? false;

                // Save changes to JSON file
                SaveProjectData();

                // Update project statistics
                UpdateProjectStatistics();
            }
        }

        private void OnTaskCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.DataContext is TaskWrapper taskWrapper)
            {
                // Update the task completion status
                taskWrapper.Task.IsCompleted = checkBox.IsChecked ?? false;
                taskWrapper.IsCompleted = checkBox.IsChecked ?? false;

                // Save changes to JSON file
                SaveProjectData();

                // Update project statistics
                UpdateProjectStatistics();
            }
        }

        private void OnTaskBorderClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is TaskWrapper taskWrapper)
            {
                // Get the current selected phase
                if (_selectedPhaseIndex >= 0 && _selectedPhaseIndex < _projectData!.Phases.Count)
                {
                    var selectedPhase = _projectData.Phases[_selectedPhaseIndex];

                    // Clear previous task selection in UI
                    foreach (TaskWrapper wrapper in TasksList.Items)
                    {
                        wrapper.IsSelected = false;
                    }

                    // Set new task selection
                    taskWrapper.IsSelected = true;
                    int taskIndex = selectedPhase.Tasks.IndexOf(taskWrapper.Task);
                    if (taskIndex >= 0)
                    {
                        _selectedTaskIndex = taskIndex;
                    }

                    // Display task details in Panel 3
                    DisplayTaskDetails(taskWrapper.Task);
                }
            }
        }

        private void DisplayTaskDetails(SoftArchitecture.Models.Task task)
        {
            // Update details header
            DetailsHeaderText.Text = $"Task Details - {task.TaskName}";

            // Clear existing content
            DetailsContentPanel.Children.Clear();

            // Create scrollable content
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var contentPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 20)
            };

            // Task Name
            var taskNameHeader = new TextBlock
            {
                Text = "Task Name",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                Margin = new Thickness(0, 0, 0, 5)
            };
            contentPanel.Children.Add(taskNameHeader);

            var taskNameText = new TextBlock
            {
                Text = task.TaskName,
                FontSize = 13,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };
            contentPanel.Children.Add(taskNameText);

            // Goal
            var goalHeader = new TextBlock
            {
                Text = "Goal",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                Margin = new Thickness(0, 0, 0, 5)
            };
            contentPanel.Children.Add(goalHeader);

            var goalText = new TextBlock
            {
                Text = task.Goal,
                FontSize = 13,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };
            contentPanel.Children.Add(goalText);

            // Status
            var statusHeader = new TextBlock
            {
                Text = "Status",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                Margin = new Thickness(0, 0, 0, 5)
            };
            contentPanel.Children.Add(statusHeader);

            var statusText = new TextBlock
            {
                Text = task.IsCompleted ? "Completed" : "In Progress",
                FontSize = 13,
                Foreground = new SolidColorBrush(task.IsCompleted ?
                    System.Windows.Media.Color.FromRgb(76, 175, 80) :
                    System.Windows.Media.Color.FromRgb(255, 152, 0)),
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 15)
            };
            contentPanel.Children.Add(statusText);

            // Files to Edit
            if (task.FilesToEdit != null && task.FilesToEdit.Count > 0)
            {
                var filesToEditHeader = new TextBlock
                {
                    Text = "Files to Edit",
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                    Margin = new Thickness(0, 0, 0, 5)
                };
                contentPanel.Children.Add(filesToEditHeader);

                foreach (var file in task.FilesToEdit)
                {
                    var filePanel = new StackPanel
                    {
                        Margin = new Thickness(10, 0, 0, 8)
                    };

                    var fileName = new TextBlock
                    {
                        Text = file.Key,
                        FontSize = 12,
                        FontWeight = FontWeights.Medium,
                        Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(63, 81, 181)),
                        TextWrapping = TextWrapping.Wrap
                    };
                    filePanel.Children.Add(fileName);

                    if (!string.IsNullOrEmpty(file.Value))
                    {
                        var fileDescription = new TextBlock
                        {
                            Text = file.Value,
                            FontSize = 11,
                            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 102, 102)),
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 2, 0, 0)
                        };
                        filePanel.Children.Add(fileDescription);
                    }

                    contentPanel.Children.Add(filePanel);
                }

                contentPanel.Children.Add(new TextBlock { Margin = new Thickness(0, 0, 0, 15) });
            }

            // Files to Create
            if (task.FilesToCreate != null && task.FilesToCreate.Count > 0)
            {
                var filesToCreateHeader = new TextBlock
                {
                    Text = "Files to Create",
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                    Margin = new Thickness(0, 0, 0, 5)
                };
                contentPanel.Children.Add(filesToCreateHeader);

                foreach (var file in task.FilesToCreate)
                {
                    var filePanel = new StackPanel
                    {
                        Margin = new Thickness(10, 0, 0, 8)
                    };

                    var fileName = new TextBlock
                    {
                        Text = file.Key,
                        FontSize = 12,
                        FontWeight = FontWeights.Medium,
                        Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 150, 136)),
                        TextWrapping = TextWrapping.Wrap
                    };
                    filePanel.Children.Add(fileName);

                    if (!string.IsNullOrEmpty(file.Value))
                    {
                        var fileDescription = new TextBlock
                        {
                            Text = file.Value,
                            FontSize = 11,
                            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 102, 102)),
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 2, 0, 0)
                        };
                        filePanel.Children.Add(fileDescription);
                    }

                    contentPanel.Children.Add(filePanel);
                }

                contentPanel.Children.Add(new TextBlock { Margin = new Thickness(0, 0, 0, 15) });
            }

            // Additional Notes
            if (!string.IsNullOrEmpty(task.AdditionalNotes))
            {
                var notesHeader = new TextBlock
                {
                    Text = "Additional Notes",
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                    Margin = new Thickness(0, 0, 0, 5)
                };
                contentPanel.Children.Add(notesHeader);

                var notesText = new TextBlock
                {
                    Text = task.AdditionalNotes,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                contentPanel.Children.Add(notesText);
            }

            scrollViewer.Content = contentPanel;
            DetailsContentPanel.Children.Add(scrollViewer);
        }

        private void SaveProjectData()
        {
            if (_projectData == null || string.IsNullOrEmpty(_currentProjectPath)) return;

            try
            {
                // Construct the full path to ToDo.json file
                string jsonPath = IOPath.Combine(_currentProjectPath, "ToDo.json");

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string jsonContent = System.Text.Json.JsonSerializer.Serialize(_projectData, options);
                File.WriteAllText(jsonPath, jsonContent);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving project data: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}