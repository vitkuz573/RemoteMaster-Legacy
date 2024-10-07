// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class ChatMessage(string id, string user, string message, DateTimeOffset timestamp, List<Attachment> attachments, string? replyToId = null)
{
    public string Id { get; } = id;

    public string User { get; } = user;

    public string Message { get; } = message;

    public DateTimeOffset Timestamp { get; } = timestamp;

    public List<Attachment> Attachments { get; } = attachments;

    public string? ReplyToId { get; } = replyToId;
}
