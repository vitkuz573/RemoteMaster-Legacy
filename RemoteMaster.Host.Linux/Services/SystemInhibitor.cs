// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Linux.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

public class SystemInhibitor(ILoginManager loginManager, ILogger<SystemInhibitor> logger) : ISystemInhibitor
{
    private IDisposable? _inhibitor;

    public async Task BlockAsync(string reason)
    {
        if (string.IsNullOrEmpty(reason))
        {
            throw new ArgumentException("Reason cannot be null or empty", nameof(reason));
        }

        try
        {
            var fd = await loginManager.InhibitAsync("shutdown:sleep", "RemoteMaster", reason, "block");

            _inhibitor = fd;

            logger.LogInformation("System inhibit applied: {Reason}", reason);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to apply system inhibit: {Message}", ex.Message);

            throw;
        }
    }

    public void Unblock()
    {
        _inhibitor?.Dispose();

        logger.LogInformation("System inhibit released.");
    }

    public void Dispose()
    {
        Unblock();
    }
}
