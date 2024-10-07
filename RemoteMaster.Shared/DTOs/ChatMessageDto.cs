// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.DTOs;

public class ChatMessageDto(string user, string message)
{
    public string? Id { get; set; }

    public string User { get; } = user;

    public string Message { get; } = message;

    public DateTimeOffset? Timestamp { get; set; }

    public string? ReplyToId { get; init; }

    public List<AttachmentDto> Attachments { get; } = [];
}
