// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class MoveDialog
{
    [Parameter]
    public EventCallback<IEnumerable<Computer>> OnNodesMoved { get; set; }

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
            var firstHostParentId = Hosts.First().Key.ParentId;
            var organization = await OrganizationRepository.GetOrganizationByUnitIdAsync(firstHostParentId);

            var currentOrganizationalUnit = organization?.OrganizationalUnits.FirstOrDefault(ou => ou.Id == firstHostParentId);

            if (currentOrganizationalUnit != null)
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

        if (user.Identity.IsAuthenticated)
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

        if (user.Identity.IsAuthenticated)
        {
            var username = user.Identity.Name;
            var appUser = await UserManager.Users
                .Include(u => u.UserOrganizationalUnits)
                .ThenInclude(uou => uou.OrganizationalUnit)
                .FirstOrDefaultAsync(u => u.UserName == username);

            if (appUser != null)
            {
                var organization = await OrganizationRepository.GetByIdAsync(organizationId);
                
                if (organization != null)
                {
                    _organizationalUnits = organization.OrganizationalUnits
                        .Where(ou => appUser.UserOrganizationalUnits.Any(uou => uou.OrganizationalUnitId == ou.Id))
                        .ToList();
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
            var targetOrganization = await OrganizationRepository.GetByIdAsync(_selectedOrganizationId);

            if (targetOrganization == null)
            {
                MudDialog.Close(DialogResult.Cancel());

                return;
            }

            var newParentUnit = targetOrganization.OrganizationalUnits.FirstOrDefault(u => u.Id == _selectedOrganizationalUnitId.Value);
            
            if (newParentUnit == null)
            {
                throw new InvalidOperationException("New parent Organizational Unit not found.");
            }

            var targetOrganizationalUnitsPath = await OrganizationalUnitService.GetFullPathAsync(newParentUnit.Id);

            if (targetOrganizationalUnitsPath.Length == 0)
            {
                throw new InvalidOperationException("Failed to get full path of the new parent.");
            }

            var unavailableHosts = new List<Computer>();

            foreach (var host in Hosts)
            {
                if (host.Value != null)
                {
                    var hostMoveRequest = new HostMoveRequest(
                        host.Key.MacAddress,
                        targetOrganization.Name,
                        targetOrganizationalUnitsPath
                    );

                    await host.Value.InvokeAsync("MoveHost", hostMoveRequest);
                }
                else
                {
                    unavailableHosts.Add(host.Key);
                }
            }

            if (unavailableHosts.Count > 0)
            {
                await AppendHostMoveRequests(unavailableHosts, targetOrganization.Name, targetOrganizationalUnitsPath);
            }

            foreach (var computer in unavailableHosts)
            {
                var currentParentUnit = computer.Parent;

                currentParentUnit.MoveComputerToUnit(computer.Id, newParentUnit);
            }

            await OrganizationRepository.UpdateAsync(targetOrganization);
            await OrganizationRepository.SaveChangesAsync();

            await OnNodesMoved.InvokeAsync(Hosts.Keys);

            MudDialog.Close(DialogResult.Ok(true));
        }
    }

    private static async Task AppendHostMoveRequests(List<Computer> unavailableHosts, string targetOrganization, string[] targetOrganizationalUnits)
    {
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var applicationData = Path.Combine(programDataPath, "RemoteMaster", "Server");

        if (!Directory.Exists(applicationData))
        {
            Directory.CreateDirectory(applicationData);
        }

        var hostMoveRequestsFilePath = Path.Combine(applicationData, "HostMoveRequests.json");

        List<HostMoveRequest> hostMoveRequests;

        if (File.Exists(hostMoveRequestsFilePath))
        {
            var existingJson = await File.ReadAllTextAsync(hostMoveRequestsFilePath);
            hostMoveRequests = JsonSerializer.Deserialize<List<HostMoveRequest>>(existingJson) ?? [];
        }
        else
        {
            hostMoveRequests = [];
        }

        foreach (var host in unavailableHosts)
        {
            var existingRequest = hostMoveRequests.FirstOrDefault(r => r.MacAddress == host.MacAddress);

            if (existingRequest != null)
            {
                existingRequest.NewOrganization = targetOrganization;
                existingRequest.NewOrganizationalUnit = targetOrganizationalUnits;
            }
            else
            {
                hostMoveRequests.Add(new HostMoveRequest(host.MacAddress, targetOrganization, targetOrganizationalUnits));
            }
        }

        var json = JsonSerializer.Serialize(hostMoveRequests, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(hostMoveRequestsFilePath, json);
    }
}
