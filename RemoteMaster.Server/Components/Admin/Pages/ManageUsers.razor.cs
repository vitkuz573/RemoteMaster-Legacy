// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Components.Admin.Pages;

public partial class ManageUsers
{
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    private ApplicationUser _user = default!;
    private List<IdentityRole> _roles = [];

    protected async override Task OnInitializedAsync()
    {
        _user = await UserAccessor.GetRequiredUserAsync(HttpContext);

        _roles = await RoleManager.Roles
            .Where(role => role.Name != "SystemAdministrator")
            .ToListAsync();
    }

    private async Task OnValidSubmitAsync()
    {
        var user = new ApplicationUser
        {
            UserName = Input.Username,
            Email = Input.Email,
            OrganizationId = _user.OrganizationId
        };

        await UserManager.CreateAsync(user);
        await UserManager.AddToRoleAsync(user, Input.Role);

        RedirectManager.RedirectToCurrentPageWithStatus("User created", HttpContext);
    }

    private sealed class InputModel
    {
        [Display(Name = "Username")]
        public string Username { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Role")]
        public string Role { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
