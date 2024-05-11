// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Dialogs;

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
        await DialogService.ShowAsync<MyDialog>("Header");
    }

    private async Task ShowConfirmationDialog()
    {
        var confirmed = await DialogService.ShowMessageBox("Confirm", "Are you sure?", "Yes", "No");
        // Process confirmation response
    }
}
