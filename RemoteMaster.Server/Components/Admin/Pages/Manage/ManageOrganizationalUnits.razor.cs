// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

[Authorize(Roles = "RootAdministrator")]
public partial class ManageOrganizationalUnits
{
    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    private List<OrganizationalUnit> _organizationalUnits = [];

    protected override void OnInitialized()
    {
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
        }
        else
        {
            organizationalUnit = CreateOrganizationalUnit(Input.Name);
            await dbContext.OrganizationalUnits.AddAsync(organizationalUnit);
        }

        await dbContext.SaveChangesAsync();

        LoadOrganizationalUnits();

        NavigationManager.NavigateTo("Admin/Organizations", true);

        Input = new InputModel();
    }

    private void LoadOrganizationalUnits()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _organizationalUnits = [.. dbContext.OrganizationalUnits];
    }

    private static OrganizationalUnit CreateOrganizationalUnit(string name)
    {
        return new OrganizationalUnit
        {
            Name = name
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
            Id = organizationalUnit.OrganizationId,
            Name = organizationalUnit.Name
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
