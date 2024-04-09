// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class ScreenRecorderDialog
{
    private string _outputPath;
    private uint _durationInSeconds;
    private uint _videoQuality;

    public ScreenRecorderDialog()
    {
        var requesterName = Environment.MachineName;
        var currentDate = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        _durationInSeconds = 0;
        _outputPath = $@"C:\{requesterName}_{currentDate}.mp4";
    }

    private async Task StartRecording()
    {
        await ComputerCommandService.Execute(Hosts, async (_, connection) =>
        {
            var screenRecordingRequest = new ScreenRecordingRequest(_outputPath)
            {
                Duration = _durationInSeconds,
                VideoQuality = _videoQuality
            };

            await connection.InvokeAsync("SendStartScreenRecording", screenRecordingRequest);
        });

        MudDialog.Close(DialogResult.Ok(true));
    }

    private async Task StopRecording()
    {
        await ComputerCommandService.Execute(Hosts, async (_, connection) =>
        {
            await connection.InvokeAsync("SendStopScreenRecording");
        });

        MudDialog.Close(DialogResult.Ok(true));
    }
}
