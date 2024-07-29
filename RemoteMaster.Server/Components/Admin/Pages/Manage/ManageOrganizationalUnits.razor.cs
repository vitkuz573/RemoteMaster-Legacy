// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Admin.Dialogs;
using RemoteMaster.Server.Entities;

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

        if (Input.Id.HasValue)
        {
            var result = await NodesService.UpdateNodeAsync(
                new OrganizationalUnit { Id = Input.Id.Value, Name = Input.Name, OrganizationId = Input.OrganizationId, ParentId = Input.ParentId },
                ou =>
                {
                    ou.Name = Input.Name;
                    ou.OrganizationId = Input.OrganizationId;
                    ou.ParentId = Input.ParentId;
                    return ou;
                });

            if (result.IsSuccess)
            {
                await OnOrganizationalUnitSaved("Organizational unit updated successfully.");
            }
            else
            {
                _message = string.Join("; ", result.Errors.Select(e => e.Message));
            }
        }
        else
        {
            var newUnit = new OrganizationalUnit { Name = Input.Name, OrganizationId = Input.OrganizationId, ParentId = Input.ParentId };
            var result = await NodesService.AddNodesAsync(new List<OrganizationalUnit> { newUnit });

            if (result.IsSuccess)
            {
                await OnOrganizationalUnitSaved("Organizational unit created successfully.");
            }
            else
            {
                _message = string.Join("; ", result.Errors.Select(e => e.Message));
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

    private async Task LoadOrganizationsAsync()
    {
        var result = await NodesService.GetNodesAsync<Organization>();

        if (result.IsSuccess)
        {
            _organizations = result.Value.ToList();
        }
        else
        {
            _message = string.Join("; ", result.Errors.Select(e => e.Message));
        }
    }

    private async Task LoadOrganizationalUnitsAsync()
    {
        var result = await NodesService.GetNodesAsync<OrganizationalUnit>();

        if (result.IsSuccess)
        {
            _organizationalUnits = result.Value.ToList();
            FilterOrganizationalUnits();
        }
        else
        {
            _message = string.Join("; ", result.Errors.Select(e => e.Message));
        }
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
        var result = await NodesService.RemoveNodesAsync(new List<OrganizationalUnit> { organizationalUnit });

        if (result.IsSuccess)
        {
            await LoadOrganizationalUnitsAsync();
            _message = "Organizational unit deleted successfully.";
        }
        else
        {
            _message = string.Join("; ", result.Errors.Select(e => e.Message));
        }
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
