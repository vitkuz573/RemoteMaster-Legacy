// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Host.Windows.Models;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Models;
using static RemoteMaster.Shared.Models.Message;

namespace RemoteMaster.Host.Windows.Services;

public class ScriptService(IHubContext<ControlHub, IControlClient> hubContext, ILogger<ScriptService> logger) : IScriptService
{
    public async Task Execute(ScriptExecutionRequest scriptExecutionRequest)
    {
        ArgumentNullException.ThrowIfNull(scriptExecutionRequest);

        logger.LogInformation("Executing script with shell: {Shell}", scriptExecutionRequest.Shell);

        const string publicDirectory = @"C:\Users\Public";

        var extension = scriptExecutionRequest.Shell switch
        {
            Shell.Cmd => ".bat",
            Shell.PowerShell => ".ps1",
            _ => throw new InvalidOperationException($"Unsupported shell: {scriptExecutionRequest.Shell}")
        };

        var fileName = $"{Guid.NewGuid()}{extension}";
        var tempFilePath = Path.Combine(publicDirectory, fileName);

        logger.LogInformation("Temporary file path: {TempFilePath}", tempFilePath);

        var encoding = scriptExecutionRequest.Shell switch
        {
            Shell.Cmd => new UTF8Encoding(false),
            Shell.PowerShell => new UTF8Encoding(true),
            _ => Encoding.UTF8
        };

        var scriptContent = scriptExecutionRequest.Shell == Shell.Cmd ? $"@echo off\r\n{scriptExecutionRequest.Content}" : scriptExecutionRequest.Content;

        await File.WriteAllTextAsync(tempFilePath, scriptContent, encoding);

        try
        {
            if (!File.Exists(tempFilePath))
            {
                logger.LogError("Temp file was not created: {TempFilePath}", tempFilePath);

                return;
            }

            var applicationToRun = scriptExecutionRequest.Shell switch
            {
                Shell.Cmd => $"cmd.exe /c \"{tempFilePath}\"",
                Shell.PowerShell => $"powershell.exe -ExecutionPolicy Bypass -File \"{tempFilePath}\"",
                _ => throw new ArgumentOutOfRangeException(nameof(scriptExecutionRequest))
            };

            using var process = new NativeProcess();
            process.StartInfo = new NativeProcessStartInfo
            {
                FileName = applicationToRun,
                ForceConsoleSession = true,
                DesktopName = "Default",
                CreateNoWindow = true,
                UseCurrentUserToken = !scriptExecutionRequest.AsSystem,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            process.Start();

            await hubContext.Clients.All.ReceiveMessage(new Message(process.Id.ToString(), MessageType.Service)
            {
                Meta = "pid"
            });

            var readErrorTask = ReadStreamAsync(process.StandardError!, MessageType.Error);
            var readOutputTask = ReadStreamAsync(process.StandardOutput!, MessageType.Information);

            process.WaitForExit();

            await Task.WhenAll(readErrorTask, readOutputTask);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while executing the script.");
        }
        finally
        {
            await Task.Delay(5000);

            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
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
