﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Enums;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class BootToWimDialog
{
    private bool _isShowPassword;
    private InputType _passwordInput = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

    private string _folderPath = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;

    private string _loaderCommand = string.Empty;
    private string _wimFile = string.Empty;

    protected override void OnInitialized()
    {
        _folderPath = Options.Value.FolderPath;
        _loaderCommand = Options.Value.LoaderCommand;
        _wimFile = Options.Value.WimFile;
        _username = Options.Value.Username;
        _password = Options.Value.Password;
    }

    private async Task Boot()
    {
        try
        {
            var scriptBuilder = new StringBuilder();

            scriptBuilder.AppendLine("net use * /delete /y");
            scriptBuilder.AppendLine($"net use {_folderPath} /USER:{_username} {_password}");
            scriptBuilder.AppendLine($@"copy {_folderPath}\{_wimFile} C:\ /Y");

            var loaderCommand = _loaderCommand
                .Replace("{folderPath}", _folderPath)
                .Replace("{wimFile}", FileSystem.Path.GetFileName(_wimFile));

            scriptBuilder.AppendLine(loaderCommand);

            var scriptExecutionRequest = new ScriptExecutionRequest(scriptBuilder.ToString(), Shell.Cmd)
            {
                AsSystem = true
            };

            await HostCommandService.ExecuteAsync(Hosts, async (_, connection) => await connection!.InvokeAsync("ExecuteScript", scriptExecutionRequest));
        }
        catch (Exception)
        {
            // ignored
        }

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
