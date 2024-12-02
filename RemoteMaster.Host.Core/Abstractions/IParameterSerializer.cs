// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface IParameterSerializer
{
    bool CanHandle(ILaunchParameter parameter);

    void Deserialize(string? value, ILaunchParameter parameter);

    string? Serialize(ILaunchParameter parameter, string name);
}
