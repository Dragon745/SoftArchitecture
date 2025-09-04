using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SoftArchitecture.Models
{
    public class ProjectData
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectDescription { get; set; } = string.Empty;
        public string ProjectGoal { get; set; } = string.Empty;
        public ObservableCollection<Phase> Phases { get; set; } = new ObservableCollection<Phase>();
    }

    public class Phase : INotifyPropertyChanged
    {
        private string _phaseName = string.Empty;
        private string _goal = string.Empty;
        private bool _isCompleted = false;
        private bool _isSelected = false;
        private ObservableCollection<Task> _tasks = new ObservableCollection<Task>();
        private string _displayPhaseName = string.Empty;

        public string PhaseName
        {
            get => _phaseName;
            set => SetProperty(ref _phaseName, value);
        }

        public string DisplayPhaseName
        {
            get => _displayPhaseName;
            set => SetProperty(ref _displayPhaseName, value);
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

        public ObservableCollection<Task> Tasks
        {
            get => _tasks;
            set => SetProperty(ref _tasks, value);
        }

        public string TaskCountText
        {
            get
            {
                int completedTasks = Tasks.Count(t => t.IsCompleted);
                return $"{completedTasks}/{Tasks.Count} tasks completed";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
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

    public class Task : INotifyPropertyChanged
    {
        private string _taskName = string.Empty;
        private string _goal = string.Empty;
        private bool _isCompleted = false;
        private Dictionary<string, string> _filesToEdit = new Dictionary<string, string>();
        private Dictionary<string, string> _filesToCreate = new Dictionary<string, string>();
        private string _additionalNotes = string.Empty;

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

        public Dictionary<string, string> FilesToEdit
        {
            get => _filesToEdit;
            set => SetProperty(ref _filesToEdit, value);
        }

        public Dictionary<string, string> FilesToCreate
        {
            get => _filesToCreate;
            set => SetProperty(ref _filesToCreate, value);
        }

        public string AdditionalNotes
        {
            get => _additionalNotes;
            set => SetProperty(ref _additionalNotes, value);
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
}
