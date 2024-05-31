// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

[Authorize(Roles = "RootAdministrator")]
public partial class ManageOrganizationalUnits
{
    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    private List<Organization> _organizations = [];
    private List<OrganizationalUnit> _organizationalUnits = [];
    private List<UserViewModel> _users = [];
    private bool _showUserManagementModal = false;
    private OrganizationalUnit? _selectedOrganizationalUnit;

    protected override void OnInitialized()
    {
        LoadOrganizations();
        LoadOrganizationalUnits();
    }

    private async Task OnValidSubmitAsync()
    {
        OrganizationalUnit organizationalUnit;

        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (Input.Id.HasValue)
        {
            organizationalUnit = dbContext.OrganizationalUnits.Find(Input.Id.Value);

            if (organizationalUnit == null)
            {
                return;
            }

            organizationalUnit.Name = Input.Name;
            organizationalUnit.OrganizationId = Input.OrganizationId;
        }
        else
        {
            organizationalUnit = CreateOrganizationalUnit(Input.Name, Input.OrganizationId);
            await dbContext.OrganizationalUnits.AddAsync(organizationalUnit);
        }

        await dbContext.SaveChangesAsync();

        LoadOrganizationalUnits();

        NavigationManager.Refresh();

        Input = new InputModel();
    }

    private void LoadOrganizations()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _organizations = [.. dbContext.Organizations];
    }

    private void LoadOrganizationalUnits()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _organizationalUnits = [.. dbContext.OrganizationalUnits];
    }

    private static OrganizationalUnit CreateOrganizationalUnit(string name, Guid organizationId)
    {
        return new OrganizationalUnit
        {
            Name = name,
            OrganizationId = organizationId
        };
    }

    private async Task DeleteOrganizationalUnit(OrganizationalUnit organizationalUnit)
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.OrganizationalUnits.Remove(organizationalUnit);
        await dbContext.SaveChangesAsync();

        LoadOrganizationalUnits();
    }

    private void EditOrganizationalUnit(OrganizationalUnit organizationalUnit)
    {
        Input = new InputModel
        {
            Id = organizationalUnit.NodeId,
            Name = organizationalUnit.Name,
            OrganizationId = organizationalUnit.OrganizationId
        };
    }

    private void ManageUsers(OrganizationalUnit organizationalUnit)
    {
        _selectedOrganizationalUnit = organizationalUnit;
        LoadUsers();
        _showUserManagementModal = true;
    }

    private void LoadUsers()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _users = [.. dbContext.Users
            .Include(u => u.AccessibleOrganizationalUnits)
            .Select(u => new UserViewModel
            {
                UserId = u.Id,
                UserName = u.UserName,
                IsSelected = u.AccessibleOrganizationalUnits.Any(ou => ou.NodeId == _selectedOrganizationalUnit!.NodeId)
            })];
    }

    private async Task SaveUserAssignments()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var selectedUsers = _users.Where(u => u.IsSelected).Select(u => u.UserId).ToList();
        var organizationalUnit = await dbContext.OrganizationalUnits.FindAsync(_selectedOrganizationalUnit!.NodeId);

        if (organizationalUnit == null)
        {
            return;
        }

        var users = await dbContext.Users.Include(u => u.AccessibleOrganizationalUnits).ToListAsync();

        foreach (var user in users)
        {
            if (selectedUsers.Contains(user.Id))
            {
                if (!user.AccessibleOrganizationalUnits.Any(ou => ou.NodeId == organizationalUnit.NodeId))
                {
                    user.AccessibleOrganizationalUnits.Add(organizationalUnit);
                }
            }
            else
            {
                var existingUnit = user.AccessibleOrganizationalUnits.FirstOrDefault(ou => ou.NodeId == organizationalUnit.NodeId);
                
                if (existingUnit != null)
                {
                    user.AccessibleOrganizationalUnits.Remove(existingUnit);
                }
            }
        }

        await dbContext.SaveChangesAsync();

        _showUserManagementModal = false;
    }

    private sealed class InputModel
    {
        public Guid? Id { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Organization")]
        public Guid OrganizationId { get; set; }
    }


    private sealed class UserViewModel
    {
        public string UserId { get; set; }

        public string UserName { get; set; }

        public bool IsSelected { get; set; }
    }
}
