// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.ParameterHandlers;

public class BooleanParameterHandler : IParameterHandler
{
    public bool CanHandle(ILaunchParameter parameter)
    {
        return parameter == null ? throw new ArgumentNullException(nameof(parameter)) : parameter is ILaunchParameter<bool>;
    }

    public void Handle(string[] args, ILaunchParameter parameter, string name)
    {
        if (parameter != null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Parameter name cannot be null, empty, or whitespace.", nameof(name));
            }

            if (parameter is not ILaunchParameter<bool> boolParam)
            {
                return;
            }

            var value = parameter.GetValue(args);

            if (value != null)
            {
                boolParam.SetValue((bool)value);
            }
            else
            {
                boolParam.SetValue(false);
            }
        }
        else
        {
            throw new ArgumentNullException(nameof(parameter));
        }
    }
}
