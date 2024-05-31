// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class EditUsers
{
    private List<UserViewModel> _users = [];
    private List<OrganizationViewModel> _organizations = [];

    private string? SelectedUserId { get; set; }

    private UserEditModel SelectedUserModel { get; set; } = new();

    private bool ShowSuccessMessage { get; set; } = false;

    protected async override Task OnInitializedAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await LoadUsersAsync(dbContext);
        await LoadOrganizationsAsync(dbContext);
    }

    private async Task LoadUsersAsync(ApplicationDbContext dbContext)
    {
        _users = await dbContext.Users
            .Select(u => new UserViewModel
            {
                Id = u.Id,
                UserName = u.UserName
            })
            .ToListAsync();
    }

    private async Task LoadOrganizationsAsync(ApplicationDbContext dbContext)
    {
        _organizations = await dbContext.Organizations
            .Include(o => o.OrganizationalUnits)
            .Select(o => new OrganizationViewModel
            {
                Id = o.OrganizationId,
                Name = o.Name,
                OrganizationalUnits = o.OrganizationalUnits.Select(ou => new OrganizationalUnitViewModel
                {
                    Id = ou.NodeId,
                    Name = ou.Name
                }).ToList()
            }).ToListAsync();
    }

    private async Task OnValidSubmitAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await dbContext.Users
            .Include(u => u.AccessibleOrganizations)
            .Include(u => u.AccessibleOrganizationalUnits)
            .FirstOrDefaultAsync(u => u.Id == SelectedUserId);

        if (user == null)
        {
            return;
        }

        var selectedOrganizationIds = _organizations.Where(o => o.IsSelected).Select(o => o.Id).ToList();
        var selectedUnitIds = _organizations.SelectMany(o => o.OrganizationalUnits).Where(ou => ou.IsSelected).Select(ou => ou.Id).ToList();

        user.AccessibleOrganizations.Clear();
        user.AccessibleOrganizationalUnits.Clear();

        foreach (var orgId in selectedOrganizationIds)
        {
            var organization = await dbContext.Organizations.FindAsync(orgId);

            if (organization != null)
            {
                user.AccessibleOrganizations.Add(organization);
            }
        }

        foreach (var unitId in selectedUnitIds)
        {
            var unit = await dbContext.OrganizationalUnits.FindAsync(unitId);

            if (unit != null)
            {
                user.AccessibleOrganizationalUnits.Add(unit);
            }
        }

        await dbContext.SaveChangesAsync();

        ShowSuccessMessage = true;
        StateHasChanged();

        _ = Task.Delay(3000).ContinueWith(async _ =>
        {
            ShowSuccessMessage = false;
            await InvokeAsync(StateHasChanged);
        });

        NavigationManager.Refresh();
    }

    private async Task LoadCurrentUserAccess(ApplicationDbContext dbContext)
    {
        if (string.IsNullOrEmpty(SelectedUserId))
        {
            return;
        }

        var user = await dbContext.Users
            .Include(u => u.AccessibleOrganizations)
            .Include(u => u.AccessibleOrganizationalUnits)
            .FirstOrDefaultAsync(u => u.Id == SelectedUserId);

        if (user == null)
        {
            return;
        }

        foreach (var organization in _organizations)
        {
            organization.IsSelected = user.AccessibleOrganizations.Any(ao => ao.OrganizationId == organization.Id);

            foreach (var unit in organization.OrganizationalUnits)
            {
                unit.IsSelected = user.AccessibleOrganizationalUnits.Any(aou => aou.NodeId == unit.Id);
            }
        }

        StateHasChanged();
    }

    private async Task OnUserChanged(string userId)
    {
        SelectedUserId = userId;
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await LoadCurrentUserAccess(dbContext);
    }

    private static void SelectAllOrganizationalUnits(OrganizationViewModel organization)
    {
        foreach (var unit in organization.OrganizationalUnits)
        {
            unit.IsSelected = true;
        }
    }

    private static void DeselectAllOrganizationalUnits(OrganizationViewModel organization)
    {
        foreach (var unit in organization.OrganizationalUnits)
        {
            unit.IsSelected = false;
        }
    }

    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;
    }

    public class OrganizationViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsSelected { get; set; }

#pragma warning disable CA2227
        public List<OrganizationalUnitViewModel> OrganizationalUnits { get; set; } = [];
#pragma warning restore CA2227
    }

    public class OrganizationalUnitViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsSelected { get; set; }
    }

    public class UserEditModel
    {
#pragma warning disable CA2227
        public List<Guid> SelectedOrganizations { get; set; } = [];

        public List<Guid> SelectedOrganizationalUnits { get; set; } = [];
#pragma warning restore CA2227
    }
}
