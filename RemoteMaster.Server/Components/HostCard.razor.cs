// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Components;

public partial class HostCard : ComponentBase
{
    [Parameter]
    public HostDto Host { get; set; } = default!;

    [Parameter]
    public bool IsSelected { get; set; }

    private string ThumbnailUri => Host.Thumbnail != null ? $"data:image/png;base64,{Convert.ToBase64String(Host.Thumbnail)}" : "/img/notconnected.png";

    [Parameter]
    public EventCallback<bool> IsSelectedChanged { get; set; }

    private async Task HandleCheckboxChange(ChangeEventArgs e)
    {
        if (e.Value is bool isChecked)
        {
            IsSelected = isChecked;

            await IsSelectedChanged.InvokeAsync(IsSelected);
        }
    }
}
