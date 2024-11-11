// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.ParameterHandlers;

public class StringParameterHandler : IParameterHandler
{
    public bool CanHandle(ILaunchParameter parameter)
    {
        return parameter == null ? throw new ArgumentNullException(nameof(parameter)) : parameter is ILaunchParameter<string>;
    }

    public void Handle(string[] args, ILaunchParameter parameter, string name)
    {
        if (parameter == null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Parameter name cannot be null, empty, or whitespace.", nameof(name));
        }

        if (parameter is not ILaunchParameter<string> stringParam)
        {
            return;
        }

        var value = parameter.GetValue(args);

        if (value != null)
        {
            stringParam.SetValue((string)value);
        }
        else if (parameter.IsRequired)
        {
            throw new ArgumentException($"Required parameter '{name}' is missing.");
        }
    }
}