﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;

namespace RemoteMaster.Host.Windows.Services;

public class NativeProcessFactory(ISessionService sessionService, ICommandLineProvider commandLineProvider, IProcessService processService, IFileSystem fileSystem) : INativeProcessFactory
{
    public IProcess Create(INativeProcessOptions options)
    {
        if (options is not NativeProcessOptions nativeOptions)
        {
            throw new ArgumentException("Invalid process options for Windows platform.", nameof(options));
        }

        return new NativeProcess(nativeOptions, sessionService, commandLineProvider, processService, fileSystem);
    }
}
