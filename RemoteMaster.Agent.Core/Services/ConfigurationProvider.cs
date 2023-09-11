// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Reflection;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Agent.Core.Services;

public class ConfigurationProvider : IConfigurationProvider
{
    public AgentConfigurationDto Fetch()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        return new AgentConfigurationDto
        {
            AppVersion = version
        };
    }
}
