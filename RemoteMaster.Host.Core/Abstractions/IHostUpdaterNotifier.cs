// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using static RemoteMaster.Shared.Models.Message;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IHostUpdaterNotifier
{
    /// <summary>
    /// Sends a notification with the specified message, severity, and optional metadata.
    /// </summary>
    /// <param name="message">The content of the notification.</param>
    /// <param name="severity">The severity level of the notification.</param>
    /// <param name="meta">Optional metadata associated with the notification.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyAsync(string message, MessageSeverity severity, string? meta = null);
}
