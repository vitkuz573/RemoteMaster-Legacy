// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Defines the contract for sending event notifications.
/// </summary>
public interface IEventNotificationService
{
    /// <summary>
    /// Sends a notification asynchronously.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SendNotificationAsync(string message);
}
