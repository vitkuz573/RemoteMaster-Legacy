// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
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

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    private string _path;
    private string _shell;

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private async Task RunScript()
    {
        // await ComputerCommandService.Execute(Hosts, async (computer, connection) => await connection.InvokeAsync("SendScript", fileContent, shellType));

        MudDialog.Close(DialogResult.Ok(true));
    }

    private void UploadFiles(InputFileChangeEventArgs e)
    {
        _path = e.File.Name;

        _shell = Path.GetExtension(e.File.Name) switch
        {
            ".bat" => "cmd",
            ".cmd" => "cmd",
            ".ps1" => "powershell"
        };
    }
}
