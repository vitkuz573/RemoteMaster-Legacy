// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.ParameterHandlers;

public class StringParameterHandler : IParameterHandler
{
    public bool CanHandle(ILaunchParameter parameter) => parameter is ILaunchParameter<string>;

    public void Handle(string[] args, ILaunchParameter parameter, string name)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        var value = args
            .Where(arg => arg.StartsWith($"--{name}=", StringComparison.OrdinalIgnoreCase))
            .Select(arg => arg[(arg.IndexOf('=') + 1)..])
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(value))
        {
            parameter.SetValue(value);
        }
    }
}
