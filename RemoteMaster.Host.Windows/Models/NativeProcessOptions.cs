// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Windows.Models;

public struct NativeProcessOptions() : INativeProcessOptions
{
    public int? SessionId { get; set; } = null;

    public bool ForceConsoleSession { get; set; } = true;

    public string DesktopName { get; set; } = "Default";

    public bool UseCurrentUserToken { get; set; } = false;
}
