// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Services;

public class UpdaterServiceConfigProvider
{
    public string ServiceName { get; } = "RCSUpdater";

    public string ServiceDisplayName { get; } = "Remote Control Updater";

    public string ServiceStartType { get; } = "auto";

    public IEnumerable<string>? ServiceDependencies { get; } = null;
}
