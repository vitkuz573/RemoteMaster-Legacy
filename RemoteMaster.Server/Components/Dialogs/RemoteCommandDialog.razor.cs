// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class RemoteCommandDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    private string _host = string.Empty;
    private string _localFilePath = string.Empty;
    private string _command = "install";
    private string _arguments = string.Empty;

    private void Confirm()
    {
        var result = RemoteExecutionService.ExecuteApplication(_host, _localFilePath, $"{_command} {_arguments}");

        if (result.IsSuccess)
        {
            SnackBar.Add("Remote command executed successfully.", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            var errorMessage = result.Errors.FirstOrDefault()?.Message ?? "Unknown error occurred.";
            SnackBar.Add($"Failed to execute remote command: {errorMessage}", Severity.Error);
        }
    }

    private void Cancel() => MudDialog.Close(DialogResult.Cancel());
}
