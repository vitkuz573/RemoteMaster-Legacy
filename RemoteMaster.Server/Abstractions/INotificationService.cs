// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Abstractions;

public interface INotificationService
{
    Task<bool> AreNewNotificationsAvailable();

    Task MarkNotificationsAsRead();

    Task MarkNotificationsAsRead(string id);

    Task<NotificationMessage> GetMessageById(string id);

    Task<IDictionary<NotificationMessage, bool>> GetNotifications();

    Task AddNotification(NotificationMessage message);
}