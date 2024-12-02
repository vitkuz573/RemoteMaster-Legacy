// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.ParameterSerializers;

public class StringParameterSerializer : BaseParameterSerializer<string>
{
    protected override object? ExtractValue(string[] args, string name, bool isRequired)
    {
        var argument = args.FirstOrDefault(arg => arg.StartsWith($"--{name}=", StringComparison.OrdinalIgnoreCase));

        if (argument != null)
        {
            return argument[(argument.IndexOf('=') + 1)..];
        }

        if (isRequired)
        {
            throw new ArgumentException($"Required parameter '{name}' is missing.");
        }

        return null;
    }

    protected override void SetValue(ILaunchParameter<string> parameter, object? value)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        if (value is string str && !string.IsNullOrWhiteSpace(str))
        {
            parameter.SetValue(str);
        }
        else
        {
            parameter.SetValue(null);
        }
    }

    protected override string? GetSerializedValue(ILaunchParameter<string> parameter, string name)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        return !string.IsNullOrWhiteSpace(parameter.Value) ? $"--{name}={parameter.Value}" : null;
    }
}
