// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Hubs;

[Authorize(Roles = "Administrator")]
public partial class LogHub : Hub<ILogClient>
{
    private readonly string _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RemoteMaster", "Host");

    [GeneratedRegex(@"(?<date>[\d-]+\s[\d:.,]+)\s\+\d+:\d+\s\[(?<level>[A-Z]+)\]\s(?<message>.*)")]
    private static partial Regex LogEntryRegex();

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
            try
            {
                await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream);
                var logContent = await reader.ReadToEndAsync();
                await Clients.Caller.ReceiveLog(logContent);
            }
            catch (IOException ex)
            {
                await Clients.Caller.ReceiveError($"Error reading log file: {ex.Message}");
            }
        }
        else
        {
            await Clients.Caller.ReceiveError("Log file not found.");
        }
    }

    public async Task GetFilteredLog(string fileName, string? level, DateTime? startDate, DateTime? endDate)
    {
        var filePath = Path.Combine(_logDirectory, fileName);

        if (!File.Exists(filePath))
        {
            await Clients.Caller.ReceiveError("Log file not found.");

            return;
        }

        try
        {
            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream);
            var logContent = await reader.ReadToEndAsync();
            var logLines = logContent.Split(Environment.NewLine);
            var filteredContent = logLines.Where(line =>
            {
                var match = LogEntryRegex().Match(line);

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
        catch (IOException ex)
        {
            await Clients.Caller.ReceiveError($"Error reading log file: {ex.Message}");
        }
    }

    public async Task DeleteLog(string fileName)
    {
        var filePath = Path.Combine(_logDirectory, fileName);

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                await Clients.Caller.ReceiveMessage($"{fileName} deleted successfully.");
            }
            else
            {
                await Clients.Caller.ReceiveError("Log file not found.");
            }
        }
        catch (IOException)
        {
            await Clients.Caller.ReceiveError("Log file is currently in use and cannot be deleted.");
        }
    }

    public async Task DeleteAllLogs()
    {
        var logFiles = Directory.GetFiles(_logDirectory, "RemoteMaster_Host*.log");

        foreach (var logFile in logFiles)
        {
            try
            {
                File.Delete(logFile);
            }
            catch (IOException)
            {
                // Ignore files that are currently in use
            }
        }

        await Clients.Caller.ReceiveMessage("All deletable log files have been removed.");
    }
}
