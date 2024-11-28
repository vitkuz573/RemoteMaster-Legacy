// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IInstanceManagerService
{
    int StartNewInstance(string? destinationPath, INativeProcessStartInfo startInfo);

    int StartNewInstance(string? destinationPath, ProcessStartInfo startInfo);
}
