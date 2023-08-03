// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.

using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Client.Services;

public class ControlFunctionsService
{
    public HubConnection ServerConnection { get; set; }

    public IEnumerable<DisplayInfo> Displays { get; set; }
}
