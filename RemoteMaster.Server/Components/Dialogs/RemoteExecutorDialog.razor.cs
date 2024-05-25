// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Management.Automation;
using System.Net;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class RemoteExecutorDialog
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
        var securePassword = new NetworkCredential("", _password).SecurePassword;
        var credential = new PSCredential(_username, securePassword);

        HostService.DeployAndExecute(_localFilePath, _remoteFilePath, _host, credential, $"--launch-mode={_launchMode}");
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
