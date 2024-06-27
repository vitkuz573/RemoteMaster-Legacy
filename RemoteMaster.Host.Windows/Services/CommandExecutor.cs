// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Hubs;
using RemoteMaster.Shared.Models;
using Serilog;
using static RemoteMaster.Shared.Models.Message;

namespace RemoteMaster.Host.Windows.Services;

public class CommandExecutor(IHubContext<ServiceHub, IServiceClient> hubContext, IProcessService processService) : ICommandExecutor
{
    public async Task ExecuteCommandAsync(string command)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo("cmd.exe", $"/c {command}")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = processService.Start(processStartInfo);

            await hubContext.Clients.All.ReceiveMessage(new Message(process.Id.ToString(), MessageType.Service)
            {
                Meta = "pid"
            });

            var readErrorTask = ReadStreamAsync(process.StandardError, MessageType.Error);
            var readOutputTask = ReadStreamAsync(process.StandardOutput, MessageType.Information);

            processService.WaitForExit(process);

            await Task.WhenAll(readErrorTask, readOutputTask);
        }
        catch (Exception ex)
        {
            Log.Error($"Error executing command: {command}. Exception: {ex.Message}");
        }
    }

    private async Task ReadStreamAsync(TextReader streamReader, MessageType messageType)
    {
        while (await streamReader.ReadLineAsync() is { } line)
        {
            await hubContext.Clients.All.ReceiveMessage(new Message(line, messageType));
        }
    }
}