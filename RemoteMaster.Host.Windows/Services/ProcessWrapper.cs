// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Extensions;

namespace RemoteMaster.Host.Windows.Services;

public class ProcessWrapper : IProcessWrapper
{
    private readonly Process _process;

    public ProcessWrapper(Process process)
    {
        _process = process ?? throw new ArgumentNullException(nameof(process));

        if (_process.HasExited)
        {
            throw new InvalidOperationException("Process has already exited.");
        }
    }

    public int Id => _process.Id;

    public StreamReader StandardOutput => _process.StandardOutput;

    public StreamReader StandardError => _process.StandardError;

    public void Kill()
    {
        _process.Kill();
    }

    public string GetCommandLine()
    {
        return _process.GetCommandLine();
    }

    public void WaitForExit()
    {
        _process.WaitForExit();
    }

    public string ReadStandardOutput()
    {
        return _process.StandardOutput.ReadToEnd();
    }

    public void Dispose()
    {
        _process?.Dispose();
    }
}
