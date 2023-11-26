// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;
using RemoteMaster.Server.Components.Dialogs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Panels;

public partial class HomePanel
{
    [Parameter]
    public object Content { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; }

    private async void OpenHostConfigurationGenerator(MouseEventArgs e)
    {
        var hostConfiguration = new HostConfiguration();

        await DialogService.ShowDialogAsync<HostConfigurationGenerator>(hostConfiguration, new DialogParameters()
        {
            Height = "240px",
            Title = $"Host Configuration Generator",
            TrapFocus = false,
            PreventDismissOnOverlayClick = true,
            PreventScroll = true
        });
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Пометьте члены как статические", Justification = "<Ожидание>")]
    private void Logout(MouseEventArgs e)
    {
    }
}
