// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.ParameterHandlers;

public class BooleanParameterHandler : BaseParameterHandler<bool>
{
    protected override void SetValue(ILaunchParameter<bool> parameter, object? value)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        if (value is bool boolValue)
        {
            parameter.SetValue(boolValue);
        }
        else
        {
            parameter.SetValue(false);
        }
    }
}
