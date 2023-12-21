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

    private string _scriptResults;
    private string _content;
    private string _manualScriptContent;
    private Shell? _shell;
    private readonly Dictionary<string, StringBuilder> _resultsPerComputer = [];
    private readonly Dictionary<string, Action<string>> _scriptResultHandlers = [];
    private readonly HashSet<HubConnection> _subscribedConnections = [];

    private async Task RunScript()
    {
        var scriptToRun = string.IsNullOrEmpty(_content) ? _manualScriptContent : _content;

        foreach (var (computer, connection) in Hosts)
        {
            if (connection != null && !_subscribedConnections.Contains(connection))
            {
                connection.On<string>("ReceiveScriptResult", (result) =>
                {
                    if (!_resultsPerComputer.TryGetValue(computer.IPAddress, out var stringBuilder))
                    {
                        stringBuilder = new StringBuilder();
                        _resultsPerComputer[computer.IPAddress] = stringBuilder;
                    }

                    stringBuilder.Append(result);
                    _scriptResults = string.Join("\n", _resultsPerComputer.Values.Select(sb => sb.ToString()));
                    InvokeAsync(StateHasChanged);
                });

                _subscribedConnections.Add(connection);
            }

            if (connection != null)
            {
                await connection.InvokeAsync("SendScript", scriptToRun, _shell);
            }
        }
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
}
