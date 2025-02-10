// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ApplicationPathProvider : IApplicationPathProvider
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<ApplicationPathProvider> _logger;

    public string RootDirectory { get; private set; }
    
    public string DataDirectory { get; private set; }
    
    public string UpdaterDirectory => _fileSystem.Path.Combine(DataDirectory, "Updater");
    
    public string UpdateDirectory => _fileSystem.Path.Combine(DataDirectory, "Update");

    public ApplicationPathProvider(IFileSystem fileSystem, ILogger<ApplicationPathProvider> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;

        InitializePaths();
        CreateEssentialDirectories();
    }

    private void InitializePaths()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            InitializeWindowsPaths();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            InitializeLinuxPaths();
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported operating system.");
        }
    }

    private void InitializeWindowsPaths()
    {
        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var commonAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        RootDirectory = _fileSystem.Path.Combine(programFilesPath, "RemoteMaster", "Host");
        DataDirectory = _fileSystem.Path.Combine(commonAppDataPath, "RemoteMaster", "Host");
    }

    private void InitializeLinuxPaths()
    {
        // Define standard Linux directories
        // /opt is commonly used for add-on application software packages
        // /var/lib is used for variable state information
        RootDirectory = _fileSystem.Path.Combine("/opt", "RemoteMaster", "Host");
        DataDirectory = _fileSystem.Path.Combine("/var", "lib", "RemoteMaster", "Host");
    }

    private void CreateEssentialDirectories()
    {
        try
        {
            if (!_fileSystem.Directory.Exists(RootDirectory))
            {
                _fileSystem.Directory.CreateDirectory(RootDirectory);
                
                _logger.LogInformation("Created RootDirectory at {Path}.", RootDirectory);
            }

            if (_fileSystem.Directory.Exists(DataDirectory))
            {
                return;
            }

            _fileSystem.Directory.CreateDirectory(DataDirectory);
                
            _logger.LogInformation("Created DataDirectory at {Path}.", DataDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to create essential directories: {Message}", ex.Message);
            throw;
        }
    }
}
