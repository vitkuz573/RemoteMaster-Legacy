// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Client.Services;

public class ControlFunctionsService
{
    public IControlHub ControlHubProxy { get; set; }

    public ServerConfigurationDto ServerConfiguration { get; set; }

    public IEnumerable<DisplayInfo> Displays { get; set; }
}
