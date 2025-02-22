// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using RemoteMaster.Server.Aggregates.HostMoveRequestAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class MoveHostsDialog
{
    [Parameter]
    public EventCallback<IEnumerable<HostDto>> OnHostsMoved { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    private string _currentOrganizationName = string.Empty;
    private string _currentOrganizationalUnitName = string.Empty;
    private List<Organization> _organizations = [];
    private List<OrganizationalUnit> _organizationalUnits = [];
    private Guid _selectedOrganizationId = Guid.Empty;
    private Guid? _selectedOrganizationalUnitId;

    protected async override Task OnInitializedAsync()
    {
        await LoadUserOrganizationsAsync();

        if (!Hosts.IsEmpty)
        {
            var firstHostParentId = Hosts.First().Key.OrganizationalUnitId;
            var organization = await ApplicationUnitOfWork.Organizations.GetOrganizationByUnitIdAsync(firstHostParentId);

            var currentOrganizationalUnit = organization?.OrganizationalUnits.FirstOrDefault(ou => ou.Id == firstHostParentId);

            if (currentOrganizationalUnit != null && organization != null)
            {
                _selectedOrganizationalUnitId = currentOrganizationalUnit.Id;
                _currentOrganizationalUnitName = currentOrganizationalUnit.Name;

                _currentOrganizationName = organization.Name;
                _selectedOrganizationId = organization.Id;

                await LoadOrganizationalUnits(organization.Id);
            }
        }
    }

    private async Task LoadUserOrganizationsAsync()
    {
        var authState = await AuthenticationStateTask;
        var user = authState.User;

        if (user.Identity!.IsAuthenticated)
        {
            var username = user.Identity.Name;
            var appUser = await UserManager.Users
                .Include(u => u.UserOrganizations)
                .ThenInclude(uo => uo.Organization)
                .FirstOrDefaultAsync(u => u.UserName == username);

            if (appUser != null)
            {
                _organizations = appUser.UserOrganizations
                    .Select(uo => uo.Organization)
                    .ToList();
            }
        }
    }

    private async Task LoadOrganizationalUnits(Guid organizationId)
    {
        var authState = await AuthenticationStateTask;
        var user = authState.User;

        if (user.Identity!.IsAuthenticated)
        {
            var username = user.Identity.Name;
            var appUser = await UserManager.Users
                .Include(u => u.UserOrganizationalUnits)
                .ThenInclude(uou => uou.OrganizationalUnit)
                .FirstOrDefaultAsync(u => u.UserName == username);

            if (appUser != null)
            {
                var organization = await ApplicationUnitOfWork.Organizations.GetByIdAsync(organizationId);
                
                if (organization != null)
                {
                    _organizationalUnits = [.. organization.OrganizationalUnits.Where(ou => appUser.UserOrganizationalUnits.Any(uou => uou.OrganizationalUnitId == ou.Id))];
                }
            }
        }
    }

    private async Task OrganizationChanged(Guid organizationId)
    {
        _selectedOrganizationId = organizationId;
        _selectedOrganizationalUnitId = null;

        await LoadOrganizationalUnits(organizationId);

        StateHasChanged();
    }

    private async Task MoveHost()
    {
        if (_selectedOrganizationalUnitId.HasValue && _selectedOrganizationalUnitId != Guid.Empty)
        {
            var targetOrganization = await ApplicationUnitOfWork.Organizations.GetByIdAsync(_selectedOrganizationId);

            if (targetOrganization == null)
            {
                MudDialog.Close(DialogResult.Cancel());
                
                return;
            }

            var newParentUnit = targetOrganization.OrganizationalUnits.FirstOrDefault(u => u.Id == _selectedOrganizationalUnitId.Value) ?? throw new InvalidOperationException("New parent Organizational Unit not found.");
            var targetOrganizationalUnitsPath = await OrganizationalUnitService.GetFullPathAsync(newParentUnit.Id);

            if (targetOrganizationalUnitsPath.Count == 0)
            {
                throw new InvalidOperationException("Failed to get full path of the new parent.");
            }

            var unavailableHosts = new List<HostDto>();

            foreach (var host in Hosts)
            {
                if (host.Value != null)
                {
                    var hostMoveRequest = new HostMoveRequest(host.Key.MacAddress, targetOrganization.Name, targetOrganizationalUnitsPath);
                    
                    await host.Value.InvokeAsync("MoveHost", hostMoveRequest);
                }
                else
                {
                    unavailableHosts.Add(host.Key);
                }

                var currentParentUnitId = host.Key.OrganizationalUnitId;
                
                await ApplicationUnitOfWork.Organizations.MoveHostAsync(host.Key.OrganizationId, _selectedOrganizationId, host.Key.Id, currentParentUnitId, newParentUnit.Id);
            }

            await ApplicationUnitOfWork.CommitAsync();

            await OnHostsMoved.InvokeAsync(Hosts.Keys);

            if (unavailableHosts.Count > 0)
            {
                await AppendHostMoveRequests(unavailableHosts, targetOrganization.Name, targetOrganizationalUnitsPath);
            }

            MudDialog.Close(DialogResult.Ok(true));
        }
    }

    private async Task AppendHostMoveRequests(List<HostDto> unavailableHosts, string targetOrganization, List<string> targetOrganizationalUnits)
    {
        foreach (var host in unavailableHosts)
        {
            var existingRequest = (await HostMoveRequestUnitOfWork.HostMoveRequests.FindAsync(r => r.MacAddress.Equals(host.MacAddress))).FirstOrDefault();

            if (existingRequest != null)
            {
                existingRequest.SetOrganization(targetOrganization);
                existingRequest.SetOrganizationalUnit(targetOrganizationalUnits);

                HostMoveRequestUnitOfWork.HostMoveRequests.Update(existingRequest);
            }
            else
            {
                await HostMoveRequestUnitOfWork.HostMoveRequests.AddAsync(new HostMoveRequest(host.MacAddress, targetOrganization, targetOrganizationalUnits));
            }

            await HostMoveRequestUnitOfWork.CommitAsync();
        }
    }
}
