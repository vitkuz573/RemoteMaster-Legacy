// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ProcessWrapper : IProcessWrapper
{
    private readonly Process _process;
    private readonly ICommandLineProvider _commandLineProvider;

    public ProcessWrapper(Process process, ICommandLineProvider commandLineProvider)
    {
        _process = process ?? throw new ArgumentNullException(nameof(process));
        _commandLineProvider = commandLineProvider;

        if (_process.HasExited)
        {
            throw new InvalidOperationException("Process has already exited.");
        }
    }

    public int Id => _process.Id;

    public StreamReader StandardOutput => _process.StandardOutput;

    public StreamReader StandardError => _process.StandardError;

    public void Start()
    {
        _process.Start();
    }

    public void Kill()
    {
        _process.Kill();
    }

    public string GetCommandLine()
    {
        return _commandLineProvider.GetCommandLine(_process);
    }

    public void WaitForExit()
    {
        _process.WaitForExit();
    }

    public void Dispose()
    {
        _process.Dispose();
    }
}
