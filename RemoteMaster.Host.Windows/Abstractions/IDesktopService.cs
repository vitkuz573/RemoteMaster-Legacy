// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;

namespace RemoteMaster.Host.Windows.Abstractions;

public interface IDesktopService
{
    bool GetCurrentDesktop([NotNullWhen(true)] out string? desktopName);

    bool SwitchToInputDesktop();
}
