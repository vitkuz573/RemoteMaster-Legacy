// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class CommonDialogWrapper<TDialog> where TDialog : ComponentBase
{
#pragma warning disable CA2227
    [Parameter]
    public ConcurrentDictionary<ComputerDto, HubConnection?> Hosts { get; set; } = default!;
#pragma warning restore CA2227

    [Parameter]
    public string HubPath { get; set; } = default!;

    [Parameter]
    public bool StartConnection { get; set; }

    [Parameter]
    public bool RequireConnections { get; set; }

#pragma warning disable CA2227
    [Parameter]
    public IDictionary<string, object> AdditionalParameters { get; set; } = new Dictionary<string, object>();
#pragma warning restore CA2227
}
