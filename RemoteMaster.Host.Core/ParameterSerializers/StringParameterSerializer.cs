// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.ParameterSerializers;

public class StringParameterSerializer : BaseParameterSerializer<string>
{
    protected override void SetValue(ILaunchParameter<string> parameter, string? value)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        if (string.IsNullOrWhiteSpace(value) && parameter.IsRequired)
        {
            throw new ArgumentException($"Required parameter '{parameter.Name}' is missing or empty.");
        }

        parameter.SetValue(value);
    }

    protected override string? GetSerializedValue(ILaunchParameter<string> parameter, string name)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        return !string.IsNullOrWhiteSpace(parameter.Value) ? $"--{name}={parameter.Value}" : null;
    }
}
