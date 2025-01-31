// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Linux.Abstractions;

namespace RemoteMaster.Host.Linux.LinuxServices;

public class HostService(IFileSystem fileSystem, ILogger<HostService> logger) : AbstractDaemon(fileSystem, logger)
{
    public override string Name => "RCHost";

    protected override string Description => "RemoteMaster Control Service enables advanced remote management and control functionalities for authorized clients. It provides seamless access to system controls, resource management, and real-time support capabilities, ensuring efficient and secure remote operations.";

    protected override string BinPath => "/opt/RemoteMaster/Host/RemoteMaster.Host";

    protected override IDictionary<string, string?> Arguments { get; } = new Dictionary<string, string?>
    {
        ["service"] = null
    };

    protected async override Task ExecuteDaemonAsync(CancellationToken cancellationToken)
    {
        // Implement the core logic of RCHost daemon here.
        // For demonstration, we'll log a heartbeat every 10 seconds.

        while (!cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug($"{Name} daemon heartbeat at {DateTime.UtcNow}.");

            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }
}
