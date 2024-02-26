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

    private readonly Dictionary<Computer, ComputerResults> _resultsPerComputer = [];
    private readonly HashSet<HubConnection> _subscribedConnections = [];

    protected override void OnInitialized()
    {
        _folderPath = ApplicationSettings.Value.ExecutablesRoot;
    }

    private async Task Confirm()
    {
        foreach (var (computer, connection) in Hosts)
        {
            await connection.InvokeAsync("SendStartUpdater", _folderPath, _username, _password);

            var updaterHubConnection = new HubConnectionBuilder()
                .WithUrl($"http://{computer.IpAddress}:5200/hubs/updater")
                .AddMessagePackProtocol()
                .Build();

            Thread.Sleep(3000);

            await updaterHubConnection.StartAsync();

            if (_subscribedConnections.Contains(connection))
            {
                continue;
            }

            updaterHubConnection.On<ScriptResult>("ReceiveScriptResult", async scriptResult =>
            {
                UpdateResultsForComputer(computer, scriptResult);
                await InvokeAsync(StateHasChanged);
            });

            _subscribedConnections.Add(connection);
        }
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
