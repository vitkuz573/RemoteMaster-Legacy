// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using FluentResults;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Interface for executing commands on computers.
/// </summary>
public interface IComputerCommandService
{
    /// <summary>
    /// Executes the specified action on each computer in the provided dictionary.
    /// </summary>
    /// <param name="computers">A dictionary of computers and their corresponding SignalR connections.</param>
    /// <param name="actionOnComputer">The action to execute on each computer.</param>
    /// <returns>A result indicating the success or failure of the operation.</returns>
    Task<Result> Execute(ConcurrentDictionary<Computer, HubConnection?> computers, Func<Computer, HubConnection, Task> actionOnComputer);
}
