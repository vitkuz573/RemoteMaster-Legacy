// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using static RemoteMaster.Shared.Models.Message;

namespace RemoteMaster.Shared.Models;

public class Message(string text, MessageType type)
{
    public string Text { get; } = text;

    public MessageType Type { get; } = type;

    public string? Meta { get; init; }

    public enum MessageType
    {
        Information,
        Error,
        Warning,
        Service
    }
}
