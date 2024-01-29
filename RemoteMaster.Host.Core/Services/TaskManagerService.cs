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

public class TaskManagerService : ITaskManagerService
{
    private static readonly ConcurrentDictionary<string, byte[]> _iconCache = new();

    public List<ProcessInfo> GetRunningProcesses()
    {
        var processes = Process.GetProcesses();
        var processList = new List<ProcessInfo>();

        Parallel.ForEach(processes, (process) =>
        {
            try
            {
                var cpuUsage = GetCpuUsage(process);
                var icon = GetProcessIcon(process);

                processList.Add(new ProcessInfo
                {
                    Id = process.Id,
                    Name = process.ProcessName,
                    MemoryUsage = process.WorkingSet64,
                    CpuUsage = cpuUsage,
                    ProcessPath = process.MainModule.FileName,
                    Icon = icon
                });
            }
            catch (Exception ex)
            {
                // Логирование или обработка исключений, если не удается получить доступ к каким-либо данным процесса.
            }
        });

        return processList;
    }

    private static byte[] GetProcessIcon(Process process)
    {
        var filePath = process.MainModule.FileName;

        if (!_iconCache.TryGetValue(filePath, out var icon))
        {
            try
            {
                using var processIcon = Icon.ExtractAssociatedIcon(filePath);
                
                if (processIcon != null)
                {
                    using var ms = new MemoryStream();
                    processIcon.ToBitmap().Save(ms, ImageFormat.Png);
                    icon = ms.ToArray();
                    _iconCache[filePath] = icon;
                }
            }
            catch
            {
                // В случае ошибки возвращается null или стандартная иконка
                // Пример: return GetDefaultIcon();
                return null;
            }
        }

        return icon;
    }

    public void KillProcess(int processId)
    {
        var process = Process.GetProcessById(processId);
        process.Kill();
    }

    public void StartProcess(string processPath)
    {
        Process.Start(processPath);
    }

    private static double GetCpuUsage(Process process)
    {
        return 0.0;
    }
}