// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.ParameterSerializers;

public class BooleanParameterSerializer : BaseParameterSerializer<bool>
{
    protected override object? ExtractValue(string[] args, string name, bool isRequired)
    {
        return args.Any(arg => arg.Equals($"--{name}", StringComparison.OrdinalIgnoreCase));
    }

    protected override void SetValue(ILaunchParameter<bool> parameter, object? value)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        parameter.SetValue(value is true);
    }

    protected override string? GetSerializedValue(ILaunchParameter<bool> parameter, string name)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        return parameter.Value ? $"--{name}" : null;
    }
}
