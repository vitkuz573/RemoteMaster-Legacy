// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using RemoteMaster.Server.Entities;
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
            var currentOrganizationalUnitResult = await NodesService.GetNodesAsync<OrganizationalUnit>(node => node.Id == firstHostParentId);

            if (currentOrganizationalUnitResult.IsSuccess && currentOrganizationalUnitResult.Value.Any())
            {
                var currentOU = currentOrganizationalUnitResult.Value.First();

                _selectedOrganizationalUnitId = currentOU.Id;
                _currentOrganizationalUnitName = currentOU.Name;

                var currentOrganization = _organizations.FirstOrDefault(org => org.Id == currentOU.OrganizationId);

                if (currentOrganization != null)
                {
                    _currentOrganizationName = currentOrganization.Name;
                    _selectedOrganizationId = currentOrganization.Id;

                    await LoadOrganizationalUnits(currentOrganization.Id);
                }
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
                _organizationalUnits = appUser.UserOrganizationalUnits
                    .Where(uou => uou.OrganizationalUnit.OrganizationId == organizationId)
                    .Select(uou => uou.OrganizationalUnit)
                    .ToList();
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
        if (_selectedOrganizationalUnitId != Guid.Empty)
        {
            var targetOrganizationResult = await NodesService.GetNodesAsync<Organization>(o => o.Id == _selectedOrganizationId);

            if (!targetOrganizationResult.IsSuccess || !targetOrganizationResult.Value.Any())
            {
                MudDialog.Close(DialogResult.Cancel());
                return;
            }

            var targetOrganization = targetOrganizationResult.Value.First().Name;

            var newParentResult = await NodesService.GetNodesAsync<OrganizationalUnit>(ou => ou.Id == _selectedOrganizationalUnitId);
            
            if (!newParentResult.IsSuccess || !newParentResult.Value.Any())
            {
                throw new InvalidOperationException("New parent Organizational Unit not found.");
            }

            var newParent = newParentResult.Value.First();
            var targetOrganizationalUnitsPathResult = await NodesService.GetFullPathAsync(newParent);
            
            if (!targetOrganizationalUnitsPathResult.IsSuccess)
            {
                throw new InvalidOperationException("Failed to get full path of the new parent.");
            }

            var targetOrganizationalUnitsPath = targetOrganizationalUnitsPathResult.Value;

            if (targetOrganizationalUnitsPath.Length > 0)
            {
                var nodeIds = Hosts.Select(host => host.Key.Id);
                var unavailableHosts = new List<Computer>();

                foreach (var host in Hosts)
                {
                    if (host.Value != null)
                    {
                        var hostMoveRequest = new HostMoveRequest(host.Key.MacAddress, targetOrganization, targetOrganizationalUnitsPath);

                        await host.Value.InvokeAsync("MoveHost", hostMoveRequest);
                    }
                    else
                    {
                        unavailableHosts.Add(host.Key);
                    }
                }

                if (unavailableHosts.Count != 0)
                {
                    await AppendHostMoveRequests(unavailableHosts, targetOrganization, targetOrganizationalUnitsPath);
                }

                foreach (var nodeId in nodeIds)
                {
                    var nodeResult = await NodesService.GetNodesAsync<Computer>(c => c.Id == nodeId);

                    if (!nodeResult.IsSuccess || !nodeResult.Value.Any())
                    {
                        continue;
                    }

                    var moveNodeResult = await NodesService.MoveNodeAsync(nodeResult.Value.First(), newParent);
                    
                    if (!moveNodeResult.IsSuccess)
                    {
                        throw new InvalidOperationException("Failed to move node.");
                    }
                }
            }

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
