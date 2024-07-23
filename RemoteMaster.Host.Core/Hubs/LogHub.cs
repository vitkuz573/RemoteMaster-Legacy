// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Hubs;

[Authorize(Roles = "Administrator")]
public class LogHub : Hub<ILogClient>
{
    private readonly string _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RemoteMaster", "Host");

    public async Task GetLogFiles()
    {
        if (!Directory.Exists(_logDirectory))
        {
            await Clients.Caller.ReceiveError("Log directory not found.");

            return;
        }

        var logFiles = Directory.GetFiles(_logDirectory, "RemoteMaster_Host*.log");
        var fileNames = logFiles.Select(Path.GetFileName).ToList();

        await Clients.Caller.ReceiveLogFiles(fileNames);
    }

    public async Task GetLog(string fileName)
    {
        var filePath = Path.Combine(_logDirectory, fileName);

        if (File.Exists(filePath))
        {
            var logContent = await File.ReadAllTextAsync(filePath);
            await Clients.Caller.ReceiveLog(logContent);
        }
        else
        {
            await Clients.Caller.ReceiveError("Log file not found.");
        }
    }
}