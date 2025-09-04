using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SoftArchitecture.Models
{
    public class FirebaseDatabaseData
    {
        public Dictionary<string, Collection> Collections { get; set; } = new Dictionary<string, Collection>();
    }

    public class Collection : INotifyPropertyChanged
    {
        private string _collectionDescription = string.Empty;
        private string _collectionAdditionalNote = string.Empty;
        private Dictionary<string, Field> _fields = new Dictionary<string, Field>();

        public string CollectionDescription
        {
            get => _collectionDescription;
            set => SetProperty(ref _collectionDescription, value);
        }

        public string CollectionAdditionalNote
        {
            get => _collectionAdditionalNote;
            set => SetProperty(ref _collectionAdditionalNote, value);
        }

        public Dictionary<string, Field> Fields
        {
            get => _fields;
            set => SetProperty(ref _fields, value);
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

    public class Field : INotifyPropertyChanged
    {
        private string _fieldDescription = string.Empty;
        private string _additionalNote = string.Empty;

        public string FieldDescription
        {
            get => _fieldDescription;
            set => SetProperty(ref _fieldDescription, value);
        }

        public string AdditionalNote
        {
            get => _additionalNote;
            set => SetProperty(ref _additionalNote, value);
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
