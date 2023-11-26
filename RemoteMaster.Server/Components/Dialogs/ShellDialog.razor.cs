// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.FluentUI.AspNetCore.Components;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Dialogs;

#pragma warning disable CA2227

public partial class ShellDialog
{
    [CascadingParameter]
    public FluentDialog Dialog { get; set; } = default!;

    [Parameter]
    public Dictionary<Computer, HubConnection> Content { get; set; } = default!;

    [Inject]
    public IComputerCommandService ComputerCommandService { get; set; } = default!;

    private async Task Connect()
    {
        await Dialog.CloseAsync();
    }
}
