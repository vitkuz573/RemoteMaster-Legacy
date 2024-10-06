// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Aggregates.TelegramBotAggregate;
using Telegram.Bot;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class ManageTelegramBot
{
    private InputModel Input { get; set; } = new();

    private readonly Dictionary<int, string> _userNames = [];
    private TelegramBot? _botSettings;
    private string? _newChatId;
    private string? _message;

    protected async override Task OnInitializedAsync()
    {
        _botSettings = await TelegramBotService.GetBotSettingsAsync();

        if (_botSettings != null)
        {
            Input = new InputModel
            {
                Id = _botSettings.Id,
                IsEnabled = _botSettings.IsEnabled,
                BotToken = _botSettings.BotToken,
                ChatIds = _botSettings.ChatIds.Select(c => c.ChatId).ToList()
            };
        }

        foreach (var chatId in Input.ChatIds)
        {
            var userName = await ResolveChatIdToUserNameAsync(chatId);

            _userNames[chatId] = userName ?? $"Unknown user ({chatId})";
        }
    }

    private async Task<string?> ResolveChatIdToUserNameAsync(int chatId)
    {
        if (_botSettings != null)
        {
            var botClient = new TelegramBotClient(_botSettings.BotToken);
            var chat = await botClient.GetChatAsync(chatId);
            var userName = $"{chat.FirstName} {chat.LastName}";

            return userName;
        }

        return string.Empty;
    }

    private async Task AddChatId()
    {
        if (string.IsNullOrEmpty(_newChatId))
        {
            return;
        }

        if (int.TryParse(_newChatId, out var chatId))
        {
            if (_botSettings != null)
            {
                await TelegramBotService.AddNewChatIdAsync(_botSettings.Id, chatId);
            }

            Input.ChatIds.Add(chatId);

            var userName = await ResolveChatIdToUserNameAsync(chatId);
            _userNames[chatId] = userName ?? $"Unknown user ({chatId})";

            _newChatId = string.Empty;

            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task RemoveChatId(int chatId)
    {
        if (_botSettings != null)
        {
            await TelegramBotService.RemoveChatIdAsync(_botSettings.Id, chatId);
        }

        Input.ChatIds.Remove(chatId);

        await InvokeAsync(StateHasChanged);
    }

    private async Task SaveSettings()
    {
        if (_botSettings != null)
        {
            _botSettings.UpdateSettings(Input.IsEnabled, Input.BotToken);

            foreach (var chatId in Input.ChatIds.Where(chatId => _botSettings.ChatIds.All(c => c.ChatId != chatId)))
            {
                _botSettings.AddChatId(chatId);
            }

            var chatIdsToRemove = _botSettings.ChatIds
                .Where(c => !Input.ChatIds.Contains(c.ChatId))
                .Select(c => c.ChatId)
                .ToList();

            foreach (var chatId in chatIdsToRemove)
            {
                _botSettings.RemoveChatId(chatId);
            }

            await TelegramBotService.UpdateBotSettingsAsync(_botSettings);

            (Configuration as IConfigurationRoot)?.Reload();

            _message = "Settings saved successfully.";
        }
    }

    private async Task TestNotification()
    {
        await EventNotificationService.SendNotificationAsync("Test notification");
    }

    public class InputModel
    {
        public int Id { get; set; }

        public bool IsEnabled { get; set; }

        public string BotToken { get; set; } = string.Empty;

#pragma warning disable CA2227
        public List<int> ChatIds { get; set; } = [];
#pragma warning restore CA2227
    }
}
