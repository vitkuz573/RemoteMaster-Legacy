// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

#pragma warning disable CA2227

public class CommonDialogBase : ComponentBase
{
    [CascadingParameter]
    protected MudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public ConcurrentDictionary<Computer, HubConnection?> Hosts { get; set; } = default!;

    [Parameter]
    public string ContentStyle { get; set; } = default!;

    [Parameter]
    public RenderFragment Content { get; set; } = default!;

    [Parameter]
    public RenderFragment Actions { get; set; } = default!;

    protected void Cancel()
    {
        MudDialog.Cancel();
    }
}
