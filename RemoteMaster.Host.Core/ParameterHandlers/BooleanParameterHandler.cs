// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.ParameterHandlers;

public class BooleanParameterHandler : IParameterHandler
{
    public bool CanHandle(ILaunchParameter parameter) => parameter is ILaunchParameter<bool>;

    public void Handle(string[] args, ILaunchParameter parameter, string name)
    {
        if (parameter is ILaunchParameter<bool> boolParam)
        {
            var isPresent = args.Any(arg => arg.Equals($"--{name}", StringComparison.OrdinalIgnoreCase));

            boolParam.SetValue(isPresent.ToString().ToLower());
        }
    }
}
