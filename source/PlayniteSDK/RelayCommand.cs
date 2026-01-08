using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace Playnite.SDK
{
    /// <summary>
    ///
    /// </summary>
    public abstract class RelayCommandBase : ICommand
    {
        /// <summary>
        ///
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { canExecuteChanged += value; }
            remove { canExecuteChanged -= value; }
        }

        private event EventHandler canExecuteChanged;

        /// <summary>
        /// Raises <see cref="CanExecuteChanged"/> to refresh command state.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public abstract bool CanExecute(object parameter);

        /// <summary>
        ///
        /// </summary>
        /// <param name="parameter"></param>
        public abstract void Execute(object parameter);
    }

    /// <summary>
    ///
    /// </summary>
    public class RelayCommand : RelayCommandBase
    {
        private readonly Func<bool> canExecute;
        private readonly Action execute;

        /// <summary>
        ///
        /// </summary>
        /// <param name="execute"></param>
        public RelayCommand(Action execute)
            : this(execute, null)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="execute"></param>
        /// <summary>
        ///
        /// </summary>
        /// <param name="execute"></param>
        /// <param name="canExecute"></param>
        public RelayCommand(Action execute, Func<bool> canExecute)
            : this(execute, canExecute, null)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="execute"></param>
        /// <param name="canExecute"></param>
        /// <param name="gestureText">Optional gesture hint for UI display.</param>
        public RelayCommand(Action execute, Func<bool> canExecute, string gestureText)
        {
            this.execute = execute;
            this.canExecute = canExecute;
            GestureText = gestureText;
        }

        /// <summary>
        /// Optional gesture hint for UI display.
        /// </summary>
        public string GestureText { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public override bool CanExecute(object parameter = null)
        {
            if (canExecute == null)
            {
                return true;
            }

            return canExecute();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="parameter"></param>
        public override void Execute(object parameter = null)
        {
            execute();
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RelayCommand<T> : RelayCommandBase
    {
        private readonly Predicate<T> canExecute;
        private readonly Action<T> execute;

        /// <summary>
        ///
        /// </summary>
        /// <param name="execute"></param>
        public RelayCommand(Action<T> execute)
            : this(execute, null)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="execute"></param>
        /// <summary>
        ///
        /// </summary>
        /// <param name="execute"></param>
        /// <param name="canExecute"></param>
        public RelayCommand(Action<T> execute, Predicate<T> canExecute)
            : this(execute, canExecute, null)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="execute"></param>
        /// <param name="canExecute"></param>
        /// <param name="gestureText">Optional gesture hint for UI display.</param>
        public RelayCommand(Action<T> execute, Predicate<T> canExecute, string gestureText)
        {
            this.execute = execute;
            this.canExecute = canExecute;
            GestureText = gestureText;
        }

        /// <summary>
        /// Optional gesture hint for UI display.
        /// </summary>
        public string GestureText { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public override bool CanExecute(object parameter)
        {
            if (canExecute == null)
            {
                return true;
            }

            return canExecute((T)parameter);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="parameter"></param>
        public override void Execute(object parameter)
        {
            if (parameter is T param)
            {
                execute(param);
            }
            else
            {
                execute(default);
            }
        }
    }
}
