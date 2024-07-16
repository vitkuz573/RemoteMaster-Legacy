// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class InMemoryNotificationService : INotificationService
{
    private readonly ConcurrentDictionary<string, NotificationMessage> _notifications = new();
    private readonly ConcurrentDictionary<string, bool> _readStatus = new();

    public Task<bool> AreNewNotificationsAvailable()
    {
        return Task.FromResult(_notifications.Any(kv => !_readStatus.ContainsKey(kv.Key) || !_readStatus[kv.Key]));
    }

    public Task MarkNotificationsAsRead()
    {
        foreach (var key in _notifications.Keys)
        {
            _readStatus[key] = true;
        }

        return Task.CompletedTask;
    }

    public Task MarkNotificationsAsRead(string id)
    {
        _readStatus[id] = true;
        
        return Task.CompletedTask;
    }

    public Task<NotificationMessage> GetMessageById(string id)
    {
        _notifications.TryGetValue(id, out var message);

        return Task.FromResult(message);
    }

    public Task<IDictionary<NotificationMessage, bool>> GetNotifications()
    {
        var result = _notifications.ToDictionary(kv => kv.Value, kv => _readStatus.ContainsKey(kv.Key) && _readStatus[kv.Key]);
        
        return Task.FromResult<IDictionary<NotificationMessage, bool>>(result);
    }

    public Task AddNotification(NotificationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _notifications[message.Id] = message;
        
        return Task.CompletedTask;
    }
}