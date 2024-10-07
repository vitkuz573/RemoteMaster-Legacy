// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.DirectoryServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class DomainMembershipDialog
{
    private bool _isShowPassword;
    private InputType _passwordInput = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
    private string _domain = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;

    private async Task JoinDomain()
    {
        EnsureDomainInUsername();

        var credential = new Credentials(_username, _password);
        var domainJoinRequest = new DomainJoinRequest(_domain, credential);

        await HostCommandService.Execute(Hosts, async (_, connection) => await connection!.InvokeAsync("SendJoinToDomain", domainJoinRequest));

        MudDialog.Close(DialogResult.Ok(true));
    }

    private async Task LeaveDomain()
    {
        var credential = new Credentials(_username, _password);
        var domainUnjoinRequest = new DomainUnjoinRequest(credential);

        await HostCommandService.Execute(Hosts, async (_, connection) => await connection!.InvokeAsync("SendUnjoinFromDomain", domainUnjoinRequest));

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
            using var rootDse = new DirectoryEntry("LDAP://RootDSE");
            var ldapDomain = (string)rootDse.Properties["defaultNamingContext"].Value!;

            _domain = ldapDomain.Replace("DC=", "").Replace(',', '.');
        }
        catch (COMException ex)
        {
            Snackbar.Add($"Error during domain discovery: {ex.Message}", Severity.Error);

            Log.Error(ex, "Error during domain discovery.");
        }
    }

    private void EnsureDomainInUsername()
    {
        if (!string.IsNullOrWhiteSpace(_username) && !string.IsNullOrWhiteSpace(_domain) && !_username.Contains("@"))
        {
            _username += $"@{_domain}";
        }
    }
}
