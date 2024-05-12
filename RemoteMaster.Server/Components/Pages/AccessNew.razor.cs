// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Dialogs;
using RemoteMaster.Server.Components.Library.Models;

namespace RemoteMaster.Server.Components.Pages;

public partial class AccessNew
{
    [Parameter]
    public string Host { get; set; }

    private bool FeatureXEnabled = true;
    private bool FeatureYEnabled = false;
    private bool RemoteAccessEnabled = true;
    private bool LocalAccessEnabled = false;
    
    private void OnFeatureXChanged(bool isChecked) => FeatureXEnabled = isChecked;

    private void OnFeatureYChanged(bool isChecked) => FeatureYEnabled = isChecked;

    private void OnRemoteAccessChanged(bool isChecked) => RemoteAccessEnabled = isChecked;

    private void OnLocalAccessChanged(bool isChecked) => LocalAccessEnabled = isChecked;

    private async Task ShowInformationDialog()
    {
        var parameters = new DialogParameters<MyDialog>
        {
            { x => x.Param, "Param Value" }
        };

        var options = new DialogOptions
        {
            BackdropClick = true,
            NoHeader = false,
            FullScreen = true,
        };

        await DialogService.ShowAsync<MyDialog>("Header", parameters, options);
    }

    private async Task ShowMessageBox()
    {
        await DialogService.ShowMessageBox(new MessageBoxOptions()
        {
            Title = "messagebox",
            Message = "test",
            NoText = "No",
            CancelText = "Cancel"
        });
    }
}
