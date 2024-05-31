// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Admin.Pages;

[Authorize(Roles = "RootAdministrator")]
public partial class ManageOrganizations
{
    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    private List<Organization> _organizations = [];
    private bool _showUserManagementModal = false;
    private Organization? _selectedOrganization;
    private List<UserViewModel> _users = [];

    protected override void OnInitialized()
    {
        LoadOrganizations();
    }

    private async Task OnValidSubmitAsync()
    {
        Organization organization;

        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (Input.Id.HasValue)
        {
            organization = dbContext.Organizations.Find(Input.Id.Value);
            
            if (organization == null)
            {
                return;
            }

            organization.Name = Input.Name;
            organization.Locality = Input.Locality;
            organization.State = Input.State;
            organization.Country = Input.Country;
        }
        else
        {
            organization = CreateOrganization(Input.Name, Input.Locality, Input.State, Input.Country);
            await dbContext.Organizations.AddAsync(organization);
        }

        await dbContext.SaveChangesAsync();

        LoadOrganizations();

        NavigationManager.NavigateTo("Admin/Organizations", true);

        Input = new InputModel();
    }

    private void LoadOrganizations()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _organizations = [.. dbContext.Organizations];
    }

    private static Organization CreateOrganization(string name, string locality, string state, string country)
    {
        return new Organization
        {
            Name = name,
            Locality = locality,
            State = state,
            Country = country
        };
    }

    private async Task DeleteOrganization(Organization organization)
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Organizations.Remove(organization);
        await dbContext.SaveChangesAsync();

        LoadOrganizations();
    }

    private void ManageUsers(Organization organization)
    {
        _selectedOrganization = organization;
        LoadUsers();
        _showUserManagementModal = true;
    }

    private void LoadUsers()
    {
        _users = [.. ApplicationDbContext.Users
            .Include(u => u.AccessibleOrganizations)
            .Select(u => new UserViewModel
            {
                UserId = u.Id,
                UserName = u.UserName,
                IsSelected = u.AccessibleOrganizations.Contains(_selectedOrganization)
            })];
    }

    private async Task SaveUserAssignments()
    {
        var usersToUpdate = _users.Where(u => u.IsSelected).Select(u => u.UserId).ToList();

        foreach (var user in ApplicationDbContext.Users.Include(u => u.AccessibleOrganizations))
        {
            if (usersToUpdate.Contains(user.Id))
            {
                if (!user.AccessibleOrganizations.Contains(_selectedOrganization))
                {
                    user.AccessibleOrganizations.Add(_selectedOrganization);
                }
            }
            else
            {
                user.AccessibleOrganizations.Remove(_selectedOrganization);
            }
        }

        await ApplicationDbContext.SaveChangesAsync();
        _showUserManagementModal = false;
    }

    private void EditOrganization(Organization organization)
    {
        Input = new InputModel
        {
            Id = organization.OrganizationId,
            Name = organization.Name
        };
    }

    private sealed class InputModel
    {
        public Guid? Id { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Locality")]
        public string Locality { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "State")]
        public string State { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Country")]
        public string Country { get; set; }
    }

    private sealed class UserViewModel
    {
        public string UserId { get; set; }

        public string UserName { get; set; }

        public bool IsSelected { get; set; }
    }
}
