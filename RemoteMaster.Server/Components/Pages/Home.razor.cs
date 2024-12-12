// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Components.Dialogs;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Pages;

[Authorize]
public partial class Home
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    private List<UnifiedTreeItemData> _treeItems = [];

    private readonly List<HostDto> _selectedHosts = [];
    private readonly ConcurrentDictionary<IPAddress, HostDto> _availableHosts = new();
    private readonly ConcurrentDictionary<IPAddress, HostDto> _unavailableHosts = new();
    private readonly ConcurrentDictionary<IPAddress, HostDto> _pendingHosts = new();

    private ClaimsPrincipal? _user;
    private ApplicationUser? _currentUser;

    private IDictionary<NotificationMessage, bool>? _messages;

    private CancellationTokenSource? _logonCts;

    private string? _accessToken;

    protected async override Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateTask;

        _user = authState.User;

        var userId = _user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID is not found.");

        _currentUser = await UserManager.Users
            .Include(u => u.UserOrganizations)
            .ThenInclude(uo => uo.Organization)
            .Include(u => u.UserOrganizationalUnits)
            .ThenInclude(uou => uou.OrganizationalUnit)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (_currentUser == null)
        {
            return;
        }

        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (_currentUser == null)
        {
            return;
        }

        var nodes = await LoadNodes();

        _treeItems = nodes.Select(node => new UnifiedTreeItemData(node)).ToList();

        var accessTokenResult = await AccessTokenProvider.GetAccessTokenAsync(_currentUser.Id);

        _accessToken = accessTokenResult.IsSuccess ? accessTokenResult.Value : null;

        _messages = await NotificationService.GetNotifications();
    }

    private bool DrawerOpen { get; set; }

    private bool UserHasClaim(string claimType, string claimValue) => _user?.HasClaim(claimType, claimValue) ?? false;

    private bool UserHasAnyClaim(params string[] claimTypes)  => claimTypes.Any(ct => _user?.Claims.Any(claim => claim.Type == ct) ?? false);

    private async Task<IEnumerable<Organization>> LoadNodes()
    {
        if (_currentUser == null)
        {
            return [];
        }

        return await OrganizationService.GetOrganizationsWithAccessibleUnitsAsync(_currentUser.Id);
    }

    private void ToggleDrawer() => DrawerOpen = !DrawerOpen;

    private void OpenCertificateRenewTasks() => NavigationManager.NavigateTo("/certificates/tasks");

    private async Task PublishCrl()
    {
        var crlResult = await CrlService.GenerateCrlAsync();

        if (!crlResult.IsSuccess)
        {
            Logger.LogError("Failed to generate CRL: {Message}", crlResult.Errors.FirstOrDefault()?.Message);
            SnackBar.Add("Failed to generate CRL", Severity.Error);
            
            return;
        }

        var publishResult = await CrlService.PublishCrlAsync(crlResult.Value);

        if (publishResult.IsSuccess)
        {
            SnackBar.Add("CRL successfully published", Severity.Success);
        }
        else
        {
            Logger.LogError("Failed to publish CRL: {Message}", publishResult.Errors.FirstOrDefault()?.Message);
            SnackBar.Add("Failed to publish CRL", Severity.Error);
        }
    }

    private void ManageProfile() => NavigationManager.NavigateTo("/Account/Manage");

    private void Logout() => NavigationManager.NavigateTo("/Account/Logout");

    private async Task OnNodeSelected(object? node)
    {
        if (_logonCts != null)
        {
            try
            {
                await _logonCts.CancelAsync();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                _logonCts.Dispose();
            }
        }

        _logonCts = new CancellationTokenSource();

        _selectedHosts.Clear();
        _availableHosts.Clear();
        _unavailableHosts.Clear();
        _pendingHosts.Clear();

        if (node == null)
        {
            return;
        }

        switch (node)
        {
            case Organization:
                break;
            case OrganizationalUnit orgUnit:
                if (!orgUnit.Hosts.Any())
                {
                    return;
                }

                await LoadHosts(orgUnit, _logonCts.Token);
                break;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadHosts(OrganizationalUnit orgUnit, CancellationToken cancellationToken)
    {
        var hosts = orgUnit.Hosts.ToList();

        var newPendingHosts = new ConcurrentDictionary<IPAddress, HostDto>();

        foreach (var host in hosts.Where(host => !_availableHosts.ContainsKey(host.IpAddress) && !_unavailableHosts.ContainsKey(host.IpAddress)))
        {
            var hostDto = new HostDto(host.Name, host.IpAddress, host.MacAddress)
            {
                Id = host.Id,
                OrganizationId = host.Parent!.OrganizationId,
                OrganizationalUnitId = host.ParentId,
                Thumbnail = null
            };

            newPendingHosts.TryAdd(hostDto.IpAddress, hostDto);
        }

        _pendingHosts.Clear();

        foreach (var kvp in newPendingHosts)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _pendingHosts.TryAdd(kvp.Key, kvp.Value);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task<bool> ShowSslWarningDialog(IPAddress ipAddress, SslPolicyErrors sslPolicyErrors, CertificateInfo certificateInfo)
    {
        var parameters = new DialogParameters<SslWarningDialog>
        {
            { d => d.IpAddress, ipAddress },
            { d => d.SslPolicyErrors, sslPolicyErrors },
            { d => d.CertificateInfo, certificateInfo }
        };

        var dialog = await DialogService.ShowAsync<SslWarningDialog>("SSL Certificate Warning", parameters);
        var result = await dialog.Result;

        return !result?.Canceled ?? throw new InvalidOperationException("Result not found.");
    }

    private void SelectHost(HostDto hostDto, bool isSelected)
    {
        if (isSelected)
        {
            if (!_selectedHosts.Contains(hostDto))
            {
                _selectedHosts.Add(hostDto);
            }
        }
        else
        {
            _selectedHosts.Remove(hostDto);
        }

        InvokeAsync(StateHasChanged);
    }
}
