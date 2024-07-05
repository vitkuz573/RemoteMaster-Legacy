// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Components.Admin.Dialogs;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class ManageOrganizationalUnits
{
    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    private List<Organization> _organizations = [];
    private List<OrganizationalUnit> _organizationalUnits = [];

    private ConfirmationDialog confirmationDialog;
    private OrganizationalUnit? _organizationalUnitToDelete;

    protected async override Task OnInitializedAsync()
    {
        await LoadOrganizationsAsync();
        await LoadOrganizationalUnitsAsync();
    }

    private async Task OnValidSubmitAsync()
    {
        if (Input.OrganizationId == Guid.Empty)
        {
            return;
        }

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

        await LoadOrganizationalUnitsAsync();

        NavigationManager.Refresh();

        Input = new InputModel();
    }

    private async Task LoadOrganizationsAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _organizations = await dbContext.Organizations.ToListAsync();
    }

    private async Task LoadOrganizationalUnitsAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _organizationalUnits = await dbContext.OrganizationalUnits
                                              .Include(ou => ou.Organization)
                                              .ToListAsync();
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

        await LoadOrganizationalUnitsAsync();
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

    private void ShowDeleteConfirmation(OrganizationalUnit organizationalUnit)
    {
        _organizationalUnitToDelete = organizationalUnit;

        var parameters = new Dictionary<string, string>
        {
            { "Organization", organizationalUnit.Organization.Name },
            { "Organizational Unit", organizationalUnit.Name }
        };

        confirmationDialog.Show(parameters);
    }

    private async Task OnConfirmDelete(bool confirmed)
    {
        if (confirmed && _organizationalUnitToDelete != null)
        {
            await DeleteOrganizationalUnit(_organizationalUnitToDelete);
            _organizationalUnitToDelete = null;
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
