// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;

namespace RemoteMaster.Host.Core.Abstractions;

public interface INativeProcess : IDisposable
{
    int Id { get; }

    StreamWriter? StandardInput { get; }

    StreamReader? StandardOutput { get; }

    StreamReader? StandardError { get; }

    void Start(ProcessStartInfo startInfo);
}
