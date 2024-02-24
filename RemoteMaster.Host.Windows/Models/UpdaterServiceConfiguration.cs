// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Windows.Abstractions;
using System.IO;
using System.Collections.Generic;

namespace RemoteMaster.Host.Windows.Models;

public class UpdaterServiceConfiguration : IServiceConfiguration
{
    private const string MainAppName = "RemoteMaster";
    private const string SubAppName = "Updater";

    public string Name => "RCUpdater";
    
    public string DisplayName => "RemoteMaster Update Service";
    
    public string BinPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), MainAppName, SubAppName, $"{MainAppName}.{SubAppName}.exe");
    
    public IDictionary<string, string?> Arguments { get; } = new Dictionary<string, string?> { { "--launch-mode", "Updater" } };

    public string Description => "RemoteMaster Update Service is designed for manual initiation from the managing host to check for software updates, download, and install them, ensuring the system remains up-to-date with all security patches and improvements.";

    public string StartType => "manual";
    
    public IEnumerable<string>? Dependencies => null;
    
    public int ResetPeriod => 0;
    
    public string FirstFailureAction => "none";
    
    public string SecondFailureAction => "none";
    
    public string SubsequentFailuresAction => "none";
    
    public string? RebootMessage => null;
    
    public string? RestartCommand => null;
}
