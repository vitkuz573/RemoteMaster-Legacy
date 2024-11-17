// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.ParameterHandlers;

public abstract class BaseParameterHandler<T> : IParameterHandler
{
    public bool CanHandle(ILaunchParameter parameter)
    {
        return parameter == null ? throw new ArgumentNullException(nameof(parameter)) : parameter is ILaunchParameter<T>;
    }

    public void Handle(string[] args, ILaunchParameter parameter, string name)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(parameter);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Parameter name cannot be null, empty, or whitespace.", nameof(name));
        }

        if (parameter is not ILaunchParameter<T> typedParameter)
        {
            return;
        }

        var value = parameter.GetValue(args);

        SetValue(typedParameter, value);
    }

    protected abstract void SetValue(ILaunchParameter<T> parameter, object? value);
}
