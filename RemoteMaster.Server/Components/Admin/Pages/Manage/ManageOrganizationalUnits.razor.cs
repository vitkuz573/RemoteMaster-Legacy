// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Aggregates.OrganizationalUnitAggregate;
using RemoteMaster.Server.Components.Admin.Dialogs;

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

        var organization = await OrganizationRepository.GetByIdAsync(Input.OrganizationId);
        
        if (organization == null)
        {
            _message = "Error: Organization not found.";
            return;
        }

        OrganizationalUnit? parent = null;

        if (Input.ParentId.HasValue)
        {
            parent = await OrganizationalUnitRepository.GetByIdAsync(Input.ParentId.Value);
        }

        if (Input.Id.HasValue)
        {
            var organizationalUnit = await OrganizationalUnitRepository.GetByIdAsync(Input.Id.Value);
            
            if (organizationalUnit == null)
            {
                _message = "Error: Organizational unit not found.";
                return;
            }

            organizationalUnit.ChangeName(Input.Name);

            if (parent != null)
            {
                organizationalUnit.ChangeParent(parent);
            }

            await OrganizationalUnitRepository.UpdateAsync(organizationalUnit);

            _message = "Organizational unit updated successfully.";
        }
        else
        {
            var newUnit = new OrganizationalUnit(Input.Name, organization, parent);
            organization.AddOrganizationalUnit(newUnit);

            await OrganizationalUnitRepository.AddAsync(newUnit);

            _message = "Organizational unit created successfully.";
        }

        await OrganizationalUnitRepository.SaveChangesAsync();

        await OnOrganizationalUnitSaved(_message);
    }

    private async Task OnOrganizationalUnitSaved(string message)
    {
        await LoadOrganizationalUnitsAsync();
        NavigationManager.Refresh();
        Input = new InputModel();
        _message = message;
    }

    private async Task LoadOrganizationsAsync()
    {
        var organizations = await OrganizationRepository.GetAllAsync();

        _organizations = organizations.ToList();
    }

    private async Task LoadOrganizationalUnitsAsync()
    {
        var organizationalUnits = await OrganizationalUnitRepository.GetAllAsync();
        
        _organizationalUnits = organizationalUnits.ToList();
        
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

    private async Task DeleteOrganizationalUnit(OrganizationalUnit organizationalUnit)
    {
        await OrganizationalUnitRepository.DeleteAsync(organizationalUnit);
        await OrganizationalUnitRepository.SaveChangesAsync();
        
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
                ? new ValidationResult("Please select a valid organization.", [nameof(OrganizationId)])
                : ValidationResult.Success;
        }

        public static ValidationResult? ValidateParentId(Guid? parentId, ValidationContext _)
        {
            return parentId == Guid.Empty
                ? new ValidationResult("Please select a valid parent organizational unit.", [nameof(ParentId)])
                : ValidationResult.Success;
        }
    }
}
