// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Models;

public class UpdaterAbstractService : AbstractService
{
    private const string MainAppName = "RemoteMaster";
    private const string SubAppName = "Updater";

    public override string Name => "RCUpdater";
    
    protected override string DisplayName => "RemoteMaster Update Service";

    protected override string BinPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), MainAppName, SubAppName, $"{MainAppName}.{SubAppName}.exe");

    protected override IDictionary<string, string?> Arguments { get; } = new Dictionary<string, string?> { { "--launch-mode", "Updater" } };

    protected override string Description => "RemoteMaster Update Service is designed for manual initiation from the managing host to check for software updates, download, and install them, ensuring the system remains up-to-date with all security patches and improvements.";

    protected override string StartType => "manual";

    protected override IEnumerable<string>? Dependencies => null;

    protected override int ResetPeriod => 0;

    protected override string FirstFailureAction => "none";

    protected override string SecondFailureAction => "none";

    protected override string SubsequentFailuresAction => "none";

    protected override string? RebootMessage => null;

    protected override string? RestartCommand => null;
}
