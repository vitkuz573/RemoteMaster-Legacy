
// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.RegularExpressions;
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

    public async Task GetFilteredLog(string fileName, string? level, DateTime? startDate, DateTime? endDate)
    {
        var filePath = Path.Combine(_logDirectory, fileName);

        if (!File.Exists(filePath))
        {
            await Clients.Caller.ReceiveError("Log file not found.");
            return;
        }

        var logContent = await File.ReadAllLinesAsync(filePath);
        var filteredContent = logContent.Where(line =>
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
}