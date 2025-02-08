// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ProcessWrapper(Process process, ICommandLineProvider commandLineProvider) : IProcess
{
    private bool _disposed;

    public int Id => process.Id;

    public int ExitCode => process.ExitCode;

    public StreamWriter StandardInput => process.StandardInput;

    public StreamReader StandardOutput => process.StandardOutput;

    public StreamReader StandardError => process.StandardError;

    public bool HasExited
    {
        get
        {
            try
            {
                return process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    public void Start(ProcessStartInfo startInfo)
    {
        process.StartInfo = startInfo;
        process.Start();
    }

    public void Kill()
    {
        process.Kill();
    }

    public string[] GetCommandLine()
    {
        return commandLineProvider.GetCommandLine(this);
    }

    public bool WaitForExit(uint millisecondsTimeout)
    {
        return process.WaitForExit((int)Math.Min(millisecondsTimeout, int.MaxValue));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            process.Dispose();
        }

        _disposed = true;
    }
}
