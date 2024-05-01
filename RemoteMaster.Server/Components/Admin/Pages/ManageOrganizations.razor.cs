// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Admin.Pages;

[Authorize(Roles = "RootAdministrator")]
public partial class ManageOrganizations
{
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    private List<Organization> _organizations = [];

    protected override void OnInitialized()
    {
        _organizations = [.. ApplicationDbContext.Organizations];
    }

    private async Task OnValidSubmitAsync()
    {
        var organization = CreateOrganization(Input.Name);

        await ApplicationDbContext.Organizations.AddAsync(organization);
        await ApplicationDbContext.SaveChangesAsync();

        RedirectManager.RedirectToCurrentPageWithStatus("Organization created", HttpContext);
    }

    private static Organization CreateOrganization(string name)
    {
        return new Organization
        {
            Name = name
        };
    }

    private sealed class InputModel
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Name")]
        public string Name { get; set; }
    }
}
