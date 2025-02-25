// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;
using static RemoteMaster.Shared.Models.Message;

namespace RemoteMaster.Host.Linux.Services;

public class ScriptService(IApplicationPathProvider applicationPathProvider, IFileSystem fileSystem, IShellScriptHandlerFactory shellScriptHandlerFactory, IProcessWrapperFactory processWrapperFactory, IHubContext<ControlHub, IControlClient> hubContext, ILogger<ScriptService> logger) : IScriptService
{
    public async Task ExecuteAsync(ScriptExecutionRequest scriptExecutionRequest)
    {
        ArgumentNullException.ThrowIfNull(scriptExecutionRequest);

        logger.LogInformation("Executing script with shell: {Shell}", scriptExecutionRequest.Shell);

        var scriptHandler = shellScriptHandlerFactory.Create(scriptExecutionRequest.Shell);

        var publicDirectory = applicationPathProvider.DataDirectory;

        var fileName = $"{fileSystem.Path.GetRandomFileName()}{scriptHandler.FileExtension}";
        var tempFilePath = fileSystem.Path.Combine(publicDirectory, fileName);

        logger.LogInformation("Temporary file path: {TempFilePath}", tempFilePath);

        var scriptContent = scriptHandler.FormatScript(scriptExecutionRequest.Content);

        await fileSystem.File.WriteAllTextAsync(tempFilePath, scriptContent, scriptHandler.FileEncoding);

        try
        {
            if (!fileSystem.File.Exists(tempFilePath))
            {
                logger.LogError("Temp file was not created: {TempFilePath}", tempFilePath);

                return;
            }

            var process = processWrapperFactory.Create();

            process.Start(new ProcessStartInfo
            {
                FileName = scriptHandler.ExecutableName,
                Arguments = scriptHandler.GetArguments(tempFilePath),
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            await hubContext.Clients.All.ReceiveMessage(new Message(process.Id.ToString(), MessageSeverity.Information)
            {
                Meta = MessageMeta.ProcessIdInformation
            });

            var readErrorTask = ReadStreamAsync(process.StandardError, MessageSeverity.Error);
            var readOutputTask = ReadStreamAsync(process.StandardOutput, MessageSeverity.Information);

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
