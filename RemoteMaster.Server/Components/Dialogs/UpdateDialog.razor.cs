// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Models;
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
        var updateTasks = new List<Task>();

        foreach (var (computer, connection) in Hosts)
        {
            var updateTask = Task.Run(async () =>
            {
                var accessToken = HttpContextAccessor.HttpContext?.Request.Cookies["accessToken"];

                var updaterConnection = new HubConnectionBuilder()
                .WithUrl($"https://{computer.IpAddress}:6001/hubs/updater", options =>
                {
                    options.Headers.Add("Authorization", $"Bearer {accessToken}");
                })
                .AddMessagePackProtocol()
                .Build();

                var updateRequest = new UpdateRequest(_folderPath)
                {
                    UserCredentials = new Credentials()
                    {
                        Username = _username,
                        Password = _password
                    },
                    ForceUpdate = _forceUpdate,
                    AllowDowngrade = _allowDowngrade
                };

                await connection.InvokeAsync("SendStartUpdater", updateRequest);

                if (!_subscribedConnections.Contains(connection))
                {
                    connection.On<ScriptResult>("ReceiveScriptResult", async scriptResult =>
                    {
                        UpdateResultsForComputer(computer, scriptResult);
                        await InvokeAsync(StateHasChanged);
                    });

                    _subscribedConnections.Add(connection);
                }

                await updaterConnection.StartAsync();

                if (!_subscribedConnections.Contains(updaterConnection))
                {
                    updaterConnection.On<ScriptResult>("ReceiveScriptResult", async scriptResult =>
                    {
                        UpdateResultsForComputer(computer, scriptResult);
                        await InvokeAsync(StateHasChanged);
                    });

                    _subscribedConnections.Add(updaterConnection);
                }
            });

            updateTasks.Add(updateTask);
        }

        await Task.WhenAll(updateTasks);
    }

    private void UpdateResultsForComputer(Computer computer, ScriptResult scriptResult)
    {
        if (!_resultsPerComputer.TryGetValue(computer, out var results))
        {
            results = new ComputerResults();
            _resultsPerComputer[computer] = results;
        }

        if (scriptResult.Meta == "pid")
        {
            results.LastPid = int.Parse(scriptResult.Message);
        }
        else
        {
            results.Messages.AppendLine(scriptResult.Message);
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
}
