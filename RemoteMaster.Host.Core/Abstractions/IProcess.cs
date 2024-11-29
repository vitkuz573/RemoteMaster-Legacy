// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IProcess : IDisposable
{
    int Id { get; }
    
    StreamWriter? StandardInput { get; }
    
    StreamReader? StandardOutput { get; }
    
    StreamReader? StandardError { get; }
    
    bool HasExited { get; }

    void Start(ProcessStartInfo startInfo);
    
    void Kill();
    
    string GetCommandLine();
    
    bool WaitForExit(uint millisecondsTimeout = uint.MaxValue);
}
