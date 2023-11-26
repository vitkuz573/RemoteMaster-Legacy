// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class HostConfigurationGenerator
{
    [Parameter]
    public HostConfiguration Content { get; set; } = default!;

    [CascadingParameter]
    public FluentDialog Dialog { get; set; } = default!;

    private async Task GenerateConfiguration()
    {
        await Dialog.CloseAsync();
    }

    private async Task DownloadHost()
    {
        await Dialog.CloseAsync();
    }
}
