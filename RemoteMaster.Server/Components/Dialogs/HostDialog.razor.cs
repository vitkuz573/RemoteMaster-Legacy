// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Models;
using Host = RemoteMaster.Server.Aggregates.OrganizationAggregate.Host;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class HostDialog
{
    [Parameter]
    public Host Host { get; set; } = default!;

    private readonly HostInfo _model = new();

    protected override void OnInitialized()
    {
        _model.Name = Host.Name;
        _model.IpAddress = Host.IpAddress;
        _model.MacAddress = Host.MacAddress;
    }

#pragma warning disable
    private async Task OnValidSubmit(EditContext context)
    {
    }
}
