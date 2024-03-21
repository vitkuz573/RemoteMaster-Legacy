// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class ScriptResult
{
    public string Message { get; init; }

    public MessageType Type { get; set; }

    public string? Meta { get; init; }

    public enum MessageType
    {
        Information,
        Error,
        Warning,
        Service
    }
}
