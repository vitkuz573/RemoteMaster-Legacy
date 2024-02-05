// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class ScreenRecorderDialog
{
    private string _outputPath;
    private uint _durationInSeconds;

    protected override void OnInitialized()
    {
        var requesterName = Environment.MachineName;
        var currentDate = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _outputPath = $@"C:\{requesterName}_{currentDate}.mp4";
        _durationInSeconds = 0;
    }

    private async Task StartRecording()
    {
        await ComputerCommandService.Execute(Hosts, async (computer, connection) =>
        {
            await connection.InvokeAsync("SendStartScreenRecording", _outputPath, _durationInSeconds);
        });

        MudDialog.Close(DialogResult.Ok(true));
    }

    private async Task StopRecording()
    {
        await ComputerCommandService.Execute(Hosts, async (computer, connection) =>
        {
            await connection.InvokeAsync("SendStopScreenRecording");
        });

        MudDialog.Close(DialogResult.Ok(true));
    }
}
