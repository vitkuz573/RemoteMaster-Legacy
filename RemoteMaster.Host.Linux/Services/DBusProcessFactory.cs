// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Linux.Models;

namespace RemoteMaster.Host.Linux.Services;

public class DBusProcessFactory(ICommandLineProvider commandLineProvider, ILogger<DBusProcess> logger) : INativeProcessFactory
{
    public IProcess Create(INativeProcessOptions options)
    {
        if (options is not DBusProcessOptions nativeOptions)
        {
            throw new ArgumentException("Invalid process options for Linux platform.", nameof(options));
        }

        return new DBusProcess(nativeOptions, commandLineProvider, logger);
    }
}
