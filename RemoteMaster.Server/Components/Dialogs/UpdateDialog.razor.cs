﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Compression;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Extensions;
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
    private uint _waitForClientConnectionTimeout = 0;
    private bool _forceUpdate;
    private bool _allowDowngrade;

    private readonly Dictionary<HostDto, HostResults> _resultsPerHost = [];
    private readonly HashSet<HubConnection> _subscribedConnections = [];

    private string? _accessToken;

    protected async override Task OnInitializedAsync()
    {
        _folderPath = UpdateOptions.Value.ExecutablesRoot;
        _username = UpdateOptions.Value.UserName;
        _password = UpdateOptions.Value.Password;
        _forceUpdate = UpdateOptions.Value.ForceUpdate;
        _allowDowngrade = UpdateOptions.Value.AllowDowngrade;

        var httpContext = HttpContextAccessor.HttpContext;
        var user = httpContext.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID is not found.");
        var accessTokenResult = await AccessTokenProvider.GetAccessTokenAsync(userId);

        _accessToken = accessTokenResult.IsSuccess ? accessTokenResult.Value : null;
    }

    private async Task Confirm()
    {
        var updateTasks = new List<Task>();

        foreach (var (host, connection) in Hosts)
        {
            var updateTask = Task.Run(async () =>
            {
                var updaterConnection = new HubConnectionBuilder()
                .WithUrl($"https://{host.IpAddress}:6001/hubs/updater", options =>
                {
                    options.AccessTokenProvider = async () => await Task.FromResult(_accessToken);
                })
                .AddMessagePackProtocol(options => options.Configure())
                .Build();

                if (!_subscribedConnections.Contains(updaterConnection))
                {
                    updaterConnection.On<Message>("ReceiveMessage", async message =>
                    {
                        UpdateResultsForHost(host, message);
                        await InvokeAsync(StateHasChanged);
                    });

                    _subscribedConnections.Add(updaterConnection);
                }

                var updateRequest = new UpdateRequest(_folderPath)
                {
                    UserCredentials = new Credentials(_username, _password),
                    WaitForClientConnectionTimeout = _waitForClientConnectionTimeout,
                    ForceUpdate = _forceUpdate,
                    AllowDowngrade = _allowDowngrade
                };

                await connection!.InvokeAsync("SendStartUpdater", updateRequest);

                if (!_subscribedConnections.Contains(connection!))
                {
                    connection!.On<Message>("ReceiveMessage", async message =>
                    {
                        if (message.Text.Contains("Updater instance on port"))
                        {
                            await updaterConnection.StartAsync();
                        }
                        else
                        {
                            UpdateResultsForHost(host, message);
                            await InvokeAsync(StateHasChanged);
                        }
                    });

                    _subscribedConnections.Add(connection!);
                }
            });

            updateTasks.Add(updateTask);
        }

        await Task.WhenAll(updateTasks);
    }

    private void UpdateResultsForHost(HostDto host, Message message)
    {
        if (!_resultsPerHost.TryGetValue(host, out var results))
        {
            results = new HostResults();
            _resultsPerHost[host] = results;
        }

        if (message.Meta == MessageMeta.ProcessIdInformation)
        {
            results.LastPid = int.Parse(message.Text);
        }
        else
        {
            results.Messages.Add(message);
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
        _resultsPerHost.Clear();
    }

    private async Task ExportResults()
    {
        var zipMemoryStream = new MemoryStream();

        using (var archive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var (host, results) in _resultsPerHost)
            {
                var fileName = $"results_{host.Name}_{host.IpAddress}.txt";
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
