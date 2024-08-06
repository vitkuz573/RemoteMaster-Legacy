// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.DTOs;

public class ChatMessageDto
{
    public string? Id { get; set; }

    public string User { get; set; }

    public string Message { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public string? ReplyToId { get; set; }

#pragma warning disable CA2227
    public List<AttachmentDto>? Attachments { get; set; }
#pragma warning restore CA2227
}
