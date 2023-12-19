// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Abstractions;

public interface IComputerCommandService
{
    Task Execute(ConcurrentDictionary<Computer, HubConnection?> computers, Func<Computer, HubConnection, Task> actionOnComputer);
}
