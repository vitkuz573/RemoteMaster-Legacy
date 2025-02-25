// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Hubs;

public class TaskManagerHub(ITaskManagerService taskManagerService) : Hub<ITaskManagerClient>
{
    [Authorize(Policy = "ViewProcessesPolicy")]
    [HubMethodName("GetRunningProcesses")]
    public async Task GetRunningProcessesAsync()
    {
        var processes = taskManagerService.GetRunningProcesses();

        await Clients.Caller.ReceiveRunningProcesses(processes);
    }

    [Authorize(Policy = "KillProcessPolicy")]
    [HubMethodName("KillProcess")]
    public async Task KillProcessAsync(int processId)
    {
        taskManagerService.KillProcess(processId);

        await GetRunningProcessesAsync();
    }

    [Authorize(Policy = "StartProcessPolicy")]
    public void StartProcess(string processPath)
    {
        taskManagerService.StartProcess(processPath);
    }
}
