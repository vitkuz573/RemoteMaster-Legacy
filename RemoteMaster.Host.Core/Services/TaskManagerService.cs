// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class TaskManagerService : ITaskManagerService
{
    public List<ProcessInfo> GetRunningProcesses()
    {
        var processes = Process.GetProcesses();
        var processList = new List<ProcessInfo>();

        foreach (var process in processes)
        {
            try
            {
                var cpuUsage = GetCpuUsage(process);
                processList.Add(new ProcessInfo
                {
                    Id = process.Id,
                    Name = process.ProcessName,
                    MemoryUsage = process.WorkingSet64,
                    CpuUsage = cpuUsage,
                    ProcessPath = process.MainModule.FileName
                });
            }
            catch (Exception ex)
            {
                // Логирование или обработка исключений, если не удается получить доступ к каким-либо данным процесса.
            }
        }

        return processList;
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