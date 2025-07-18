﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using FluentResults;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Interface for executing commands on hosts.
/// </summary>
public interface IHostCommandService
{
    /// <summary>
    /// Executes the specified action on each host in the provided dictionary.
    /// </summary>
    /// <param name="hosts">A dictionary of hosts and their corresponding SignalR connections.</param>
    /// <param name="action">The action to execute on each host.</param>
    /// <returns>A result indicating the success or failure of the operation.</returns>
    Task<Result> ExecuteAsync(ConcurrentDictionary<HostDto, HubConnection?> hosts, Func<HostDto, HubConnection?, Task> action);
}
