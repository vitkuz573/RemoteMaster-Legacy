// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.DirectoryServices;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components;

#pragma warning disable CA2227

public partial class DomainManagementDialog
{
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; }

    [Parameter]
    public Dictionary<Computer, HubConnection> Hosts { get; set; }

    [Inject]
    private IComputerCommandService ComputerCommandService { get; set; }

    private bool _isShowPassword;
    private InputType _passwordInput = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
    private string _domain;
    private string _username;
    private string _password;

    protected override void OnInitialized()
    {
        _domain = GetDomainName();
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }

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

    private static string GetDomainName()
    {
        try
        {
            using var rootDSE = new DirectoryEntry("LDAP://RootDSE");
            var ldapDomain = (string)rootDSE.Properties["defaultNamingContext"].Value;
            
            return ConvertLdapDomainToNormal(ldapDomain);
        }
        catch (COMException)
        {
            return string.Empty;
        }
    }

    private static string ConvertLdapDomainToNormal(string ldapDomain)
    {
        var parts = ldapDomain.Split(',');

        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = parts[i].Replace("DC=", "");
        }

        return string.Join('.', parts);
    }
}
