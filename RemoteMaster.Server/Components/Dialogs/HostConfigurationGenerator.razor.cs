// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using RemoteMaster.Server.Entities;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;
using Serilog;

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

        var countriesResult = CountryProvider.GetCountries();

        if (countriesResult.IsSuccess)
        {
            _countries = countriesResult.Value;
        }
        else
        {
            Log.Error("Failed to load countries: {Message}", countriesResult.Errors.FirstOrDefault()?.Message);
        }

        HttpClient.BaseAddress = new Uri("http://127.0.0.1:5254");
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

    public async Task DownloadHost()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/HostConfiguration/downloadHost");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.remotemaster.v1+json"));

        var response = await HttpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsByteArrayAsync();

            var contentDisposition = response.Content.Headers.ContentDisposition;
            var fileName = contentDisposition?.FileName?.Trim('\"') ?? "RemoteMaster.Host.exe";

            var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/fileUtils.js");

            await module.InvokeVoidAsync("downloadDataAsFile", Convert.ToBase64String(content), fileName, "application/octet-stream;base64");
        }
        else
        {
            // Handle error response
        }
    }

    private async Task LoadUserOrganizationsAsync()
    {
        var authState = await AuthenticationStateTask;
        var user = authState.User;

        if (user.Identity.IsAuthenticated)
        {
            var username = user.Identity.Name;
            var appUser = await UserManager.Users
                .Include(u => u.UserOrganizations)
                .ThenInclude(uo => uo.Organization)
                .FirstOrDefaultAsync(u => u.UserName == username);

            if (appUser != null)
            {
                _organizations = appUser.UserOrganizations
                    .Select(uo => uo.Organization)
                    .ToList();
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
                        .Include(u => u.UserOrganizationalUnits)
                        .ThenInclude(uou => uou.OrganizationalUnit)
                        .FirstOrDefaultAsync(u => u.UserName == username);

                    if (appUser != null)
                    {
                        _organizationalUnits = appUser.UserOrganizationalUnits
                            .Where(uou => uou.OrganizationalUnit.OrganizationId == organization.Id)
                            .Select(uou => uou.OrganizationalUnit)
                            .ToList();
                    }
                }
            }
        }
    }

    private async Task OrganizationChanged(string? value)
    {
        _selectedOrganization = value;
        _selectedOrganizationalUnit = null;

        await LoadOrganizationalUnitsAsync();

        var selectedOrg = _organizations.FirstOrDefault(org => org.Name == value);

        if (selectedOrg != null)
        {
            _model.Subject.Locality = selectedOrg.Address.Locality;
            _model.Subject.State = selectedOrg.Address.State;
            _model.Subject.Country = selectedOrg.Address.Country;

            StateHasChanged();
        }
    }

    private void OrganizationalUnitChanged(string? value)
    {
        _selectedOrganizationalUnit = value;

        StateHasChanged();
    }
}
