// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Tests;

public class NullScope : IDisposable
{
    public static NullScope Instance { get; } = new();

    public void Dispose() { }
}
