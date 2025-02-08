// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Linux.Abstractions;

namespace RemoteMaster.Host.Linux.LinuxServices;

public class HostService(IFileSystem fileSystem, IProcessWrapperFactory processWrapperFactory, IApplicationPathProvider applicationPathProvider, ILogger<HostService> logger) : AbstractDaemon(fileSystem, processWrapperFactory, logger)
{
    private readonly IFileSystem _fileSystem = fileSystem;

    public override string Name => "RCHost";

    protected override string BinPath => _fileSystem.Path.Combine(applicationPathProvider.RootDirectory, "RemoteMaster.Host");

    protected override string WorkingDirectory => applicationPathProvider.RootDirectory;

    protected override IDictionary<string, string?> Arguments { get; } = new Dictionary<string, string?>
    {
        ["service"] = null
    };

    protected override string Description => "RemoteMaster Control Service enables advanced remote management and control functionalities for authorized clients. It provides seamless access to system controls, resource management, and real-time support capabilities, ensuring efficient and secure remote operations.";
}
