// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Host.Windows.Models;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class ScriptService(IHubContext<ControlHub, IControlClient> hubContext) : IScriptService
{
    public async Task Execute(Shell shell, string script, bool asSystem)
    {
        Log.Information("Executing script with shell: {Shell}", shell);

        var publicDirectory = @"C:\Users\Public";

        var extension = shell switch
        {
            Shell.Cmd => ".bat",
            Shell.PowerShell => ".ps1",
            _ => throw new InvalidOperationException($"Unsupported shell: {shell}")
        };

        var fileName = $"{Guid.NewGuid()}{extension}";
        var tempFilePath = Path.Combine(publicDirectory, fileName);

        Log.Information("Temporary file path: {TempFilePath}", tempFilePath);

        var scriptContent = shell == Shell.Cmd ? "@echo off\r\n" + script : script;
        await File.WriteAllTextAsync(tempFilePath, scriptContent);

        try
        {
            if (!File.Exists(tempFilePath))
            {
                Log.Error("Temp file was not created: {TempFilePath}", tempFilePath);

                return;
            }

            var applicationToRun = shell switch
            {
                Shell.Cmd => $"cmd.exe /c \"{tempFilePath}\"",
                Shell.PowerShell => $"powershell.exe -ExecutionPolicy Bypass -File \"{tempFilePath}\"",
                _ => "",
            };

            var options = new NativeProcessStartInfo(applicationToRun, -1)
            {
                ForceConsoleSession = true,
                DesktopName = "default",
                CreateNoWindow = true,
                UseCurrentUserToken = !asSystem,
                InheritHandles = true
            };

            var process = NativeProcess.Start(options);

            var readErrorTask = process.StandardError.ReadToEndAsync();
            var readOutputTask = process.StandardOutput.ReadToEndAsync();

            await Task.WhenAll(readErrorTask, readOutputTask);

            var error = await readErrorTask;
            var output = await readOutputTask;

            process.WaitForExit();

            Log.Information("Error: {Error}", error);
            Log.Information("Output: {Output}", output);

            if (!string.IsNullOrEmpty(error))
            {
                await hubContext.Clients.All.ReceiveScriptResult(new ScriptResult
                {
                    Message = error,
                    Type = ScriptResult.MessageType.Error
                });
            }

            if (!string.IsNullOrEmpty(output))
            {
                await hubContext.Clients.All.ReceiveScriptResult(new ScriptResult
                {
                    Message = output,
                    Type = ScriptResult.MessageType.Output
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while executing the script.");
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
}
