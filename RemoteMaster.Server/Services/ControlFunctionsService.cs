// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class ControlFunctionsService
{
    public IControlHub ControlHubProxy { get; set; }

    public HubConnection AgentConnection { get; set; }

    public AgentConfigurationDto AgentConfiguration { get; set; }

    public ClientConfigurationDto ClientConfiguration { get; set; }

    public IEnumerable<DisplayInfo> Displays { get; set; }
}
