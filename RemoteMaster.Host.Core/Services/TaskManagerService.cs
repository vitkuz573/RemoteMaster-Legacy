// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class TaskManagerService(IProcessService processService, IProcessWrapperFactory processWrapperFactory) : ITaskManagerService
{
    private static readonly ConcurrentDictionary<string, byte[]> IconCache = new();

    public List<ProcessInfo> GetRunningProcesses()
    {
        var processes = Process.GetProcesses();
        var processList = new List<ProcessInfo>();

        Parallel.ForEach(processes, (process) =>
        {
            try
            {
                var processPath = process.MainModule?.FileName ?? "N/A";
                var icon = GetProcessIcon(processPath);

                processList.Add(new ProcessInfo(process.Id, process.ProcessName)
                {
                    MemoryUsage = process.WorkingSet64,
                    Icon = icon
                });
            }
            catch (Exception)
            {
                // ignored
            }
        });

        return processList;
    }

    private static byte[]? GetProcessIcon(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        if (IconCache.TryGetValue(filePath, out var icon))
        {
            return icon;
        }

        try
        {
            using var processIcon = Icon.ExtractAssociatedIcon(filePath);

            if (processIcon != null)
            {
                using var ms = new MemoryStream();
                processIcon.ToBitmap().Save(ms, ImageFormat.Png);
                icon = ms.ToArray();
                IconCache.TryAdd(filePath, icon);
            }
        }
        catch
        {
            return null;
        }

        return icon;
    }

    public void KillProcess(int processId)
    {
        var process = processService.GetProcessById(processId);

        process.Kill();
    }

    public void StartProcess(string processPath)
    {
        var process = processWrapperFactory.Create();

        process.Start(new ProcessStartInfo(processPath));
    }
}
