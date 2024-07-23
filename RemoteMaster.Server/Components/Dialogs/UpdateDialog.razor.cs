// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Compression;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class UpdateDialog
{
    private bool _isShowPassword;
    private InputType _passwordInput = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

    private string _folderPath = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _forceUpdate = false;
    private bool _allowDowngrade = false;

    private readonly Dictionary<Computer, ComputerResults> _resultsPerComputer = [];
    private readonly HashSet<HubConnection> _subscribedConnections = [];

    protected override void OnInitialized()
    {
        _folderPath = ApplicationSettings.Value.ExecutablesRoot;
    }

    private async Task Confirm()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        var userId = UserManager.GetUserId(httpContext.User);

        var updateTasks = new List<Task>();

        foreach (var (computer, connection) in Hosts)
        {
            var updateTask = Task.Run(async () =>
            {
                var updaterConnection = new HubConnectionBuilder()
                .WithUrl($"https://{computer.IpAddress}:6001/hubs/updater", options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        var accessTokenResult = await AccessTokenProvider.GetAccessTokenAsync(userId);
                        return accessTokenResult.IsSuccess ? accessTokenResult.Value : null;
                    };
                })
                .AddMessagePackProtocol()
                .Build();

                var updateRequest = new UpdateRequest(_folderPath)
                {
                    UserCredentials = new Credentials(_username, _password),
                    ForceUpdate = _forceUpdate,
                    AllowDowngrade = _allowDowngrade
                };

                await connection.InvokeAsync("SendStartUpdater", updateRequest);

                if (!_subscribedConnections.Contains(connection))
                {
                    connection.On<Message>("ReceiveMessage", async message =>
                    {
                        UpdateResultsForComputer(computer, message);
                        await InvokeAsync(StateHasChanged);
                    });

                    _subscribedConnections.Add(connection);
                }

                await updaterConnection.StartAsync();

                if (!_subscribedConnections.Contains(updaterConnection))
                {
                    updaterConnection.On<Message>("ReceiveMessage", async message =>
                    {
                        UpdateResultsForComputer(computer, message);
                        await InvokeAsync(StateHasChanged);
                    });

                    _subscribedConnections.Add(updaterConnection);
                }
            });

            updateTasks.Add(updateTask);
        }

        await Task.WhenAll(updateTasks);
    }

    private void UpdateResultsForComputer(Computer computer, Message message)
    {
        if (!_resultsPerComputer.TryGetValue(computer, out var results))
        {
            results = new ComputerResults();
            _resultsPerComputer[computer] = results;
        }

        if (message.Meta == "pid")
        {
            results.LastPid = int.Parse(message.Text);
        }
        else
        {
            results.Messages.AppendLine(message.Text);
        }
    }

    private void TogglePasswordVisibility()
    {
        if (_isShowPassword)
        {
            _isShowPassword = false;
            _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
            _passwordInput = InputType.Password;
        }
        else
        {
            _isShowPassword = true;
            _passwordInputIcon = Icons.Material.Filled.Visibility;
            _passwordInput = InputType.Text;
        }
    }

    private void CleanResults()
    {
        _resultsPerComputer.Clear();
    }

    private async Task ExportResults()
    {
        var zipMemoryStream = new MemoryStream();

        using (var archive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var (computer, results) in _resultsPerComputer)
            {
                var fileName = $"results_{computer.Name}_{computer.IpAddress}.txt";
                var fileContent = results.ToString();

                var zipEntry = archive.CreateEntry(fileName, CompressionLevel.Fastest);
                await using var entryStream = zipEntry.Open();
                await using var streamWriter = new StreamWriter(entryStream);
                await streamWriter.WriteAsync(fileContent);
            }
        }

        zipMemoryStream.Position = 0;
        var base64Zip = Convert.ToBase64String(zipMemoryStream.ToArray());

        var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/fileUtils.js");

        await module.InvokeVoidAsync("downloadDataAsFile", base64Zip, "RemoteMaster_Results.zip", "application/zip;base64");
    }
}
