// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Hubs;

[Authorize(Roles = "Administrator")]
public class TaskManagerHub(ITaskManagerService taskManagerService) : Hub<ITaskManagerClient>
{
    public async Task GetRunningProcesses()
    {
        var processes = taskManagerService.GetRunningProcesses();
        await Clients.Caller.ReceiveRunningProcesses(processes);
    }

    public async Task KillProcess(int processId)
    {
        taskManagerService.KillProcess(processId);
        await GetRunningProcesses();
    }

    public void StartProcess(string processPath)
    {
        taskManagerService.StartProcess(processPath);
    }
}
