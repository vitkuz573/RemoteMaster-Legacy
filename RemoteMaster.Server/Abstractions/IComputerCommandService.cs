// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using FluentResults;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Interface for executing commands on computers.
/// </summary>
public interface IComputerCommandService
{
    /// <summary>
    /// Executes the specified action on each computer in the provided dictionary.
    /// </summary>
    /// <param name="hosts">A dictionary of computers and their corresponding SignalR connections.</param>
    /// <param name="action">The action to execute on each computer.</param>
    /// <returns>A result indicating the success or failure of the operation.</returns>
    Task<Result> Execute(ConcurrentDictionary<ComputerDto, HubConnection?> hosts, Func<ComputerDto, HubConnection?, Task> action);
}
