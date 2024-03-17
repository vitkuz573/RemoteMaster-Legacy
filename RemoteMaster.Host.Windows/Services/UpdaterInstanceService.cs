// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Shared.Models;
using Serilog;
using static RemoteMaster.Shared.Models.ScriptResult;

namespace RemoteMaster.Host.Windows.Services;

public class UpdaterInstanceService(IHubContext<UpdaterHub, IUpdaterClient> hubContext) : IUpdaterInstanceService
{
    private readonly string _argument = $"--launch-mode=updater";
    private readonly string _sourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "RemoteMaster.Host.exe");
    private readonly string _executablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "Updater", "RemoteMaster.Host.exe");

    public async Task Start(UpdateRequest updateRequest)
    {
        ArgumentNullException.ThrowIfNull(updateRequest);

        var additionalArguments = BuildArguments(updateRequest.FolderPath, updateRequest.UserCredentials, updateRequest.ForceUpdate, updateRequest.AllowDowngrade);

        try
        {
            await StartNewInstance(additionalArguments);
            Log.Information("Successfully started a new instance of the host.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error starting new instance of the host. Executable path: {ExecutablePath}", _executablePath);
        }
    }

    private string BuildArguments(string folderPath, Credentials? userCredentials, bool force, bool allowDowngrade)
    {
        var arguments = new StringBuilder(_argument);
        var escapedFolderPath = "\"" + folderPath.Replace("\"", "\\\"") + "\"";

        arguments.Append($" --folder-path={escapedFolderPath}");

        if (userCredentials == null)
        {
            return arguments.ToString();
        }

        if (!string.IsNullOrEmpty(userCredentials.Username))
        {
            var escapedUsername = "\"" + userCredentials.Username.Replace("\"", "\\\"") + "\"";
            arguments.Append($" --username={escapedUsername}");
        }

        if (string.IsNullOrEmpty(userCredentials.Password))
        {
            return arguments.ToString();
        }

        var escapedPassword = "\"" + userCredentials.Password.Replace("\"", "\\\"") + "\"";
        arguments.Append($" --password={escapedPassword}");

        if (force)
        {
            arguments.Append(" --force-update");
        }

        if (allowDowngrade)
        {
            arguments.Append(" --allow-downgrade");
        }

        return arguments.ToString();
    }

    private async Task StartNewInstance(string additionalArguments)
    {
        try
        {
            var executableDirectory = Path.GetDirectoryName(_executablePath);

            if (!Directory.Exists(executableDirectory))
            {
                Log.Information("Creating directory {ExecutableDirectory} for the executable.", executableDirectory);
                Directory.CreateDirectory(executableDirectory);
            }

            Log.Information("Copying executable from {SourcePath} to {ExecutablePath}", _sourcePath, _executablePath);
            File.Copy(_sourcePath, _executablePath, true);
            Log.Information("Successfully copied the executable.");

            using var process = new Process();

            process.StartInfo = new ProcessStartInfo(_executablePath, additionalArguments)
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            process.Start();

            await hubContext.Clients.All.ReceiveScriptResult(new ScriptResult
            {
                Message = process.Id.ToString(),
                Type = MessageType.Service,
                Meta = "pid"
            });

            var readErrorTask = ReadStreamAsync(process.StandardError, MessageType.Error);
            var readOutputTask = ReadStreamAsync(process.StandardOutput, MessageType.Output);

            await process.WaitForExitAsync();

            await Task.WhenAll(readErrorTask, readOutputTask);

            Log.Information("Started a new instance of the host with options: {@Options}", process.StartInfo);
        }
        catch (IOException ioEx)
        {
            Log.Error(ioEx, "IO error occurred while copying the executable. Source: {SourcePath}, Destination: {ExecutablePath}", _sourcePath, _executablePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error starting new instance of the host. Executable path: {Path}", _executablePath);
        }
    }

    private async Task ReadStreamAsync(TextReader streamReader, MessageType messageType)
    {
        while (await streamReader.ReadLineAsync() is { } line)
        {
            await hubContext.Clients.All.ReceiveScriptResult(new ScriptResult
            {
                Message = line,
                Type = messageType
            });
        }
    }
}
