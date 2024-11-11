// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.ParameterHandlers;

public class StringParameterHandler : BaseParameterHandler<string>
{
    protected override void SetValue(ILaunchParameter<string> parameter, object? value)
    {
        if (value != null)
        {
            parameter.SetValue(value.ToString());
        }
    }
}
