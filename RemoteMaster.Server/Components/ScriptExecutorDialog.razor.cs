// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components;

#pragma warning disable CA2227

public partial class ScriptExecutorDialog
{
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; }

    [Parameter]
    public Dictionary<Computer, HubConnection> Hosts { get; set; }

    [Inject]
    private IComputerCommandService ComputerCommandService { get; set; }

    private readonly Dictionary<Computer, string> _scriptResults = new();
    private string _content;
    private string _shell;

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private async Task RunScript()
    {
        await ComputerCommandService.Execute(Hosts, async (computer, connection) => {
            connection.On<string>("ReceiveScriptResult", async (result) =>
            {
                _scriptResults[computer] = result;

                await InvokeAsync(StateHasChanged);
            });

            await connection.InvokeAsync("SendScript", _content, _shell);
        });

        MudDialog.Close(DialogResult.Ok(true));
    }

    private async Task UploadFiles(InputFileChangeEventArgs e)
    {
        using var reader = new StreamReader(e.File.OpenReadStream());

        _content = await reader.ReadToEndAsync();

        _shell = Path.GetExtension(e.File.Name) switch
        {
            ".bat" => "CMD",
            ".cmd" => "CMD",
            ".ps1" => "PowerShell"
        };
    }
}
