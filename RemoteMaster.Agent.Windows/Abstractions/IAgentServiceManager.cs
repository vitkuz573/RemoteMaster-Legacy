// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Agent.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Agent.Abstractions;

public interface IAgentServiceManager
{
    event Action<string, MessageType> MessageReceived;

    Task<bool> InstallOrUpdateService(ConfigurationModel configuration, string hostName, string ipv4Address);
    
    Task<bool> UninstallService(ConfigurationModel configuration, string hostName);
}
