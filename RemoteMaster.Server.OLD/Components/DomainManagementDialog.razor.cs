// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.DirectoryServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Components;

public partial class DomainManagementDialog
{
    [Inject]
    private ISnackbar Snackbar { get; set; }

    [Inject]
    private IComputerCommandService ComputerCommandService { get; set; }

    private bool _isShowPassword;
    private InputType _passwordInput = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
    private string? _domain;
    private string? _username;
    private string? _password;

    private async Task JoinDomain()
    {
        await ComputerCommandService.Execute(Hosts, async (computer, connection) => await connection.InvokeAsync("SendJoinToDomain", _domain, _username, _password));

        MudDialog.Close(DialogResult.Ok(true));
    }

    private async Task LeaveDomain()
    {
        await ComputerCommandService.Execute(Hosts, async (computer, connection) => await connection.InvokeAsync("SendUnjoinFromDomain", _username, _password));

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

    [SupportedOSPlatform("windows")]
    private void DomainDiscovery()
    {
        try
        {
            using var rootDSE = new DirectoryEntry("LDAP://RootDSE");
            var ldapDomain = (string)rootDSE.Properties["defaultNamingContext"].Value!;
            
            if (ldapDomain != null)
            {
                _domain = ldapDomain.Replace("DC=", "").Replace(',', '.');
            }
        }
        catch (COMException ex)
        {
            Snackbar.Add($"Error during domain discovery: {ex.Message}", Severity.Error);

            Log.Error(ex, "Error during domain discovery.");
        }
    }
}
