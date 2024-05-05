// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Admin.Pages;

[Authorize(Roles = "RootAdministrator")]
public partial class ManageOrganizations
{
    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    private List<Organization> _organizations = [];

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
        }
        else
        {
            organization = CreateOrganization(Input.Name);
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

    private static Organization CreateOrganization(string name)
    {
        return new Organization
        {
            Name = name
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
    }
}
