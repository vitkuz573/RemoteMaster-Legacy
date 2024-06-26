// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Windows.Abstractions;

public interface IProcessWrapper : IDisposable
{
    int Id { get; }

    void Kill();

    string GetCommandLine();

    void WaitForExit();

    string ReadStandardOutput();
}