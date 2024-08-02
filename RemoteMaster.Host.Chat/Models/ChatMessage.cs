// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Chat.Models;

public class ChatMessage
{
    public string Id { get; set; }

    public string User { get; set; }

    public string Message { get; set; }
}