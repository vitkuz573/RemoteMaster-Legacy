// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using MudBlazor;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Extensions;

public static class MessageSeverityExtensions
{
    public static Color GetColor(this Message.MessageSeverity severity)
    {
        return severity switch
        {
            Message.MessageSeverity.Error => Color.Error,
            Message.MessageSeverity.Warning => Color.Warning,
            Message.MessageSeverity.Information => Color.Default,
            _ => Color.Default
        };
    }
}
