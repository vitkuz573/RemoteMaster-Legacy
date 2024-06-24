// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class HostConfigurationGenerator
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    private readonly HostConfiguration _model = new();
    private string? _selectedOrganization;
    private string? _selectedOrganizationalUnit;
    private List<Organization> _organizations = [];
    private List<OrganizationalUnit> _organizationalUnits = [];
    private List<Country> _countries = [];

    protected async override Task OnInitializedAsync()
    {
        var hostInformation = HostInformationService.GetHostInformation();

        _model.Server = hostInformation.Name;
        _model.Subject = new();

        await LoadUserOrganizationsAsync();

        _countries = CountryProvider.GetCountries();
    }

    private async Task OnValidSubmit(EditContext context)
    {
        _model.Subject.Organization = _selectedOrganization;
        _model.Subject.OrganizationalUnit = [_selectedOrganizationalUnit];

        var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/fileUtils.js");

        var jsonContent = JsonSerializer.Serialize(_model, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await module.InvokeVoidAsync("downloadDataAsFile", jsonContent, "RemoteMaster.Host.json", "application/json");
    }

    public void DownloadHost()
    {
        NavigationManager.NavigateTo("api/HostConfiguration/download-host", true);
    }

    private async Task LoadUserOrganizationsAsync()
    {
        var authState = await AuthenticationStateTask;
        var user = authState.User;

        if (user.Identity.IsAuthenticated)
        {
            var username = user.Identity.Name;
            var appUser = await UserManager.Users
                                           .Include(u => u.AccessibleOrganizations)
                                           .FirstOrDefaultAsync(u => u.UserName == username);

            if (appUser != null)
            {
                _organizations = [.. appUser.AccessibleOrganizations];
            }
        }
    }

    private async Task LoadOrganizationalUnitsAsync()
    {
        if (!string.IsNullOrEmpty(_selectedOrganization))
        {
            var organization = _organizations.FirstOrDefault(o => o.Name == _selectedOrganization);
            
            if (organization != null)
            {
                var authState = await AuthenticationStateTask;
                var user = authState.User;

                if (user.Identity.IsAuthenticated)
                {
                    var username = user.Identity.Name;
                    var appUser = await UserManager.Users
                                                   .Include(u => u.AccessibleOrganizationalUnits)
                                                   .FirstOrDefaultAsync(u => u.UserName == username);

                    if (appUser != null)
                    {
                        _organizationalUnits = appUser.AccessibleOrganizationalUnits
                                                      .Where(ou => ou.OrganizationId == organization.NodeId)
                                                      .ToList();
                    }
                }
            }
        }
    }

    private async Task OrganizationChanged(string value)
    {
        _selectedOrganization = value;
        _selectedOrganizationalUnit = null;

        await LoadOrganizationalUnitsAsync();

        var selectedOrg = _organizations.FirstOrDefault(org => org.Name == value);

        if (selectedOrg != null)
        {
            _model.Subject.Locality = selectedOrg.Locality;
            _model.Subject.State = selectedOrg.State;
            _model.Subject.Country = selectedOrg.Country;

            StateHasChanged();
        }
    }

    private void OrganizationalUnitChanged(string value)
    {
        _selectedOrganizationalUnit = value;

        StateHasChanged();
    }
}
