// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class RemoteCommandDialog
{
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; }

    private string _localFilePath;
    private string _remoteFilePath;
    private string _host;
    private string _username;
    private string _password;
    private string _launchMode = "install";

    private bool _isShowPassword;
    private InputType _passwordInput = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

    private void Confirm()
    {
        var result = RemoteSchtasksService.CopyAndExecuteRemoteFile(_localFilePath, _host, _remoteFilePath, _username, _password, $"--launch-mode={_launchMode}");

        if (result.IsSuccess)
        {
            Snackbar.Add("Remote command executed successfully.", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            var errorMessage = result.Errors.FirstOrDefault()?.Message ?? "Unknown error occurred.";
            Snackbar.Add($"Failed to execute remote command: {errorMessage}", Severity.Error);
        }
    }

    private void TogglePasswordVisibility()
    {
        if (_isShowPassword)
        {
            _isShowPassword = false;
            _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
            _passwordInput = InputType.Password;
        }
        else
        {
            _isShowPassword = true;
            _passwordInputIcon = Icons.Material.Filled.Visibility;
            _passwordInput = InputType.Text;
        }
    }

    private void Cancel() => MudDialog.Close(DialogResult.Cancel());
}
