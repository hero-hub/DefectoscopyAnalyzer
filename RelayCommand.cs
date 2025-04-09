using System;
using System.Windows.Input;

namespace DefectoscopyAnalyzer
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object>? _executeWithParameter;
        private readonly Action? _execute;
        private readonly Func<bool>? _canExecute;

        // Конструктор для методов с параметром (Action<object>)
        public RelayCommand(Action<object> execute, Func<bool>? canExecute = null)
        {
            _executeWithParameter = execute ?? throw new ArgumentNullException(nameof(execute));
            _execute = null;
            _canExecute = canExecute;
        }

        // Конструктор для методов без параметров (Action)
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _executeWithParameter = null;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();
        public void Execute(object? parameter)
        {
            if (_executeWithParameter != null)
                _executeWithParameter(parameter);
            else
                _execute?.Invoke();
        }
    }
}