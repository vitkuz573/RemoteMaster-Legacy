// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using RemoteMaster.Server.Entities;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class HostDialog
{
    [Parameter]
    public Computer Host { get; set; } = default!;

    private readonly HostInfo _model = new();

    protected override void OnInitialized()
    {
        _model.SetName(Host.Name);
        _model.SetIpAddress(Host.IpAddress);
        _model.SetMacAddress(Host.MacAddress);
    }

#pragma warning disable
    private async Task OnValidSubmit(EditContext context)
    {
    }
}
