// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Host.Core.Hubs;

[Authorize(Roles = "Administrator")]
public class ScreenRecorderHub(IScreenRecorderService screenRecorderService) : Hub<IScreenRecorderClient>
{
    public async Task SendStartScreenRecording(ScreenRecordingRequest screenRecordingRequest)
    {
        await screenRecorderService.StartRecordingAsync(screenRecordingRequest);
    }

    public async Task SendStopScreenRecording()
    {
        await screenRecorderService.StopRecordingAsync();
    }
}
