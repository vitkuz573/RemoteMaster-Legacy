// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Hubs;

public class LogHub(IFileSystem fileSystem) : Hub<ILogClient>
{
    private readonly string _logDirectory = fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RemoteMaster", "Host");

    [Authorize(Policy = "ViewLogFilesPolicy")]
    public async Task GetLogFiles()
    {
        if (!fileSystem.Directory.Exists(_logDirectory))
        {
            await Clients.Caller.ReceiveError("Log directory not found.");

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
                await Clients.Caller.ReceiveError($"Error reading log file: {ex.Message}");
            }
        }
        else
        {
            await Clients.Caller.ReceiveError("Log file not found.");
        }
    }

    [Authorize(Policy = "FilterLogPolicy")]
    public async Task GetFilteredLog(string fileName, string? level, DateTime? startDate, DateTime? endDate)
    {
        var filePath = fileSystem.Path.Combine(_logDirectory, fileName);

        if (!fileSystem.File.Exists(filePath))
        {
            await Clients.Caller.ReceiveError("Log file not found.");

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
            await Clients.Caller.ReceiveError($"Error reading log file: {ex.Message}");
        }
    }

    [Authorize(Policy = "DeleteLogsPolicy")]
    public async Task DeleteAllLogs()
    {
        if (!fileSystem.Directory.Exists(_logDirectory))
        {
            await Clients.Caller.ReceiveError("Log directory not found.");

            return;
        }

        try
        {
            var logFiles = fileSystem.Directory.GetFiles(_logDirectory, "RemoteMaster_Host*.log");

            if (logFiles.Length == 0)
            {
                await Clients.Caller.ReceiveError("No log files found to delete.");

                return;
            }

            foreach (var logFile in logFiles)
            {
                fileSystem.File.Delete(logFile);
            }

            await Clients.Caller.ReceiveMessage("All log files have been deleted.");
        }
        catch (Exception ex)
        {
            await Clients.Caller.ReceiveError($"Error deleting log files: {ex.Message}");
        }
    }
}
