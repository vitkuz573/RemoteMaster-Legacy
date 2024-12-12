// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Hubs;

public class LogHub(IFileSystem fileSystem) : Hub<ILogClient>
{
    private readonly string _logDirectory = fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RemoteMaster", "Host");

    [Authorize(Policy = "ViewLogFilesPolicy")]
    public async Task GetLogFiles()
    {
        if (!fileSystem.Directory.Exists(_logDirectory))
        {
            var message = new Message("Log directory not found.", Message.MessageSeverity.Error);

            await Clients.Caller.ReceiveMessage(message);

            return;
        }

        var logFiles = fileSystem.Directory.GetFiles(_logDirectory, "RemoteMaster_Host*.log");
        var fileNames = logFiles.Select(fileSystem.Path.GetFileName).ToList();

        await Clients.Caller.ReceiveLogFiles(fileNames);
    }

    [Authorize(Policy = "ViewLogContentPolicy")]
    public async Task GetLog(string fileName)
    {
        var filePath = fileSystem.Path.Combine(_logDirectory, fileName);

        if (fileSystem.File.Exists(filePath))
        {
            try
            {
                await using var fileStream = fileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var streamReader = new StreamReader(fileStream);

                var logContent = await streamReader.ReadToEndAsync();

                await Clients.Caller.ReceiveLog(logContent);
            }
            catch (Exception ex)
            {
                var message = new Message($"Error reading log file: {ex.Message}", Message.MessageSeverity.Error);

                await Clients.Caller.ReceiveMessage(message);
            }
        }
        else
        {
            var message = new Message("Log file not found.", Message.MessageSeverity.Error);

            await Clients.Caller.ReceiveMessage(message);
        }
    }

    [Authorize(Policy = "FilterLogPolicy")]
    public async Task GetFilteredLog(string fileName, string? level, DateTime? startDate, DateTime? endDate)
    {
        var filePath = fileSystem.Path.Combine(_logDirectory, fileName);

        if (!fileSystem.File.Exists(filePath))
        {
            var message = new Message("Log file not found.", Message.MessageSeverity.Error);

            await Clients.Caller.ReceiveMessage(message);

            return;
        }

        try
        {
            await using var fileStream = fileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var streamReader = new StreamReader(fileStream);

            var logContent = await streamReader.ReadToEndAsync();
            var logLines = logContent.Split(Environment.NewLine);

            var filteredContent = logLines.Where(line =>
            {
                var match = Regex.Match(line, @"(?<date>[\d-]+\s[\d:.,]+)\s\+\d+:\d+\s\[(?<level>[A-Z]+)\]");

                if (!match.Success)
                {
                    return false;
                }

                var logDate = DateTime.Parse(match.Groups["date"].Value);
                var logLevel = match.Groups["level"].Value;

                var levelMatch = string.IsNullOrEmpty(level) || logLevel.Equals(level, StringComparison.OrdinalIgnoreCase);
                var dateMatch = (!startDate.HasValue || logDate >= startDate) && (!endDate.HasValue || logDate <= endDate);

                return levelMatch && dateMatch;
            });

            await Clients.Caller.ReceiveLog(string.Join(Environment.NewLine, filteredContent));
        }
        catch (Exception ex)
        {
            var message = new Message($"Error reading log file: {ex.Message}", Message.MessageSeverity.Error);

            await Clients.Caller.ReceiveMessage(message);
        }
    }

    [Authorize(Policy = "DeleteLogsPolicy")]
    public async Task DeleteAllLogs()
    {
        if (!fileSystem.Directory.Exists(_logDirectory))
        {
            var message = new Message("Log file not found.", Message.MessageSeverity.Error);

            await Clients.Caller.ReceiveMessage(message);

            return;
        }

        try
        {
            var logFiles = fileSystem.Directory.GetFiles(_logDirectory, "RemoteMaster_Host*.log");

            if (logFiles.Length == 0)
            {
                await Clients.Caller.ReceiveMessage(new Message("No log files found to delete.", Message.MessageSeverity.Error));

                return;
            }

            foreach (var logFile in logFiles)
            {
                fileSystem.File.Delete(logFile);
            }

            var message = new Message("All log files have been deleted.", Message.MessageSeverity.Information);

            await Clients.Caller.ReceiveMessage(message);
        }
        catch (Exception ex)
        {
            var message = new Message($"Error deleting log files: {ex.Message}", Message.MessageSeverity.Error);

            await Clients.Caller.ReceiveMessage(message);
        }
    }
}
