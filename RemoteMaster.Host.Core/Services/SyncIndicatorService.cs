// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class SyncIndicatorService : ISyncIndicatorService
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<SyncIndicatorService> _logger;

    private readonly string _syncIndicatorFilePath;

    public SyncIndicatorService(IFileSystem fileSystem, ILogger<SyncIndicatorService> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;

        _syncIndicatorFilePath = _fileSystem.Path.Combine(_fileSystem.Path.GetDirectoryName(Environment.ProcessPath)!, "sync_required.ind");
    }

    public bool IsSyncRequired()
    {
        return _fileSystem.File.Exists(_syncIndicatorFilePath);
    }

    public void SetSyncRequired()
    {
        try
        {
            _fileSystem.File.WriteAllText(_syncIndicatorFilePath, "Sync required");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create sync indicator file.");
        }
    }

    public void ClearSyncIndicator()
    {
        try
        {
            if (_fileSystem.File.Exists(_syncIndicatorFilePath))
            {
                _fileSystem.File.Delete(_syncIndicatorFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete sync indicator file.");
        }
    }
}
