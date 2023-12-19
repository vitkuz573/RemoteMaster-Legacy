// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class ScriptExecutorDialog
{
    [Inject]
    private IComputerCommandService ComputerCommandService { get; set; }

    private Dictionary<Computer, string> _scriptResults = [];
    private string _content;
    private string _manualScriptContent;
    private Shell? _shell;

    private async Task RunScript()
    {
        var scriptToRun = string.IsNullOrEmpty(_content) ? _manualScriptContent : _content;

        await ComputerCommandService.Execute(Hosts, async (computer, connection) => {
            connection.On<string>("ReceiveScriptResult", async (result) =>
            {
                _scriptResults[computer] = result;

                await InvokeAsync(StateHasChanged);
            });

            await connection.InvokeAsync("SendScript", scriptToRun, _shell);
        });

        MudDialog.Close(DialogResult.Ok(true));
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
