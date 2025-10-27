using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Spreadsheet.Core
{
    public class Cell : INotifyPropertyChanged
    {
        private string _expression = "";
        private double _value = double.NaN;
        private string _name = "";

        public string Expression
        {
            get => _expression;
            set => SetProperty(ref _expression, value);
        }

        public double Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}