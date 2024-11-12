// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
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

public class ScriptService(IFileSystem fileSystem, IHubContext<ControlHub, IControlClient> hubContext, ILogger<ScriptService> logger) : IScriptService
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
            Shell.Pwsh => ".ps1",
            _ => throw new InvalidOperationException($"Unsupported shell: {scriptExecutionRequest.Shell}")
        };

        var fileName = $"{Guid.NewGuid()}{extension}";
        var tempFilePath = fileSystem.Path.Combine(publicDirectory, fileName);

        logger.LogInformation("Temporary file path: {TempFilePath}", tempFilePath);

        var encoding = scriptExecutionRequest.Shell switch
        {
            Shell.Cmd => new UTF8Encoding(false),
            Shell.PowerShell => new UTF8Encoding(true),
            Shell.Pwsh => new UTF8Encoding(true),
            _ => throw new InvalidOperationException($"Unsupported shell: {scriptExecutionRequest.Shell}")
        };

        var scriptContent = scriptExecutionRequest.Shell == Shell.Cmd ? $"@echo off\r\n{scriptExecutionRequest.Content}" : scriptExecutionRequest.Content;

        await fileSystem.File.WriteAllTextAsync(tempFilePath, scriptContent, encoding);

        try
        {
            if (!fileSystem.File.Exists(tempFilePath))
            {
                logger.LogError("Temp file was not created: {TempFilePath}", tempFilePath);

                return;
            }

            var applicationToRun = scriptExecutionRequest.Shell switch
            {
                Shell.Cmd => $"cmd /c \"{tempFilePath}\"",
                Shell.PowerShell => $"powershell -ExecutionPolicy Bypass -File \"{tempFilePath}\"",
                Shell.Pwsh => $"pwsh -ExecutionPolicy Bypass -File \"{tempFilePath}\"",
                _ => throw new InvalidOperationException($"Unsupported shell: {scriptExecutionRequest.Shell}")
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

            await hubContext.Clients.All.ReceiveMessage(new Message(process.Id.ToString(), MessageSeverity.Service)
            {
                Meta = "pid"
            });

            var readErrorTask = ReadStreamAsync(process.StandardError!, MessageSeverity.Error);
            var readOutputTask = ReadStreamAsync(process.StandardOutput!, MessageSeverity.Information);

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

            if (fileSystem.File.Exists(tempFilePath))
            {
                fileSystem.File.Delete(tempFilePath);
            }
        }
    }

    private async Task ReadStreamAsync(TextReader streamReader, MessageSeverity messageType)
    {
        while (await streamReader.ReadLineAsync() is { } line)
        {
            await hubContext.Clients.All.ReceiveMessage(new Message(line, messageType));
        }
    }
}
