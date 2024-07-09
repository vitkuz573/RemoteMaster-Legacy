// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components;

public partial class ComputerCard : ComponentBase
{
    [Parameter]
    public Computer Computer { get; set; } = default!;

    [Parameter]
    public bool IsSelected { get; set; }

    private string ThumbnailUri => Computer.Thumbnail != null ? $"data:image/png;base64,{Convert.ToBase64String(Computer.Thumbnail)}" : "/img/notconnected.png";

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