// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;
using RemoteMaster.Server.Components.Admin.Dialogs;
using RemoteMaster.Server.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class ManageOrganizations
{
    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    private List<Organization> _organizations = [];
    private string? _message;
    private List<Country> _countries = [];
    private ConfirmationDialog? _confirmationDialog;
    private Organization? _organizationToDelete;

    protected async override Task OnInitializedAsync()
    {
        await LoadOrganizationsAsync();

        var countriesResult = CountryProvider.GetCountries();

        if (countriesResult.IsSuccess)
        {
            _countries = countriesResult.Value;
        }
        else
        {
            Log.Error("Failed to load countries: {Message}", countriesResult.Errors.FirstOrDefault()?.Message);
        }
    }

    private async Task OnValidSubmitAsync()
    {
        var address = new Address(Input.Locality, Input.State, Input.Country);

        if (Input.Id.HasValue)
        {
            var organization = await OrganizationRepository.GetByIdAsync(Input.Id.Value);
            
            if (organization == null)
            {
                _message = "Organization not found.";
                
                return;
            }

            organization.SetName(Input.Name);
            organization.SetAddress(address);

            await OrganizationRepository.UpdateAsync(organization);
            
            _message = "Organization updated successfully.";
        }
        else
        {
            var newOrganization = new Organization(Input.Name, address);

            await OrganizationRepository.AddAsync(newOrganization);
            
            _message = "Organization created successfully.";
        }

        await OrganizationRepository.SaveChangesAsync();

        await OnOrganizationSaved(_message);
    }

    private async Task OnOrganizationSaved(string message)
    {
        await LoadOrganizationsAsync();

        NavigationManager.Refresh();
        Input = new InputModel();
        
        _message = message;
    }

    private async Task LoadOrganizationsAsync()
    {
        var organizations = await OrganizationRepository.GetAllAsync();

        if (organizations != null)
        {
            _organizations = organizations.ToList();
        }
        else
        {
            _message = "Failed to load organizations.";
        }
    }

    private async Task DeleteOrganization(Organization organization)
    {
        await OrganizationRepository.DeleteAsync(organization);
        await OrganizationRepository.SaveChangesAsync();
        
        _message = "Organization deleted successfully.";
        
        await LoadOrganizationsAsync();
    }

    private void EditOrganization(Organization organization)
    {
        Input = new InputModel
        {
            Id = organization.Id,
            Name = organization.Name,
            Locality = organization.Address.Locality,
            State = organization.Address.State,
            Country = organization.Address.Country
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
}
