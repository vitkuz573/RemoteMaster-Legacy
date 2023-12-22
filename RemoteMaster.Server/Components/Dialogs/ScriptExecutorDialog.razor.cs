// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class ScriptExecutorDialog
{
    [Inject]
    private IComputerCommandService ComputerCommandService { get; set; } = default!;

    private string _content;
    private Shell? _shell;
    private bool _asSystem;
    private readonly Dictionary<string, StringBuilder> _resultsPerComputer = [];
    private readonly HashSet<HubConnection> _subscribedConnections = [];

    private async Task RunScript()
    {
        foreach (var (computer, connection) in Hosts)
        {
            if (connection != null && !_subscribedConnections.Contains(connection))
            {
                connection.On<ScriptResult>("ReceiveScriptResult", scriptResult =>
                {
                    UpdateResultsForComputer(computer.IPAddress, scriptResult);
                    InvokeAsync(StateHasChanged);
                });

                _subscribedConnections.Add(connection);
            }

            if (connection != null)
            {
                await connection.InvokeAsync("SendScript", _content, _shell, _asSystem);
            }
        }
    }

    private void UpdateResultsForComputer(string ipAddress, ScriptResult scriptResult)
    {
        if (!_resultsPerComputer.TryGetValue(ipAddress, out var stringBuilder))
        {
            stringBuilder = new StringBuilder();
            _resultsPerComputer[ipAddress] = stringBuilder;
        }

        var messagePrefix = scriptResult.Type == ScriptResult.MessageType.Error ? "[Error] " : "[Output] ";
        stringBuilder.AppendLine(messagePrefix + scriptResult.Message);
    }

    private async Task UploadFiles(InputFileChangeEventArgs e)
    {
        using var reader = new StreamReader(e.File.OpenReadStream());
        _content = await reader.ReadToEndAsync();

        _shell = Path.GetExtension(e.File.Name) switch
        {
            ".bat" => Shell.Cmd,
            ".cmd" => Shell.Cmd,
            ".ps1" => Shell.PowerShell,
            _ => _shell
        };
    }

    private void CleanResults()
    {
        _resultsPerComputer.Clear();
    }
}
