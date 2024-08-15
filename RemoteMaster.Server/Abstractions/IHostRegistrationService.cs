// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using FluentResults;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Defines the contract for host registration services.
/// </summary>
public interface IHostRegistrationService
{
    /// <summary>
    /// Registers a new host based on the provided configuration.
    /// </summary>
    /// <param name="hostConfiguration">The configuration of the host to register.</param>
    /// <returns>A <see cref="Result{T}"/> indicating the success or failure of the operation.</returns>
    Task<Result> RegisterHostAsync(HostConfiguration hostConfiguration);

    /// <summary>
    /// Checks if a host is registered based on its MAC address.
    /// </summary>
    /// <param name="macAddress">The MAC address of the host to check.</param>
    /// <returns>A <see cref="Result{T}"/> indicating the success or failure of the operation.</returns>
    Task<Result> IsHostRegisteredAsync(string macAddress);

    /// <summary>
    /// Unregisters a host based on the provided request.
    /// </summary>
    /// <param name="request">The request containing the details of the host to unregister.</param>
    /// <returns>A <see cref="Result{T}"/> indicating the success or failure of the operation.</returns>
    Task<Result> UnregisterHostAsync(HostUnregisterRequest request);

    /// <summary>
    /// Updates the information of an existing host based on the provided request.
    /// </summary>
    /// <param name="request">The request containing the updated details of the host.</param>
    /// <returns>A <see cref="Result{T}"/> indicating the success or failure of the operation.</returns>
    Task<Result> UpdateHostInformationAsync(HostUpdateRequest request);
}
