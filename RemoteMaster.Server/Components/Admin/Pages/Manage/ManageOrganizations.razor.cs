// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Admin.Dialogs;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Components.Admin.Pages.Manage;

public partial class ManageOrganizations
{
    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    private List<OrganizationDto> _organizations = [];
    private string? _message;
    private List<Country> _countries = [];
    private ConfirmationDialog? _confirmationDialog;
    private string? _organizationToDelete;

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
            Logger.LogError("Failed to load countries: {Message}", countriesResult.Errors.FirstOrDefault()?.Message);
        }
    }

    private async Task OnValidSubmitAsync()
    {
        var addressDto = new AddressDto(Input.Locality, Input.State, Input.Country);
        var dto = new OrganizationDto(Input.Id, Input.Name, addressDto);

        _message = await OrganizationService.AddOrUpdateOrganizationAsync(dto);

        await OnOrganizationSavedAsync(_message);
    }

    private async Task OnOrganizationSavedAsync(string message)
    {
        await LoadOrganizationsAsync();

        NavigationManager.Refresh();
        Input = new InputModel();
        
        _message = message;
    }

    private async Task LoadOrganizationsAsync()
    {
        _organizations = [.. await OrganizationService.GetAllOrganizationsAsync()];
    }

    private async Task DeleteOrganizationAsync(string organizationName)
    {
        _message = await OrganizationService.DeleteOrganizationAsync(organizationName);

        await LoadOrganizationsAsync();
    }

    private void EditOrganization(OrganizationDto organization)
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

    private void ShowDeleteConfirmation(string organizationName)
    {
        _organizationToDelete = organizationName;

        var parameters = new Dictionary<string, string>
        {
            { "Organization", organizationName }
        };

        _confirmationDialog?.Show(parameters);
    }

    private async Task OnConfirmDeleteAsync(bool confirmed)
    {
        if (confirmed && _organizationToDelete != null)
        {
            await DeleteOrganizationAsync(_organizationToDelete);
            _organizationToDelete = null;
        }
    }

    private sealed class InputModel
    {
        public Guid? Id { get; init; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Locality")]
        public string Locality { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "State")]
        public string State { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Country")]
        public string Country { get; set; } = string.Empty;
    }
}
