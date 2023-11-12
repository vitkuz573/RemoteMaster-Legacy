// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Components;

public partial class UpdateDialog
{
    [Inject]
    private IComputerCommandService ComputerCommandService { get; set; }

    private bool _isShowPassword;
    private InputType _passwordInput = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
    private string _folderPath;
    private string _username;
    private string _password;

    private async Task Confirm()
    {
        var isLocalFolder = !_folderPath.StartsWith("\\\\");

        await ComputerCommandService.Execute(Hosts, async (computer, connection) => await connection.InvokeAsync("SendUpdateHost", _folderPath, _username, _password, isLocalFolder));

        MudDialog.Close(DialogResult.Ok(true));
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
}
