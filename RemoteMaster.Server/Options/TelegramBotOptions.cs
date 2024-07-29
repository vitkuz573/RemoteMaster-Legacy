// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;

namespace RemoteMaster.Server.Options;

public class TelegramBotOptions
{
    [JsonPropertyName("botToken")]
    public string BotToken { get; set; }

#pragma warning disable CA2227
    [JsonPropertyName("chatIds")]
    public List<string> ChatIds { get; set; }
#pragma warning restore CA2227
}
