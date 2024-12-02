// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.LaunchModes;

namespace RemoteMaster.Host.Core.Services;

public class LaunchModeProvider : ILaunchModeProvider
{
    private readonly IReadOnlyDictionary<string, LaunchModeBase> _modes = new Dictionary<string, LaunchModeBase>(StringComparer.OrdinalIgnoreCase)
    {
        { "User", new UserMode() },
        { "Service", new ServiceMode() },
        { "Install", new InstallMode() },
        { "Updater", new UpdaterMode() },
        { "Uninstall", new UninstallMode() },
        { "Reinstall", new ReinstallMode() },
        { "Chat", new ChatMode() }
    };

    public IReadOnlyDictionary<string, LaunchModeBase> GetAvailableModes() => _modes;
}
