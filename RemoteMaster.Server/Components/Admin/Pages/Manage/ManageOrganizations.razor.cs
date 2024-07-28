// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Admin.Dialogs;
using RemoteMaster.Server.Entities;
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
        if (Input.Id.HasValue)
        {
            var result = await NodesService.UpdateNodeAsync(
                new Organization { Id = Input.Id.Value, Name = Input.Name, Locality = Input.Locality, State = Input.State, Country = Input.Country },
                org =>
                {
                    org.Name = Input.Name;
                    org.Locality = Input.Locality;
                    org.State = Input.State;
                    org.Country = Input.Country;
                    return org;
                });

            if (result.IsSuccess)
            {
                await OnOrganizationSaved("Organization updated successfully.");
            }
            else
            {
                _message = string.Join("; ", result.Errors.Select(e => e.Message));
            }
        }
        else
        {
            var newOrganization = new Organization { Name = Input.Name, Locality = Input.Locality, State = Input.State, Country = Input.Country };
            var result = await NodesService.AddNodesAsync(new List<Organization> { newOrganization });

            if (result.IsSuccess)
            {
                await OnOrganizationSaved("Organization created successfully.");
            }
            else
            {
                _message = string.Join("; ", result.Errors.Select(e => e.Message));
            }
        }
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
        var result = await NodesService.GetNodesAsync<Organization>();

        if (result.IsSuccess)
        {
            _organizations = [.. result.Value];
        }
        else
        {
            _message = string.Join("; ", result.Errors.Select(e => e.Message));
        }
    }

    private async Task DeleteOrganization(Organization organization)
    {
        var result = await NodesService.RemoveNodesAsync(new List<Organization> { organization });

        if (result.IsSuccess)
        {
            await LoadOrganizationsAsync();
            _message = "Organization deleted successfully.";
        }
        else
        {
            _message = string.Join("; ", result.Errors.Select(e => e.Message));
        }
    }

    private void EditOrganization(Organization organization)
    {
        Input = new InputModel
        {
            Id = organization.Id,
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
}
