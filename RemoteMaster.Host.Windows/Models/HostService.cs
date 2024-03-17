// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Models;

public class HostService : AbstractService
{
    public override string Name => "RCHost";

    protected override string DisplayName => "RemoteMaster Control Service";

    protected override string BinPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "RemoteMaster.Host.exe");

    protected override IDictionary<string, string?> Arguments { get; } = new Dictionary<string, string?>
    {
        { "--launch-mode", "service" }
    };

    protected override string Description => "RemoteMaster Control Service enables advanced remote management and control functionalities for authorized clients. It provides seamless access to system controls, resource management, and real-time support capabilities, ensuring efficient and secure remote operations.";

    protected override ServiceStartType StartType => ServiceStartType.Auto;

    protected override IEnumerable<string>? Dependencies => null;

    protected override int ResetPeriod => 86400;

    protected override FailureAction FirstFailureAction => FailureAction.Create(ServiceFailureActionType.Restart, 60000);

    protected override FailureAction SecondFailureAction => FailureAction.Create(ServiceFailureActionType.Restart, 60000);

    protected override FailureAction SubsequentFailuresAction => FailureAction.Create(ServiceFailureActionType.Restart, 60000);

    protected override string? RebootMessage => null;

    protected override string? RestartCommand => null;
}
