// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Components.Admin.Dialogs;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Admin.Pages;

public partial class ManageOrganizations
{
    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    private List<Organization> _organizations = [];
    private string? _message;
    private List<Country> _countries = [];
    private ConfirmationDialog? _confirmationDialog;
    private Organization? _organizationToDelete;

    protected override void OnInitialized()
    {
        LoadOrganizations();

        _countries = CountryProvider.GetCountries();
    }

    private async Task OnValidSubmitAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (Input.Id.HasValue)
        {
            if (await UpdateOrganizationAsync(dbContext))
            {
                OnOrganizationSaved("Organization updated successfully.");
            }
        }
        else
        {
            if (await CreateOrganizationAsync(dbContext))
            {
                OnOrganizationSaved("Organization created successfully.");
            }
        }
    }

    private void OnOrganizationSaved(string message)
    {
        LoadOrganizations();

        NavigationManager.Refresh();
        Input = new InputModel();

        _message = message;
    }

    private async Task<bool> UpdateOrganizationAsync(ApplicationDbContext dbContext)
    {
        var organization = await dbContext.Organizations.FindAsync(Input.Id.Value);

        if (organization == null)
        {
            _message = "Error: Organization not found.";

            return false;
        }

        if (await dbContext.Organizations.AnyAsync(o => o.Name == Input.Name && o.NodeId != Input.Id.Value))
        {
            _message = "Error: Organization with this name already exists.";

            return false;
        }

        organization.Name = Input.Name;
        organization.Locality = Input.Locality;
        organization.State = Input.State;
        organization.Country = Input.Country;

        await dbContext.SaveChangesAsync();

        return true;
    }

    private async Task<bool> CreateOrganizationAsync(ApplicationDbContext dbContext)
    {
        if (await OrganizationNameExists(Input.Name))
        {
            _message = "Error: Organization with this name already exists.";

            return false;
        }

        var organization = CreateOrganization(Input.Name, Input.Locality, Input.State, Input.Country);
        
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.SaveChangesAsync();
        
        return true;
    }

    private async Task<bool> OrganizationNameExists(string name)
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        return await dbContext.Organizations.AnyAsync(o => o.Name == name);
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
        _message = "Organization deleted successfully.";
    }

    private void EditOrganization(Organization organization)
    {
        Input = new InputModel
        {
            Id = organization.NodeId,
            Name = organization.Name,
            Locality = organization.Locality,
            State = organization.State,
            Country = organization.Country
        };
    }

    private void ShowDeleteConfirmation(Organization organization)
    {
        _organizationToDelete = organization;

        var parameters = new Dictionary<string, string>
        {
            { "Organization", organization.Name }
        };

        _confirmationDialog?.Show(parameters);
    }

    private async Task OnConfirmDelete(bool confirmed)
    {
        if (confirmed && _organizationToDelete != null)
        {
            await DeleteOrganization(_organizationToDelete);

            _organizationToDelete = null;
        }
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
