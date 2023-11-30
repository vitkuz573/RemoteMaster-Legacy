// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components;

public partial class ComputerCard
{
    [Parameter]
    public Computer Computer { get; set; } = default!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = default!;

    private string ThumbnailPath => Computer.Thumbnail != null ? $"data:image/png;base64,{Convert.ToBase64String(Computer.Thumbnail)}" : "/img/notconnected.png";

    [Parameter]
    public bool IsSelected { get; set; }

    [Parameter]
    public EventCallback<bool> IsSelectedChanged { get; set; }

    private async Task HandleCheckboxChange()
    {
        IsSelected = !IsSelected;

        await IsSelectedChanged.InvokeAsync(IsSelected);
    }
}
