// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Windows.Input;

namespace RemoteMaster.Host.Chat.Avalonia.Commands;

public class RelayCommand<T>(Action<T> execute, Func<T, bool>? canExecute = null) : ICommand
{
    public bool CanExecute(object? parameter) => canExecute == null || canExecute((T)parameter);

    public void Execute(object? parameter) => execute((T)parameter);

    public event EventHandler CanExecuteChanged;

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
