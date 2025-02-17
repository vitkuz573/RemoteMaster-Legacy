// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RemoteMaster.Server.Options;

public class TelegramBotOptions : IValidatableObject
{
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }

    [JsonPropertyName("botToken")]
    public string BotToken { get; set; } = string.Empty;

    [JsonPropertyName("chatIds")]
    public List<int> ChatIds { get; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsEnabled)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(BotToken))
        {
            yield return new ValidationResult("BotToken is required when Telegram bot is enabled.", [nameof(BotToken)]);
        }

        if (ChatIds.Count == 0)
        {
            yield return new ValidationResult("At least one chat ID is required when Telegram bot is enabled.", [nameof(ChatIds)]);
        }
    }
}
