// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.SignalR;
using Windows.Win32;

namespace RemoteMaster.Agent.Core.Hubs;

public class MaintenanceHub : Hub
{
    [SuppressMessage("Performance", "CA1822:Пометьте члены как статические", Justification = "<Ожидание>")]
    public async Task SendCtrlAltDel()
    {
        PInvoke.SendSAS(true);
        PInvoke.SendSAS(false);
    }
}
