using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BambooLauncher
{
    public class RelayCommand : ICommand
    {
        private readonly Predicate<object> _CanExecute;
        private readonly Action<object> _Execute;

        public RelayCommand(Predicate<object> canExecute, Action<object> execute)
        {
            this._CanExecute = canExecute;
            this._Execute = execute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return _CanExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _Execute(parameter);
        }
    }
}
