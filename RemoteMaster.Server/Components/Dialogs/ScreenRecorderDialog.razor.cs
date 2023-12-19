// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class ScreenRecorderDialog
{
    [Inject]
    private IComputerCommandService ComputerCommandService { get; set; } = default!;

    private string _outputFileName;
    private uint _duration;

    protected override void OnInitialized()
    {
        var requesterName = Environment.MachineName;
        var currentDate = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _outputFileName = $@"C:\{requesterName}_{currentDate}.mp4";

        base.OnInitialized();
    }

    private async Task StartRecording()
    {
        await ComputerCommandService.Execute(Hosts, async (computer, connection) => await connection.InvokeAsync("SendStartScreenRecording", _outputFileName));

        MudDialog.Close(DialogResult.Ok(true));
    }

    private async Task StopRecording()
    {
        await ComputerCommandService.Execute(Hosts, async (computer, connection) => await connection.InvokeAsync("SendStopScreenRecording"));

        MudDialog.Close(DialogResult.Ok(true));
    }
}
