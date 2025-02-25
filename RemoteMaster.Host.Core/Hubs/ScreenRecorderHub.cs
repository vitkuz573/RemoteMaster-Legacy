// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Core.Hubs;

public class ScreenRecorderHub(IScreenRecorderService screenRecorderService) : Hub<IScreenRecorderClient>
{
    [Authorize(Policy = "StartScreenRecordingPolicy")]
    [HubMethodName("SendStartScreenRecording")]
    public async Task SendStartScreenRecordingAsync(ScreenRecordingRequest screenRecordingRequest)
    {
        await screenRecorderService.StartRecordingAsync(screenRecordingRequest);
    }

    [Authorize(Policy = "StopScreenRecordingPolicy")]
    [HubMethodName("SendStopScreenRecording")]
    public async Task SendStopScreenRecordingAsync()
    {
        await screenRecorderService.StopRecordingAsync();
    }
}
