// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Agent.Abstractions;

public interface IClientService
{
    Task<bool> RegisterAsync(ConfigurationModel config, string hostName, string ipAddress, string macAddress);

    Task<bool> UnregisterAsync(ConfigurationModel config, string hostName);

    bool IsClientRunning();

    void StartClient();

    void StopClient();
}
