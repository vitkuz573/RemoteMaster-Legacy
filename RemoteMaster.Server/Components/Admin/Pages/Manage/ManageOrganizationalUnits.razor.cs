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
    private List<OrganizationalUnit> _filteredOrganizationalUnits = [];
    private string? _message;
    private ConfirmationDialog? _confirmationDialog;
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
            _message = "Error: Please select a valid organization.";
            
            return;
        }

        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (Input.Id.HasValue)
        {
            if (await UpdateOrganizationalUnitAsync(dbContext))
            {
                await OnOrganizationalUnitSaved("Organizational unit updated successfully.");
            }
        }
        else
        {
            if (await CreateOrganizationalUnitAsync(dbContext))
            {
                await OnOrganizationalUnitSaved("Organizational unit created successfully.");
            }
        }
    }

    private async Task OnOrganizationalUnitSaved(string message)
    {
        await LoadOrganizationalUnitsAsync();
        NavigationManager.Refresh();
        Input = new InputModel();
        _message = message;
    }

    private async Task<bool> UpdateOrganizationalUnitAsync(ApplicationDbContext dbContext)
    {
        var organizationalUnit = await dbContext.OrganizationalUnits.FindAsync(Input.Id.Value);

        if (organizationalUnit == null)
        {
            _message = "Error: Organizational unit not found.";
            
            return false;
        }

        if (await dbContext.OrganizationalUnits.AnyAsync(ou => ou.Name == Input.Name && ou.OrganizationId == Input.OrganizationId && ou.Id != Input.Id.Value))
        {
            _message = "Error: Organizational unit with this name already exists in the selected organization.";
           
            return false;
        }

        organizationalUnit.Name = Input.Name;
        organizationalUnit.OrganizationId = Input.OrganizationId;
        organizationalUnit.ParentId = Input.ParentId;

        await dbContext.SaveChangesAsync();

        return true;
    }

    private async Task<bool> CreateOrganizationalUnitAsync(ApplicationDbContext dbContext)
    {
        if (await dbContext.OrganizationalUnits.AnyAsync(ou => ou.Name == Input.Name && ou.OrganizationId == Input.OrganizationId))
        {
            _message = "Error: Organizational unit with this name already exists in the selected organization.";
            
            return false;
        }

        var organizationalUnit = CreateOrganizationalUnit(Input.Name, Input.OrganizationId, Input.ParentId);
        await dbContext.OrganizationalUnits.AddAsync(organizationalUnit);

        await dbContext.SaveChangesAsync();

        return true;
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
        
        FilterOrganizationalUnits();
    }

    private async Task OnOrganizationChanged(Guid organizationId)
    {
        Input.OrganizationId = organizationId;
        FilterOrganizationalUnits();
        await InvokeAsync(StateHasChanged);
    }

    private void FilterOrganizationalUnits()
    {
        _filteredOrganizationalUnits = _organizationalUnits
            .Where(ou => ou.OrganizationId == Input.OrganizationId)
            .ToList();
    }

    private static OrganizationalUnit CreateOrganizationalUnit(string name, Guid organizationId, Guid? parentId)
    {
        return new OrganizationalUnit
        {
            Name = name,
            OrganizationId = organizationId,
            ParentId = parentId
        };
    }

    private async Task DeleteOrganizationalUnit(OrganizationalUnit organizationalUnit)
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.OrganizationalUnits.Remove(organizationalUnit);
        await dbContext.SaveChangesAsync();

        await LoadOrganizationalUnitsAsync();
        _message = "Organizational unit deleted successfully.";
    }

    private void EditOrganizationalUnit(OrganizationalUnit organizationalUnit)
    {
        Input = new InputModel
        {
            Id = organizationalUnit.Id,
            Name = organizationalUnit.Name,
            OrganizationId = organizationalUnit.OrganizationId,
            ParentId = organizationalUnit.ParentId
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

        _confirmationDialog?.Show(parameters);
    }

    private async Task OnConfirmDelete(bool confirmed)
    {
        if (confirmed && _organizationalUnitToDelete != null)
        {
            await DeleteOrganizationalUnit(_organizationalUnitToDelete);
            _organizationalUnitToDelete = null;
        }
    }

    public sealed class InputModel
    {
        public Guid? Id { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Organization")]
        [CustomValidation(typeof(InputModel), nameof(ValidateOrganizationId))]
        public Guid OrganizationId { get; set; }

        [Display(Name = "Parent Organizational Unit")]
        [CustomValidation(typeof(InputModel), nameof(ValidateParentId))]
        public Guid? ParentId { get; set; }

        public static ValidationResult? ValidateOrganizationId(Guid organizationId, ValidationContext _)
        {
            return organizationId == Guid.Empty
                ? new ValidationResult("Please select a valid organization.", new[] { nameof(OrganizationId) })
                : ValidationResult.Success;
        }

        public static ValidationResult? ValidateParentId(Guid? parentId, ValidationContext _)
        {
            return parentId == Guid.Empty
                ? new ValidationResult("Please select a valid parent organizational unit.", new[] { nameof(ParentId) })
                : ValidationResult.Success;
        }
    }
}
